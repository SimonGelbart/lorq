# LORQ reusable agent rules

These rules apply to every AI-assisted increment on **LORQ — Ledger for Orchestrated Run Quality**.

## Product direction

- LORQ is a shard-safe orchestration and evidence ledger for agent, tool, and skill evaluations.
- LORQ is not an LLM intelligence benchmark.
- Treat `docs/roadmap/LORQ_PRACTICAL_ROADMAP.md` as the delivery source of truth.
- Treat `docs/roadmap/LORQ_ROADMAP_GOAL_REFINEMENT.md` as the product compass.
- Preserve Python v0 as the baseline/prototype until it produces a frozen deterministic orchestration benchmark.
- Do not let Python v0 become the final product.
- Do not start broad .NET implementation before the frozen Python orchestration benchmark exists.

## Target product loop

```bash
lorq run --no-judge --out shard-001
lorq run --no-judge --out shard-002
lorq merge shard-001 shard-002 --out experiment-001
lorq judge --input experiment-001 --name judge-primary
lorq report --input experiment-001 --primary-judgement judge-primary
```

## Current roadmap discipline

Before changing code or fixtures:

1. Inspect the relevant repo layout and roadmap files.
2. Identify the practical roadmap increment being advanced.
3. Confirm the work fits that increment.
4. Amend the practical roadmap if the discovered plan needs to change.

At the end of each increment, report:

- current practical-roadmap position
- what changed
- what validation was run
- what remains
- next increment

## Source-control boundary

- `lorq/` is the candidate GitHub repository.
- `internal/` is non-source-controlled workspace material for handoffs, scratch files, scripts, logs, generated outputs, extracted archives, and session notes.
- Do not put scratch files, generated logs, extracted archives, one-off handoffs, local caches, or temporary run outputs inside `lorq/`.
- Generated fixtures may live under `lorq/fixtures/` only when they are intentional, documented, and suitable for source control.

## Required documentation and changelog updates

- Every increment must update `CHANGELOG.md`.
- Every behavior, schema, package, CLI, adapter, fixture, report, or roadmap change must be documented.
- JSON is canonical for reports; Markdown is a rendering, not the source of truth.

## Python v0 migration baseline rules

- Python v0 work should focus on the frozen deterministic orchestration benchmark.
- The migration benchmark must use fake deterministic agent and fake deterministic judge adapters.
- The benchmark must test orchestration, package integrity, merge behavior, judgement separation, and report rendering.
- The migration gate must not require real LLM calls, Codex quality, or Copilot quality.
- Adapter output must be a full evidence contract, not just a final answer.

## Adapter direction

- External adapters should use a file-based one-shot protocol first.
- A run-cell adapter invocation should write canonical output files to its output directory.
- Stdout and stderr are logs only; they are not the canonical evidence contract.
- Copilot SDK is the first industrial built-in adapter target after the frozen benchmark foundation.
- Codex is used heavily for local and stress testing through a process/CLI adapter.

## Validation expectation

Run the current applicable validation at the end of each increment. For the Python v0 baseline, prefer:

```bash
cd python
python -m pytest
PYTHONPATH=. python -m eval_runner.cli --suite-root .. --validate-config
PYTHONPATH=. python -m eval_runner.cli --run-conformance
```

If validation cannot run, record why in the changelog and session handoff.
