from __future__ import annotations

import json
import re
from pathlib import Path
from typing import Any, Iterable

from .utils import read_json, read_text, write_json, write_text
from .pricing import normalize_usage
from .contracts import EVENT_SUMMARY_SCHEMA_VERSION, NORMALIZED_EVENT_SCHEMA_VERSION, with_schema


def walk_json(value: Any) -> Iterable[Any]:
    if isinstance(value, dict):
        yield value
        for child in value.values():
            yield from walk_json(child)
    elif isinstance(value, list):
        for child in value:
            yield from walk_json(child)


def iter_jsonl(path: Path) -> Iterable[tuple[int, dict[str, Any]]]:
    if not path.exists():
        return
    for line_no, line in enumerate(read_text(path).splitlines(), start=1):
        if not line.strip():
            continue
        try:
            value = json.loads(line)
        except json.JSONDecodeError:
            continue
        if isinstance(value, dict):
            yield line_no, value


def write_jsonl(path: Path, rows: list[dict[str, Any]]) -> None:
    write_text(path, "".join(json.dumps(row, ensure_ascii=False) + "\n" for row in rows))


def _camel_to_snake(value: str) -> str:
    value = re.sub(r"(?<!^)(?=[A-Z])", "_", value).lower()
    return value


def canonical_event_type(value: Any) -> str:
    """Return a stable event type string for SDK enums, raw strings, or classes.

    The Copilot SDK docs describe canonical event names such as
    `assistant.message`, `tool.execution_start`, and `session.idle`. Python SDK
    objects may expose enum-like objects whose string representation varies, so
    this helper accepts `.value`, `.name`, and readable class names too.
    """
    if value is None:
        return ""
    raw = getattr(value, "value", None)
    if isinstance(raw, str) and raw:
        return raw
    raw = getattr(value, "name", None)
    if isinstance(raw, str) and raw:
        return raw.lower().replace("_", ".")
    text = str(value).strip()
    if not text:
        return ""
    if text.startswith("SessionEventType."):
        text = text.split(".", 1)[1].lower().replace("_", ".")
    return text


def data_get(data: dict[str, Any], *names: str) -> Any:
    for name in names:
        if name in data:
            return data[name]
    # tolerate Python model_dump snake_case and SDK docs camelCase variants
    lowered = {str(key).lower().replace("_", ""): key for key in data.keys()}
    for name in names:
        key = lowered.get(name.lower().replace("_", ""))
        if key is not None:
            return data[key]
    return None


def command_from_tool_arguments(arguments: Any) -> str | None:
    if isinstance(arguments, str):
        stripped = arguments.strip()
        return stripped or None
    if not isinstance(arguments, dict):
        return None
    for key in (
        "fullCommandText",
        "full_command_text",
        "command",
        "cmd",
        "shell_command",
        "script",
        "input",
        "query",
    ):
        value = data_get(arguments, key)
        if isinstance(value, str) and value.strip():
            return value.strip()
    commands = data_get(arguments, "commands")
    if isinstance(commands, list):
        parts: list[str] = []
        for item in commands:
            if isinstance(item, str):
                parts.append(item)
            elif isinstance(item, dict):
                part = command_from_tool_arguments(item)
                if part:
                    parts.append(part)
        if parts:
            return " && ".join(parts)
    return None


def _new_event(
    *,
    sequence: int,
    backend: str,
    source: str,
    raw_line: int | None,
    raw_type: str | None,
    event_type: str,
    **extra: Any,
) -> dict[str, Any]:
    row: dict[str, Any] = {
        "schema_version": NORMALIZED_EVENT_SCHEMA_VERSION,
        "sequence": sequence,
        "backend": backend,
        "source": source,
        "raw_line": raw_line,
        "raw_type": raw_type,
        "event_type": event_type,
    }
    row.update({key: value for key, value in extra.items() if value is not None})
    return row


