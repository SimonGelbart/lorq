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

from eval_runner.lorq_package import LorqPackageError, attach_lorq_deterministic_judgement, cell_id_for_parts, merge_lorq_run_shards, render_lorq_package_report


def _write_lorq_shard(package_root: Path, *, shard_id: str, cells: list[dict]) -> None:
    for cell in cells:
        cell_id = cell["cell_id"]
        cell.setdefault("schema_version", "lorq.cell-evidence.v1alpha1")
        cell.setdefault("contract_version", "lorq.contract.v1alpha1")
        cell.setdefault("shard_id", shard_id)
        cell.setdefault("source", {"implementation": "test"})
        cell.setdefault("fingerprint", {"repo": "demo", "repo_type": "local", "ref": "HEAD", "commit": "abc", "dirty": False, "is_git_repo": True})
        cell.setdefault("adapter_output", {"final_answer_present": True, "usage": {}, "counts": {}, "trace": {}, "validation": {}})
        cell.setdefault("evidence_refs", {"cell_dir": f"runs/{shard_id}/cells/{cell_id}", "cell_result": f"runs/{shard_id}/cells/{cell_id}/cell_result.json"})
        write_json(package_root / ".lorq" / "cells" / f"{cell_id}.json", cell)
        write_json(package_root / "runs" / shard_id / "cells" / cell_id / "cell_result.json", cell)
        if cell.get("adapter_warning"):
            write_json(package_root / "runs" / shard_id / "cells" / cell_id / "adapter.evidence.json", {"integrity_warnings": [cell["adapter_warning"]]})
    write_json(package_root / "runs" / shard_id / "shard.manifest.json", {
        "schema_version": "lorq.run-shard-manifest.v1alpha1",
        "contract_version": "lorq.contract.v1alpha1",
        "shard_id": shard_id,
        "cell_count": len(cells),
        "cell_ids": sorted(cell["cell_id"] for cell in cells),
    })
    write_json(package_root / ".lorq" / "integrity.json", {
        "schema_version": "lorq.integrity.v1alpha1",
        "contract_version": "lorq.contract.v1alpha1",
        "ok": True,
        "warnings": [],
    })


def test_merge_lorq_run_shards_builds_experiment_indexes_and_missing_coverage(tmp_path: Path):
    shard_001 = tmp_path / "shard-001"
    shard_002 = tmp_path / "shard-002"
    baseline_cell = cell_id_for_parts("case-a", "baseline", 1)
    graphify_cell = cell_id_for_parts("case-a", "graphify", 1)
    _write_lorq_shard(shard_001, shard_id="shard-001", cells=[{
        "cell_id": baseline_cell,
        "case_id": "case-a",
        "mode_id": "baseline",
        "attempt_id": "attempt-001",
        "status": "completed",
        "adapter_warning": "preserved adapter warning",
    }])
    _write_lorq_shard(shard_002, shard_id="shard-002", cells=[{
        "cell_id": graphify_cell,
        "case_id": "case-a",
        "mode_id": "graphify",
        "attempt_id": "attempt-001",
        "status": "completed",
    }])
    benchmark = tmp_path / "benchmark.yaml"
    benchmark.write_text(
        "shape:\n  attempts_per_case_mode: 1\ncases:\n  - id: case-a\nmodes:\n  - id: baseline\n  - id: graphify\n  - id: graphify-plus\n",
        encoding="utf-8",
    )

    result = merge_lorq_run_shards([shard_001, shard_002], tmp_path / "experiment", package_id="experiment-001", benchmark_file=benchmark)

    missing = cell_id_for_parts("case-a", "graphify-plus", 1)
    assert result["ok"] is True
    assert result["cell_count"] == 2
    assert result["expected_cell_count"] == 3
    assert result["missing_cell_ids"] == [missing]
    assert (tmp_path / "experiment" / "runs" / "shard-001" / "cells" / baseline_cell / "cell_result.json").exists()
    assert (tmp_path / "experiment" / "runs" / "shard-002" / "cells" / graphify_cell / "cell_result.json").exists()

    coverage = read_json(tmp_path / "experiment" / ".lorq" / "coverage.json")
    assert coverage["missing_cells"] == [missing]
    integrity = read_json(tmp_path / "experiment" / ".lorq" / "integrity.json")
    warning_types = [warning["type"] for warning in integrity["warnings"]]
    assert "missing_expected_cell" in warning_types
    assert "adapter_integrity_warning" in warning_types
    assert integrity["ok"] is True


