from __future__ import annotations

import csv
import json
from pathlib import Path
from typing import Any

from .aggregate import build_aggregates
from .contracts import AGGREGATE_SUMMARY_SCHEMA_VERSION, SUMMARY_SCHEMA_VERSION, with_schema
from .utils import ensure_dir, read_json, write_json, write_text


def _answer_metrics(validation: dict[str, Any]) -> dict[str, Any]:
    answer = validation.get("answer") or {}
    evidence = answer.get("evidence") or {}
    return {
        "answer_empty": evidence.get("answer_empty"),
        "answer_file_reference_count": evidence.get("file_reference_count"),
        "answer_unique_file_reference_count": evidence.get("unique_file_reference_count"),
        "answer_existing_file_reference_count": evidence.get("existing_file_reference_count"),
        "answer_missing_file_reference_count": evidence.get("missing_file_reference_count"),
        "answer_worktree_available": evidence.get("worktree_available"),
    }


def _behavior_metrics(validation: dict[str, Any]) -> dict[str, Any]:
    behavior = validation.get("behavior") or {}
    trace = behavior.get("trace") or {}
    return {
        "behavior_ok": behavior.get("ok"),
        "behavior_hard_passed": behavior.get("hard_passed"),
        "behavior_hard_total": behavior.get("hard_total"),
        "behavior_soft_passed": behavior.get("soft_passed"),
        "behavior_soft_total": behavior.get("soft_total"),
        "trace_source": trace.get("trace_source"),
        "normalized_event_count": trace.get("normalized_event_count"),
        "normalized_event_type_counts": json.dumps(trace.get("normalized_event_type_counts") or {}, sort_keys=True),
        "trace_command_count": trace.get("command_count"),
        "trace_search_count": trace.get("search_count"),
        "trace_source_read_count": trace.get("source_read_count"),
        "trace_graphify_command_count": trace.get("graphify_command_count"),
        "trace_graphify_query_count": trace.get("graphify_query_count"),
        "trace_generic_graphify_query_count": trace.get("generic_graphify_query_count"),
        "trace_specific_graphify_query_count": trace.get("specific_graphify_query_count"),
        "trace_source_after_graph": trace.get("source_after_graph"),
    }



def _judge_metrics(validation: dict[str, Any]) -> dict[str, Any]:
    judge = validation.get("judge") or {}
    run_summary = judge.get("run_summary") or {}
    usage = run_summary.get("usage") or {}
    return {
        "judge_enabled": judge.get("enabled"),
        "judge_ok": judge.get("ok"),
        "judge_overall_score": judge.get("overall_score"),
        "judge_confidence": judge.get("confidence"),
        "judge_error": judge.get("error"),
        "judge_elapsed_ms": run_summary.get("elapsed_ms"),
        "judge_input_tokens": usage.get("input_tokens"),
        "judge_output_tokens": usage.get("output_tokens"),
        "judge_total_tokens": usage.get("total_tokens"),
    }


def _setup_summary(record: dict[str, Any]) -> dict[str, Any]:
    setup = record.get("setup") or {}
    return setup.get("setup") or {}


def _setup_metrics(record: dict[str, Any]) -> dict[str, Any]:
    setup = _setup_summary(record)
    failed = setup.get("failed_required_command")
    return {
        "setup_ok": setup.get("ok"),
        "setup_elapsed_ms": setup.get("elapsed_ms"),
        "setup_command_count": setup.get("command_count", len(setup.get("commands") or [])),
        "setup_failed_command": failed,
    }


def _fmt(value: Any) -> str:
    if value is None:
        return ""
    if isinstance(value, float):
        return f"{value:.3f}".rstrip("0").rstrip(".")
    return str(value)


def _pct(value: Any) -> str:
    if value is None or value == "":
        return ""
    try:
        return f"{float(value) * 100:.1f}%"
    except (TypeError, ValueError):
        return str(value)


