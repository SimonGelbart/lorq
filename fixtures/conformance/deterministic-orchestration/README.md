# Deterministic orchestration migration benchmark

This fixture is the Python v0 bridge toward the frozen LORQ migration benchmark.
It uses no real LLM calls. The agent and judge outputs are read from deterministic fixture files so the benchmark tests orchestration, evidence capture, shard export, and integrity handling rather than model quality.

## Contents

```text
cases/                  Three small benchmark cases.
modes/                  baseline, graphify, and graphify-plus fixture modes.
fixtures/fake-agent.yaml Deterministic adapter cell outputs.
fixtures/fake-judge.yaml Deterministic judge scores.
fake_project/           Tiny repository copied into each run worktree.
benchmark.yaml          Target benchmark shape and remaining edge fixtures.
```

## Current runnable slice

The current slice can produce two Python v0 result shards, export them into LORQ v1-alpha run-shard packages, and merge those shards into a migration experiment package:

```bash
cd python
PYTHONPATH=. python -m eval_runner.cli \
  --suite-root ../fixtures/conformance/deterministic-orchestration \
  --repo ../fixtures/conformance/deterministic-orchestration/fake_project \
  --modes baseline,graphify,graphify-plus \
  --cases successful-comparison \
  --agent-profile deterministic-fake \
  --out ../../results/deterministic-benchmark/python-results/shard-001 \
  --worktree-root ../../results/deterministic-benchmark/worktrees/shard-001 \
  --dirty-policy allow \
  --cleanup never

PYTHONPATH=. python -m eval_runner.cli \
  --suite-root ../fixtures/conformance/deterministic-orchestration \
  --out ../../results/deterministic-benchmark/python-results/shard-001 \
  --export-lorq-shard ../../results/deterministic-benchmark/shard-001 \
  --lorq-shard-id shard-001 \
  --lorq-package-id deterministic-benchmark
```

Generate the second candidate shard by running `no-final-answer` for all modes, then `skipped-coverage` for only `baseline,graphify` with `--resume`; this intentionally leaves one omitted cell for coverage-review work in the future merge/report increment.


Merge the two exported shards:

```bash
cd python
PYTHONPATH=. python -m eval_runner.cli \
  --merge-lorq-shards ../../results/deterministic-benchmark/shard-001 ../../results/deterministic-benchmark/shard-002 \
  --lorq-merge-out ../../results/deterministic-benchmark/experiment-001 \
  --lorq-package-id deterministic-benchmark \
  --lorq-benchmark ../fixtures/conformance/deterministic-orchestration/benchmark.yaml
```

The merged package intentionally reports one missing expected cell: `skipped-coverage__graphify-plus__attempt-001`.

## Scope boundary

This benchmark fixture is not the final product implementation. Python v0 remains a deterministic baseline generator. .NET v1 will later consume the frozen outputs as migration fixtures.

## Negative merge fixtures

`edge-fixtures/duplicate-cell-conflict/` and `edge-fixtures/fingerprint-mismatch/` are intentionally committed tiny LORQ packages. They verify that the migration-only merge fails by default on duplicate cell ids and incompatible repository fingerprints.

## Frozen golden baseline

The committed full-loop expected outputs live under `fixtures/golden/deterministic-orchestration/`. They contain two run shards, one merged experiment package, one `judge-primary` deterministic judgement pass, canonical reports, and per-case review packs.
