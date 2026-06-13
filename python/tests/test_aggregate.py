from eval_runner.aggregate import build_aggregates, summarize_group


def _record(mode, case, rep, hard_passed, hard_total, soft_passed=0, soft_total=0, tokens=100, elapsed=1000):
    return {
        "mode": mode,
        "case": case,
        "prompt_style": "neutral",
        "repetition": rep,
        "agent_summary": {
            "exit_code": 0,
            "elapsed_ms": elapsed,
            "usage": {"total_tokens": tokens},
        },
        "validation": {
            "ok": hard_passed == hard_total,
            "hard_passed": hard_passed,
            "hard_total": hard_total,
            "soft_passed": soft_passed,
            "soft_total": soft_total,
            "behavior": {
                "ok": True,
                "hard_passed": 0,
                "hard_total": 0,
                "soft_passed": 1,
                "soft_total": 1,
                "trace": {
                    "command_count": 10,
                    "search_count": 2,
                    "source_read_count": 3,
                    "graphify_query_count": 1,
                    "generic_graphify_query_count": 0,
                    "specific_graphify_query_count": 1,
                },
            },
        },
    }


def test_summarize_group_rates_and_variance():
    records = [
        _record("a", "case", 1, 2, 2, 1, 2, tokens=100, elapsed=1000),
        _record("a", "case", 2, 1, 2, 2, 2, tokens=200, elapsed=2000),
    ]
    summary = summarize_group(records)
    assert summary["runs"] == 2
    assert summary["hard_pass_rate"] == 0.75
    assert summary["soft_pass_rate"] == 0.75
    assert summary["validation_score"] == 0.75
    assert summary["avg_total_tokens"] == 150
    assert summary["stdev_total_tokens"] is not None


def test_build_aggregates_selects_best_validation_score():
    records = [
        _record("weaker", "case", 1, 1, 2),
        _record("stronger", "case", 1, 2, 2),
    ]
    aggregates = build_aggregates(records)
    assert aggregates["by_case"][0]["winner_heuristic"] == "stronger"
    assert len(aggregates["by_mode_case_prompt"]) == 2