def _agent_backend(record: dict[str, Any]) -> str:
    agent = record.get("agent_summary") or {}
    return str(agent.get("backend") or agent.get("agent") or "")


def _agent_output_format(record: dict[str, Any]) -> str:
    agent = record.get("agent_summary") or {}
    return str(agent.get("output_format") or "")


def _run_status_counts(run_records: list[dict[str, Any]]) -> dict[str, int]:
    counts: dict[str, int] = {}
    for record in run_records:
        status = str(record.get("run_status") or "unknown")
        counts[status] = counts.get(status, 0) + 1
    return counts


def _fairness_warnings(run_records: list[dict[str, Any]]) -> list[str]:
    warnings: list[str] = []
    if not run_records:
        return warnings

    prompt_styles = {str(r.get("prompt_style") or "") for r in run_records}
    if len(prompt_styles) > 1:
        warnings.append(
            "Multiple prompt styles are present. Do not compare neutral, weak-hint, and forced runs as one benchmark."
        )
    if "forced" in prompt_styles:
        warnings.append(
            "Forced prompt runs are present. Treat them as capability/ceiling tests, not naturalistic availability tests."
        )

    backends = {_agent_backend(r) for r in run_records if _agent_backend(r)}
    if len(backends) > 1:
        warnings.append(
            "Multiple agent backends are present. Trace completeness, token accounting, and command visibility may not be comparable."
        )

    output_formats = {_agent_output_format(r) for r in run_records if _agent_output_format(r)}
    if len(output_formats) > 1:
        warnings.append(
            "Multiple agent output formats are present. Behavior metrics may be stronger for structured JSONL than for text transcripts."
        )

    status_counts = _run_status_counts(run_records)
    failed = sum(v for k, v in status_counts.items() if k != "completed")
    if failed:
        warnings.append(
            f"{failed} run(s) did not complete successfully. Check failed_runs.md before drawing quality conclusions."
        )
    if status_counts.get("setup_failed"):
        warnings.append(
            "At least one setup failed before agent execution. Those runs measure setup reliability, not answer quality."
        )
    if status_counts.get("agent_failed"):
        warnings.append(
            "At least one agent invocation failed. Backend availability, auth, timeout, or CLI errors may dominate results."
        )

    dirty_repos = {str(r.get("repo") or "repo") for r in run_records if (r.get("repo_status") or {}).get("dirty")}
    if dirty_repos:
        warnings.append(
            "At least one source repository was dirty during evaluation. Commit or snapshot the repo for reproducible comparisons."
        )

    agent_check_failures = [r for r in run_records if (r.get("agent_check") or {}).get("ok") is False]
    if agent_check_failures:
        warnings.append(
            "One or more selected agent profiles failed the lightweight availability check before execution."
        )

    text_traces = []
    for record in run_records:
        trace = (((record.get("validation") or {}).get("behavior") or {}).get("trace") or {})
        if trace.get("trace_source") in {"text", "text-transcript", "best-effort-text"}:
            text_traces.append(record)
    if text_traces:
        warnings.append(
            "Some behavior metrics come from text transcripts. Command extraction is best-effort and may undercount exploration."
        )

    cleaned = [r for r in run_records if (r.get("cleanup") or {}).get("removed")]
    if cleaned:
        warnings.append(
            "Some run worktrees were cleaned after execution. Evidence validation used saved artifacts; manual source re-checks may need reruns."
        )

    return warnings


def _failure_reason(record: dict[str, Any]) -> str:
    status = record.get("run_status") or "unknown"
    if status == "setup_failed":
        setup = _setup_summary(record)
        failed = setup.get("failed_required_command") or "unknown setup command"
        return f"required setup failed: {failed}"
    if status == "agent_failed":
        agent = record.get("agent_summary") or {}
        return agent.get("error_category") or agent.get("reason") or "agent did not exit successfully"
    if status == "validation_failed":
        validation = record.get("validation") or {}
        failed_results = [r for r in validation.get("results", []) if not r.get("ok") and r.get("hard", True)]
        if failed_results:
            return str(failed_results[0].get("message") or failed_results[0].get("id") or "hard validation failed")
        return "validation failed"
    if status == "judge_failed":
        judge = ((record.get("validation") or {}).get("judge") or {})
        return str(judge.get("error") or "judge failed")
    if status == "completed":
        return "completed"
    return str(status)