def _codex_assistant_text(obj: dict[str, Any]) -> str | None:
    for key in ("message", "text", "content", "final_answer", "answer"):
        val = obj.get(key)
        if isinstance(val, str) and val.strip():
            return val.strip()
    content = obj.get("content")
    if isinstance(content, list):
        chunks: list[str] = []
        for item in content:
            if isinstance(item, dict):
                text = item.get("text") or item.get("content")
                if isinstance(text, str):
                    chunks.append(text)
            elif isinstance(item, str):
                chunks.append(item)
        if chunks:
            return "".join(chunks).strip()
    return None


def _codex_command(obj: dict[str, Any]) -> str | None:
    for key in ("cmd", "command", "shell_command"):
        value = obj.get(key)
        if isinstance(value, str) and value.strip():
            return value.strip()
        if isinstance(value, list) and value and all(isinstance(item, str) for item in value):
            return " ".join(value).strip()
    action = obj.get("action")
    if isinstance(action, dict):
        command = _codex_command(action)
        if command:
            return command
    args = obj.get("args") or obj.get("arguments")
    if isinstance(args, dict):
        command = command_from_tool_arguments(args)
        if command:
            return command
    return None


def normalize_codex_jsonl(path: Path) -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []
    seen_commands: set[tuple[int, str]] = set()
    sequence = 0
    for line_no, raw_event in iter_jsonl(path):
        for obj in walk_json(raw_event):
            if not isinstance(obj, dict):
                continue
            raw_type = str(obj.get("type") or obj.get("item_type") or "")
            lower_type = raw_type.lower()
            if raw_type in {"agent_message", "assistant_message", "message", "final_answer"}:
                text = _codex_assistant_text(obj)
                if text:
                    sequence += 1
                    rows.append(_new_event(
                        sequence=sequence,
                        backend="codex-cli",
                        source="stdout.raw.jsonl",
                        raw_line=line_no,
                        raw_type=raw_type,
                        event_type="assistant_message",
                        text=text,
                    ))
            command = _codex_command(obj)
            if command:
                key = (line_no, command)
                if key not in seen_commands:
                    seen_commands.add(key)
                    sequence += 1
                    rows.append(_new_event(
                        sequence=sequence,
                        backend="codex-cli",
                        source="stdout.raw.jsonl",
                        raw_line=line_no,
                        raw_type=raw_type,
                        event_type="tool_call",
                        tool=data_get(obj, "tool", "name", "toolName") or "shell",
                        command=command,
                        arguments=obj.get("args") or obj.get("arguments"),
                    ))
            if "usage" in lower_type or any(k in obj for k in ("input_tokens", "output_tokens", "total_tokens")):
                usage: dict[str, Any] = {}
                for key in ("input_tokens", "output_tokens", "total_tokens", "cached_input_tokens", "reasoning_output_tokens"):
                    if isinstance(obj.get(key), int):
                        usage[key] = obj[key]
                usage = normalize_usage(usage)
                if usage:
                    sequence += 1
                    rows.append(_new_event(
                        sequence=sequence,
                        backend="codex-cli",
                        source="stdout.raw.jsonl",
                        raw_line=line_no,
                        raw_type=raw_type,
                        event_type="usage",
                        usage=usage,
                    ))
            if "error" in lower_type:
                sequence += 1
                rows.append(_new_event(
                    sequence=sequence,
                    backend="codex-cli",
                    source="stdout.raw.jsonl",
                    raw_line=line_no,
                    raw_type=raw_type,
                    event_type="error",
                    text=_codex_assistant_text(obj),
                ))
    return rows


def _copilot_text(data: dict[str, Any], outer: dict[str, Any]) -> str | None:
    outer_text = outer.get("assistant_text")
    if isinstance(outer_text, str) and outer_text.strip():
        return outer_text.strip()
    for key in ("content", "deltaContent", "delta_content", "reasoningText", "reasoning_text"):
        value = data_get(data, key)
        if isinstance(value, str) and value.strip():
            return value.strip()
    return None


