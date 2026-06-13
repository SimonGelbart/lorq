from __future__ import annotations

import re
from pathlib import Path
from typing import Any

from ..utils import read_text, write_json
from ..contracts import VALIDATION_RESULT_SCHEMA_VERSION, with_schema
from .behavior import run_behavior_validators
from .evidence import build_evidence_metrics, normalize_required_symbol, symbol_near_file_reference, worktree_from_manifest


def _contains(text: str, needle: str, case_sensitive: bool = False) -> bool:
    if case_sensitive:
        return needle in text
    return needle.lower() in text.lower()


def _path_contains(text: str, file_path: str) -> bool:
    normalized_text = text.replace("\\", "/")
    normalized_path = file_path.replace("\\", "/")
    return normalized_path in normalized_text


def _regex_contains(text: str, pattern: str) -> bool:
    return re.search(pattern, text, flags=re.IGNORECASE | re.MULTILINE) is not None


def _run_answer_validators(answer: str, case: dict[str, Any], mode: dict[str, Any], output_dir: Path) -> dict[str, Any]:
    validation = case.get("validation") or {}
    evidence_config = validation.get("required_evidence") or {}
    evidence = build_evidence_metrics(output_dir, answer)
    checks: list[dict[str, Any]] = []

    checks.append({
        "type": "answer_not_empty",
        "ok": not evidence["answer_empty"],
        "message": "answer.md should contain a non-empty final answer",
    })

    required_symbols = [normalize_required_symbol(item) for item in (validation.get("required_symbols", []) or [])]
    require_symbol_near_file = bool(evidence_config.get("require_symbol_near_file_reference", False))
    for item in required_symbols:
        symbol = str(item.get("symbol") or "")
        if not symbol:
            checks.append({"type": "required_symbol", "value": item, "ok": False, "message": "required symbol is empty"})
            continue
        ok = _contains(answer, symbol, case_sensitive=False)
        checks.append({"type": "required_symbol", "value": symbol, "ok": ok})
        must_be_near_file = bool(item.get("must_be_near_file_reference", require_symbol_near_file))
        if must_be_near_file:
            near = symbol_near_file_reference(answer, symbol, evidence["file_references"])
            checks.append({
                "type": "required_symbol_source_grounded",
                "value": symbol,
                "ok": near,
                "message": "symbol should appear near a repository file reference",
            })

    worktree = worktree_from_manifest(output_dir)
    for file_path in validation.get("required_files", []) or []:
        file_path = str(file_path)
        mentioned = _path_contains(answer, file_path)
        checks.append({"type": "required_file", "value": file_path, "ok": mentioned})
        if evidence_config.get("require_existing_required_files", True) and worktree and worktree.exists():
            rel = Path(file_path.replace("\\", "/"))
            exists = not rel.is_absolute() and ".." not in rel.parts and (worktree / rel).is_file()
            checks.append({
                "type": "required_file_exists",
                "value": file_path,
                "ok": exists,
                "message": "required file path should exist in the evaluated worktree",
            })

    for concept in validation.get("expected_concepts", []) or []:
        ok = _contains(answer, str(concept), case_sensitive=False)
        checks.append({"type": "expected_concept", "value": concept, "ok": ok, "soft": True})

    for claim in validation.get("forbidden_claims", []) or []:
        found = _contains(answer, str(claim), case_sensitive=False)
        checks.append({"type": "forbidden_claim", "value": claim, "ok": not found})

    for pattern in validation.get("forbidden_patterns", []) or []:
        found = _regex_contains(answer, str(pattern))
        checks.append({"type": "forbidden_pattern", "value": pattern, "ok": not found})

    min_existing_refs = evidence_config.get("min_existing_file_references")
    if min_existing_refs is not None:
        actual = evidence["existing_file_reference_count"] if evidence["worktree_available"] else evidence["file_reference_count"]
        checks.append({
            "type": "minimum_existing_file_references",
            "ok": actual >= int(min_existing_refs),
            "actual": actual,
            "expected_min": int(min_existing_refs),
        })

    min_source_files = evidence_config.get("min_source_files")
    if min_source_files is not None:
        actual = evidence["existing_file_reference_count"] if evidence["worktree_available"] else evidence["unique_file_reference_count"]
        checks.append({
            "type": "minimum_source_files",
            "ok": actual >= int(min_source_files),
            "actual": actual,
            "expected_min": int(min_source_files),
        })

    max_missing = evidence_config.get("max_missing_cited_files")
    if max_missing is not None and evidence["worktree_available"]:
        actual = evidence["missing_file_reference_count"]
        checks.append({
            "type": "max_missing_cited_files",
            "ok": actual <= int(max_missing),
            "actual": actual,
            "expected_max": int(max_missing),
        })

    # Always surface hallucinated/missing citations as a soft warning when the worktree is available.
    if evidence["worktree_available"] and evidence["file_reference_count"] > 0:
        checks.append({
            "type": "cited_file_paths_exist",
            "ok": evidence["missing_file_reference_count"] == 0,
            "actual_missing": evidence["missing_file_reference_count"],
            "missing": evidence["missing_file_references"],
            "soft": True,
        })

    hard_checks = [check for check in checks if not check.get("soft")]
    soft_checks = [check for check in checks if check.get("soft")]
    return {
        "ok": all(check["ok"] for check in hard_checks),
        "hard_passed": sum(1 for check in hard_checks if check["ok"]),
        "hard_total": len(hard_checks),
        "soft_passed": sum(1 for check in soft_checks if check["ok"]),
        "soft_total": len(soft_checks),
        "checks": checks,
        "evidence": evidence,
    }


def _combine(answer_result: dict[str, Any], behavior_result: dict[str, Any]) -> dict[str, Any]:
    hard_passed = answer_result["hard_passed"] + behavior_result["hard_passed"]
    hard_total = answer_result["hard_total"] + behavior_result["hard_total"]
    soft_passed = answer_result["soft_passed"] + behavior_result["soft_passed"]
    soft_total = answer_result["soft_total"] + behavior_result["soft_total"]
    return {
        "ok": answer_result["ok"] and behavior_result["ok"],
        "hard_passed": hard_passed,
        "hard_total": hard_total,
        "soft_passed": soft_passed,
        "soft_total": soft_total,
    }


def run_validators(output_dir: Path, case: dict[str, Any], mode: dict[str, Any]) -> dict[str, Any]:
    answer = read_text(output_dir / "answer.md")
    answer_result = _run_answer_validators(answer, case, mode, output_dir)
    behavior_result = run_behavior_validators(output_dir, case, mode)
    combined = _combine(answer_result, behavior_result)
    result = with_schema({
        "case_id": case.get("id"),
        "mode_id": mode.get("id"),
        **combined,
        "answer": answer_result,
        "behavior": behavior_result,
        # Keep the old flat field for compatibility with v0.1 readers.
        "checks": answer_result["checks"] + behavior_result["checks"],
    }, VALIDATION_RESULT_SCHEMA_VERSION)
    write_json(output_dir / "validation.json", result)
    write_json(output_dir / "behavior.summary.json", behavior_result)
    return result
