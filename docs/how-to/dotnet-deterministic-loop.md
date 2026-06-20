# Run the deterministic .NET loop

This guide runs the no-token .NET package loop against the frozen deterministic fixtures.

## Prerequisites

- .NET 10 SDK.
- A checkout of this repository.
- Commands run from the repository root unless noted otherwise.

## 1. Build and test

```bash
cd dotnet
dotnet build Lorq.slnx
dotnet test --solution Lorq.slnx --disable-logo --minimum-expected-tests 42
cd ..
```

## 2. Produce a deterministic run shard

```bash
dotnet run --project dotnet/src/Lorq.Cli -- \
  run \
  --no-judge \
  --suite-root fixtures/conformance/deterministic-orchestration \
  --out internal/generated/dotnet-run-shard/shard-001
```

The default adapter is deterministic and does not call a real LLM.

## 3. Merge existing golden shards

```bash
dotnet run --project dotnet/src/Lorq.Cli -- \
  merge-shards \
  fixtures/golden/deterministic-orchestration/shard-001 \
  fixtures/golden/deterministic-orchestration/shard-002 \
  --out internal/generated/dotnet-full-loop/experiment-001 \
  --package-id deterministic-benchmark \
  --benchmark fixtures/conformance/deterministic-orchestration/benchmark.yaml
```

## 4. Attach deterministic judgement

```bash
dotnet run --project dotnet/src/Lorq.Cli -- \
  judge-package \
  internal/generated/dotnet-full-loop/experiment-001 \
  --name judge-primary \
  --fixture fixtures/conformance/deterministic-orchestration/fixtures/fake-judge.yaml
```

## 5. Render the report

```bash
dotnet run --project dotnet/src/Lorq.Cli -- \
  report-package \
  internal/generated/dotnet-full-loop/experiment-001 \
  --primary-judgement judge-primary
```

The command writes:

```text
reports/report.json
reports/report.md
reports/cases/<case-id>/case-review.json
reports/cases/<case-id>/case-review.md
.lorq/report.json
```

## 6. Validate the package

```bash
dotnet run --project dotnet/src/Lorq.Cli -- \
  validate-package internal/generated/dotnet-full-loop/experiment-001
```

A valid package returns a JSON summary with `ok: true`.
