from eval_runner.prompts import render_prompt


def test_render_prompt():
    prompt = render_prompt("Task: {task}\nMode: {mode_id}", {"id": "c1", "task": "Do X"}, {"id": "m1"}, "neutral")
    assert "Task: Do X" in prompt
    assert "Mode: m1" in prompt