def _validation_failure_lines(validation: dict[str, Any], *, limit: int = 12) -> list[str]:
    lines: list[str] = []
    for result in validation.get("results", []) or []:
        if result.get("ok"):
            continue
        hardness = "hard" if result.get("hard", True) else "soft"
        msg = result.get("message") or result.get("id") or "validation failed"
        lines.append(f"- [{hardness}] {msg}")
        if len(lines) >= limit:
            break
    behavior = validation.get("behavior") or {}
    for result in behavior.get("results", []) or []:
        if result.get("ok"):
            continue
        hardness = "hard" if result.get("hard", True) else "soft"
        msg = result.get("message") or result.get("id") or "behavior validation failed"
        lines.append(f"- [behavior/{hardness}] {msg}")
        if len(lines) >= limit:
            break
    return lines


def _diagnostic_files(run_dir: Path) -> list[tuple[str, Path]]:
    candidates = [
        "summary.json",
        "run.manifest.json",
        "prompt.txt",
        "answer.md",
        "validation.json",
        "agent.summary.json",
        "events.summary.json",
        "events.normalized.jsonl",
        "stderr.txt",
        "setup/setup.summary.json",
        "environment.txt",
    ]
    return [(name, run_dir / name) for name in candidates if (run_dir / name).exists()]


def _write_run_scorecard(results_root: Path, run_records: list[dict[str, Any]]) -> None:
    csv_path = results_root / "scorecard.csv"
    fields = [
        "mode",
        "prompt_style",
        "case",
        "category",
        "repetition",
        "run_status",
        "setup_ok",
        "setup_elapsed_ms",
        "setup_command_count",
        "setup_failed_command",
        "agent_backend",
        "agent_output_format",
        "agent_ok",
        "agent_error_category",
        "agent_check_ok",
        "agent_check_error_category",
        "exit_code",
        "timed_out",
        "elapsed_ms",
        "input_tokens",
        "output_tokens",
        "total_tokens",
        "json_events",
        "tool_events",
        "command_events",
        "validation_ok",
        "hard_passed",
        "hard_total",
        "soft_passed",
        "soft_total",
        "answer_empty",
        "answer_file_reference_count",
        "answer_unique_file_reference_count",
        "answer_existing_file_reference_count",
        "answer_missing_file_reference_count",
        "answer_worktree_available",
        "behavior_ok",
        "behavior_hard_passed",
        "behavior_hard_total",
        "behavior_soft_passed",
        "behavior_soft_total",
        "trace_source",
        "normalized_event_count",
        "normalized_event_type_counts",
        "trace_command_count",
        "trace_search_count",
        "trace_source_read_count",
        "trace_graphify_command_count",
        "trace_graphify_query_count",
        "trace_generic_graphify_query_count",
        "trace_specific_graphify_query_count",
        "trace_source_after_graph",
        "judge_enabled",
        "judge_ok",
        "judge_overall_score",
        "judge_confidence",
        "judge_error",
        "judge_elapsed_ms",
        "judge_input_tokens",
        "judge_output_tokens",
        "judge_total_tokens",
        "run_dir",
    ]
    with csv_path.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=fields)
        writer.writeheader()
        for record in run_records:
            agent = record.get("agent_summary") or {}
            usage = agent.get("usage") or {}
            counts = agent.get("counts") or {}
            validation = record.get("validation") or {}
            row = {
                "mode": record.get("mode"),
                "prompt_style": record.get("prompt_style"),
                "case": record.get("case"),
                "category": record.get("category"),
                "repetition": record.get("repetition"),
                "run_status": record.get("run_status"),
                "agent_backend": agent.get("backend") or agent.get("agent"),
                "agent_output_format": agent.get("output_format"),
                "agent_ok": agent.get("ok"),
                "agent_error_category": agent.get("error_category"),
                "agent_check_ok": (record.get("agent_check") or {}).get("ok"),
                "agent_check_error_category": (record.get("agent_check") or {}).get("error_category"),
                "exit_code": agent.get("exit_code"),
                "timed_out": agent.get("timed_out"),
                "elapsed_ms": agent.get("elapsed_ms"),
                "input_tokens": usage.get("input_tokens"),
                "output_tokens": usage.get("output_tokens"),
                "total_tokens": usage.get("total_tokens"),
                "json_events": counts.get("json_events"),
                "tool_events": counts.get("tool_events"),
                "command_events": counts.get("command_events"),
                "validation_ok": validation.get("ok"),
                "hard_passed": validation.get("hard_passed"),
                "hard_total": validation.get("hard_total"),
                "soft_passed": validation.get("soft_passed"),
                "soft_total": validation.get("soft_total"),
                "run_dir": record.get("run_dir"),
            }
            row.update(_setup_metrics(record))
            row.update(_answer_metrics(validation))
            row.update(_behavior_metrics(validation))
            row.update(_judge_metrics(validation))
            writer.writerow(row)


