from __future__ import annotations

import json
from pathlib import Path

import yaml

from eval_runner.agents import DeterministicFakeAgent, create_agent
from eval_runner.judge import DeterministicFakeJudge
from eval_runner.utils import write_json, write_text


def _write_snapshots(run_dir: Path, *, case_id: str = "case-a", mode_id: str = "mode-a", repetition: int = 1) -> None:
    snapshots = run_dir / "snapshots"
    snapshots.mkdir(parents=True)
    write_json(snapshots / "case.json", {"id": case_id, "title": "Case A", "task": "Explain fixture behavior."})
    write_json(snapshots / "mode.json", {"id": mode_id, "description": "Mode A"})
    write_json(run_dir / "run.manifest.json", {"case": case_id, "mode": mode_id, "repetition": repetition})


def test_create_agent_supports_deterministic_fake_backend(tmp_path: Path):
    fixture = tmp_path / "agent.yaml"
    fixture.write_text("schema_version: lorq.fake-agent-fixture.v1alpha1\ncells: []\n", encoding="utf-8")
    agent = create_agent({"backend": "deterministic-fake", "fixture_file": str(fixture)})
    assert isinstance(agent, DeterministicFakeAgent)
    assert agent.check_availability()["ok"] is True


def test_deterministic_fake_agent_writes_full_evidence_files(tmp_path: Path):
    fixture = tmp_path / "agent.yaml"
    fixture.write_text(
        yaml.safe_dump(
            {
                "schema_version": "lorq.fake-agent-fixture.v1alpha1",
                "cells": [
                    {
                        "case": "case-a",
                        "mode": "mode-a",
                        "attempt": 1,
                        "status": "completed",
                        "final_answer": "LedgerWriter records ShardManifest evidence.",
                        "elapsed_ms": 42,
                        "usage": {"input_tokens": 10, "output_tokens": 5},
                        "events": [
                            {"type": "tool.command", "command": "inspect fixtures"},
                            {"type": "assistant.message", "text": "done"},
                        ],
                        "artifacts": [{"path": "trace/events.jsonl", "kind": "trace"}],
                        "integrity_warnings": ["synthetic warning"],
                    }
                ],
            },
            sort_keys=False,
        ),
        encoding="utf-8",
    )
    run_dir = tmp_path / "run"
    run_dir.mkdir()
    _write_snapshots(run_dir)

    summary = DeterministicFakeAgent(str(fixture)).run(tmp_path, "prompt", run_dir)

    assert summary["backend"] == "deterministic-fake"
    assert summary["final_answer_present"] is True
    assert summary["usage"]["total_tokens"] == 15
    assert (run_dir / "answer.md").read_text(encoding="utf-8") == "LedgerWriter records ShardManifest evidence.\n"
    assert (run_dir / "adapter.evidence.json").exists()
    normalized = (run_dir / "events.normalized.jsonl").read_text(encoding="utf-8").strip().splitlines()
    assert len(normalized) == 2
    assert json.loads(normalized[0])["source"] == "deterministic-fake-agent"


def test_deterministic_fake_agent_can_model_missing_final_answer(tmp_path: Path):
    fixture = tmp_path / "agent.yaml"
    fixture.write_text(
        yaml.safe_dump(
            {
                "schema_version": "lorq.fake-agent-fixture.v1alpha1",
                "cells": [
                    {
                        "case": "case-a",
                        "mode": "mode-a",
                        "attempt": 1,
                        "status": "no_final_answer",
                        "final_answer": "",
                        "elapsed_ms": 13,
                    }
                ],
            }
        ),
        encoding="utf-8",
    )
    run_dir = tmp_path / "run"
    run_dir.mkdir()
    _write_snapshots(run_dir)

    summary = DeterministicFakeAgent(str(fixture)).run(tmp_path, "prompt", run_dir)

    assert summary["error_category"] == "no_final_answer"
    assert summary["final_answer_present"] is False
    assert (run_dir / "answer.md").read_text(encoding="utf-8") == ""


def test_deterministic_fake_judge_uses_cell_fixture(tmp_path: Path):
    fixture = tmp_path / "judge.yaml"
    fixture.write_text(
        yaml.safe_dump(
            {
                "schema_version": "lorq.fake-judge-fixture.v1alpha1",
                "judgements": [
                    {
                        "case": "case-a",
                        "mode": "mode-a",
                        "attempt": 1,
                        "ok": True,
                        "overall_score": 4,
                        "confidence": "high",
                        "dimensions": {"correctness": {"score": 4, "rationale": "fixture"}},
                        "summary": "deterministic judgement",
                    }
                ],
            },
            sort_keys=False,
        ),
        encoding="utf-8",
    )
    run_dir = tmp_path / "run"
    run_dir.mkdir()
    _write_snapshots(run_dir)
    write_text(run_dir / "prompt.txt", "prompt")
    write_text(run_dir / "answer.md", "answer")

    result = DeterministicFakeJudge(str(fixture)).run(
        worktree=tmp_path,
        output_dir=run_dir,
        case={"id": "case-a", "title": "Case A", "task": "Task"},
        mode={"id": "mode-a"},
        rubric={"id": "rubric"},
        validation={"ok": True},
    )

    assert result["judge_adapter"] == "deterministic-fake"
    assert result["overall_score"] == 4.0
    assert result["fixture"]["case"] == "case-a"
    assert (run_dir / "judge.json").exists()
