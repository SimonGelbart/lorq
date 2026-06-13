from pathlib import Path

from eval_runner.events import normalize_codex_jsonl, normalize_copilot_sdk_jsonl, summarize_normalized_events

FIXTURES = Path(__file__).parent / "fixtures"


def test_codex_fixture_normalizes_tool_and_usage():
    events = normalize_codex_jsonl(FIXTURES / "codex_exec_session.jsonl")
    assert any(event["event_type"] == "tool_call" and event.get("command") == "rg -n PermissionService src" for event in events)
    assert any(event["event_type"] == "assistant_message" for event in events)
    summary = summarize_normalized_events(events)
    assert summary["usage"]["input_tokens"] == 42
    assert summary["usage"]["output_tokens"] == 7


def test_copilot_sdk_fixture_normalizes_documented_event_flow():
    events = normalize_copilot_sdk_jsonl(FIXTURES / "copilot_sdk_session.jsonl")
    types = [event["event_type"] for event in events]
    assert "assistant_message" in types
    assert "tool_request" in types
    assert "tool_call" in types
    assert "tool_result" in types
    assert "usage" in types
    assert "session_idle" in types
    assert any(event.get("command") == "rg -n PermissionService src" for event in events)
    summary = summarize_normalized_events(events)
    assert summary["usage"]["input_tokens"] == 100
    assert summary["usage"]["output_tokens"] == 15