def _write_aggregate_scorecard(results_root: Path, aggregates: dict[str, Any]) -> None:
    csv_path = results_root / "aggregate_scorecard.csv"
    fields = [
        "mode",
        "prompt_style",
        "case",
        "runs",
        "completed_rate",
        "setup_ok_rate",
        "setup_failed_count",
        "agent_failed_count",
        "validation_failed_count",
        "judge_failed_count",
        "avg_setup_elapsed_ms",
        "avg_setup_command_count",
        "validation_score",
        "validation_ok_rate",
        "hard_pass_rate",
        "soft_pass_rate",
        "behavior_ok_rate",
        "avg_answer_file_reference_count",
        "avg_answer_existing_file_reference_count",
        "avg_answer_missing_file_reference_count",
        "behavior_hard_pass_rate",
        "behavior_soft_pass_rate",
        "exit_success_rate",
        "avg_elapsed_ms",
        "stdev_elapsed_ms",
        "avg_total_tokens",
        "stdev_total_tokens",
        "avg_command_count",
        "avg_search_count",
        "avg_source_read_count",
        "avg_graphify_query_count",
        "avg_generic_graphify_query_count",
        "avg_specific_graphify_query_count",
        "judge_ok_rate",
        "avg_judge_overall_score",
        "stdev_judge_overall_score",
    ]
    with csv_path.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=fields)
        writer.writeheader()
        for item in aggregates.get("by_mode_case_prompt", []):
            summary = item["summary"]
            row = {"mode": item["mode"], "prompt_style": item["prompt_style"], "case": item["case"]}
            row.update({field: summary.get(field) for field in fields if field not in row})
            writer.writerow(row)


