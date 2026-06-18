# .NET deterministic full-loop parity

Increment 2 freezes the .NET package model against the Python v0 deterministic migration baseline.

The .NET loop covered in this increment is intentionally package-only:

```bash
dotnet run --project dotnet/src/Lorq.Cli -- \
  merge-shards \
  fixtures/golden/deterministic-orchestration/shard-001 \
  fixtures/golden/deterministic-orchestration/shard-002 \
  --out internal/generated/dotnet-full-loop/experiment-001 \
  --package-id deterministic-benchmark \
  --benchmark fixtures/conformance/deterministic-orchestration/benchmark.yaml

dotnet run --project dotnet/src/Lorq.Cli -- \
  judge-package \
  internal/generated/dotnet-full-loop/experiment-001 \
  --name judge-primary \
  --fixture fixtures/conformance/deterministic-orchestration/fixtures/fake-judge.yaml

dotnet run --project dotnet/src/Lorq.Cli -- \
  report-package \
  internal/generated/dotnet-full-loop/experiment-001 \
  --primary-judgement judge-primary
```

## Parity rule

The generated package must be byte-stable against:

```text
fixtures/golden/deterministic-orchestration/experiment-001
```

The parity test compares the complete generated experiment package, not only selected summaries. This includes:

- `experiment.yaml`
- copied run-shard evidence under `runs/`
- rebuilt `.lorq/` indexes
- deterministic judgement pass files
- canonical report files
- per-case review packs

## Scope boundary

This does not mean .NET can run adapters yet. Increment 2 only proves that .NET owns the package model and can reproduce the frozen migration package from deterministic shard evidence.

Future increments may implement `lorq run`, external adapters, and industrial runtime integrations only after they preserve this package contract.
