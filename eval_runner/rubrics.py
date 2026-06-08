from __future__ import annotations

from pathlib import Path
from typing import Any

from .config import list_yaml_files, load_yaml
from .schema import validate_rubric


def load_rubrics(rubrics_dir: Path) -> dict[str, dict[str, Any]]:
    rubrics: dict[str, dict[str, Any]] = {}
    for path in list_yaml_files(rubrics_dir):
        data = load_yaml(path)
        if not data.get("id"):
            data["id"] = path.stem
        validate_rubric(data, path)
        rubric_id = data.get("id") or path.stem
        data["id"] = rubric_id
        data["_path"] = str(path)
        rubrics[rubric_id] = data
    return rubrics


def resolve_rubric(rubrics: dict[str, dict[str, Any]], case: dict[str, Any], default_rubric: str | None = None) -> dict[str, Any]:
    rubric_id = case.get("rubric") or default_rubric
    if not rubric_id:
        return {}
    if rubric_id not in rubrics:
        available = ", ".join(sorted(rubrics)) or "<none>"
        raise ValueError(f"Unknown rubric '{rubric_id}' for case '{case.get('id')}'. Available rubrics: {available}")
    return rubrics[rubric_id]
