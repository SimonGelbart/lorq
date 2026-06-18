# Deterministic `run --no-judge`

Increment 3 adds the first narrow .NET runtime slice:

```bash
dotnet run --project dotnet/src/Lorq.Cli -- \
  run \
  --no-judge \
  --suite-root fixtures/conformance/deterministic-orchestration \
  --out internal/generated/dotnet-run-shard/shard-001
```

The command is intentionally limited to the deterministic migration benchmark. It does not run Codex, Copilot, or any real LLM. It proves the file-adapter boundary and run-shard package writer before broader runtime orchestration exists.

## Flow

```text
CLI run command
→ planned shard selection from benchmark.yaml
→ one file-adapter request per cell
→ deterministic fake file adapter or external one-shot process adapter
→ full adapter evidence contract
→ run-shard package writer
→ .lorq index rebuild
```

## Defaults

When omitted, the command derives:

- `--shard-id` from the `--out` directory name.
- `--package-id deterministic-benchmark`.
- `--benchmark benchmark.yaml` under `--suite-root`.
- `--adapter-fixture fixtures/fake-agent.yaml` under `--suite-root`.

Only `--no-judge` is supported in this slice. Judgement attachment remains a separate package operation.

## External process adapter

`run --no-judge` can now launch an external file adapter process for each planned cell:

```bash
dotnet run --project dotnet/src/Lorq.Cli -- \
  run \
  --no-judge \
  --suite-root fixtures/conformance/deterministic-orchestration \
  --out internal/generated/dotnet-run-shard/shard-001 \
  --adapter-command /path/to/adapter \
  --adapter-arg --optional-adapter-flag
```

The deterministic fake adapter remains the default no-token implementation. Use `--adapter-command` only for file-protocol adapters that read `LORQ_ADAPTER_REQUEST` and write `LORQ_ADAPTER_EVIDENCE`.

## Evidence boundary

Each generated cell contains a full adapter evidence file at:

```text
runs/<shard>/cells/<cell>/adapter.evidence.json
```

The evidence uses `lorq.file-adapter-evidence.v1alpha1` and records adapter identity, status, final-answer path, usage, timing, process output paths, trace events, artifacts, integrity warnings, and diagnostics. It is not just a final answer.

## Current limitation

This command is still deterministic fixture orchestration only. General case/mode loading, workspace materialization, repository checkout, Codex, and Copilot remain future Increment 3 work.
