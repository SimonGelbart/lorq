# .NET deterministic full-loop parity

The .NET implementation preserves byte-stable parity with the frozen deterministic package baseline.

The package loop is:

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

The parity test compares the complete generated experiment package, including:

- `experiment.yaml`
- copied run-shard evidence under `runs/`
- rebuilt `.lorq/` indexes
- deterministic judgement pass files
- canonical report files
- per-case review packs

## Boundary

Parity protects the package contract. Real adapter behavior can evolve independently as long as normalized evidence still produces valid packages and reports.
