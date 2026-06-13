from __future__ import annotations

from pathlib import Path
from typing import Any

from .config import list_yaml_files, load_yaml
from .schema import validate_mode


def load_modes(modes_dir: Path) -> dict[str, dict[str, Any]]:
    modes: dict[str, dict[str, Any]] = {}
    for path in list_yaml_files(modes_dir):
        data = load_yaml(path)
        if not data.get("id"):
            data["id"] = path.stem
        validate_mode(data, path)
        mode_id = data.get("id") or path.stem
        data["id"] = mode_id
        data["_path"] = str(path)
        modes[mode_id] = data
    return modes


def select_modes(all_modes: dict[str, dict[str, Any]], selected: str | None) -> list[dict[str, Any]]:
    if not selected:
        return list(all_modes.values())
    ids = [item.strip() for item in selected.split(",") if item.strip()]
    missing = [mid for mid in ids if mid not in all_modes]
    if missing:
        raise ValueError(f"Unknown mode(s): {', '.join(missing)}. Available: {', '.join(sorted(all_modes))}")
    return [all_modes[mid] for mid in ids]
