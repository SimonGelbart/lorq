from __future__ import annotations

import re
from pathlib import Path
from typing import Any

from ..utils import read_json

# Keep this conservative: require either a slash or a well-known project file suffix
# so prose like "service.cs" is not treated as a file unless it looks like a path.
_FILE_EXTENSIONS = (
    "cs", "csproj", "sln", "fs", "vb", "java", "kt", "scala", "go", "rs",
    "py", "js", "jsx", "ts", "tsx", "mjs", "cjs", "css", "scss", "html", "cshtml",
    "xml", "json", "yaml", "yml", "toml", "md", "txt", "csv", "sql",
    "sh", "bash", "ps1", "bat", "cmd", "dockerfile",
)
_FILE_PATTERN = re.compile(
    r"(?<![A-Za-z0-9_./-])"
    r"([A-Za-z0-9_.@+~-]+(?:/[A-Za-z0-9_.@+~-]+)+\."
    r"(?:" + "|".join(re.escape(ext) for ext in sorted(_FILE_EXTENSIONS, key=len, reverse=True)) + r")"
    r")"
)
_BACKTICK_PATTERN = re.compile(r"`([^`\n]+)`")


def _normalize_path(value: str) -> str:
    value = value.strip().strip("'\".,;:()[]{}<>")
    value = value.replace("\\", "/")
    while value.startswith("./"):
        value = value[2:]
    return value


def extract_file_references(text: str) -> list[str]:
    """Extract likely repository-relative file references from answer text."""
    refs: list[str] = []

    def add(candidate: str) -> None:
        normalized = _normalize_path(candidate)
        if not normalized or "://" in normalized:
            return
        if "/" not in normalized:
            return
        lower = normalized.lower()
        if not any(lower.endswith("." + ext) for ext in _FILE_EXTENSIONS):
            return
        if normalized not in refs:
            refs.append(normalized)

    for match in _FILE_PATTERN.finditer(text):
        add(match.group(1))

    # Backticks often hold paths with punctuation adjacent to them. Re-scan each token.
    for match in _BACKTICK_PATTERN.finditer(text):
        token = match.group(1).strip()
        if "/" in token:
            add(token)

    return refs


def _safe_relative(path: str) -> Path | None:
    candidate = Path(path.replace("\\", "/"))
    if candidate.is_absolute() or ".." in candidate.parts:
        return None
    return candidate


def resolve_existing_file_refs(worktree: Path | None, refs: list[str]) -> dict[str, Any]:
    """Return existing/missing file refs. Missing is unknown if the worktree is unavailable."""
    if worktree is None or not worktree.exists():
        return {
            "worktree_available": False,
            "existing": [],
            "missing": [],
            "unknown": refs,
        }

    existing: list[str] = []
    missing: list[str] = []
    for ref in refs:
        rel = _safe_relative(ref)
        if rel is None:
            missing.append(ref)
            continue
        if (worktree / rel).is_file():
            existing.append(ref)
        else:
            missing.append(ref)
    return {
        "worktree_available": True,
        "existing": existing,
        "missing": missing,
        "unknown": [],
    }


def worktree_from_manifest(output_dir: Path) -> Path | None:
    manifest = read_json(output_dir / "run.manifest.json", default={}) or {}
    raw = manifest.get("worktree")
    if not raw:
        return None
    return Path(raw)


def source_file_count(refs: list[str]) -> int:
    return len(set(refs))


def symbol_near_file_reference(answer: str, symbol: str, refs: list[str], *, window: int = 400) -> bool:
    if not symbol or not refs:
        return False
    lower = answer.lower()
    symbol_lower = symbol.lower()
    symbol_positions = [match.start() for match in re.finditer(re.escape(symbol_lower), lower)]
    if not symbol_positions:
        return False
    ref_positions: list[int] = []
    for ref in refs:
        ref_lower = ref.lower()
        ref_positions.extend(match.start() for match in re.finditer(re.escape(ref_lower), lower))
    if not ref_positions:
        return False
    return any(abs(s - r) <= window for s in symbol_positions for r in ref_positions)


def normalize_required_symbol(value: Any) -> dict[str, Any]:
    if isinstance(value, str):
        return {"symbol": value}
    if isinstance(value, dict):
        symbol = value.get("symbol") or value.get("name") or value.get("value")
        out = dict(value)
        out["symbol"] = str(symbol) if symbol is not None else ""
        return out
    return {"symbol": str(value) if value is not None else ""}


def build_evidence_metrics(output_dir: Path, answer: str) -> dict[str, Any]:
    refs = extract_file_references(answer)
    worktree = worktree_from_manifest(output_dir)
    resolved = resolve_existing_file_refs(worktree, refs)
    return {
        "answer_empty": not bool(answer.strip()),
        "file_references": refs,
        "file_reference_count": len(refs),
        "unique_file_reference_count": source_file_count(refs),
        "worktree": str(worktree) if worktree else None,
        "worktree_available": resolved["worktree_available"],
        "existing_file_references": resolved["existing"],
        "existing_file_reference_count": len(resolved["existing"]),
        "missing_file_references": resolved["missing"],
        "missing_file_reference_count": len(resolved["missing"]),
        "unknown_file_references": resolved["unknown"],
        "unknown_file_reference_count": len(resolved["unknown"]),
    }
