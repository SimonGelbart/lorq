# Codex CLI file-adapter smoke wrapper

This example adapts a local Codex CLI invocation to the LORQ one-shot file-adapter protocol.

It is intended for optional local smoke checks only. It is not part of deterministic CI and should not be used to update golden fixtures automatically.

```bash
lorq run --no-judge \
  --suite-root fixtures/conformance/deterministic-orchestration \
  --out ../results/codex-smoke/shard-001 \
  --adapter-command python3 \
  --adapter-arg examples/adapters/codex-cli-file-adapter/lorq_codex_cli_adapter.py \
  --adapter-profile codex-cli \
  --codex-command codex \
  --codex-arg exec \
  --codex-arg --json
```

The wrapper reads `LORQ_ADAPTER_REQUEST`, invokes the command identified by `LORQ_CODEX_COMMAND` and `LORQ_CODEX_ARGUMENTS`, and writes `adapter-evidence.json` plus referenced answer/stdout/stderr files.
