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
→ per-cell workspace planning
→ local repository copy materialization
→ mode materialize.copy application
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

## Workspace materialization

`run --no-judge` now creates a disposable workspace per planned cell before invoking the adapter. For the deterministic conformance suite, the case `repo:` value resolves through `eval.config.yaml` to `fake_project`, which is copied into the cell workspace.

By default, materialized workspaces are written beside the output shard as `<out>.workspaces/<cell-id>/`, keeping scratch material outside the package root. Use `--work-root <path>` to choose a dedicated workspace root; relative paths are resolved from the current process directory and then grouped by shard id and cell id.

Mode files may declare `materialize.copy` entries. Each `from` path is resolved from `--suite-root` and copied to the requested `to` path inside the cell workspace. Setup commands are still not executed in this slice.

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

This command is still deterministic fixture orchestration only. Local repository copy materialization exists, but Git worktree/clone checkout, dirty-policy enforcement, setup command execution, Codex, and Copilot remain future Increment 3 work.
