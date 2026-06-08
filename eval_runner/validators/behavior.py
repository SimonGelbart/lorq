from __future__ import annotations

from pathlib import Path
from typing import Any

from ..trace import analyze_trace


def run_behavior_validators(output_dir: Path, case: dict[str, Any], mode: dict[str, Any]) -> dict[str, Any]:
    expectations = mode.get("expectations") or {}
    case_behavior = (case.get("validation") or {}).get("behavior") or {}
    trace = analyze_trace(output_dir, mode)
    checks: list[dict[str, Any]] = []

    if expectations.get("should_avoid_generic_graph_queries") or case_behavior.get("avoid_generic_graph_queries"):
        max_generic = int(expectations.get("max_generic_graph_queries", case_behavior.get("max_generic_graph_queries", 0)))
        actual = trace["generic_graphify_query_count"]
        checks.append({
            "type": "avoid_generic_graph_queries",
            "ok": actual <= max_generic,
            "actual": actual,
            "expected_max": max_generic,
            "soft": False,
        })

    if "max_graph_queries_before_source_fallback" in expectations:
        max_queries = int(expectations["max_graph_queries_before_source_fallback"])
        actual = trace["graphify_query_count"]
        # This is a guardrail, not a hard correctness requirement; exact runs may legitimately need more.
        checks.append({
            "type": "graph_query_budget",
            "ok": actual <= max_queries or trace["source_after_graph"],
            "actual": actual,
            "expected_max_before_source_fallback": max_queries,
            "source_after_graph": trace["source_after_graph"],
            "soft": True,
        })

    if expectations.get("should_verify_with_source") or case_behavior.get("should_verify_with_source"):
        # Soft by default because source verification can happen through search + snippets,
        # but it is a useful warning for graph-heavy answers.
        checks.append({
            "type": "source_verification_observed",
            "ok": trace["source_read_count"] > 0,
            "actual_source_read_count": trace["source_read_count"],
            "soft": True,
        })

    if case_behavior.get("require_graphify_query"):
        checks.append({
            "type": "require_graphify_query",
            "ok": trace["graphify_query_count"] > 0,
            "actual": trace["graphify_query_count"],
            "soft": False,
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
        "trace": trace,
    }
