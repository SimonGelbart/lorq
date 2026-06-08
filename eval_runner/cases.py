from __future__ import annotations

from pathlib import Path
from typing import Any

from .config import list_yaml_files, load_yaml
from .schema import validate_case


def load_cases(cases_dir: Path) -> dict[str, dict[str, Any]]:
    cases: dict[str, dict[str, Any]] = {}
    for path in list_yaml_files(cases_dir):
        data = load_yaml(path)
        if not data.get("id"):
            data["id"] = path.stem
        validate_case(data, path)
        case_id = data.get("id") or path.stem
        data["id"] = case_id
        data["_path"] = str(path)
        cases[case_id] = data
    return cases


def select_cases(
    all_cases: dict[str, dict[str, Any]],
    selected_cases: str | None,
    selected_categories: str | None,
) -> list[dict[str, Any]]:
    chosen = list(all_cases.values())

    if selected_cases:
        ids = [item.strip() for item in selected_cases.split(",") if item.strip()]
        missing = [cid for cid in ids if cid not in all_cases]
        if missing:
            raise ValueError(f"Unknown case(s): {', '.join(missing)}. Available: {', '.join(sorted(all_cases))}")
        chosen = [all_cases[cid] for cid in ids]

    if selected_categories:
        categories = {item.strip() for item in selected_categories.split(",") if item.strip()}
        chosen = [case for case in chosen if case.get("category") in categories]

    return chosen
