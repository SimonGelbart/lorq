# LORQ

**LORQ** means **Ledger for Orchestrated Run Quality**.

LORQ is a shard-safe orchestration ledger for agent, tool, and skill evaluations. It captures execution evidence, merges split runs into experiment packages, attaches judgement passes, and produces decision-grade reports based on quality, time, price, and integrity.

LORQ is not primarily an LLM intelligence benchmark. The product goal is orchestration, evidence capture, package integrity, adapter conformance, and decision-grade reporting.

## Repository layout

```text
lorq/
  .agents/           Source-controlled reusable AI-agent operating rules.
  cases/             Shared benchmark case definitions.
  modes/             Shared mode definitions.
  pricing/           Shared pricing profiles.
  execution/         Shared execution assets and skill/tool materialization inputs.
  schemas/           Shared JSON schemas.
  prompt_styles/     Shared prompt style definitions.
  rubrics/           Shared validation and evaluation rubrics.
  repositories/      Shared repository definitions.
  examples/          Shared example suites and fixture repos.
  fixtures/          Intentional conformance/golden/generated fixtures.
  docs/              Product roadmap, operating rules, architecture, and Python v0 docs.
  python/            Current Python v0 prototype and tests.
  dotnet/            Future .NET LORQ v1 implementation skeleton.
```

## Current state

The repository currently contains the Python v0 prototype reorganized into the target monorepo architecture.

The immediate practical goal is to freeze a deterministic orchestration benchmark in Python v0, then use that as the comparable baseline for the .NET implementation.

## Quick validation

```bash
cd python
python -m pytest
PYTHONPATH=. python -m eval_runner.cli --suite-root .. --validate-config
PYTHONPATH=. python -m eval_runner.cli --run-conformance
```

## Source-control boundary

Only the `lorq/` directory is intended to become the GitHub repository.

In delivery artifacts, `internal/` is reserved for handoffs, scratch scripts, session logs, and misc agent material. Do not commit `internal/` by default.
