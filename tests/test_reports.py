from pathlib import Path

from eval_runner.reports import write_reports


def test_write_reports_creates_aggregate_outputs(tmp_path: Path):
    records = [
        {
            "mode": "mode-a",
            "case": "case-a",
            "prompt_style": "neutral",
            "category": "demo",
            "repetition": 1,
            "run_dir": str(tmp_path / "run"),
            "agent_summary": {
                "exit_code": 0,
                "elapsed_ms": 1000,
                "usage": {"total_tokens": 123},
                "counts": {"json_events": 1, "tool_events": 0, "command_events": 0},
            },
            "validation": {
                "ok": True,
                "hard_passed": 1,
                "hard_total": 1,
                "soft_passed": 0,
                "soft_total": 0,
                "behavior": {
                    "ok": True,
                    "hard_passed": 0,
                    "hard_total": 0,
                    "soft_passed": 0,
                    "soft_total": 0,
                    "trace": {},
                },
            },
        }
    ]
    write_reports(tmp_path, records)
    assert (tmp_path / "scorecard.csv").exists()
    assert (tmp_path / "aggregate_scorecard.csv").exists()
    assert (tmp_path / "aggregates.json").exists()
    assert (tmp_path / "case_comparison.md").exists()
    assert "mode-a" in (tmp_path / "case_comparison.md").read_text()

from eval_runner.reports import compare_result_sets, explain_run_markdown


def test_reports_create_ux_markdowns(tmp_path: Path):
    records = [
        {
            "mode": "mode-a",
            "case": "case-a",
            "prompt_style": "forced",
            "category": "demo",
            "repetition": 1,
            "run_status": "agent_failed",
            "run_dir": str(tmp_path / "run"),
            "agent_check": {"ok": False, "error_category": "command_not_found"},
            "agent_summary": {
                "backend": "text-agent",
                "output_format": "text",
                "exit_code": 1,
                "error_category": "nonzero_exit",
                "elapsed_ms": 1000,
                "usage": {"total_tokens": 123},
                "counts": {},
            },
            "validation": {"ok": False, "hard_passed": 0, "hard_total": 1, "soft_passed": 0, "soft_total": 0, "behavior": {"trace": {"trace_source": "text"}}},
            "setup": {"setup": {"ok": True, "elapsed_ms": 10, "command_count": 0}},
        }
    ]
    write_reports(tmp_path, records)
    assert (tmp_path / "fairness_warnings.md").exists()
    assert (tmp_path / "failed_runs.md").exists()
    assert (tmp_path / "mode_summary.md").exists()
    assert "Forced prompt" in (tmp_path / "fairness_warnings.md").read_text()
    assert "agent_failed" in (tmp_path / "failed_runs.md").read_text()


def test_explain_run_markdown_includes_diagnostics(tmp_path: Path):
    run_dir = tmp_path / "run"
    run_dir.mkdir()
    (run_dir / "summary.json").write_text("{}")
    record = {
        "mode": "mode-a",
        "case": "case-a",
        "prompt_style": "neutral",
        "repetition": 1,
        "run_status": "validation_failed",
        "run_dir": str(run_dir),
        "agent_summary": {"backend": "codex", "exit_code": 0, "usage": {}},
        "validation": {"ok": False, "hard_passed": 0, "hard_total": 1, "results": [{"ok": False, "hard": True, "message": "missing symbol"}]},
        "setup": {"setup": {"ok": True, "elapsed_ms": 1, "command_count": 0}},
    }
    md = explain_run_markdown(record)
    assert "Run Diagnostic" in md
    assert "missing symbol" in md
    assert "summary.json" in md


def test_compare_result_sets_markdown():
    md = compare_result_sets([
        ("a", [{"run_status": "completed", "agent_summary": {"usage": {}, "elapsed_ms": 1}, "validation": {"ok": True}, "setup": {"setup": {"ok": True}}}]),
        ("b", []),
    ])
    assert "Result Set Comparison" in md
    assert "a" in md
    assert "b" in md