def _summary_markdown(run_records: list[dict[str, Any]], aggregates: dict[str, Any]) -> str:
    lines = ["# Eval Summary", ""]
    if not run_records:
        lines.append("No runs were executed.")
        return "\n".join(lines) + "\n"

    lines.extend([
        "## Aggregate by mode and case",
        "",
        "| Mode | Prompt | Case | Runs | Completed | Setup OK | Val score | Judge | Hard | Soft | Behavior | Setup ms | Agent ms | Avg tokens | Avg graph q | Avg generic q |",
        "|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|",
    ])
    for item in aggregates.get("by_mode_case_prompt", []):
        s = item["summary"]
        lines.append(
            f"| {item['mode']} | {item['prompt_style']} | {item['case']} | "
            f"{s.get('runs')} | {_pct(s.get('completed_rate'))} | {_pct(s.get('setup_ok_rate'))} | "
            f"{_fmt(s.get('validation_score'))} | {_fmt(s.get('avg_judge_overall_score'))} | "
            f"{_pct(s.get('hard_pass_rate'))} | {_pct(s.get('soft_pass_rate'))} | {_pct(s.get('behavior_ok_rate'))} | "
            f"{_fmt(s.get('avg_setup_elapsed_ms'))} | {_fmt(s.get('avg_elapsed_ms'))} | {_fmt(s.get('avg_total_tokens'))} | "
            f"{_fmt(s.get('avg_graphify_query_count'))} | {_fmt(s.get('avg_generic_graphify_query_count'))} |"
        )

    lines.extend(["", "## Mode-level aggregate", ""])
    lines.append("| Mode | Prompt | Runs | Completed | Setup OK | Val score | Judge | Validation OK | Behavior OK | Setup ms | Agent ms | Tokens |")
    lines.append("|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|")
    for item in aggregates.get("by_mode", []):
        s = item["summary"]
        lines.append(
            f"| {item['mode']} | {item['prompt_style']} | {s.get('runs')} | {_pct(s.get('completed_rate'))} | {_pct(s.get('setup_ok_rate'))} | "
            f"{_fmt(s.get('validation_score'))} | {_fmt(s.get('avg_judge_overall_score'))} | "
            f"{_pct(s.get('validation_ok_rate'))} | {_pct(s.get('behavior_ok_rate'))} | "
            f"{_fmt(s.get('avg_setup_elapsed_ms'))} | {_fmt(s.get('avg_elapsed_ms'))} | {_fmt(s.get('avg_total_tokens'))} |"
        )

    lines.extend(["", "## Per-run scorecard", ""])
    lines.append("| Mode | Agent | Prompt | Case | Rep | Status | Setup | Exit | Validation | Behavior | Judge | Graphify q | Generic q | Source reads | Elapsed ms | Tokens |")
    lines.append("|---|---|---|---|---:|---|---|---:|---|---|---:|---:|---:|---:|---:|---:|")
    for record in run_records:
        agent = record.get("agent_summary") or {}
        usage = agent.get("usage") or {}
        validation = record.get("validation") or {}
        behavior = validation.get("behavior") or {}
        trace = behavior.get("trace") or {}
        behavior_score = f"{behavior.get('hard_passed')}/{behavior.get('hard_total')} hard"
        setup = _setup_summary(record)
        lines.append(
            f"| {record.get('mode')} | {agent.get('backend') or agent.get('agent')} | {record.get('prompt_style')} | {record.get('case')} | "
            f"{record.get('repetition')} | {record.get('run_status', '')} | {_fmt(setup.get('ok'))} | {agent.get('exit_code')} | "
            f"{validation.get('hard_passed')}/{validation.get('hard_total')} hard | "
            f"{behavior_score} | "
            f"{_fmt((validation.get('judge') or {}).get('overall_score'))} | "
            f"{trace.get('graphify_query_count', '')} | "
            f"{trace.get('generic_graphify_query_count', '')} | "
            f"{trace.get('source_read_count', '')} | "
            f"{agent.get('elapsed_ms')} | {usage.get('total_tokens', '')} |"
        )

    lines.extend(["", "## Notes", ""])
    lines.append("`Val score` is a deterministic proxy: 80% hard-validator pass rate + 20% soft-validator pass rate when both exist. It is not an LLM quality score.")
    lines.append("Behavior metrics are trace-derived heuristics. They are best used to compare runs, not as absolute correctness judgements.")
    lines.append("For text-oriented CLIs, command extraction is best-effort from stdout transcripts and may be incomplete compared with Codex JSONL traces.")
    lines.append("Judge scores are optional LLM assessments and are reported separately from deterministic validation.")
    lines.append("Setup metrics are recorded separately from agent runtime so pre-agent setup costs remain visible.")
    lines.append("Agent availability checks are lightweight preflight checks; a failed check warns that the backend may fail before spending tokens.")
    return "\n".join(lines) + "\n"


