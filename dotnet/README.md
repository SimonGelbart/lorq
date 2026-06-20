# .NET LORQ implementation

This directory contains the .NET LORQ implementation.

The .NET implementation now owns the deterministic package loop against the frozen Python v0 baseline: run shards, merge, deterministic judgement attachment, report rendering, package validation, index rebuilding, and file-adapter evidence contracts.

## Projects

- `Lorq.Cli` — command-line entry point, hosted composition, parsing, command dispatch, and JSON console summaries.
- `Lorq.Core` — package model, validation, merge, index rebuilds, deterministic judgement attachment, and run package services.
- `Lorq.Reporting` — deterministic package report rendering.
- `Lorq.Adapters.Process` — file-based one-shot adapter protocol contracts, deterministic fake file adapter, external process adapter, and Codex-oriented wrapper profile.
- `Lorq.Adapters.Copilot` — reserved for the first-class industrial Copilot SDK adapter.

## Build and test

```bash
dotnet build Lorq.slnx
dotnet test --solution Lorq.slnx --disable-logo --minimum-expected-tests 42
```

## Common commands

Run one deterministic no-judge shard:

```bash
dotnet run --project src/Lorq.Cli -- \
  run \
  --no-judge \
  --suite-root ../fixtures/conformance/deterministic-orchestration \
  --out ../internal/generated/dotnet-run-shard/shard-001
```

Validate a package:

```bash
dotnet run --project src/Lorq.Cli -- \
  validate-package ../fixtures/golden/deterministic-orchestration/experiment-001
```

Merge deterministic run shards:

```bash
dotnet run --project src/Lorq.Cli -- \
  merge-shards \
  ../fixtures/golden/deterministic-orchestration/shard-001 \
  ../fixtures/golden/deterministic-orchestration/shard-002 \
  --out ../internal/generated/dotnet-merge-writer/experiment-001 \
  --package-id deterministic-benchmark \
  --benchmark ../fixtures/conformance/deterministic-orchestration/benchmark.yaml
```

Attach deterministic fake judgement:

```bash
dotnet run --project src/Lorq.Cli -- \
  judge-package \
  ../internal/generated/dotnet-merge-writer/experiment-001 \
  --name judge-primary \
  --fixture ../fixtures/conformance/deterministic-orchestration/fixtures/fake-judge.yaml
```

Render deterministic reports:

```bash
dotnet run --project src/Lorq.Cli -- \
  report-package \
  ../internal/generated/dotnet-merge-writer/experiment-001 \
  --primary-judgement judge-primary
```

## Documentation

- `../docs/how-to/dotnet-deterministic-loop.md` — end-to-end deterministic loop.
- `../docs/reference/cli.md` — command reference.
- `../docs/reference/package-validation.md` — validation scope and stable error codes.
- `docs/cli-architecture.md` — CLI composition and command handler boundary.
- `docs/run-no-judge.md` — deterministic run-shard behavior.
- `docs/adapters/file-adapter-protocol.md` — file-adapter evidence contract.
- `docs/adapters/codex-file-adapter-profile.md` — Codex wrapper profile.
- `docs/engineering-guidelines.md` — architecture, style, testing, and pattern rules.

## Current boundaries

- The default path remains deterministic and no-token.
- `--adapter-command` can invoke external one-shot file adapters.
- `--adapter-profile codex-cli` passes Codex wrapper metadata to an external adapter; LORQ itself does not call Codex.
- General Git checkout/worktree orchestration, setup command execution, direct Codex runtime behavior, and Copilot SDK runtime behavior remain future adapter/runtime work.
