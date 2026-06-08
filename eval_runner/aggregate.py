from __future__ import annotations

from collections import defaultdict
from statistics import mean, stdev
from typing import Any, Iterable


def _num(value: Any) -> float | None:
    if value is None or value == "":
        return None
    if isinstance(value, bool):
        return float(int(value))
    try:
        return float(value)
    except (TypeError, ValueError):
        return None


def _avg(values: Iterable[Any]) -> float | None:
    nums = [_num(value) for value in values]
    nums = [value for value in nums if value is not None]
    if not nums:
        return None
    return round(mean(nums), 3)


def _stdev(values: Iterable[Any]) -> float | None:
    nums = [_num(value) for value in values]
    nums = [value for value in nums if value is not None]
    if len(nums) < 2:
        return None
    return round(stdev(nums), 3)


def _rate(passed: Any, total: Any) -> float | None:
    p = _num(passed)
    t = _num(total)
    if p is None or t is None or t == 0:
        return None
    return round(p / t, 4)


def _bool_rate(values: Iterable[Any]) -> float | None:
    known = [value for value in values if value is not None]
    if not known:
        return None
    return round(sum(1 for value in known if bool(value)) / len(known), 4)


def _agent(record: dict[str, Any]) -> dict[str, Any]:
    return record.get("agent_summary") or {}


def _usage(record: dict[str, Any]) -> dict[str, Any]:
    return _agent(record).get("usage") or {}


def _validation(record: dict[str, Any]) -> dict[str, Any]:
    return record.get("validation") or {}


def _behavior(record: dict[str, Any]) -> dict[str, Any]:
    return (_validation(record).get("behavior") or {})


def _answer(record: dict[str, Any]) -> dict[str, Any]:
    return (_validation(record).get("answer") or {})


def _evidence(record: dict[str, Any]) -> dict[str, Any]:
    return (_answer(record).get("evidence") or {})


def _trace(record: dict[str, Any]) -> dict[str, Any]:
    return (_behavior(record).get("trace") or {})


def _judge(record: dict[str, Any]) -> dict[str, Any]:
    return (_validation(record).get("judge") or {})


def _setup(record: dict[str, Any]) -> dict[str, Any]:
    setup = record.get("setup") or {}
    return setup.get("setup") or {}


def _run_completed(record: dict[str, Any]) -> bool:
    return record.get("run_status") == "completed"


def summarize_group(records: list[dict[str, Any]]) -> dict[str, Any]:
    hard_passed = sum(int(_num(_validation(r).get("hard_passed")) or 0) for r in records)
    hard_total = sum(int(_num(_validation(r).get("hard_total")) or 0) for r in records)
    soft_passed = sum(int(_num(_validation(r).get("soft_passed")) or 0) for r in records)
    soft_total = sum(int(_num(_validation(r).get("soft_total")) or 0) for r in records)
    behavior_hard_passed = sum(int(_num(_behavior(r).get("hard_passed")) or 0) for r in records)
    behavior_hard_total = sum(int(_num(_behavior(r).get("hard_total")) or 0) for r in records)
    behavior_soft_passed = sum(int(_num(_behavior(r).get("soft_passed")) or 0) for r in records)
    behavior_soft_total = sum(int(_num(_behavior(r).get("soft_total")) or 0) for r in records)

    hard_rate = _rate(hard_passed, hard_total)
    soft_rate = _rate(soft_passed, soft_total)
    behavior_hard_rate = _rate(behavior_hard_passed, behavior_hard_total)
    behavior_soft_rate = _rate(behavior_soft_passed, behavior_soft_total)

    # Stable, transparent proxy for sorting. It is not an LLM quality score.
    if hard_rate is None and soft_rate is None:
        validation_score = None
    elif soft_rate is None:
        validation_score = hard_rate
    elif hard_rate is None:
        validation_score = soft_rate
    else:
        validation_score = round((hard_rate * 0.8) + (soft_rate * 0.2), 4)

    return {
        "runs": len(records),
        "completed_rate": _bool_rate(_run_completed(r) for r in records),
        "setup_ok_rate": _bool_rate(_setup(r).get("ok") for r in records),
        "setup_failed_count": sum(1 for r in records if r.get("run_status") == "setup_failed"),
        "agent_failed_count": sum(1 for r in records if r.get("run_status") == "agent_failed"),
        "validation_failed_count": sum(1 for r in records if r.get("run_status") == "validation_failed"),
        "judge_failed_count": sum(1 for r in records if r.get("run_status") == "judge_failed"),
        "avg_setup_elapsed_ms": _avg(_setup(r).get("elapsed_ms") for r in records),
        "avg_setup_command_count": _avg(_setup(r).get("command_count") for r in records),
        "exit_success_rate": _bool_rate((_agent(r).get("exit_code") == 0) for r in records),
        "validation_ok_rate": _bool_rate(_validation(r).get("ok") for r in records),
        "behavior_ok_rate": _bool_rate(_behavior(r).get("ok") for r in records),
        "avg_answer_file_reference_count": _avg(_evidence(r).get("file_reference_count") for r in records),
        "avg_answer_existing_file_reference_count": _avg(_evidence(r).get("existing_file_reference_count") for r in records),
        "avg_answer_missing_file_reference_count": _avg(_evidence(r).get("missing_file_reference_count") for r in records),
        "hard_passed": hard_passed,
        "hard_total": hard_total,
        "hard_pass_rate": hard_rate,
        "soft_passed": soft_passed,
        "soft_total": soft_total,
        "soft_pass_rate": soft_rate,
        "validation_score": validation_score,
        "behavior_hard_passed": behavior_hard_passed,
        "behavior_hard_total": behavior_hard_total,
        "behavior_hard_pass_rate": behavior_hard_rate,
        "behavior_soft_passed": behavior_soft_passed,
        "behavior_soft_total": behavior_soft_total,
        "behavior_soft_pass_rate": behavior_soft_rate,
        "avg_elapsed_ms": _avg(_agent(r).get("elapsed_ms") for r in records),
        "stdev_elapsed_ms": _stdev(_agent(r).get("elapsed_ms") for r in records),
        "avg_total_tokens": _avg(_usage(r).get("total_tokens") for r in records),
        "stdev_total_tokens": _stdev(_usage(r).get("total_tokens") for r in records),
        "avg_input_tokens": _avg(_usage(r).get("input_tokens") for r in records),
        "avg_output_tokens": _avg(_usage(r).get("output_tokens") for r in records),
        "avg_command_count": _avg(_trace(r).get("command_count") for r in records),
        "avg_search_count": _avg(_trace(r).get("search_count") for r in records),
        "avg_source_read_count": _avg(_trace(r).get("source_read_count") for r in records),
        "avg_graphify_query_count": _avg(_trace(r).get("graphify_query_count") for r in records),
        "avg_generic_graphify_query_count": _avg(_trace(r).get("generic_graphify_query_count") for r in records),
        "avg_specific_graphify_query_count": _avg(_trace(r).get("specific_graphify_query_count") for r in records),
        "judge_ok_rate": _bool_rate(_judge(r).get("ok") for r in records if _judge(r).get("enabled")),
        "avg_judge_overall_score": _avg(_judge(r).get("overall_score") for r in records),
        "stdev_judge_overall_score": _stdev(_judge(r).get("overall_score") for r in records),
    }


