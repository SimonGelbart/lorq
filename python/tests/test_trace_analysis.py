from pathlib import Path

from eval_runner.trace import analyze_trace, classify_graphify_query, extract_commands_from_jsonl


def test_extract_commands_from_codex_like_jsonl(tmp_path: Path):
    raw = tmp_path / "stdout.raw.jsonl"
    raw.write_text(
        '{"type":"item.completed","item":{"type":"local_shell_call","action":{"command":"rg -n PermissionService src"}}}\n'
        '{"type":"item.completed","item":{"type":"local_shell_call","action":{"command":"graphify query PermissionService AuthorizeAdminAttribute"}}}\n',
        encoding="utf-8",
    )
    commands = extract_commands_from_jsonl(raw)
    assert [item["command"] for item in commands] == [
        "rg -n PermissionService src",
        "graphify query PermissionService AuthorizeAdminAttribute",
    ]


def test_graphify_query_specificity():
    generic = classify_graphify_query("graphify query Admin permissions")
    specific = classify_graphify_query("graphify query PermissionService AuthorizeAdminAttribute CheckPermissionAttribute")
    assert generic["is_generic"] is True
    assert generic["is_specific"] is False
    assert specific["is_specific"] is True
    assert "PermissionService" in specific["symbol_like_terms"]


def test_analyze_trace_counts(tmp_path: Path):
    (tmp_path / "stdout.raw.jsonl").write_text(
        '{"cmd":"graphify query Admin permissions"}\n'
        '{"cmd":"sed -n 1,80p src/Foo.cs"}\n'
        '{"cmd":"graphify query PermissionService AuthorizeAdminAttribute"}\n',
        encoding="utf-8",
    )
    analysis = analyze_trace(tmp_path, {"expectations": {"should_avoid_generic_graph_queries": True}})
    assert analysis["graphify_query_count"] == 2
    assert analysis["generic_graphify_query_count"] == 1
    assert analysis["specific_graphify_query_count"] == 1
    assert analysis["source_read_count"] == 1
    assert analysis["source_after_graph"] is True
