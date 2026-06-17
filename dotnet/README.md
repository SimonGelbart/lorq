# .NET LORQ implementation

This directory contains the future LORQ v1 implementation.

The Python v0 deterministic orchestration baseline is now frozen under `fixtures/golden/deterministic-orchestration/`. The .NET work starts from package IO and validation against that baseline before adding run, merge, judge, report, or industrial adapters.

## Current projects

- `Lorq.Cli` - command-line entry point. Currently exposes validation-only bootstrap commands.
- `Lorq.Core` - experiment, run shard, merge-input, package, judgement, and report domain model plus package validation.
- `Lorq.Reporting` - JSON command summary shaping.
- `Lorq.Adapters.Copilot` - reserved for the first-class industrial Copilot SDK adapter.
- `Lorq.Adapters.Process` - reserved for one-shot file-based external adapter protocol and Codex process adapter foundation.

## Current validation commands

```bash
cd dotnet
dotnet build Lorq.slnx
dotnet run --project tests/Lorq.Core.Tests
```

Validate a frozen package:

```bash
dotnet run --project src/Lorq.Cli -- \
  validate-package ../fixtures/golden/deterministic-orchestration/experiment-001
```

Validate merge inputs for conflict checks:

```bash
dotnet run --project src/Lorq.Cli -- \
  validate-merge-inputs \
  ../fixtures/conformance/deterministic-orchestration/edge-fixtures/duplicate-cell-conflict/shard-a \
  ../fixtures/conformance/deterministic-orchestration/edge-fixtures/duplicate-cell-conflict/shard-b
```

See `docs/package-validation.md` for the validator scope and stable error codes.

## Not implemented yet

- `lorq run`
- `lorq merge`
- `lorq judge`
- `lorq report`
- real Codex or Copilot integration

Those will be implemented only after the package model and fixture compatibility remain stable.
