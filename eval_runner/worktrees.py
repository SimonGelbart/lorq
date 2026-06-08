from __future__ import annotations

import shlex
import shutil
import time
from pathlib import Path
from typing import Any

from .lifecycle import remove_execution_path, write_generated_marker
from .utils import copy_path, ensure_dir, ensure_relative_inside, is_git_repo, run_command, write_json, write_text


class WorktreeManager:
    def __init__(self, suite_root: Path, worktree_root: Path, strategy: str = "git-worktree") -> None:
        self.suite_root = suite_root
        self.worktree_root = ensure_dir(worktree_root)
        write_generated_marker(self.worktree_root, kind="worktree-root")
        self.strategy = strategy

    def create_from_repo(self, repo: dict[str, Any], dest: Path, *, ref: str | None = None, allowed_root: Path | None = None) -> dict[str, Any]:
        repo_type = repo.get("type", "local")
        ref = ref or repo.get("ref") or "HEAD"
        remove_execution_path(dest, allowed_root=(allowed_root or self.worktree_root), require_marker=True)
        ensure_dir(dest.parent)

        if repo_type == "local":
            source = Path(str(repo["path"])).expanduser().resolve()
            if self.strategy == "git-worktree" and is_git_repo(source):
                record = self._git_worktree(source, dest, ref)
            else:
                record = self._copy_source(source, dest)
            write_generated_marker(dest, kind="worktree", metadata={"strategy": record.get("strategy"), "source": str(source), "ref": ref})
            return record

        if repo_type == "git":
            url = repo["url"]
            record = self._git_clone(url, dest, ref)
            write_generated_marker(dest, kind="worktree", metadata={"strategy": record.get("strategy"), "source": url, "ref": ref})
            return record

        raise ValueError(f"Unsupported repository type: {repo_type}")

    def _git_worktree(self, source: Path, dest: Path, ref: str) -> dict[str, Any]:
        # Remove stale Git registration if Git knows this path but the folder is gone.
        run_command(["git", "-C", str(source), "worktree", "remove", "--force", str(dest)], cwd=source, shell=False)
        result = run_command(
            ["git", "-C", str(source), "worktree", "add", "--force", "--detach", str(dest), ref],
            cwd=source,
            timeout_seconds=300,
            shell=False,
        )
        if result["exit_code"] != 0:
            raise RuntimeError(f"git worktree add failed: {result['stderr']}")
        return {"strategy": "git-worktree", "source": str(source), "dest": str(dest), "ref": ref, "command": result}

    def _copy_source(self, source: Path, dest: Path) -> dict[str, Any]:
        if not source.exists():
            raise FileNotFoundError(source)
        shutil.copytree(source, dest, ignore=shutil.ignore_patterns(".git"))
        return {"strategy": "copy", "source": str(source), "dest": str(dest)}

    def _git_clone(self, url: str, dest: Path, ref: str) -> dict[str, Any]:
        result = run_command(["git", "clone", "--depth", "1", "--branch", ref, url, str(dest)], cwd=dest.parent, timeout_seconds=900, shell=False)
        if result["exit_code"] != 0:
            remove_execution_path(dest, allowed_root=self.worktree_root, require_marker=False)
            result = run_command(["git", "clone", url, str(dest)], cwd=dest.parent, timeout_seconds=900, shell=False)
            if result["exit_code"] != 0:
                raise RuntimeError(f"git clone failed: {result['stderr']}")
            checkout = run_command(["git", "checkout", ref], cwd=dest, timeout_seconds=300, shell=False)
            if checkout["exit_code"] != 0:
                raise RuntimeError(f"git checkout failed: {checkout['stderr']}")
        return {"strategy": "clone", "source": url, "dest": str(dest), "ref": ref, "command": result}


def materialize_mode(suite_root: Path, worktree: Path, mode: dict[str, Any]) -> list[dict[str, Any]]:
    records: list[dict[str, Any]] = []
    suite_root = suite_root.resolve()
    worktree = worktree.resolve()
    for entry in (mode.get("materialize") or {}).get("copy", []) or []:
        raw_from = Path(str(entry["from"])).expanduser()
        allow_absolute_from = bool(entry.get("allow_absolute_from", False))
        if raw_from.is_absolute():
            if not allow_absolute_from:
                raise ValueError(f"materialize.copy.from is absolute but allow_absolute_from is not true: {raw_from}")
            src = raw_from.resolve()
        else:
            src = (suite_root / raw_from).resolve()
            ensure_relative_inside(suite_root, src, label="materialize.copy.from")
        dst = ensure_relative_inside(worktree, worktree / entry["to"], label="materialize.copy.to")
        copy_path(src, dst, overwrite=entry.get("overwrite", True))
        records.append({"action": "copy", "from": str(src), "to": str(dst)})
    return records


def run_pre_agent_commands(worktree: Path, mode: dict[str, Any], log_dir: Path) -> dict[str, Any]:
    ensure_dir(log_dir)
    commands = (mode.get("pre_agent") or {}).get("commands", []) or []
    started = time.time()
    summary: dict[str, Any] = {
        "commands": [],
        "ok": True,
        "command_count": 0,
        "required_failed": False,
        "failed_required_command": None,
        "elapsed_ms": 0,
    }
    full_log: list[str] = []
    for command in commands:
        command_id = command.get("id") or command.get("name") or f"command-{len(summary['commands'])+1}"
        cwd = ensure_relative_inside(worktree, worktree / command.get("cwd", "."), label=f"pre_agent.commands[{command_id}].cwd")
        timeout = command.get("timeout_seconds")
        required = command.get("required", True)
        env = command.get("env") or {}
        if "argv" in command:
            command_value = [str(part) for part in command["argv"]]
            shell = bool(command.get("shell", False))
            display_command = " ".join(command_value)
        else:
            raw_run = str(command["run"])
            shell = bool(command.get("shell", False))
            command_value = raw_run if shell else shlex.split(raw_run)
            display_command = raw_run
        result = run_command(command_value, cwd=cwd, timeout_seconds=timeout, env=env, shell=shell)
        failed = result["exit_code"] != 0 or bool(result["timed_out"])
        record = {
            "id": command_id,
            "run": display_command,
            "argv": command_value if isinstance(command_value, list) else None,
            "shell": shell,
            "cwd": str(cwd),
            "exit_code": result["exit_code"],
            "timed_out": result["timed_out"],
            "elapsed_ms": result["elapsed_ms"],
            "required": required,
            "failed": failed,
            "stdout_path": f"{command_id}.stdout.txt",
            "stderr_path": f"{command_id}.stderr.txt",
        }
        write_text(log_dir / record["stdout_path"], result["stdout"])
        write_text(log_dir / record["stderr_path"], result["stderr"])
        full_log.append(f"$ {display_command}\n# cwd: {cwd}\n# shell: {shell}\n# exit: {result['exit_code']}\n\nSTDOUT\n{result['stdout']}\n\nSTDERR\n{result['stderr']}\n")
        if required and failed:
            summary["ok"] = False
            summary["required_failed"] = True
            summary["failed_required_command"] = command_id
        summary["commands"].append(record)
        if required and failed and not bool(command.get("continue_on_failure", False)):
            full_log.append(f"# stopping setup after required failure in {command_id}\n")
            break
    summary["command_count"] = len(summary["commands"])
    summary["elapsed_ms"] = int((time.time() - started) * 1000)
    write_text(log_dir / "setup.log", "\n\n---\n\n".join(full_log))
    write_json(log_dir / "setup.summary.json", summary)
    return summary
