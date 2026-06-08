from __future__ import annotations

import json
import re
import time
from pathlib import Path
from typing import Any

from .executor import extract_codex_answer, extract_usage_and_counts
from .utils import read_text, run_command, write_json, write_text


def _safe_json(value: Any) -> str:
    return json.dumps(value, indent=2, ensure_ascii=False)


def build_judge_prompt(
    *,
    case: dict[str, Any],
    mode: dict[str, Any],
    rubric: dict[str, Any],
    prompt: str,
    answer: str,
    validation: dict[str, Any],
) -> str:
    """Build a compact judge prompt.

    The judge sees the task, the answer, the deterministic validation summary, and the rubric.
    It does not need the full trace JSONL, which can be huge and expensive.
    """
    behavior = validation.get("behavior") or {}
    trace = behavior.get("trace") or {}
    answer_checks = (validation.get("answer") or {}).get("checks") or []
    behavior_checks = behavior.get("checks") or []
    compact_validation = {
        "ok": validation.get("ok"),
        "hard_passed": validation.get("hard_passed"),
        "hard_total": validation.get("hard_total"),
        "soft_passed": validation.get("soft_passed"),
        "soft_total": validation.get("soft_total"),
        "answer_checks": answer_checks,
        "behavior_checks": behavior_checks,
        "trace_metrics": trace,
    }

    return f"""You are an evaluation judge for repository-exploration agent answers.

Judge the answer quality only. Do not reward a mode because it used a specific tool. Reward correctness, completeness, source-grounding, focus, and useful caveats.

Return ONLY valid JSON. Do not include markdown fences.

Expected JSON schema:
{{
  "ok": true,
  "overall_score": 1.0,
  "confidence": "low|medium|high",
  "dimensions": {{
    "<dimension_name>": {{"score": 1, "rationale": "..."}}
  }},
  "strengths": ["..."],
  "weaknesses": ["..."],
  "missing_or_questionable": ["..."],
  "summary": "..."
}}

Scoring scale:
1 = poor or mostly wrong
2 = weak / incomplete
3 = acceptable but with important gaps
4 = strong with minor gaps
5 = excellent

Rubric:
{_safe_json(rubric)}

Case:
{_safe_json({"id": case.get("id"), "title": case.get("title"), "category": case.get("category"), "task": case.get("task"), "validation": case.get("validation")})}

Mode metadata, for context only:
{_safe_json({"id": mode.get("id"), "description": mode.get("description")})}

Original prompt given to the agent:
{prompt}

Deterministic validation summary:
{_safe_json(compact_validation)}

Agent answer to judge:
{answer}
"""


def parse_json_from_text(text: str) -> tuple[dict[str, Any] | None, str | None]:
    stripped = text.strip()
    if not stripped:
        return None, "empty judge response"
    try:
        return json.loads(stripped), None
    except json.JSONDecodeError:
        pass

    # Best-effort extraction for responses that accidentally include prose or fences.
    fenced = re.search(r"```(?:json)?\s*(\{.*?\})\s*```", stripped, flags=re.DOTALL)
    if fenced:
        try:
            return json.loads(fenced.group(1)), None
        except json.JSONDecodeError as exc:
            return None, f"invalid fenced JSON: {exc}"

    first = stripped.find("{")
    last = stripped.rfind("}")
    if first != -1 and last != -1 and last > first:
        candidate = stripped[first : last + 1]
        try:
            return json.loads(candidate), None
        except json.JSONDecodeError as exc:
            return None, f"invalid embedded JSON: {exc}"

    return None, "no JSON object found in judge response"


def normalize_judge_payload(payload: dict[str, Any] | None, error: str | None = None) -> dict[str, Any]:
    if payload is None:
        return {
            "enabled": True,
            "ok": False,
            "error": error or "judge did not return a parseable payload",
            "overall_score": None,
            "confidence": None,
            "dimensions": {},
        }

    dimensions = payload.get("dimensions") if isinstance(payload.get("dimensions"), dict) else {}
    scores: list[float] = []
    for item in dimensions.values():
        if isinstance(item, dict):
            try:
                scores.append(float(item.get("score")))
            except (TypeError, ValueError):
                continue
    overall = payload.get("overall_score")
    try:
        overall_score = float(overall)
    except (TypeError, ValueError):
        overall_score = round(sum(scores) / len(scores), 3) if scores else None

    return {
        "enabled": True,
        "ok": bool(payload.get("ok", overall_score is not None)),
        "error": None,
        "overall_score": overall_score,
        "confidence": payload.get("confidence"),
        "dimensions": dimensions,
        "strengths": payload.get("strengths") or [],
        "weaknesses": payload.get("weaknesses") or [],
        "missing_or_questionable": payload.get("missing_or_questionable") or [],
        "summary": payload.get("summary") or "",
        "raw": payload,
    }


class CodexCliJudge:
    def __init__(self, command: str = "codex", args: list[str] | None = None, timeout_seconds: int | None = None) -> None:
        self.command = command
        self.args = args or ["exec", "--json"]
        self.timeout_seconds = timeout_seconds

    def run(
        self,
        *,
        worktree: Path,
        output_dir: Path,
        case: dict[str, Any],
        mode: dict[str, Any],
        rubric: dict[str, Any],
        validation: dict[str, Any],
    ) -> dict[str, Any]:
        prompt = read_text(output_dir / "prompt.txt")
        answer = read_text(output_dir / "answer.md")
        judge_prompt = build_judge_prompt(
            case=case,
            mode=mode,
            rubric=rubric,
            prompt=prompt,
            answer=answer,
            validation=validation,
        )
        write_text(output_dir / "judge.prompt.txt", judge_prompt)

        cmd = [self.command, *self.args]
        start = time.time()
        result = run_command(cmd, cwd=worktree, timeout_seconds=self.timeout_seconds, input_text=judge_prompt, shell=False)
        elapsed_ms = int((time.time() - start) * 1000)

        stdout = result["stdout"]
        stderr = result["stderr"]
        judge_text = extract_codex_answer(stdout)
        parsed, parse_error = parse_json_from_text(judge_text)
        normalized = normalize_judge_payload(parsed, parse_error)
        usage_counts = extract_usage_and_counts(stdout)

        run_summary = {
            "agent": "codex-cli-judge",
            "command": cmd,
            "exit_code": result["exit_code"],
            "timed_out": result["timed_out"],
            "elapsed_ms": elapsed_ms,
            "usage": usage_counts["usage"],
            "counts": usage_counts["counts"],
        }
        normalized["run_summary"] = run_summary

        write_text(output_dir / "judge.stdout.raw.jsonl", stdout)
        write_text(output_dir / "judge.stderr.txt", stderr)
        write_text(output_dir / "judge.answer.txt", judge_text)
        write_json(output_dir / "judge.json", normalized)
        write_json(output_dir / "judge.summary.json", run_summary)
        return normalized
