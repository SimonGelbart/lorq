# .NET LORQ implementation

This directory contains the future LORQ v1 implementation.

The Python v0 deterministic orchestration baseline is now frozen under `fixtures/golden/deterministic-orchestration/`. The .NET work starts from package IO and validation against that baseline before adding run, merge, judge, report, or industrial adapters.

## Engineering standard

New .NET code follows clean architecture boundaries, object-calisthenics discipline in domain code, modern C# where it adds value, deliberate pattern use, and TUnit tests through Microsoft.Testing.Platform on .NET 10. See `docs/engineering-guidelines.md`.

## Current projects

- `Lorq.Cli` - command-line entry point. Currently exposes deterministic run-shard export, package validation, index rebuild, deterministic merge, deterministic judgement, and deterministic report bootstrap commands.
- `Lorq.Core` - experiment, run shard, merge-input, package, judgement, and report domain model plus package validation.
- `Lorq.Reporting` - JSON command summary shaping.
- `Lorq.Adapters.Copilot` - reserved for the first-class industrial Copilot SDK adapter.
- `Lorq.Adapters.Process` - file-based one-shot adapter protocol contracts, deterministic fake file adapter, external one-shot process adapter, and Codex-oriented process profile foundation.

## Current validation commands

```bash
cd dotnet
dotnet build Lorq.slnx
dotnet test --solution Lorq.slnx --disable-logo --minimum-expected-tests 39
```


Run one deterministic no-judge shard:

```bash
dotnet run --project src/Lorq.Cli -- \
  run \
  --no-judge \
  --suite-root ../fixtures/conformance/deterministic-orchestration \
  --out ../internal/generated/dotnet-run-shard/shard-001
```

Validate a frozen package:

```bash
dotnet run --project src/Lorq.Cli -- \
  validate-package ../fixtures/golden/deterministic-orchestration/experiment-001
```


Rebuild package indexes from an existing package into a target package root:

```bash
dotnet run --project src/Lorq.Cli -- \
  rebuild-indexes \
  ../fixtures/golden/deterministic-orchestration/experiment-001 \
  ../internal/generated/dotnet-index-rebuild/experiment-001
```


Merge frozen deterministic run shards into an experiment package:

```bash
dotnet run --project src/Lorq.Cli -- \
  merge-shards \
  ../fixtures/golden/deterministic-orchestration/shard-001 \
  ../fixtures/golden/deterministic-orchestration/shard-002 \
  --out ../internal/generated/dotnet-merge-writer/experiment-001 \
  --package-id deterministic-benchmark \
  --benchmark ../fixtures/conformance/deterministic-orchestration/benchmark.yaml
```


Attach a deterministic fake judgement pass to a merged experiment package:

```bash
dotnet run --project src/Lorq.Cli -- \
  judge-package \
  ../internal/generated/dotnet-merge-writer/experiment-001 \
  --name judge-primary \
  --fixture ../fixtures/conformance/deterministic-orchestration/fixtures/fake-judge.yaml
```


Render canonical deterministic report artifacts from a judged package:

```bash
dotnet run --project src/Lorq.Cli -- \
  report-package \
  ../internal/generated/dotnet-merge-writer/experiment-001 \
  --primary-judgement judge-primary
```

Validate merge inputs for conflict checks:

```bash
dotnet run --project src/Lorq.Cli -- \
  validate-merge-inputs \
  ../fixtures/conformance/deterministic-orchestration/edge-fixtures/duplicate-cell-conflict/shard-a \
  ../fixtures/conformance/deterministic-orchestration/edge-fixtures/duplicate-cell-conflict/shard-b
```

See `docs/package-validation.md` for the validator scope and stable error codes. See `docs/adapters/file-adapter-protocol.md` for the file-based adapter contract. See `docs/engineering-guidelines.md` for .NET architecture, style, testing, and pattern rules.

## Full-loop parity

Increment 2 freezes .NET package parity against the Python v0 deterministic migration baseline. The full package-only loop is:

```bash
merge-shards -> judge-package -> report-package -> validate-package
```

The TUnit suite compares the complete generated experiment package with `fixtures/golden/deterministic-orchestration/experiment-001`. See `docs/full-loop-parity.md`.

## Not implemented yet

- general `lorq run` execution orchestration beyond the deterministic fake file adapter slice
- production `lorq judge` backed by real/external judge adapters
- production/general `lorq report` beyond the deterministic frozen package report bootstrap
- real Codex or Copilot integration

Those will be implemented only after the package model, merge writer, and fixture compatibility remain stable.

## Documentation

- `docs/cli-architecture.md` describes the command handler boundary for `Lorq.Cli`.
- `docs/run-no-judge.md` describes the deterministic `run --no-judge` slice.


## External file adapters

Increment 3 includes the first external one-shot file adapter runner. `run --no-judge` can pass `--adapter-command` plus repeated `--adapter-arg` values. The external process receives `LORQ_ADAPTER_REQUEST` and `LORQ_ADAPTER_EVIDENCE` environment variables and must write the full `lorq.file-adapter-evidence.v1alpha1` contract.

The Codex-oriented adapter profile is now available through `--adapter-profile codex-cli`. It configures a wrapper process with Codex metadata but intentionally does not invoke real Codex from LORQ; see `docs/adapters/codex-file-adapter-profile.md`.
