# Backend contract

Agent backends are adapters that receive:

```text
worktree path
rendered prompt
run output directory
```

They must write at least:

```text
prompt.txt
answer.md
stdout.raw.jsonl or stdout.raw.txt
stderr.txt
agent.summary.json
```

They should also produce normalized events directly or allow the shared normalizer to produce `events.normalized.jsonl`.

`agent.summary.json` should include:

```json
{
  "backend": "codex-cli",
  "output_format": "codex-jsonl",
  "exit_code": 0,
  "timed_out": false,
  "elapsed_ms": 1000,
  "ok": true,
  "error_category": null,
  "usage": {},
  "counts": {}
}
```

Supported backend status categories include:

```text
command_not_found
timeout
nonzero_exit
missing_python_package
runtime_error
```
