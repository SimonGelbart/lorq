from pathlib import Path

from eval_runner.cli import _mode_setup_scope
from eval_runner.worktrees import run_pre_agent_commands


def test_per_case_is_alias_for_per_run():
    assert _mode_setup_scope({"pre_agent": {"setup_scope": "per-case"}}, None) == "per-run"
    assert _mode_setup_scope({"pre_agent": {"setup_scope": "per-run"}}, "per-case") == "per-run"


def test_default_setup_scope_is_per_run():
    assert _mode_setup_scope({}, None) == "per-run"


def test_pre_agent_command_supports_argv_without_shell(tmp_path: Path):
    mode = {
        "pre_agent": {
            "commands": [
                {
                    "id": "write-file",
                    "argv": ["python", "-c", "from pathlib import Path; Path('ok.txt').write_text('ok')"],
                    "cwd": ".",
                    "required": True,
                }
            ]
        }
    }
    summary = run_pre_agent_commands(tmp_path, mode, tmp_path / "logs")
    assert summary["ok"] is True
    assert (tmp_path / "ok.txt").read_text() == "ok"
    assert summary["commands"][0]["shell"] is False


def test_pre_agent_run_string_is_split_without_shell(tmp_path: Path):
    mode = {
        "pre_agent": {
            "commands": [
                {
                    "id": "python-version",
                    "run": "python --version",
                    "cwd": ".",
                    "required": True,
                }
            ]
        }
    }
    summary = run_pre_agent_commands(tmp_path, mode, tmp_path / "logs")
    assert summary["ok"] is True
    assert summary["commands"][0]["shell"] is False


def test_run_command_missing_executable_returns_127(tmp_path: Path):
    from eval_runner.utils import run_command
    result = run_command(["definitely-not-a-real-agent-eval-command"], cwd=tmp_path, shell=False)
    assert result["exit_code"] == 127
    assert result["timed_out"] is False
