from eval_runner.agents import CliAgent, create_agent, extract_answer, resolve_agent_profile, split_args


def test_codex_profile_uses_stdin_and_jsonl():
    agent = create_agent({"backend": "codex", "command": "codex", "args": ["exec", "--json"]})
    assert agent.backend_id == "codex-cli"
    assert agent.input_mode == "stdin"
    assert agent.output_format == "codex-jsonl"


def test_copilot_profile_puts_prompt_in_argument():
    agent = create_agent({"backend": "copilot", "command": "copilot", "args": ["--prompt", "{prompt}"]})
    cmd, input_text, summary_cmd = agent._render_command("hello")
    assert cmd == ["copilot", "--prompt", "hello"]
    assert input_text is None
    assert summary_cmd == ["copilot", "--prompt", "<PROMPT>"]


def test_generic_agent_can_append_prompt_arg():
    agent = CliAgent(command="agent", args=["run"], input_mode="argument", prompt_arg="-p", output_format="text")
    cmd, input_text, summary_cmd = agent._render_command("task")
    assert cmd == ["agent", "run", "-p", "task"]
    assert input_text is None
    assert summary_cmd == ["agent", "run", "-p", "<PROMPT>"]


def test_extract_text_answer_preserves_stdout():
    assert extract_answer("final answer", output_format="text") == "final answer\n"


def test_split_args_uses_shell_like_quotes():
    assert split_args('--prompt "hello world"', []) == ["--prompt", "hello world"]


def test_resolve_agent_profile_from_config():
    profile_id, profile = resolve_agent_profile({"agent": {"profile": "copilot"}, "agent_profiles": {"copilot": {"backend": "copilot"}}})
    assert profile_id == "copilot"
    assert profile["backend"] == "copilot"


def test_copilot_sdk_profile_creates_sdk_agent():
    agent = create_agent({"backend": "copilot-sdk", "model": "gpt-5", "permission_policy": "approve_all"})
    assert agent.backend_id == "github-copilot-sdk"
    assert agent.model == "gpt-5"
    assert agent.permission_policy == "approve_all"
    assert agent.output_format == "copilot-sdk-events"


def test_extract_copilot_sdk_answer_from_events():
    event = '{"data_type":"AssistantMessageData","assistant_text":"hello from copilot"}\n'
    assert extract_answer(event, output_format="copilot-sdk-events") == "hello from copilot\n"
