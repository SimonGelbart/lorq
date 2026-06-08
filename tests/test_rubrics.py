from pathlib import Path

from eval_runner.rubrics import load_rubrics, resolve_rubric


def test_load_and_resolve_rubrics(tmp_path: Path):
    rubrics_dir = tmp_path / "rubrics"
    rubrics_dir.mkdir()
    (rubrics_dir / "basic.yaml").write_text("id: basic\ndimensions:\n  correctness:\n    weight: 1\n", encoding="utf-8")
    rubrics = load_rubrics(rubrics_dir)
    resolved = resolve_rubric(rubrics, {"id": "case-1", "rubric": "basic"})
    assert resolved["id"] == "basic"
