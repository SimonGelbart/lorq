from __future__ import annotations

import shutil
import time
from pathlib import Path
from typing import Any

from . import __version__
from .utils import ensure_dir, read_json, rm_rf, run_command, write_json

MARKER_FILENAME = ".agent-eval-generated.json"


def is_relative_to(path: Path, root: Path) -> bool:
    try:
        path.resolve().relative_to(root.resolve())
        return True
    except ValueError:
        return False


def require_inside(path: Path, root: Path, label: str = "path") -> None:
    if not is_relative_to(path, root):
        raise ValueError(f"Refusing to clean {label} outside configured root: {path} not under {root}")


def marker_path(path: Path) -> Path:
    return path / MARKER_FILENAME


def has_generated_marker(path: Path) -> bool:
    return marker_path(path).exists()


def read_generated_marker(path: Path) -> dict[str, Any] | None:
    return read_json(marker_path(path), default=None)


def write_generated_marker(path: Path, *, kind: str, metadata: dict[str, Any] | None = None) -> dict[str, Any]:
    ensure_dir(path)
    payload = {
        "generated_by": "generic-agent-eval-runner",
        "suite_version": __version__,
        "kind": kind,
        "created_at_unix": int(time.time()),
        "metadata": metadata or {},
    }
    write_json(marker_path(path), payload)
    return payload


def require_generated_marker(path: Path, label: str = "path") -> None:
    if not has_generated_marker(path):
        raise ValueError(f"Refusing to remove unmarked {label}: {path} has no {MARKER_FILENAME}")


def _looks_like_git_worktree(path: Path) -> bool:
    return (path / ".git").exists()


def remove_git_worktree(path: Path) -> dict[str, Any]:
    """Best-effort removal of a Git worktree without assuming the source repo path."""
    record: dict[str, Any] = {
        "path": str(path),
        "attempted": False,
        "removed": False,
        "exit_code": None,
        "stderr": "",
    }
    if not path.exists() or not _looks_like_git_worktree(path):
        return record

    record["attempted"] = True
    result = run_command(["git", "-C", str(path), "worktree", "remove", "--force", str(path)], cwd=path.parent, timeout_seconds=120, shell=False)
    record.update({"exit_code": result["exit_code"], "stderr": result["stderr"]})
    if result["exit_code"] == 0 and not path.exists():
        record["removed"] = True
        return record

    common = run_command(["git", "-C", str(path), "rev-parse", "--git-common-dir"], cwd=path.parent, timeout_seconds=60, shell=False)
    if common["exit_code"] == 0 and common["stdout"].strip():
        common_dir = Path(common["stdout"].strip())
        if not common_dir.is_absolute():
            common_dir = (path / common_dir).resolve()
        result2 = run_command(["git", f"--git-dir={common_dir}", "worktree", "remove", "--force", str(path)], cwd=path.parent, timeout_seconds=120, shell=False)
        record.update({"exit_code": result2["exit_code"], "stderr": result2["stderr"]})
        if result2["exit_code"] == 0 and not path.exists():
            record["removed"] = True
            return record

    return record


def remove_execution_path(
    path: Path,
    *,
    allowed_root: Path | None = None,
    allow_missing: bool = True,
    require_marker: bool = True,
) -> dict[str, Any]:
    """Safely remove a generated execution folder.

    Destructive removal requires both: path inside allowed_root when provided,
    and a generated marker file by default. The marker requirement can be
    disabled only for controlled internal cleanup of pre-existing stale paths.
    """
    path = path.resolve()
    if allowed_root is not None:
        require_inside(path, allowed_root.resolve(), "execution path")
    record: dict[str, Any] = {
        "path": str(path),
        "existed": path.exists(),
        "marker": read_generated_marker(path),
        "git_worktree": None,
        "removed": False,
        "skipped": False,
    }
    if not path.exists():
        if allow_missing:
            record["removed"] = True
            return record
        raise FileNotFoundError(path)
    if require_marker and not has_generated_marker(path):
        record["skipped"] = True
        record["reason"] = f"missing {MARKER_FILENAME}"
        raise ValueError(f"Refusing to remove unmarked generated path: {path}")

    git_record = remove_git_worktree(path)
    record["git_worktree"] = git_record
    if path.exists():
        rm_rf(path)
    record["removed"] = not path.exists()
    return record


