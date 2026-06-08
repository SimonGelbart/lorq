from __future__ import annotations

import sys
from pathlib import Path

from eval_runner.agents import CliAgent, check_agent_availability, create_agent
from eval_runner.cli import main


def test_cli_agent_availability_finds_python():
    agent = CliAgent(
        backend_id="python-test",
        command=sys.executable,
        args=["-c", "print('agent')"],
        output_format="text",
        availability_args=["--version"],
    )
    check = agent.check_availability()
    assert check["ok"] is True
    assert check["available"] is True
    assert check["resolved_path"]
    assert check["version_check"]["ok"] is True


def test_cli_agent_availability_reports_missing_command():
    agent = CliAgent(
        backend_id="missing-test",
        command="definitely-not-installed-agent-eval-test",
        args=[],
        output_format="text",
    )
    check = agent.check_availability()
    assert check["ok"] is False
    assert check["available"] is False
    assert check["error_category"] == "command_not_found"


def test_cli_agent_summary_records_nonzero_failure(tmp_path: Path):
    agent = CliAgent(
        backend_id="failing-test",
        command=sys.executable,
        args=["-c", "import sys; print('bad'); sys.exit(3)"],
        input_mode="stdin",
        output_format="text",
        availability_args=["--version"],
    )
    summary = agent.run(tmp_path, "prompt", tmp_path / "run")
    assert summary["ok"] is False
    assert summary["error_category"] == "nonzero_exit"
    assert summary["exit_code"] == 3


def test_copilot_sdk_availability_is_cheap_and_structured():
    agent = create_agent({"backend": "copilot-sdk", "model": "gpt-5"})
    check = agent.check_availability()
    assert check["backend"] == "github-copilot-sdk"
    assert check["package"] == "github-copilot-sdk"
    assert "ok" in check
    if not check["ok"]:
        assert check["error_category"] == "missing_python_package"


def test_check_agent_cli_selected_profile_for_missing_command():
    code = main([
        "--suite-root", ".",
        "--agent-backend", "generic",
        "--agent-command", "definitely-not-installed-agent-eval-test",
        "--check-agent",
    ])
    assert code == 2
