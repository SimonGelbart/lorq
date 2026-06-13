from __future__ import annotations

import os
import platform
import sys
from pathlib import Path
from typing import Any

from . import __version__
from .contracts import RUN_MANIFEST_SCHEMA_VERSION, with_schema
from .utils import run_command, write_json, write_text


def _cmd_version(argv: list[str], cwd: Path) -> dict[str, Any]:
    result = run_command(argv, cwd=cwd, timeout_seconds=20, shell=False)
    text = (result.get("stdout") or result.get("stderr") or "").strip().splitlines()
    return {
        "command": argv,
        "available": result.get("exit_code") == 0,
        "exit_code": result.get("exit_code"),
        "version": text[0] if text else "",
    }


def capture_environment(cwd: Path) -> dict[str, Any]:
    """Capture lightweight reproducibility metadata for a run."""
    commands = {
        "git": ["git", "--version"],
        "codex": ["codex", "--version"],
        "copilot": ["copilot", "--version"],
        "graphify": ["graphify", "--version"],
    }
    return {
        "schema_version": "agent-eval.environment.v1",
        "contract_version": "agent-eval.contract.v1",
        "suite_version": __version__,
        "python": sys.version.replace("\n", " "),
        "platform": platform.platform(),
        "executable": sys.executable,
        "cwd": str(cwd),
        "env_present": {
            key: bool(os.environ.get(key))
            for key in ("OPENAI_API_KEY", "GITHUB_TOKEN", "ANTHROPIC_API_KEY")
        },
        "commands": {name: _cmd_version(argv, cwd) for name, argv in commands.items()},
    }


def write_environment_files(output_dir: Path, cwd: Path) -> dict[str, Any]:
    metadata = capture_environment(cwd)
    write_json(output_dir / "environment.json", metadata)
    lines = [
        f"suite_version: {metadata['suite_version']}",
        f"python: {metadata['python']}",
        f"platform: {metadata['platform']}",
        "",
        "commands:",
    ]
    for name, info in metadata["commands"].items():
        status = "available" if info.get("available") else "missing/error"
        lines.append(f"- {name}: {status} {info.get('version', '')}".rstrip())
    write_text(output_dir / "environment.txt", "\n".join(lines) + "\n")
    return metadata


def write_run_snapshots(
    output_dir: Path,
    *,
    mode: dict[str, Any],
    case: dict[str, Any],
    repo_id: str,
    repo: dict[str, Any],
    repo_status: dict[str, Any],
    prompt_style: str,
    prompt_template: str,
    agent_profile_id: str,
    agent_profile: dict[str, Any],
    repetition: int,
    worktree: Path | None = None,
) -> dict[str, Any]:
    """Store exact inputs that define a run so later report-only analysis is reproducible."""
    snapshots_dir = output_dir / "snapshots"
    snapshots_dir.mkdir(parents=True, exist_ok=True)
    write_json(snapshots_dir / "mode.json", mode)
    write_json(snapshots_dir / "case.json", case)
    write_json(snapshots_dir / "repo.json", {"id": repo_id, "config": repo, "status": repo_status})
    write_json(snapshots_dir / "agent_profile.json", {"id": agent_profile_id, "profile": agent_profile})
    write_text(snapshots_dir / "prompt_style.txt", prompt_template)
    manifest = with_schema({
        "suite_version": __version__,
        "repo": repo_id,
        "repo_status": repo_status,
        "mode": mode.get("id"),
        "case": case.get("id"),
        "prompt_style": prompt_style,
        "agent_profile": agent_profile_id,
        "repetition": repetition,
        "worktree": str(worktree) if worktree else None,
    }, RUN_MANIFEST_SCHEMA_VERSION)
    write_json(output_dir / "run.manifest.json", manifest)
    return manifest
