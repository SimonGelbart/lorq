from __future__ import annotations

import json
import re
from pathlib import Path
from typing import Any, Iterable

from .utils import read_text
from .events import normalize_events

# Commands that usually read or inspect source files. This is intentionally broad
# and shell-oriented so it works across Codex event-shape changes.
SOURCE_READ_PATTERNS = [
    r"\bcat\s+",
    r"\bsed\s+(-n\s+)?",
    r"\bhead\s+",
    r"\btail\s+",
    r"\bless\s+",
    r"\bpython\b.*\b(open|read_text|Path\().*",
]

SEARCH_PATTERNS = [
    r"\brg\b",
    r"\bgrep\b",
    r"\bfind\b",
    r"\bfd\b",
]

GRAPHIFY_QUERY_PATTERN = re.compile(r"(^|[;&|]\s*|\b)(?:npx\s+)?graphify\s+query\b", re.IGNORECASE)
GRAPHIFY_ANY_PATTERN = re.compile(r"(^|[;&|]\s*|\b)(?:npx\s+)?graphify\b", re.IGNORECASE)

# Generic nouns that produced weak default-Graphify queries in the original evals.
# This list is deliberately configurable in mode expectations, but these defaults
# catch the common problematic cases without being specific to nopCommerce.
DEFAULT_GENERIC_TERMS = {
    "admin",
    "architecture",
    "auth",
    "authentication",
    "authorization",
    "cart",
    "controller",
    "customer",
    "database",
    "flow",
    "login",
    "model",
    "order",
    "permission",
    "permissions",
    "plugin",
    "plugins",
    "route",
    "routes",
    "service",
    "services",
    "user",
}

WORD_RE = re.compile(r"[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)?")
CAMEL_OR_SYMBOL_RE = re.compile(r"(?:[A-Z][a-z0-9]+){2,}|I[A-Z][A-Za-z0-9]+|[A-Za-z_][A-Za-z0-9_]*Async\b|[A-Za-z_][A-Za-z0-9_]*(?:Controller|Service|Attribute|Factory|Provider|Manager|Repository|Model|Record|Plugin|Consumer)\b")


def walk_json(value: Any) -> Iterable[Any]:
    if isinstance(value, dict):
        yield value
        for child in value.values():
            yield from walk_json(child)
    elif isinstance(value, list):
        for child in value:
            yield from walk_json(child)


def iter_jsonl(path: Path) -> Iterable[dict[str, Any]]:
    for line in read_text(path).splitlines():
        if not line.strip():
            continue
        try:
            value = json.loads(line)
        except json.JSONDecodeError:
            continue
        if isinstance(value, dict):
            yield value


def _stringify_command(value: Any) -> str | None:
    if isinstance(value, str):
        stripped = value.strip()
        return stripped if stripped else None
    if isinstance(value, list) and value and all(isinstance(item, str) for item in value):
        return " ".join(value).strip()
    return None


def _extract_command_from_obj(obj: dict[str, Any]) -> str | None:
    # Known / likely event variants from agent CLIs:
    # - {"cmd": "rg ..."}
    # - {"command": "rg ..."}
    # - {"action": {"command": "rg ..."}}
    # - {"args": {"cmd": "rg ..."}}
    # - {"item": {"type": "local_shell_call", "action": {"command": "..."}}}
    for key in ("cmd", "command", "shell_command"):
        command = _stringify_command(obj.get(key))
        if command:
            return command

    action = obj.get("action")
    if isinstance(action, dict):
        for key in ("cmd", "command", "shell_command"):
            command = _stringify_command(action.get(key))
            if command:
                return command

    args = obj.get("args") or obj.get("arguments")
    if isinstance(args, dict):
        for key in ("cmd", "command", "shell_command"):
            command = _stringify_command(args.get(key))
            if command:
                return command

    return None


