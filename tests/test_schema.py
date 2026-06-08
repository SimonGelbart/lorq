from __future__ import annotations

from pathlib import Path

import pytest
import yaml

from eval_runner.cli import main
from eval_runner.config import load_config
from eval_runner.modes import load_modes
from eval_runner.prompts import validate_prompt_styles_dir
from eval_runner.schema import SchemaError, validate_case, validate_mode


def test_validate_mode_rejects_bad_setup_scope():
    with pytest.raises(SchemaError) as exc:
        validate_mode(
            {
                "id": "bad-mode",
                "pre_agent": {"setup_scope": "permode", "commands": []},
            },
            "bad.yaml",
        )
    assert "setup_scope" in str(exc.value)
    assert "permode" in str(exc.value)


def test_validate_mode_requires_command_argv_or_run():
    with pytest.raises(SchemaError) as exc:
        validate_mode(
            {
                "id": "bad-command",
                "pre_agent": {"commands": [{"id": "prep"}]},
            },
            "bad-command.yaml",
        )
    assert "argv" in str(exc.value) or "run" in str(exc.value)


def test_validate_case_requires_task():
    with pytest.raises(SchemaError) as exc:
        validate_case({"id": "case", "title": "Case"}, "case.yaml")
    assert "task" in str(exc.value)


def test_prompt_style_must_include_task_placeholder(tmp_path: Path):
    prompt_dir = tmp_path / "prompt_styles"
    prompt_dir.mkdir()
    (prompt_dir / "bad.txt").write_text("No placeholder here", encoding="utf-8")
    with pytest.raises(SchemaError) as exc:
        validate_prompt_styles_dir(prompt_dir)
    assert "{task}" in str(exc.value)


def test_config_validation_fails_early_for_unknown_agent_profile(tmp_path: Path):
    (tmp_path / "eval.config.yaml").write_text(
        yaml.safe_dump(
            {
                "agent": {"profile": "missing"},
                "agent_profiles": {
                    "codex": {"backend": "codex", "command": "codex", "args": ["exec", "--json"]}
                },
            }
        ),
        encoding="utf-8",
    )
    with pytest.raises(SchemaError) as exc:
        load_config(tmp_path)
    assert "unknown agent profile" in str(exc.value)


def test_load_modes_validates_each_file(tmp_path: Path):
    modes_dir = tmp_path / "modes"
    modes_dir.mkdir()
    (modes_dir / "bad.yaml").write_text("id: bad\npre_agent:\n  setup_scope: wrong\n", encoding="utf-8")
    with pytest.raises(SchemaError):
        load_modes(modes_dir)


def test_cli_validate_config_success_for_packaged_suite():
    assert main(["--suite-root", ".", "--validate-config"]) == 0
