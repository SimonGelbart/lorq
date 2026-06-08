from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
from typing import Any

import yaml

from .schema import validate_config


@dataclass(frozen=True)
class SuitePaths:
    root: Path
    config_path: Path

    @property
    def modes_dir(self) -> Path:
        return self.root / "modes"

    @property
    def cases_dir(self) -> Path:
        return self.root / "cases"

    @property
    def prompt_styles_dir(self) -> Path:
        return self.root / "prompt_styles"

    @property
    def execution_dir(self) -> Path:
        return self.root / "execution"

    @property
    def rubrics_dir(self) -> Path:
        return self.root / "rubrics"


def load_yaml(path: Path) -> dict[str, Any]:
    if not path.exists():
        raise FileNotFoundError(path)
    with path.open("r", encoding="utf-8") as f:
        data = yaml.safe_load(f) or {}
    if not isinstance(data, dict):
        raise ValueError(f"Expected YAML mapping in {path}")
    return data


def load_config(suite_root: Path, config_name: str = "eval.config.yaml") -> tuple[SuitePaths, dict[str, Any]]:
    suite_root = suite_root.resolve()
    config_path = (suite_root / config_name).resolve()
    config = load_yaml(config_path)
    validate_config(config, config_path)
    return SuitePaths(root=suite_root, config_path=config_path), config


def list_yaml_files(directory: Path) -> list[Path]:
    if not directory.exists():
        return []
    return sorted([p for p in directory.iterdir() if p.suffix in {".yaml", ".yml"}])