def extract_commands_from_jsonl(path: Path) -> list[dict[str, Any]]:
    commands: list[dict[str, Any]] = []
    seen: set[tuple[int, str]] = set()
    for line_no, event in enumerate(iter_jsonl(path), start=1):
        for obj in walk_json(event):
            if not isinstance(obj, dict):
                continue
            command = _extract_command_from_obj(obj)
            if not command:
                continue
            key = (line_no, command)
            if key in seen:
                continue
            seen.add(key)
            typ = str(obj.get("type") or obj.get("item_type") or "")
            commands.append({"line": line_no, "command": command, "type": typ})
    return commands


TEXT_COMMAND_LINE_RE = re.compile(r"^(?:\s*(?:\$|>|❯)\s+|\s{2,})(?P<cmd>(?:rg|grep|find|fd|cat|sed|head|tail|less|python|git|npx|graphify)\b.*)$", re.IGNORECASE)
FENCED_BLOCK_RE = re.compile(r"```(?:bash|sh|shell|console|text)?\s*\n(?P<body>.*?)```", re.IGNORECASE | re.DOTALL)


def extract_commands_from_text(path: Path, *, line_offset: int = 0) -> list[dict[str, Any]]:
    """Best-effort command extraction from text-only agent transcripts.

    Codex JSONL traces are preferred when available. This fallback exists for
    CLIs such as GitHub Copilot CLI whose programmatic output may be plain text.
    It extracts only obvious shell commands from prompts, console-style lines, or
    fenced blocks; absence of commands should not be read as proof that no tools
    were used.
    """
    text = read_text(path)
    if not text.strip():
        return []
    commands: list[dict[str, Any]] = []
    seen: set[tuple[int, str]] = set()

    def add(line_no: int, command: str) -> None:
        command = command.strip()
        if not command:
            return
        key = (line_no, command)
        if key in seen:
            return
        seen.add(key)
        commands.append({"line": line_offset + line_no, "command": command, "type": "text_transcript"})

    for line_no, line in enumerate(text.splitlines(), start=1):
        match = TEXT_COMMAND_LINE_RE.match(line)
        if match:
            add(line_no, match.group("cmd"))

    # Also inspect fenced code blocks whose lines may not have shell prompts.
    for block in FENCED_BLOCK_RE.finditer(text):
        prior_lines = text[: block.start()].count("\n")
        for idx, line in enumerate(block.group("body").splitlines(), start=1):
            stripped = line.strip()
            if not stripped or stripped.startswith("#"):
                continue
            if re.match(r"^(rg|grep|find|fd|cat|sed|head|tail|less|python|git|npx|graphify)\b", stripped, flags=re.IGNORECASE):
                add(prior_lines + idx, stripped)

    return commands


def _matches_any(command: str, patterns: list[str]) -> bool:
    return any(re.search(pattern, command, flags=re.IGNORECASE) for pattern in patterns)


def is_search_command(command: str) -> bool:
    return _matches_any(command, SEARCH_PATTERNS)


def is_source_read_command(command: str) -> bool:
    return _matches_any(command, SOURCE_READ_PATTERNS)


def is_graphify_command(command: str) -> bool:
    return GRAPHIFY_ANY_PATTERN.search(command) is not None


def is_graphify_query(command: str) -> bool:
    return GRAPHIFY_QUERY_PATTERN.search(command) is not None


def extract_graphify_query_text(command: str) -> str:
    """Extract best-effort query text from a graphify query shell command."""
    match = GRAPHIFY_QUERY_PATTERN.search(command)
    if not match:
        return command
    text = command[match.end():].strip()
    # Drop common flags and shell redirects in a very conservative way.
    text = re.split(r"\s*(?:[|;&]|>|2>)\s*", text, maxsplit=1)[0].strip()
    # Remove simple flags but preserve flag values if we cannot know semantics.
    parts = text.split()
    cleaned: list[str] = []
    skip_next = False
    for part in parts:
        if skip_next:
            skip_next = False
            continue
        if part.startswith("--"):
            # Heuristic for common options that take a value.
            if part in {"--limit", "--top", "--format", "--json", "--out"}:
                skip_next = True
            continue
        if part.startswith("-"):
            continue
        cleaned.append(part.strip('"\''))
    return " ".join(cleaned).strip() or text