def test_merge_lorq_run_shards_fails_by_default_on_duplicate_cells(tmp_path: Path):
    cell_id = cell_id_for_parts("case-a", "baseline", 1)
    shard_001 = tmp_path / "shard-001"
    shard_002 = tmp_path / "shard-002"
    cell = {"cell_id": cell_id, "case_id": "case-a", "mode_id": "baseline", "attempt_id": "attempt-001", "status": "completed"}
    _write_lorq_shard(shard_001, shard_id="shard-001", cells=[dict(cell)])
    _write_lorq_shard(shard_002, shard_id="shard-002", cells=[dict(cell)])

    try:
        merge_lorq_run_shards([shard_001, shard_002], tmp_path / "experiment")
    except LorqPackageError as exc:
        assert "Duplicate LORQ cell ids" in str(exc)
    else:  # pragma: no cover - explicit assertion message is clearer.
        raise AssertionError("expected duplicate cell merge failure")


def test_merge_lorq_run_shards_fails_by_default_on_fingerprint_mismatch(tmp_path: Path):
    shard_001 = tmp_path / "shard-001"
    shard_002 = tmp_path / "shard-002"
    _write_lorq_shard(shard_001, shard_id="shard-001", cells=[{
        "cell_id": cell_id_for_parts("case-a", "baseline", 1),
        "case_id": "case-a",
        "mode_id": "baseline",
        "attempt_id": "attempt-001",
        "status": "completed",
        "fingerprint": {"repo": "demo", "repo_type": "local", "ref": "HEAD", "commit": "abc", "dirty": False, "is_git_repo": True},
    }])
    _write_lorq_shard(shard_002, shard_id="shard-002", cells=[{
        "cell_id": cell_id_for_parts("case-a", "graphify", 1),
        "case_id": "case-a",
        "mode_id": "graphify",
        "attempt_id": "attempt-001",
        "status": "completed",
        "fingerprint": {"repo": "demo", "repo_type": "local", "ref": "HEAD", "commit": "def", "dirty": False, "is_git_repo": True},
    }])

    try:
        merge_lorq_run_shards([shard_001, shard_002], tmp_path / "experiment")
    except LorqPackageError as exc:
        assert "incompatible repository fingerprints" in str(exc)
    else:  # pragma: no cover
        raise AssertionError("expected fingerprint mismatch merge failure")



def _write_fake_judge_fixture(path: Path, entries: list[dict]) -> None:
    import yaml

    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(yaml.safe_dump({
        "schema_version": "lorq.fake-judge-fixture.v1alpha1",
        "judgements": entries,
    }, sort_keys=False), encoding="utf-8")


