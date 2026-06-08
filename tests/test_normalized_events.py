from pathlib import Path

from eval_runner.events import normalize_copilot_sdk_jsonl, normalize_codex_jsonl, normalize_events, summarize_normalized_events
from eval_runner.trace import analyze_trace
from eval_runner.agents import extract_copilot_sdk_answer


def test_normalize_codex_shell_events(tmp_path: Path):
    raw = tmp_path / "stdout.raw.jsonl"
    raw.write_text(
        '{"type":"item.completed","item":{"type":"local_shell_call","action":{"command":"rg -n Foo src"}}}\n'
        '{"type":"assistant_message","message":"Done"}\n',
        encoding="utf-8",
    )
    events = normalize_codex_jsonl(raw)
    assert [e["event_type"] for e in events] == ["tool_call", "assistant_message"]
    assert events[0]["command"] == "rg -n Foo src"


def test_normalize_copilot_sdk_tool_and_usage_events(tmp_path: Path):
    raw = tmp_path / "stdout.raw.jsonl"
    raw.write_text(
        '{"type":"assistant.message","data_type":"AssistantMessageData","data":{"messageId":"m1","content":"I will inspect files","toolRequests":[{"toolCallId":"t1","name":"bash","arguments":{"fullCommandText":"rg -n PermissionService src"}}]}}\n'
        '{"type":"tool.execution_start","data":{"toolCallId":"t1","toolName":"bash","arguments":{"fullCommandText":"rg -n PermissionService src"}}}\n'
        '{"type":"tool.execution_complete","data":{"toolCallId":"t1","success":true,"result":{"content":"src/Foo.cs:1"}}}\n'
        '{"type":"assistant.usage","data":{"model":"gpt-5","inputTokens":100,"outputTokens":20,"cost":0.1}}\n'
        '{"type":"session.idle","data_type":"SessionIdleData","data":{}}\n',
        encoding="utf-8",
    )
    events = normalize_copilot_sdk_jsonl(raw)
    event_types = [event["event_type"] for event in events]
    assert "assistant_message" in event_types
    assert "tool_request" in event_types
    assert "tool_call" in event_types
    assert "tool_result" in event_types
    assert "usage" in event_types
    assert "session_idle" in event_types
    commands = [event.get("command") for event in events if event.get("command")]
    assert "rg -n PermissionService src" in commands
    summary = summarize_normalized_events(events)
    assert summary["usage"]["input_tokens"] == 100
    assert summary["usage"]["output_tokens"] == 20


def test_analyze_trace_uses_normalized_copilot_events(tmp_path: Path):
    (tmp_path / "agent.summary.json").write_text('{"backend":"github-copilot-sdk","output_format":"copilot-sdk-events"}', encoding="utf-8")
    (tmp_path / "stdout.raw.jsonl").write_text(
        '{"type":"tool.execution_start","data":{"toolCallId":"t1","toolName":"bash","arguments":{"fullCommandText":"graphify query Admin permissions"}}}\n'
        '{"type":"tool.execution_start","data":{"toolCallId":"t2","toolName":"bash","arguments":{"fullCommandText":"sed -n 1,80p src/Foo.cs"}}}\n'
        '{"type":"tool.execution_start","data":{"toolCallId":"t3","toolName":"bash","arguments":{"fullCommandText":"graphify query PermissionService AuthorizeAdminAttribute"}}}\n',
        encoding="utf-8",
    )
    analysis = analyze_trace(tmp_path, {"expectations": {"should_avoid_generic_graph_queries": True}})
    assert analysis["trace_source"] == "normalized"
    assert analysis["normalized_event_count"] == 3
    assert analysis["graphify_query_count"] == 2
    assert analysis["generic_graphify_query_count"] == 1
    assert analysis["specific_graphify_query_count"] == 1
    assert (tmp_path / "events.normalized.jsonl").exists()
    assert (tmp_path / "events.summary.json").exists()


def test_extract_copilot_sdk_answer_prefers_final_message_over_delta():
    stdout = (
        '{"type":"assistant.message_delta","data_type":"AssistantMessageDeltaData","data":{"deltaContent":"Hel"},"assistant_text":"Hel"}\n'
        '{"type":"assistant.message_delta","data_type":"AssistantMessageDeltaData","data":{"deltaContent":"lo"},"assistant_text":"lo"}\n'
        '{"type":"assistant.message","data_type":"AssistantMessageData","data":{"content":"Hello"},"assistant_text":"Hello"}\n'
    )
    assert extract_copilot_sdk_answer(stdout) == "Hello\n"


def test_normalize_events_default_codex_when_no_agent_summary(tmp_path: Path):
    (tmp_path / "stdout.raw.jsonl").write_text('{"cmd":"rg -n Foo src"}\n', encoding="utf-8")
    events = normalize_events(tmp_path)
    assert events[0]["event_type"] == "tool_call"
    assert events[0]["command"] == "rg -n Foo src"
