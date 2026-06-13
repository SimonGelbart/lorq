from pathlib import Path

from eval_runner.trace import extract_commands_from_text


def test_extract_commands_from_text_console_lines(tmp_path: Path):
    p = tmp_path / "stdout.raw.txt"
    p.write_text("Suggestion:\n  rg -n \"PermissionService\" src\n$ graphify query PermissionService\n", encoding="utf-8")
    commands = extract_commands_from_text(p)
    assert any(c["command"].startswith("rg -n") for c in commands)
    assert any(c["command"].startswith("graphify query") for c in commands)


def test_extract_commands_from_text_fenced_block(tmp_path: Path):
    p = tmp_path / "stdout.raw.txt"
    p.write_text("```bash\nrg -n Customer src\ngit status\n```\n", encoding="utf-8")
    commands = extract_commands_from_text(p)
    assert [c["command"] for c in commands] == ["rg -n Customer src", "git status"]
