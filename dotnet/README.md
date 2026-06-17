# .NET LORQ implementation

This directory contains the future LORQ v1 implementation.

The Python v0 deterministic orchestration baseline is now frozen under `fixtures/golden/deterministic-orchestration/`. The .NET work starts from package IO and validation against that baseline before adding run, merge, judge, report, or industrial adapters.

## Engineering standard

New .NET code follows clean architecture boundaries, object-calisthenics discipline in domain code, modern C# where it adds value, deliberate pattern use, and TUnit tests through Microsoft.Testing.Platform on .NET 10. See `docs/engineering-guidelines.md`.

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
dotnet test --solution Lorq.slnx --disable-logo --minimum-expected-tests 7
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

Validate merge inputs for conflict checks:

```bash
dotnet run --project src/Lorq.Cli -- \
  validate-merge-inputs \
  ../fixtures/conformance/deterministic-orchestration/edge-fixtures/duplicate-cell-conflict/shard-a \
  ../fixtures/conformance/deterministic-orchestration/edge-fixtures/duplicate-cell-conflict/shard-b
```

See `docs/package-validation.md` for the validator scope and stable error codes. See `docs/engineering-guidelines.md` for .NET architecture, style, testing, and pattern rules.

## Not implemented yet

- `lorq run`
- `lorq merge`
- `lorq judge`
- `lorq report`
- real Codex or Copilot integration

Those will be implemented only after the package model and fixture compatibility remain stable.
