from pathlib import Path

import pytest

from eval_runner.schema import SchemaError, validate_mode
from eval_runner.utils import redact_data, redact_secrets, write_json, write_text
from eval_runner.worktrees import materialize_mode, run_pre_agent_commands


def test_mode_schema_rejects_unknown_keys_and_agent_prompt_style():
    with pytest.raises(SchemaError) as exc:
        validate_mode({"id": "bad", "agent": {"prompt_style": "neutral"}}, "bad.yaml")
    assert "unknown field" in str(exc.value)
    assert "$.agent" in str(exc.value)


def test_materialize_rejects_destination_escape(tmp_path: Path):
    suite = tmp_path / "suite"
    worktree = tmp_path / "worktree"
    (suite / "execution").mkdir(parents=True)
    (worktree).mkdir()
    (suite / "execution" / "file.txt").write_text("ok", encoding="utf-8")
    mode = {"id": "bad", "materialize": {"copy": [{"from": "execution/file.txt", "to": "../escape.txt"}]}}
    with pytest.raises(ValueError, match="escapes allowed root"):
        materialize_mode(suite, worktree, mode)


def test_pre_agent_rejects_cwd_escape(tmp_path: Path):
    worktree = tmp_path / "worktree"
    worktree.mkdir()
    mode = {
        "id": "bad",
        "pre_agent": {
            "commands": [
                {"id": "escape", "argv": ["echo", "x"], "cwd": "..", "required": True}
            ]
        },
    }
    with pytest.raises(ValueError, match="escapes allowed root"):
        run_pre_agent_commands(worktree, mode, tmp_path / "logs")


def test_setup_stops_after_required_failure(tmp_path: Path):
    worktree = tmp_path / "worktree"
    worktree.mkdir()
    mode = {
        "id": "stop",
        "pre_agent": {
            "commands": [
                {"id": "fail", "argv": ["python", "-c", "import sys; sys.exit(4)"], "required": True},
                {"id": "should-not-run", "argv": ["python", "-c", "print('ran')"], "required": True},
            ]
        },
    }
    summary = run_pre_agent_commands(worktree, mode, tmp_path / "logs")
    assert summary["ok"] is False
    assert summary["failed_required_command"] == "fail"
    assert [cmd["id"] for cmd in summary["commands"]] == ["fail"]


def test_setup_can_continue_after_required_failure_when_requested(tmp_path: Path):
    worktree = tmp_path / "worktree"
    worktree.mkdir()
    mode = {
        "id": "continue",
        "pre_agent": {
            "commands": [
                {"id": "fail", "argv": ["python", "-c", "import sys; sys.exit(4)"], "required": True, "continue_on_failure": True},
                {"id": "runs", "argv": ["python", "-c", "print('ran')"], "required": True},
            ]
        },
    }
    summary = run_pre_agent_commands(worktree, mode, tmp_path / "logs")
    assert summary["ok"] is False
    assert [cmd["id"] for cmd in summary["commands"]] == ["fail", "runs"]


def test_secret_redaction_in_text_and_json(tmp_path: Path, monkeypatch: pytest.MonkeyPatch):
    monkeypatch.setenv("MY_API_KEY", "supersecretvalue")
    assert "supersecretvalue" not in redact_secrets("token=supersecretvalue")
    payload = redact_data({"GITHUB_TOKEN": "supersecretvalue", "nested": {"text": "abc supersecretvalue xyz"}})
    assert payload["GITHUB_TOKEN"] == "[REDACTED]"
    assert "supersecretvalue" not in payload["nested"]["text"]
    write_text(tmp_path / "out.txt", "secret=supersecretvalue")
    write_json(tmp_path / "out.json", {"secret": "supersecretvalue"})
    assert "supersecretvalue" not in (tmp_path / "out.txt").read_text(encoding="utf-8")
    assert "supersecretvalue" not in (tmp_path / "out.json").read_text(encoding="utf-8")
