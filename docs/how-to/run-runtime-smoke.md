# Run an optional runtime smoke adapter

Use runtime smoke adapters after the deterministic fake loop and file-adapter conformance pass. Smoke adapters prove a real runtime boundary can produce LORQ-compliant evidence, but they are not deterministic CI gates.

Keep generated outputs under `internal/generated/` or another local workspace. Do not update golden fixtures from a real runtime smoke run.

## Codex CLI smoke

The Codex CLI smoke path uses the regular file-adapter process boundary. LORQ launches a wrapper, the wrapper may invoke Codex, and the wrapper writes normal `adapter-evidence.json`.

```bash
lorq run --no-judge \
  --suite-root fixtures/conformance/deterministic-orchestration \
  --out ../internal/generated/codex-smoke/shard-001 \
  --adapter-command python3 \
  --adapter-arg examples/adapters/codex-cli-file-adapter/lorq_codex_cli_adapter.py \
  --adapter-profile codex-cli \
  --codex-command codex \
  --codex-arg exec \
  --codex-arg --json
```

`--adapter-command` names the wrapper. `--codex-command` and repeated `--codex-arg` values are passed to the wrapper through profile environment variables.

Before using the wrapper in `run --no-judge`, run conformance:

```bash
lorq adapter conformance \
  --adapter-command python3 \
  --adapter-arg examples/adapters/codex-cli-file-adapter/lorq_codex_cli_adapter.py \
  --out ../internal/generated/codex-smoke-conformance
```

## Copilot SDK smoke boundary

The Copilot SDK boundary is represented as a file-adapter profile named `copilot-sdk`. The profile injects metadata into an external wrapper process. The wrapper is still responsible for normal request/evidence files.

```bash
lorq run --no-judge \
  --suite-root fixtures/conformance/deterministic-orchestration \
  --out ../internal/generated/copilot-smoke/shard-001 \
  --adapter-command <copilot-wrapper> \
  --adapter-profile copilot-sdk
```

This branch intentionally does not add a hard dependency on the Copilot SDK. A future wrapper can use SDK-specific types internally as long as it normalizes results into the file-adapter evidence contract.

## Runtime metadata

Smoke adapters should record provider/runtime metadata under `adapter.runtime` in evidence. Package and report code may preserve that data, but provider-specific fields must not become package validation assumptions.

Stable metadata fields include:

- `provider`
- `runtime`
- `runtime_version`
- `profile`
- `command`
- `permission_profile`
- `output_format`
- `extensions`

## Boundary rules

- Deterministic CI remains fake/no-token.
- Real runtime smoke output is local diagnostic data.
- Runtime wrappers must pass file-adapter conformance before package runs.
- Golden fixtures should only be updated from deterministic fixture workflows.