def test_attach_lorq_deterministic_judgement_writes_named_pass(tmp_path: Path):
    package_root = tmp_path / "experiment"
    cell_id = cell_id_for_parts("case-a", "baseline", 1)
    _write_lorq_shard(package_root, shard_id="shard-001", cells=[{
        "cell_id": cell_id,
        "case_id": "case-a",
        "mode_id": "baseline",
        "attempt_id": "attempt-001",
        "status": "completed",
    }])
    write_json(package_root / ".lorq" / "coverage.json", {
        "schema_version": "lorq.coverage.v1alpha1",
        "contract_version": "lorq.contract.v1alpha1",
        "missing_cells": [cell_id_for_parts("case-a", "graphify", 1)],
    })
    fixture = tmp_path / "fake-judge.yaml"
    _write_fake_judge_fixture(fixture, [{
        "case": "case-a",
        "mode": "baseline",
        "attempt": 1,
        "ok": True,
        "overall_score": 4,
        "confidence": "high",
        "dimensions": {"correctness": {"score": 4, "rationale": "fixture"}},
        "summary": "deterministic score",
    }])

    result = attach_lorq_deterministic_judgement(package_root, judge_name="judge-primary", fixture_file=fixture)

    assert result["ok"] is True
    assert result["judged_cell_count"] == 1
    assert result["missing_expected_cell_ids"] == [cell_id_for_parts("case-a", "graphify", 1)]
    assert result["score_summary"]["overall_average"] == 4.0

    cell_judgement = read_json(package_root / "judgements" / "judge-primary" / "cells" / f"{cell_id}.json")
    assert cell_judgement["schema_version"] == "lorq.cell-judgement.v1alpha1"
    assert cell_judgement["quality"]["overall_score"] == 4.0
    assert cell_judgement["source"]["backend"] == "deterministic-fake"
    assert cell_judgement["source"]["real_llm_used"] is False

    manifest = read_json(package_root / "judgements" / "judge-primary" / "judgement.manifest.json")
    indexed_manifest = read_json(package_root / ".lorq" / "judgements" / "judge-primary.json")
    assert manifest == indexed_manifest
    assert manifest["judgement_name"] == "judge-primary"


def test_attach_lorq_deterministic_judgement_fails_on_missing_present_cell_fixture(tmp_path: Path):
    package_root = tmp_path / "experiment"
    cell_id = cell_id_for_parts("case-a", "baseline", 1)
    _write_lorq_shard(package_root, shard_id="shard-001", cells=[{
        "cell_id": cell_id,
        "case_id": "case-a",
        "mode_id": "baseline",
        "attempt_id": "attempt-001",
        "status": "completed",
    }])
    fixture = tmp_path / "fake-judge.yaml"
    _write_fake_judge_fixture(fixture, [{
        "case": "other-case",
        "mode": "baseline",
        "attempt": 1,
        "overall_score": 2,
        "dimensions": {},
    }])

    try:
        attach_lorq_deterministic_judgement(package_root, judge_name="judge-primary", fixture_file=fixture)
    except LorqPackageError as exc:
        assert "Missing deterministic judgement fixture entries" in str(exc)
    else:  # pragma: no cover
        raise AssertionError("expected missing deterministic judgement failure")



def test_render_lorq_package_report_writes_json_markdown_and_case_packs(tmp_path: Path):
    package_root = tmp_path / "experiment"
    baseline_cell = cell_id_for_parts("case-a", "baseline", 1)
    graphify_cell = cell_id_for_parts("case-a", "graphify", 1)
    _write_lorq_shard(package_root, shard_id="shard-001", cells=[
        {
            "cell_id": baseline_cell,
            "case_id": "case-a",
            "mode_id": "baseline",
            "attempt_id": "attempt-001",
            "status": "completed",
            "adapter_output": {"final_answer_present": True, "usage": {}, "counts": {}, "trace": {}, "validation": {}},
        },
        {
            "cell_id": graphify_cell,
            "case_id": "case-a",
            "mode_id": "graphify",
            "attempt_id": "attempt-001",
            "status": "no_final_answer",
            "adapter_output": {"final_answer_present": False, "usage": {}, "counts": {}, "trace": {}, "validation": {}},
        },
    ])
    write_text(package_root / "experiment.yaml", "package_schema_version: 1\npackage_kind: experiment\npackage_id: experiment-001\nshards:\n  - shard-001\ncell_count: 2\n")
    missing = cell_id_for_parts("case-a", "graphify-plus", 1)
    write_json(package_root / ".lorq" / "coverage.json", {
        "schema_version": "lorq.coverage.v1alpha1",
        "contract_version": "lorq.contract.v1alpha1",
        "cell_count": 2,
        "expected_cell_count": 3,
        "missing_cells": [missing],
    })
    write_json(package_root / ".lorq" / "integrity.json", {
        "schema_version": "lorq.integrity.v1alpha1",
        "contract_version": "lorq.contract.v1alpha1",
        "ok": True,
        "warnings": [{"type": "missing_expected_cell", "cell_id": missing, "severity": "warning"}],
    })
    fixture = tmp_path / "fake-judge.yaml"
    _write_fake_judge_fixture(fixture, [
        {"case": "case-a", "mode": "baseline", "attempt": 1, "overall_score": 4, "dimensions": {}},
        {"case": "case-a", "mode": "graphify", "attempt": 1, "overall_score": 2, "dimensions": {}},
    ])
    attach_lorq_deterministic_judgement(package_root, judge_name="judge-primary", fixture_file=fixture)

    result = render_lorq_package_report(package_root, primary_judgement="judge-primary")

    assert result["ok"] is True
    assert result["case_pack_count"] == 1
    report = read_json(package_root / "reports" / "report.json")
    assert report["schema_version"] == "lorq.report.v1alpha1"
    assert report["package"]["package_id"] == "experiment-001"
    assert report["summary"]["missing_expected_cell_ids"] == [missing]
    assert report["summary"]["status_counts"] == {"completed": 1, "no_final_answer": 1}
    assert report["primary_judgement"]["source"]["real_llm_used"] is False
    assert (package_root / "reports" / "report.md").read_text(encoding="utf-8").startswith("# LORQ package report")
    case_pack = read_json(package_root / "reports" / "cases" / "case-a" / "case-review.json")
    assert case_pack["schema_version"] == "lorq.case-review-pack.v1alpha1"
    assert case_pack["missing_expected_cell_ids"] == [missing]
    assert (package_root / ".lorq" / "report.json").exists()