def _case_comparison_markdown(aggregates: dict[str, Any]) -> str:
    lines = ["# Per-case Comparative Report", ""]
    cases = aggregates.get("by_case", [])
    if not cases:
        lines.append("No case aggregates were generated.")
        return "\n".join(lines) + "\n"

    for case_item in cases:
        lines.append(f"## {case_item['case']} — {case_item['prompt_style']}")
        lines.append("")
        winner = case_item.get("winner_heuristic")
        if winner:
            lines.append(f"Heuristic winner: **{winner}**")
            lines.append("")
        lines.append("| Mode | Runs | Completed | Setup OK | Val score | Judge | Hard | Soft | Behavior OK | Setup ms | Agent ms | Tokens | Avg graph q | Avg generic q | Avg source reads |")
        lines.append("|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|")
        for mode_item in case_item.get("modes", []):
            s = mode_item["summary"]
            lines.append(
                f"| {mode_item['mode']} | {s.get('runs')} | {_pct(s.get('completed_rate'))} | {_pct(s.get('setup_ok_rate'))} | "
                f"{_fmt(s.get('validation_score'))} | {_fmt(s.get('avg_judge_overall_score'))} | "
                f"{_pct(s.get('hard_pass_rate'))} | {_pct(s.get('soft_pass_rate'))} | {_pct(s.get('behavior_ok_rate'))} | "
                f"{_fmt(s.get('avg_setup_elapsed_ms'))} | {_fmt(s.get('avg_elapsed_ms'))} | {_fmt(s.get('avg_total_tokens'))} | "
                f"{_fmt(s.get('avg_graphify_query_count'))} | {_fmt(s.get('avg_generic_graphify_query_count'))} | "
                f"{_fmt(s.get('avg_source_read_count'))} |"
            )
        lines.append("")
        lines.append("Interpretation checklist:")
        lines.append("")
        lines.append("- Prefer higher hard pass rate before considering cost.")
        lines.append("- Check whether behavior improvements come with unacceptable token/time regressions.")
        lines.append("- Treat the heuristic winner as a triage signal, not as a final judgement.")
        lines.append("")
    return "\n".join(lines) + "\n"


def _fairness_warnings_markdown(run_records: list[dict[str, Any]]) -> str:
    lines = ["# Fairness Warnings", ""]
    warnings = _fairness_warnings(run_records)
    if not warnings:
        lines.append("No obvious fairness warnings were detected from the saved run metadata.")
        return "\n".join(lines) + "\n"
    lines.append("Review these before using the report as a benchmark conclusion:")
    lines.append("")
    for warning in warnings:
        lines.append(f"- {warning}")
    lines.append("")
    lines.append("These warnings do not invalidate the run. They identify conditions that can make mode comparisons less apples-to-apples.")
    return "\n".join(lines) + "\n"


def _failed_runs_markdown(run_records: list[dict[str, Any]]) -> str:
    lines = ["# Failed / Non-completed Runs", ""]
    failed = [r for r in run_records if r.get("run_status") != "completed"]
    if not failed:
        lines.append("All runs completed successfully.")
        return "\n".join(lines) + "\n"

    lines.append("| Mode | Prompt | Case | Rep | Status | Reason | Run directory |")
    lines.append("|---|---|---|---:|---|---|---|")
    for record in failed:
        lines.append(
            f"| {record.get('mode')} | {record.get('prompt_style')} | {record.get('case')} | {record.get('repetition')} | "
            f"{record.get('run_status')} | {_failure_reason(record)} | `{record.get('run_dir')}` |"
        )

    lines.append("")
    lines.append("Open the run directory and start with `summary.json`, `stderr.txt`, `agent.summary.json`, and `validation.json`.")
    return "\n".join(lines) + "\n"