def classify_graphify_query(command: str, generic_terms: set[str] | None = None) -> dict[str, Any]:
    query = extract_graphify_query_text(command)
    generic_terms = {term.lower() for term in (generic_terms or DEFAULT_GENERIC_TERMS)}
    words = [word for word in WORD_RE.findall(query)]
    lower_words = [word.lower() for word in words]
    symbol_like = sorted(set(CAMEL_OR_SYMBOL_RE.findall(query)))
    generic_hits = sorted({word for word in lower_words if word in generic_terms})

    non_generic_words = [word for word in lower_words if word not in generic_terms]
    # A query is specific if it contains exact-looking symbols or enough non-generic tokens.
    # A query is generic if it is mostly generic nouns and has no exact-looking symbol.
    is_specific = bool(symbol_like) or len(non_generic_words) >= 3
    is_generic = not is_specific and bool(generic_hits or words)

    return {
        "command": command,
        "query": query,
        "words": words,
        "symbol_like_terms": symbol_like,
        "generic_terms": generic_hits,
        "is_specific": is_specific,
        "is_generic": is_generic,
    }


def analyze_trace(output_dir: Path, mode: dict[str, Any] | None = None) -> dict[str, Any]:
    """Analyze normalized agent events for behavior metrics.

    v0.11 routes all backends through `events.normalized.jsonl` first.
    This keeps behavior validators backend-independent while preserving the
    older command metrics used by reports.
    """
    mode = mode or {}
    expectations = mode.get("expectations") or {}
    generic_terms = set(DEFAULT_GENERIC_TERMS)
    generic_terms.update(str(term).lower() for term in expectations.get("generic_query_terms", []) or [])

    events = normalize_events(output_dir)
    commands: list[dict[str, Any]] = []
    for event in events:
        command = event.get("command")
        if not isinstance(command, str) or not command.strip():
            continue
        commands.append({
            "line": int(event.get("sequence") or event.get("raw_line") or len(commands) + 1),
            "raw_line": event.get("raw_line"),
            "sequence": event.get("sequence"),
            "command": command.strip(),
            "type": event.get("event_type") or event.get("raw_type") or "normalized_event",
            "tool": event.get("tool"),
            "backend": event.get("backend"),
        })

    trace_source = "normalized" if events else "none"

    graphify_queries = [
        classify_graphify_query(item["command"], generic_terms) | {"line": item["line"], "backend": item.get("backend")}
        for item in commands
        if is_graphify_query(item["command"])
    ]
    graphify_commands = [item for item in commands if is_graphify_command(item["command"])]
    source_reads = [item for item in commands if is_source_read_command(item["command"])]
    searches = [item for item in commands if is_search_command(item["command"])]

    first_graph_line = min((item["line"] for item in graphify_queries), default=None)
    first_source_line = min((item["line"] for item in source_reads), default=None)
    source_after_graph = False
    if first_graph_line is not None:
        source_after_graph = any(item["line"] > first_graph_line for item in source_reads)

    event_type_counts: dict[str, int] = {}
    for event in events:
        event_type = str(event.get("event_type") or "unknown")
        event_type_counts[event_type] = event_type_counts.get(event_type, 0) + 1

    return {
        "trace_source": trace_source,
        "normalized_event_count": len(events),
        "normalized_event_type_counts": event_type_counts,
        "command_count": len(commands),
        "commands": commands,
        "search_count": len(searches),
        "source_read_count": len(source_reads),
        "graphify_command_count": len(graphify_commands),
        "graphify_query_count": len(graphify_queries),
        "generic_graphify_query_count": sum(1 for item in graphify_queries if item["is_generic"]),
        "specific_graphify_query_count": sum(1 for item in graphify_queries if item["is_specific"]),
        "graphify_queries": graphify_queries,
        "source_after_graph": source_after_graph,
        "first_graph_line": first_graph_line,
        "first_source_line": first_source_line,
    }
