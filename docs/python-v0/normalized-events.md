# Normalized events contract

Backends emit different raw trace formats. `agent-eval` converts them to `events.normalized.jsonl` before behavior validation.

Each JSONL row follows `schemas/normalized-event.schema.json`.

Core fields:

```json
{
  "schema_version": "agent-eval.normalized-event.v1",
  "contract_version": "agent-eval.contract.v1",
  "sequence": 1,
  "backend": "codex-cli",
  "source": "stdout.raw.jsonl",
  "event_type": "tool_call",
  "tool": "shell",
  "command": "rg -n PermissionService src"
}
```

Stable event types:

```text
assistant_message
assistant_delta
tool_request
tool_call
tool_output
tool_result
usage
permission
session_idle
error
unknown
```

Validators must use normalized events rather than raw Codex or Copilot traces.
