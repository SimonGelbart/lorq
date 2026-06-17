from __future__ import annotations

from pathlib import Path

from eval_runner.lorq_package import cell_id_for_record, export_lorq_run_shard
from eval_runner.utils import read_json, write_json, write_text


def _write_run(results_root: Path, *, status: str = "completed", answer: str = "Answer cites src/demo.py\n") -> dict:
    run_dir = results_root / "runs" / "mode-a" / "neutral" / "case-a" / "r1"
    write_text(run_dir / "prompt.txt", "Explain the demo.")
    write_text(run_dir / "answer.md", answer)
    write_json(run_dir / "agent.summary.json", {
        "agent": "fake-deterministic",
        "backend": "fake-deterministic",
        "output_format": "text",
        "input_mode": "stdin",
        "exit_code": 0,
        "timed_out": False,
        "ok": True,
        "error_category": None,
        "elapsed_ms": 12,
        "usage": {"input_tokens": 10, "output_tokens": 5, "total_tokens": 15},
        "counts": {"json_events": 0, "tool_events": 1, "command_events": 1},
    })
    write_json(run_dir / "validation.json", {
        "ok": True,
        "hard_passed": 1,
        "hard_total": 1,
        "soft_passed": 0,
        "soft_total": 0,
    })
    write_json(run_dir / "events.summary.json", {
        "event_count": 1,
        "command_count": 1,
    })
    write_text(run_dir / "events.normalized.jsonl", '{"event_type":"tool_call","command":"cat src/demo.py"}\n')
    record = {
        "schema_version": "agent-eval.result.v1",
        "run_status": status,
        "repo": "demo",
        "repo_status": {"type": "local", "ref": "HEAD", "commit": "abc123", "dirty": False, "is_git_repo": True},
        "mode": "mode-a",
        "prompt_style": "neutral",
        "case": "case-a",
        "category": "demo",
        "repetition": 1,
        "run_dir": str(run_dir),
        "setup": {"setup": {"ok": True, "elapsed_ms": 3}},
        "agent_summary": read_json(run_dir / "agent.summary.json"),
        "validation": read_json(run_dir / "validation.json"),
    }
    write_json(run_dir / "summary.json", record)
    write_json(results_root / "summary.json", {"schema_version": "agent-eval.summary.v1", "runs": [record]})
    return record


def test_export_lorq_run_shard_writes_public_package_and_cell_index(tmp_path: Path):
    results_root = tmp_path / "results"
    record = _write_run(results_root)
    package_root = tmp_path / "package"

    result = export_lorq_run_shard(results_root, package_root, shard_id="shard-001", package_id="experiment-001")

    cell_id = cell_id_for_record(record)
    assert result["ok"] is True
    assert result["cell_ids"] == [cell_id]
    assert (package_root / "experiment.yaml").exists()
    assert (package_root / "runs" / "shard-001" / "cells" / cell_id / "answer.md").read_text() == "Answer cites src/demo.py\n"

    public_cell = read_json(package_root / "runs" / "shard-001" / "cells" / cell_id / "cell_result.json")
    indexed_cell = read_json(package_root / ".lorq" / "cells" / f"{cell_id}.json")
    assert public_cell == indexed_cell
    assert public_cell["schema_version"] == "lorq.cell-evidence.v1alpha1"
    assert public_cell["cell_id"] == "case-a__mode-a__attempt-001"
    assert public_cell["status"] == "completed"
    assert public_cell["evidence_refs"]["final_answer"] == f"runs/shard-001/cells/{cell_id}/answer.md"

    adapter_output = public_cell["adapter_output"]
    assert adapter_output["final_answer_present"] is True
    assert adapter_output["usage"]["total_tokens"] == 15
    assert adapter_output["trace"]["summary"]["command_count"] == 1
    assert {item["kind"] for item in adapter_output["artifacts"]} >= {"answer.md", "prompt.txt", "validation.json", "events.normalized.jsonl"}

    coverage = read_json(package_root / ".lorq" / "coverage.json")
    assert coverage["cell_count"] == 1
    assert coverage["status_counts"] == {"completed": 1}

    integrity = read_json(package_root / ".lorq" / "integrity.json")
    assert integrity["ok"] is True
    assert integrity["warnings"] == []


def test_export_lorq_run_shard_marks_missing_final_answer_as_integrity_warning(tmp_path: Path):
    results_root = tmp_path / "results"
    record = _write_run(results_root, answer="")
    package_root = tmp_path / "package"

    export_lorq_run_shard(results_root, package_root, shard_id="shard-002")

    cell_id = cell_id_for_record(record)
    public_cell = read_json(package_root / "runs" / "shard-002" / "cells" / cell_id / "cell_result.json")
    assert public_cell["status"] == "no_final_answer"
    assert public_cell["adapter_output"]["final_answer_present"] is False

    integrity = read_json(package_root / ".lorq" / "integrity.json")
    assert integrity["ok"] is True
    assert integrity["warnings"] == [
        {"type": "missing_final_answer", "cell_id": cell_id, "severity": "warning"},
        {"type": "non_completed_cell", "cell_id": cell_id, "status": "no_final_answer", "severity": "warning"},
    ]
