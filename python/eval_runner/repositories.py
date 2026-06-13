from __future__ import annotations

from pathlib import Path
from typing import Any

from .config import load_yaml
from .schema import validate_repositories_file, validate_repositories_map
from .utils import run_command


def load_repositories(suite_root: Path, config: dict[str, Any]) -> dict[str, dict[str, Any]]:
    repos = dict(config.get("repositories") or {})
    repo_file = suite_root / "repositories" / "repositories.yaml"
    if repo_file.exists():
        file_data = load_yaml(repo_file)
        validate_repositories_file(file_data, repo_file)
        file_repos = file_data.get("repositories", file_data)
        if file_repos is None:
            file_repos = {}
        repos.update(file_repos)
    # Validate the merged repository map too, so inline config and file config share rules.
    validate_repositories_map(repos, "merged repositories")
    return repos


def resolve_repository(repo_arg: str | None, repos: dict[str, dict[str, Any]]) -> tuple[str, dict[str, Any]]:
    if repo_arg:
        if repo_arg in repos:
            return repo_arg, repos[repo_arg]
        path = Path(repo_arg).expanduser().resolve()
        if path.exists():
            return path.name or "local-repo", {"type": "local", "path": str(path), "ref": "HEAD"}
        raise ValueError(f"Unknown repository id or path: {repo_arg}")

    if len(repos) == 1:
        key = next(iter(repos))
        return key, repos[key]
    if "default" in repos:
        return "default", repos["default"]
    raise ValueError("Please provide --repo when more than one repository is configured, or no default exists.")


def inspect_repository(repo: dict[str, Any]) -> dict[str, Any]:
    """Return best-effort reproducibility metadata for the source repository."""
    repo_type = repo.get("type", "local")
    status: dict[str, Any] = {"type": repo_type, "ok": True}
    if repo_type == "local" and repo.get("path"):
        source = Path(str(repo["path"])).expanduser().resolve()
        status.update({"path": str(source), "ref": repo.get("ref") or "HEAD"})
        is_repo = run_command(["git", "-C", str(source), "rev-parse", "--is-inside-work-tree"], cwd=source, shell=False, timeout_seconds=30)
        status["is_git_repo"] = is_repo.get("exit_code") == 0 and is_repo.get("stdout", "").strip() == "true"
        if status["is_git_repo"]:
            head = run_command(["git", "-C", str(source), "rev-parse", "HEAD"], cwd=source, shell=False, timeout_seconds=30)
            branch = run_command(["git", "-C", str(source), "branch", "--show-current"], cwd=source, shell=False, timeout_seconds=30)
            porcelain = run_command(["git", "-C", str(source), "status", "--porcelain"], cwd=source, shell=False, timeout_seconds=30)
            status.update({
                "commit": head.get("stdout", "").strip() if head.get("exit_code") == 0 else None,
                "branch": branch.get("stdout", "").strip() if branch.get("exit_code") == 0 else None,
                "dirty": bool(porcelain.get("stdout", "").strip()) if porcelain.get("exit_code") == 0 else None,
                "dirty_entries": porcelain.get("stdout", "").splitlines() if porcelain.get("exit_code") == 0 else [],
            })
        return status
    if repo_type == "git":
        status.update({"url": repo.get("url"), "ref": repo.get("ref")})
        return status
    status["ok"] = False
    status["error"] = f"Unsupported repository type for inspection: {repo_type}"
    return status
