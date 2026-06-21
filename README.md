# LORQ

**LORQ** means **Ledger for Orchestrated Run Quality**.

LORQ is a shard-safe orchestration ledger for agent, tool, and skill evaluations. It captures execution evidence, merges split runs into experiment packages, attaches judgement passes, and produces decision-grade reports based on quality, time, price, and integrity.

LORQ is not primarily an LLM intelligence benchmark. The product goal is orchestration, evidence capture, package integrity, adapter conformance, and decision-grade reporting.

## Current implementation status

The repository contains two implementation tracks:

- `python/` is the Python v0 baseline that produced the frozen deterministic conformance fixtures.
- `dotnet/` is the .NET product implementation. It now covers the deterministic package loop: run shards, merge, deterministic judgement attachment, report rendering, package validation, index rebuilds, and file-adapter evidence contracts.

The current .NET path is still deterministic and no-token by default. Real Codex and Copilot runtime integrations remain adapter work after the deterministic package and orchestration contracts stay stable.

## Repository layout

```text
lorq/
  cases/             Shared benchmark case definitions.
  modes/             Shared mode definitions.
  pricing/           Shared pricing profiles.
  execution/         Shared execution assets and skill/tool materialization inputs.
  schemas/           Shared JSON schemas.
  prompt_styles/     Shared prompt style definitions.
  rubrics/           Shared validation and evaluation rubrics.
  repositories/      Shared repository definitions.
  examples/          Shared example suites and fixture repos.
  fixtures/          Intentional conformance, golden, and generated fixtures.
  docs/              User, reference, architecture, and roadmap documentation.
  python/            Python v0 baseline implementation.
  dotnet/            .NET LORQ implementation.
```

## .NET quick validation

```bash
cd dotnet
dotnet build Lorq.slnx
dotnet test --solution Lorq.slnx --disable-logo
```

## Deterministic package loop

From the repository root, the deterministic .NET loop is:

```bash
dotnet run --project dotnet/src/Lorq.Cli -- \
  run \
  --no-judge \
  --suite-root fixtures/conformance/deterministic-orchestration \
  --out internal/generated/dotnet-run-shard/shard-001

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

dotnet run --project dotnet/src/Lorq.Cli -- \
  validate-package internal/generated/dotnet-full-loop/experiment-001
```

See `docs/how-to/dotnet-deterministic-loop.md` for the full walkthrough.

## Documentation

Documentation follows a Diátaxis-inspired split:

- `docs/how-to/` for task-oriented procedures.
- `docs/reference/` for command, package, and validation contracts.
- `docs/explanation/` for product and architecture background.
- `docs/decisions/` for architecture decision records.
- `docs/roadmap/` for product direction and delivery sequencing.

Start with `docs/README.md` for the documentation map.