def normalize_copilot_sdk_jsonl(path: Path) -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []
    sequence = 0
    for line_no, event in iter_jsonl(path):
        raw_type = canonical_event_type(event.get("type"))
        data = event.get("data") if isinstance(event.get("data"), dict) else {}
        data_type = str(event.get("data_type") or "")
        event_type = raw_type
        lower_raw = raw_type.lower()

        # Canonical SDK event types documented by GitHub include assistant.message,
        # assistant.message_delta, assistant.usage, tool.execution_start,
        # tool.execution_complete, permission.*, session.idle, and session.error.
        if lower_raw.endswith("assistant.message") or lower_raw == "assistant.message" or data_type == "AssistantMessageData":
            sequence += 1
            rows.append(_new_event(
                sequence=sequence,
                backend="github-copilot-sdk",
                source="stdout.raw.jsonl",
                raw_line=line_no,
                raw_type=raw_type or data_type,
                event_type="assistant_message",
                text=_copilot_text(data, event),
                message_id=data_get(data, "messageId", "message_id"),
                output_tokens=data_get(data, "outputTokens", "output_tokens"),
            ))
            tool_requests = data_get(data, "toolRequests", "tool_requests")
            if isinstance(tool_requests, list):
                for request in tool_requests:
                    if isinstance(request, dict):
                        arguments = data_get(request, "arguments")
                        sequence += 1
                        rows.append(_new_event(
                            sequence=sequence,
                            backend="github-copilot-sdk",
                            source="stdout.raw.jsonl",
                            raw_line=line_no,
                            raw_type=raw_type or data_type,
                            event_type="tool_request",
                            tool=data_get(request, "name", "toolName", "tool_name"),
                            tool_call_id=data_get(request, "toolCallId", "tool_call_id"),
                            command=command_from_tool_arguments(arguments),
                            arguments=arguments,
                        ))
        elif lower_raw.endswith("assistant.message_delta") or lower_raw == "assistant.message_delta" or data_type == "AssistantMessageDeltaData":
            sequence += 1
            rows.append(_new_event(
                sequence=sequence,
                backend="github-copilot-sdk",
                source="stdout.raw.jsonl",
                raw_line=line_no,
                raw_type=raw_type or data_type,
                event_type="assistant_delta",
                text=_copilot_text(data, event),
                message_id=data_get(data, "messageId", "message_id"),
            ))
        elif lower_raw.endswith("assistant.usage") or lower_raw == "assistant.usage":
            usage = {}
            for src, dest in (
                ("inputTokens", "input_tokens"),
                ("input_tokens", "input_tokens"),
                ("outputTokens", "output_tokens"),
                ("output_tokens", "output_tokens"),
                ("cacheReadTokens", "cached_input_tokens"),
                ("cache_read_tokens", "cached_input_tokens"),
                ("cacheWriteTokens", "cache_write_tokens"),
                ("cost", "cost"),
                ("duration", "duration_ms"),
            ):
                value = data_get(data, src)
                if isinstance(value, (int, float)):
                    usage[dest] = value
            usage = normalize_usage(usage)
            sequence += 1
            rows.append(_new_event(
                sequence=sequence,
                backend="github-copilot-sdk",
                source="stdout.raw.jsonl",
                raw_line=line_no,
                raw_type=raw_type or data_type,
                event_type="usage",
                usage=usage,
                model=data_get(data, "model"),
            ))
        elif lower_raw.endswith("tool.execution_start") or lower_raw == "tool.execution_start":
            arguments = data_get(data, "arguments")
            sequence += 1
            rows.append(_new_event(
                sequence=sequence,
                backend="github-copilot-sdk",
                source="stdout.raw.jsonl",
                raw_line=line_no,
                raw_type=raw_type or data_type,
                event_type="tool_call",
                tool=data_get(data, "toolName", "tool_name", "mcpToolName", "mcp_tool_name"),
                tool_call_id=data_get(data, "toolCallId", "tool_call_id"),
                command=command_from_tool_arguments(arguments),
                arguments=arguments,
            ))
        elif lower_raw.endswith("tool.user_requested") or lower_raw == "tool.user_requested":
            arguments = data_get(data, "arguments")
            sequence += 1
            rows.append(_new_event(
                sequence=sequence,
                backend="github-copilot-sdk",
                source="stdout.raw.jsonl",
                raw_line=line_no,
                raw_type=raw_type or data_type,
                event_type="tool_request",
                tool=data_get(data, "toolName", "tool_name"),
                tool_call_id=data_get(data, "toolCallId", "tool_call_id"),
                command=command_from_tool_arguments(arguments),
                arguments=arguments,
            ))
        elif lower_raw.endswith("tool.execution_partial_result") or lower_raw == "tool.execution_partial_result":
            sequence += 1
            rows.append(_new_event(
                sequence=sequence,
                backend="github-copilot-sdk",
                source="stdout.raw.jsonl",
                raw_line=line_no,
                raw_type=raw_type or data_type,
                event_type="tool_output",
                tool_call_id=data_get(data, "toolCallId", "tool_call_id"),
                text=data_get(data, "partialOutput", "partial_output"),
            ))
        elif lower_raw.endswith("tool.execution_complete") or lower_raw == "tool.execution_complete":
            result = data_get(data, "result")
            text = None
            if isinstance(result, dict):
                text = data_get(result, "content", "detailedContent", "detailed_content")
            sequence += 1
            rows.append(_new_event(
                sequence=sequence,
                backend="github-copilot-sdk",
                source="stdout.raw.jsonl",
                raw_line=line_no,
                raw_type=raw_type or data_type,
                event_type="tool_result",
                tool_call_id=data_get(data, "toolCallId", "tool_call_id"),
                success=data_get(data, "success"),
                text=text,
                error=data_get(data, "error"),
            ))
        elif lower_raw.startswith("permission.") or lower_raw.endswith("permission.requested") or lower_raw.endswith("permission.completed"):
            request = data_get(data, "request") or data
            sequence += 1
            rows.append(_new_event(
                sequence=sequence,
                backend="github-copilot-sdk",
                source="stdout.raw.jsonl",
                raw_line=line_no,
                raw_type=raw_type or data_type,
                event_type="permission",
                tool_call_id=data_get(request, "toolCallId", "tool_call_id"),
                command=command_from_tool_arguments(request),
                permission_kind=data_get(request, "kind"),
                result=data_get(data, "result"),
                text=data_get(request, "intention"),
            ))
        elif lower_raw.endswith("session.idle") or lower_raw == "session.idle" or data_type == "SessionIdleData":
            sequence += 1
            rows.append(_new_event(
                sequence=sequence,
                backend="github-copilot-sdk",
                source="stdout.raw.jsonl",
                raw_line=line_no,
                raw_type=raw_type or data_type,
                event_type="session_idle",
            ))
        elif lower_raw.endswith("session.error") or lower_raw == "session.error":
            sequence += 1
            rows.append(_new_event(
                sequence=sequence,
                backend="github-copilot-sdk",
                source="stdout.raw.jsonl",
                raw_line=line_no,
                raw_type=raw_type or data_type,
                event_type="error",
                text=data_get(data, "message"),
                error_type=data_get(data, "errorType", "error_type"),
            ))
        elif data_type:
            # Preserve unknown SDK events so fixtures remain inspectable.
            sequence += 1
            rows.append(_new_event(
                sequence=sequence,
                backend="github-copilot-sdk",
                source="stdout.raw.jsonl",
                raw_line=line_no,
                raw_type=raw_type or data_type,
                event_type="unknown",
            ))
    return rows