def _mode_summary_markdown(aggregates: dict[str, Any]) -> str:
    lines = ["# Mode Summary", ""]
    items = aggregates.get("by_mode", [])
    if not items:
        lines.append("No mode aggregates were generated.")
        return "\n".join(lines) + "\n"

    lines.append("| Mode | Prompt | Runs | Completed | Setup OK | Validation OK | Behavior OK | Val score | Judge | Setup ms | Agent ms | Tokens |")
    lines.append("|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|")
    for item in items:
        s = item["summary"]
        lines.append(
            f"| {item['mode']} | {item['prompt_style']} | {s.get('runs')} | {_pct(s.get('completed_rate'))} | "
            f"{_pct(s.get('setup_ok_rate'))} | {_pct(s.get('validation_ok_rate'))} | {_pct(s.get('behavior_ok_rate'))} | "
            f"{_fmt(s.get('validation_score'))} | {_fmt(s.get('avg_judge_overall_score'))} | "
            f"{_fmt(s.get('avg_setup_elapsed_ms'))} | {_fmt(s.get('avg_elapsed_ms'))} | {_fmt(s.get('avg_total_tokens'))} |"
        )
    lines.append("")
    lines.append("## Reading this table")
    lines.append("")
    lines.append("- `Completed` is the clean end-to-end success rate after setup, agent execution, validation, and optional judge.")
    lines.append("- `Val score` is deterministic and intentionally separate from optional judge scores.")
    lines.append("- Setup and agent timings are separate so prepared-tool cost remains visible.")
    return "\n".join(lines) + "\n"


def explain_run_markdown(record: dict[str, Any]) -> str:
    run_dir = Path(str(record.get("run_dir") or "."))
    agent = record.get("agent_summary") or {}
    validation = record.get("validation") or {}
    setup = _setup_summary(record)
    behavior = validation.get("behavior") or {}
    trace = behavior.get("trace") or {}
    evidence = ((validation.get("answer") or {}).get("evidence") or {})
    usage = agent.get("usage") or {}

    lines = [f"# Run Diagnostic — {record.get('mode')} / {record.get('case')} / r{record.get('repetition')}", ""]
    lines.append(f"Status: **{record.get('run_status')}**")
    lines.append(f"Reason: {_failure_reason(record)}")
    lines.append("")
    lines.append("## Identity")
    lines.append("")
    lines.append(f"- Mode: `{record.get('mode')}`")
    lines.append(f"- Case: `{record.get('case')}`")
    lines.append(f"- Prompt style: `{record.get('prompt_style')}`")
    lines.append(f"- Agent backend: `{agent.get('backend') or agent.get('agent')}`")
    lines.append(f"- Run directory: `{record.get('run_dir')}`")
    lines.append("")
    lines.append("## Setup")
    lines.append("")
    lines.append(f"- OK: `{setup.get('ok')}`")
    lines.append(f"- Elapsed ms: `{setup.get('elapsed_ms')}`")
    lines.append(f"- Command count: `{setup.get('command_count', len(setup.get('commands') or []))}`")
    if setup.get("failed_required_command"):
        lines.append(f"- Failed required command: `{setup.get('failed_required_command')}`")
    lines.append("")
    lines.append("## Agent")
    lines.append("")
    lines.append(f"- Exit code: `{agent.get('exit_code')}`")
    lines.append(f"- Timed out: `{agent.get('timed_out')}`")
    lines.append(f"- Error category: `{agent.get('error_category')}`")
    lines.append(f"- Elapsed ms: `{agent.get('elapsed_ms')}`")
    lines.append(f"- Total tokens: `{usage.get('total_tokens')}`")
    lines.append("")
    lines.append("## Validation")
    lines.append("")
    lines.append(f"- OK: `{validation.get('ok')}`")
    lines.append(f"- Hard: `{validation.get('hard_passed')}/{validation.get('hard_total')}`")
    lines.append(f"- Soft: `{validation.get('soft_passed')}/{validation.get('soft_total')}`")
    failures = _validation_failure_lines(validation)
    if failures:
        lines.append("")
        lines.append("### Validation failures")
        lines.extend(failures)
    lines.append("")
    lines.append("## Evidence")
    lines.append("")
    lines.append(f"- Answer empty: `{evidence.get('answer_empty')}`")
    lines.append(f"- File references: `{evidence.get('file_reference_count')}`")
    lines.append(f"- Existing file references: `{evidence.get('existing_file_reference_count')}`")
    lines.append(f"- Missing file references: `{evidence.get('missing_file_reference_count')}`")
    lines.append("")
    lines.append("## Behavior")
    lines.append("")
    lines.append(f"- Trace source: `{trace.get('trace_source')}`")
    lines.append(f"- Commands: `{trace.get('command_count')}`")
    lines.append(f"- Searches: `{trace.get('search_count')}`")
    lines.append(f"- Source reads: `{trace.get('source_read_count')}`")
    lines.append(f"- Graphify queries: `{trace.get('graphify_query_count')}`")
    lines.append(f"- Generic Graphify queries: `{trace.get('generic_graphify_query_count')}`")
    lines.append(f"- Specific Graphify queries: `{trace.get('specific_graphify_query_count')}`")
    lines.append("")
    lines.append("## Useful files")
    lines.append("")
    for label, path in _diagnostic_files(run_dir):
        lines.append(f"- `{label}` — `{path}`")
    return "\n".join(lines) + "\n"


