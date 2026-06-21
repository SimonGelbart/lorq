# Deterministic `run --no-judge`

`run --no-judge` produces a run-shard package from the deterministic conformance benchmark without invoking a judge or real LLM.

```bash
dotnet run --project dotnet/src/Lorq.Cli -- \
  run \
  --no-judge \
  --suite-root fixtures/conformance/deterministic-orchestration \
  --out results/dotnet-run-shard/shard-001
```

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

The default deterministic fake adapter reads `fixtures/fake-agent.yaml` under the suite root. External file adapters are supported through `--adapter-command` when they implement the file-adapter protocol. Use `adapter conformance` first when wiring a new external wrapper.

## Defaults

When omitted, the command derives:

- `--shard-id` from the `--out` directory name.
- `--package-id deterministic-benchmark`.
- `--benchmark benchmark.yaml` under `--suite-root`.
- `--adapter-fixture fixtures/fake-agent.yaml` under `--suite-root`.

Only `--no-judge` is supported by this command. Judgement is a separate package operation.

## Workspace materialization

The command creates a disposable workspace per planned cell before invoking the adapter. For the deterministic conformance suite, a case `repo:` value resolves through `eval.config.yaml` to a local fixture repository, which is copied into the cell workspace.

By default, materialized workspaces are written beside the output shard as `<out>.workspaces/<cell-id>/`. Use `--work-root <path>` to choose a dedicated workspace root.

Mode files may declare `materialize.copy` entries. Each `from` path is resolved from `--suite-root` and copied to the requested `to` path inside the cell workspace.

## Evidence boundary

Each generated cell contains a full adapter evidence file at:

```text
runs/<shard>/cells/<cell>/adapter.evidence.json
```

The evidence uses `lorq.file-adapter-evidence.v1alpha1` and records adapter identity, status, final-answer path, usage, timing, process output paths, trace events, artifacts, integrity warnings, and diagnostics.

## Current limitations

- Setup commands are not executed yet.
- Git worktree/clone checkout and dirty-policy enforcement are not implemented yet.
- Codex and Copilot runtime integrations are optional adapter smoke work, not part of the deterministic gate. See `../../docs/how-to/run-runtime-smoke.md`.