def prune_git_worktrees(repo: dict[str, Any]) -> dict[str, Any]:
    if repo.get("type", "local") != "local" or not repo.get("path"):
        return {"attempted": False, "reason": "repository is not a local repo"}
    source = Path(str(repo["path"])).expanduser().resolve()
    if not (source / ".git").exists():
        return {"attempted": False, "reason": "repository has no .git directory"}
    result = run_command(["git", "-C", str(source), "worktree", "prune"], cwd=source, timeout_seconds=120, shell=False)
    return {
        "attempted": True,
        "repo": str(source),
        "exit_code": result["exit_code"],
        "elapsed_ms": result["elapsed_ms"],
        "stderr": result["stderr"],
    }


def list_generated_worktrees(worktree_root: Path) -> list[dict[str, Any]]:
    root = worktree_root.expanduser().resolve()
    if not root.exists():
        return []
    entries: list[dict[str, Any]] = []
    for child in sorted(root.iterdir()):
        if not child.is_dir():
            continue
        entries.append({
            "name": child.name,
            "path": str(child.resolve()),
            "is_git_worktree": _looks_like_git_worktree(child),
            "has_marker": has_generated_marker(child),
            "marker": read_generated_marker(child),
        })
    return entries


def clean_worktree_root(worktree_root: Path, *, yes: bool = False) -> dict[str, Any]:
    root = worktree_root.expanduser().resolve()
    if not yes:
        raise ValueError("Refusing to clean worktree root without --yes")
    ensure_dir(root)
    write_generated_marker(root, kind="worktree-root")
    records = []
    skipped = []
    for entry in list(root.iterdir()):
        if entry.name == MARKER_FILENAME:
            continue
        if not entry.is_dir():
            skipped.append({"path": str(entry), "reason": "not a directory"})
            continue
        if not has_generated_marker(entry):
            skipped.append({"path": str(entry), "reason": f"missing {MARKER_FILENAME}"})
            continue
        records.append(remove_execution_path(entry, allowed_root=root, require_marker=True))
    return {"root": str(root), "removed": records, "skipped": skipped}


def clean_results_root(results_root: Path, *, yes: bool = False) -> dict[str, Any]:
    root = results_root.expanduser().resolve()
    if not yes:
        raise ValueError("Refusing to clean results root without --yes")
    if root.exists() and any(root.iterdir()) and not has_generated_marker(root):
        raise ValueError(f"Refusing to clean unmarked non-empty results root: {root} has no {MARKER_FILENAME}")
    if root.exists():
        shutil.rmtree(root)
    ensure_dir(root)
    marker = write_generated_marker(root, kind="results-root")
    return {"root": str(root), "removed": True, "marker": marker}


def load_run_records(results_root: Path) -> list[dict[str, Any]]:
    summary = read_json(results_root / "summary.json", default={}) or {}
    runs = summary.get("runs") if isinstance(summary, dict) else None
    if isinstance(runs, list):
        return runs

    # Fallback for partially generated result folders or manually copied run artifacts.
    # Only collect summary.json files below runs/ so result-root summary.json is not duplicated.
    runs_root = results_root / "runs"
    records: list[dict[str, Any]] = []
    if runs_root.exists():
        for path in sorted(runs_root.rglob("summary.json")):
            record = read_json(path, default={}) or {}
            if isinstance(record, dict) and "mode" in record and "case" in record:
                records.append(record)
    return records


def write_lifecycle_event(results_root: Path, name: str, payload: dict[str, Any]) -> None:
    ensure_dir(results_root / "lifecycle")
    write_json(results_root / "lifecycle" / f"{name}.json", payload)