def _group_by(records: list[dict[str, Any]], keys: tuple[str, ...]) -> dict[tuple[Any, ...], list[dict[str, Any]]]:
    grouped: dict[tuple[Any, ...], list[dict[str, Any]]] = defaultdict(list)
    for record in records:
        grouped[tuple(record.get(key) for key in keys)].append(record)
    return dict(grouped)


def _sort_key_for_winner(item: dict[str, Any]) -> tuple[float, float, float, float, float]:
    """Higher is better. Cost fields are negated."""
    summary = item["summary"]
    validation_score = _num(summary.get("validation_score"))
    behavior_ok = _num(summary.get("behavior_ok_rate"))
    judge_score = _num(summary.get("avg_judge_overall_score"))
    avg_tokens = _num(summary.get("avg_total_tokens"))
    avg_elapsed = _num(summary.get("avg_elapsed_ms"))
    return (
        validation_score if validation_score is not None else -1.0,
        behavior_ok if behavior_ok is not None else -1.0,
        judge_score if judge_score is not None else -1.0,
        -(avg_tokens if avg_tokens is not None else 10**18),
        -(avg_elapsed if avg_elapsed is not None else 10**18),
    )


def build_aggregates(run_records: list[dict[str, Any]]) -> dict[str, Any]:
    by_mode_case_prompt: list[dict[str, Any]] = []
    for (mode, case, prompt_style), records in sorted(_group_by(run_records, ("mode", "case", "prompt_style")).items()):
        by_mode_case_prompt.append({
            "mode": mode,
            "case": case,
            "prompt_style": prompt_style,
            "summary": summarize_group(records),
        })

    by_mode: list[dict[str, Any]] = []
    for (mode, prompt_style), records in sorted(_group_by(run_records, ("mode", "prompt_style")).items()):
        by_mode.append({
            "mode": mode,
            "prompt_style": prompt_style,
            "summary": summarize_group(records),
        })

    by_case: list[dict[str, Any]] = []
    for (case, prompt_style), records in sorted(_group_by(run_records, ("case", "prompt_style")).items()):
        candidates = [item for item in by_mode_case_prompt if item["case"] == case and item["prompt_style"] == prompt_style]
        winner = max(candidates, key=_sort_key_for_winner)["mode"] if candidates else None
        by_case.append({
            "case": case,
            "prompt_style": prompt_style,
            "winner_heuristic": winner,
            "summary": summarize_group(records),
            "modes": candidates,
        })

    return {
        "by_mode_case_prompt": by_mode_case_prompt,
        "by_mode": by_mode,
        "by_case": by_case,
    }
