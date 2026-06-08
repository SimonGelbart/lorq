from pathlib import Path

import pytest

from eval_runner.lifecycle import clean_worktree_root, list_generated_worktrees, remove_execution_path, write_generated_marker


def test_remove_execution_path_refuses_outside_root(tmp_path: Path):
    allowed = tmp_path / "allowed"
    allowed.mkdir()
    outside = tmp_path / "outside"
    outside.mkdir()
    write_generated_marker(outside, kind="worktree")
    with pytest.raises(ValueError):
        remove_execution_path(outside, allowed_root=allowed)


def test_remove_execution_path_requires_marker(tmp_path: Path):
    root = tmp_path / "root"
    target = root / "target"
    target.mkdir(parents=True)
    with pytest.raises(ValueError):
        remove_execution_path(target, allowed_root=root)


def test_clean_worktree_root_requires_yes_and_skips_unmarked(tmp_path: Path):
    root = tmp_path / "worktrees"
    (root / "run-a").mkdir(parents=True)
    with pytest.raises(ValueError):
        clean_worktree_root(root, yes=False)
    payload = clean_worktree_root(root, yes=True)
    assert payload["removed"] == []
    assert payload["skipped"][0]["reason"].startswith("missing")
    assert (root / "run-a").exists()


def test_clean_worktree_root_removes_marked_generated_dirs(tmp_path: Path):
    root = tmp_path / "worktrees"
    generated = root / "run-a"
    generated.mkdir(parents=True)
    write_generated_marker(generated, kind="worktree")
    payload = clean_worktree_root(root, yes=True)
    assert payload["removed"][0]["removed"] is True
    assert not generated.exists()


def test_list_generated_worktrees(tmp_path: Path):
    root = tmp_path / "worktrees"
    generated = root / "run-a"
    generated.mkdir(parents=True)
    write_generated_marker(generated, kind="worktree")
    entries = list_generated_worktrees(root)
    assert entries[0]["name"] == "run-a"
    assert entries[0]["is_git_worktree"] is False
    assert entries[0]["has_marker"] is True


def test_clean_results_root_refuses_unmarked_nonempty(tmp_path: Path):
    from eval_runner.lifecycle import clean_results_root
    root = tmp_path / "results"
    root.mkdir()
    (root / "important.txt").write_text("do not remove", encoding="utf-8")
    with pytest.raises(ValueError):
        clean_results_root(root, yes=True)


def test_clean_results_root_removes_marked_nonempty(tmp_path: Path):
    from eval_runner.lifecycle import clean_results_root
    root = tmp_path / "results"
    root.mkdir()
    write_generated_marker(root, kind="results-root")
    (root / "old.txt").write_text("remove", encoding="utf-8")
    payload = clean_results_root(root, yes=True)
    assert payload["removed"] is True
    assert (root / ".agent-eval-generated.json").exists()
    assert not (root / "old.txt").exists()

from eval_runner.lifecycle import load_run_records
from eval_runner.utils import write_json


def test_load_run_records_falls_back_to_run_summaries(tmp_path):
    run_dir = tmp_path / "runs" / "m" / "p" / "c" / "r1"
    run_dir.mkdir(parents=True)
    write_json(run_dir / "summary.json", {"mode": "m", "case": "c"})
    assert load_run_records(tmp_path) == [{"mode": "m", "case": "c"}]