def load_run_record_from_path(path: Path) -> dict[str, Any]:
    candidate = path
    if path.is_dir():
        candidate = path / "summary.json"
    record = read_json(candidate, default=None)
    if not isinstance(record, dict):
        raise ValueError(f"Could not read run summary from {path}")
    if "runs" in record and isinstance(record.get("runs"), list):
        raise ValueError(f"{path} looks like a result-root summary, not a single run. Pass a run directory or run summary.json.")
    return record


def compare_result_sets(result_sets: list[tuple[str, list[dict[str, Any]]]]) -> str:
    lines = ["# Result Set Comparison", ""]
    if not result_sets:
        lines.append("No result sets were provided.")
        return "\n".join(lines) + "\n"

    lines.append("| Result set | Runs | Completed | Setup OK | Validation OK | Avg tokens | Avg agent ms | Warnings |")
    lines.append("|---|---:|---:|---:|---:|---:|---:|---:|")
    for label, records in result_sets:
        aggregates = build_aggregates(records)
        summary = aggregates.get("by_mode", [])
        # Flatten via one aggregate over all records for simple result-set comparison.
        from .aggregate import summarize_group
        s = summarize_group(records) if records else {}
        lines.append(
            f"| {label} | {s.get('runs', len(records))} | {_pct(s.get('completed_rate'))} | {_pct(s.get('setup_ok_rate'))} | "
            f"{_pct(s.get('validation_ok_rate'))} | {_fmt(s.get('avg_total_tokens'))} | {_fmt(s.get('avg_elapsed_ms'))} | "
            f"{len(_fairness_warnings(records))} |"
        )
    lines.append("")
    lines.append("This comparison is intentionally high-level. Open each result folder's `case_comparison.md` and `fairness_warnings.md` for detailed interpretation.")
    return "\n".join(lines) + "\n"


def write_reports(results_root: Path, run_records: list[dict[str, Any]]) -> None:
    ensure_dir(results_root)
    aggregates = build_aggregates(run_records)
    write_json(results_root / "summary.json", with_schema({"runs": run_records, "aggregates": aggregates}, SUMMARY_SCHEMA_VERSION))
    write_json(results_root / "aggregates.json", with_schema(aggregates, AGGREGATE_SUMMARY_SCHEMA_VERSION))
    _write_run_scorecard(results_root, run_records)
    _write_aggregate_scorecard(results_root, aggregates)
    write_text(results_root / "summary.md", _summary_markdown(run_records, aggregates))
    write_text(results_root / "case_comparison.md", _case_comparison_markdown(aggregates))
    write_text(results_root / "mode_summary.md", _mode_summary_markdown(aggregates))
    write_text(results_root / "fairness_warnings.md", _fairness_warnings_markdown(run_records))
    write_text(results_root / "failed_runs.md", _failed_runs_markdown(run_records))