TEXT_COMMAND_LINE_RE = re.compile(r"^(?:\s*(?:\$|>|❯)\s+|\s{2,})(?P<cmd>(?:rg|grep|find|fd|cat|sed|head|tail|less|python|git|npx|graphify)\b.*)$", re.IGNORECASE)
FENCED_BLOCK_RE = re.compile(r"```(?:bash|sh|shell|console|text)?\s*\n(?P<body>.*?)```", re.IGNORECASE | re.DOTALL)


def normalize_text_transcript(path: Path, *, backend: str = "text-cli") -> list[dict[str, Any]]:
    text = read_text(path)
    rows: list[dict[str, Any]] = []
    sequence = 0
    if text.strip():
        sequence += 1
        rows.append(_new_event(
            sequence=sequence,
            backend=backend,
            source=path.name,
            raw_line=1,
            raw_type="text",
            event_type="assistant_message",
            text=text.strip(),
        ))

    seen: set[tuple[int, str]] = set()

    def add(line_no: int, command: str) -> None:
        nonlocal sequence
        command = command.strip()
        if not command:
            return
        key = (line_no, command)
        if key in seen:
            return
        seen.add(key)
        sequence += 1
        rows.append(_new_event(
            sequence=sequence,
            backend=backend,
            source=path.name,
            raw_line=line_no,
            raw_type="text_transcript",
            event_type="tool_call",
            tool="shell",
            command=command,
        ))

    for line_no, line in enumerate(text.splitlines(), start=1):
        match = TEXT_COMMAND_LINE_RE.match(line)
        if match:
            add(line_no, match.group("cmd"))

    for block in FENCED_BLOCK_RE.finditer(text):
        prior_lines = text[: block.start()].count("\n")
        for idx, line in enumerate(block.group("body").splitlines(), start=1):
            stripped = line.strip()
            if not stripped or stripped.startswith("#"):
                continue
            if re.match(r"^(rg|grep|find|fd|cat|sed|head|tail|less|python|git|npx|graphify)\b", stripped, flags=re.IGNORECASE):
                add(prior_lines + idx, stripped)
    return rows


