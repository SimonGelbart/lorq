from __future__ import annotations

from pathlib import Path
from typing import Any

from .schema import validate_prompt_style_text


def load_prompt_style(prompt_styles_dir: Path, style: str) -> str:
    path = prompt_styles_dir / f"{style}.txt"
    if not path.exists():
        available = sorted(p.stem for p in prompt_styles_dir.glob("*.txt"))
        raise FileNotFoundError(f"Prompt style not found: {style}. Available: {', '.join(available)}")
    text = path.read_text(encoding="utf-8")
    validate_prompt_style_text(text, path)
    return text


def validate_prompt_styles_dir(prompt_styles_dir: Path) -> dict[str, str]:
    styles: dict[str, str] = {}
    if not prompt_styles_dir.exists():
        return styles
    for path in sorted(prompt_styles_dir.glob("*.txt")):
        text = path.read_text(encoding="utf-8")
        validate_prompt_style_text(text, path)
        styles[path.stem] = text
    return styles


def render_prompt(template: str, case: dict[str, Any], mode: dict[str, Any], prompt_style: str) -> str:
    task = str(case.get("task") or "").strip()
    if not task:
        raise ValueError(f"Case {case.get('id')} has no task")
    return template.format(
        task=task,
        case_id=case.get("id", ""),
        case_title=case.get("title", ""),
        case_category=case.get("category", ""),
        mode_id=mode.get("id", ""),
        mode_description=mode.get("description", ""),
        prompt_style=prompt_style,
    ).strip() + "\n"