def test_committed_duplicate_cell_edge_fixture_fails_by_default():
    root = Path(__file__).resolve().parents[2]
    edge = root / "fixtures" / "conformance" / "deterministic-orchestration" / "edge-fixtures" / "duplicate-cell-conflict"
    try:
        merge_lorq_run_shards([edge / "shard-a", edge / "shard-b"], edge / "_tmp-should-not-be-created")
    except LorqPackageError as exc:
        assert "Duplicate LORQ cell ids" in str(exc)
    else:  # pragma: no cover
        raise AssertionError("expected committed duplicate-cell fixture to fail")


def test_committed_fingerprint_mismatch_edge_fixture_fails_by_default():
    root = Path(__file__).resolve().parents[2]
    edge = root / "fixtures" / "conformance" / "deterministic-orchestration" / "edge-fixtures" / "fingerprint-mismatch"
    try:
        merge_lorq_run_shards([edge / "shard-a", edge / "shard-b"], edge / "_tmp-should-not-be-created")
    except LorqPackageError as exc:
        assert "incompatible repository fingerprints" in str(exc)
    else:  # pragma: no cover
        raise AssertionError("expected committed fingerprint-mismatch fixture to fail")


def test_frozen_deterministic_golden_outputs_have_full_loop_and_no_local_paths():
    root = Path(__file__).resolve().parents[2]
    golden = root / "fixtures" / "golden" / "deterministic-orchestration"
    expected_missing = "skipped-coverage__graphify-plus__attempt-001"

    report = read_json(golden / "experiment-001" / "reports" / "report.json")
    assert report["schema_version"] == "lorq.report.v1alpha1"
    assert report["package"]["package_id"] == "deterministic-benchmark"
    assert report["summary"]["cell_count"] == 8
    assert report["summary"]["expected_cell_count"] == 9
    assert report["summary"]["missing_expected_cell_ids"] == [expected_missing]
    assert report["primary_judgement"]["source"]["real_llm_used"] is False
    assert (golden / "experiment-001" / "reports" / "report.md").exists()
    for case_id in ["successful-comparison", "no-final-answer", "skipped-coverage"]:
        assert (golden / "experiment-001" / "reports" / "cases" / case_id / "case-review.json").exists()
        assert (golden / "experiment-001" / "reports" / "cases" / case_id / "case-review.md").exists()

    for path in golden.rglob("*"):
        if not path.is_file():
            continue
        text = path.read_text(encoding="utf-8")
        assert "/mnt/data" not in text
        assert "lorq_finish" not in text
        assert "internal/generated" not in text
