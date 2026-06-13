# Agent backends

The evaluator supports multiple backend profiles.

## Codex CLI

Default profile:

```yaml
codex:
  backend: codex
  command: codex
  args: [exec, --json]
  input_mode: stdin
  output_format: codex-jsonl
```

Run:

```bash
agent-eval --agent-profile codex --check-agent
```

## GitHub Copilot SDK

Install:

```bash
pip install -e '.[copilot]'
```

Profile:

```yaml
copilot-sdk:
  backend: copilot-sdk
  model: gpt-5
  reasoning_effort: medium
  permission_policy: approve_all
  output_format: copilot-sdk-events
```

Run only inside isolated disposable worktrees when using `approve_all`.

## Plain Copilot CLI fallback

The `copilot-cli` profile is a simple text CLI fallback. It has weaker trace support than Codex or Copilot SDK.

## Generic CLI backend

Use `backend: generic` for fake agents or local scripts:

```yaml
local-fake:
  backend: generic
  command: python
  args: [execution/scripts/fake_agent.py]
  input_mode: stdin
  output_format: text
```

This is useful for CI tests that should not spend agent tokens.
