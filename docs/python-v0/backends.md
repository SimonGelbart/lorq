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

## Deterministic fake adapter

Use `backend: deterministic-fake` only for LORQ migration fixtures. It never invokes an LLM or external process. Instead, it reads a YAML/JSON fixture keyed by `case|mode|attempt`, writes the normal Python v0 run files, and adds `adapter.evidence.json` so package export can verify a full evidence contract.

```yaml
deterministic-fake:
  backend: deterministic-fake
  fixture_file: fixtures/fake-agent.yaml
```

The fixture supports deterministic answers, missing-final-answer cells, synthetic timing, usage counts, normalized events, artifact references, and integrity warnings. This adapter is for orchestration migration gates, not product scoring.

The paired deterministic judge is enabled through the `judge` block:

```yaml
judge:
  enabled: true
  backend: deterministic-fake
  fixture_file: fixtures/fake-judge.yaml
```

In Python v0 the judge still runs in the legacy per-run location. Future LORQ product work should attach deterministic judgements to merged experiment packages.