def normalize_events(output_dir: Path, *, force: bool = False) -> list[dict[str, Any]]:
    normalized_path = output_dir / "events.normalized.jsonl"
    if normalized_path.exists() and not force:
        return [event for _, event in iter_jsonl(normalized_path)]

    agent_summary = read_json(output_dir / "agent.summary.json", default={}) or {}
    output_format = str(agent_summary.get("output_format") or "").lower()
    backend = str(agent_summary.get("backend") or agent_summary.get("agent") or "").lower()
    raw_jsonl = output_dir / "stdout.raw.jsonl"

    if output_format in {"copilot-sdk-events", "copilot-sdk-jsonl", "github-copilot-sdk"} or backend == "github-copilot-sdk":
        events = normalize_copilot_sdk_jsonl(raw_jsonl)
    elif output_format in {"codex-jsonl", "jsonl", "codex"} or backend == "codex-cli" or (raw_jsonl.exists() and raw_jsonl.stat().st_size > 0):
        events = normalize_codex_jsonl(raw_jsonl)
    else:
        events = normalize_text_transcript(output_dir / "stdout.raw.txt", backend=backend or "text-cli")

    write_jsonl(normalized_path, events)
    summary = summarize_normalized_events(events)
    write_json(output_dir / "events.summary.json", summary)
    return events


def summarize_normalized_events(events: list[dict[str, Any]]) -> dict[str, Any]:
    counts: dict[str, int] = {}
    backends: set[str] = set()
    usage: dict[str, Any] = {}
    for event in events:
        event_type = str(event.get("event_type") or "unknown")
        counts[event_type] = counts.get(event_type, 0) + 1
        if event.get("backend"):
            backends.add(str(event["backend"]))
        event_usage = event.get("usage")
        if isinstance(event_usage, dict):
            for key, value in event_usage.items():
                if isinstance(value, (int, float)):
                    usage[key] = usage.get(key, 0) + value
    return with_schema({
        "event_count": len(events),
        "event_type_counts": counts,
        "backends": sorted(backends),
        "usage": usage,
    }, EVENT_SUMMARY_SCHEMA_VERSION)
