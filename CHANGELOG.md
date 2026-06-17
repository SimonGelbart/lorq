# Changelog

All notable changes to LORQ should be documented here.

## 2026-06-18 - Increment 1 fixture: migration package merge

### Roadmap position

Current increment: Increment 1, Python frozen conformance baseline. This session added the migration-only package merge needed to combine deterministic run shards before package-level judgement and reporting.

### Added

- Added Python v0 `--merge-lorq-shards` with `--lorq-merge-out`, `--lorq-benchmark`, `--lorq-package-id`, and `--lorq-allow-incompatible` for v1-alpha run-shard merge.
- Added `merge_lorq_run_shards` to build a merged experiment package with copied shard payloads, rebuilt `.lorq/cells/`, merged coverage, fingerprint, integrity, and merge-log indexes.
- Added expected-cell coverage from deterministic `benchmark.yaml`, surfacing intentionally omitted cells as `missing_cells`.
- Added default merge failure behavior for duplicate cell IDs and incompatible repository fingerprints.
- Added merge-result schema under `schemas/lorq-package-merge-result.v1alpha.schema.json`.
- Added tests for successful merge coverage, duplicate-cell failure, and fingerprint-mismatch failure.

### Changed

- Extended LORQ package integrity output to preserve adapter-level integrity warnings from `adapter.evidence.json`.
- Updated deterministic fixture and Python package docs to include the shard merge command.

### Validation

Executed during this increment:

- `cd python && python -m pytest -q` -> 80 passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --suite-root .. --validate-config` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --suite-root ../fixtures/conformance/deterministic-orchestration --validate-config` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --run-conformance` -> passed.
- Merged deterministic `shard-001` and `shard-002` candidate packages into `internal/generated/deterministic-benchmark/experiment-001`; merge returned 8 present cells, 9 expected cells, and 1 missing expected cell.

### Known limitations

- Package-level deterministic judgement attachment, canonical `reports/report.json`, Markdown report rendering, per-case packs, duplicate/fingerprint edge fixture packages, and committed golden outputs are still pending.
- No roadmap amendment was required.

## 2026-06-18 - Increment 1 fixture: deterministic adapter and judge

### Roadmap position

Current increment: Increment 1, Python frozen conformance baseline. This session added the deterministic fake adapter and fake judge fixture pair needed to start generating the no-LLM orchestration benchmark shards.

### Added

- Added Python v0 `deterministic-fake` agent backend that reads per-cell fixture data and writes full evidence files, including `adapter.evidence.json`, normalized events, usage/counts, timing, artifact refs, and integrity warnings.
- Added Python v0 deterministic fake judge support through `judge.backend: deterministic-fake` and `judge.fixture_file`.
- Added self-contained deterministic orchestration benchmark material under `fixtures/conformance/deterministic-orchestration/`, including cases, modes, prompt style, rubric, tiny fake repository, fake agent fixture, and fake judge fixture.
- Added v1-alpha fake agent and fake judge fixture schemas under `schemas/`.
- Added Python tests for deterministic fake agent output, missing-final-answer modeling, and deterministic fake judge output.

### Changed

- Extended config validation and JSON schemas to accept deterministic fake agent and judge fixture settings.
- Extended LORQ package export to copy `adapter.evidence.json` into cell evidence directories when present.
- Documented deterministic fake adapters in Python v0 backend and package-export docs.

### Validation

Executed during this increment:

- `cd python && python -m pytest -q` -> 77 passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --suite-root .. --validate-config` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --suite-root ../fixtures/conformance/deterministic-orchestration --validate-config` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --run-conformance` -> passed.
- Generated two candidate deterministic run-shard packages under `internal/generated/deterministic-benchmark/`: `shard-001` with 3 cells and `shard-002` with 5 cells.
- Ran a deterministic fake judge smoke test with `--judge --judge-backend deterministic-fake`.

### Known limitations

- The two-shard merge command, package-level judge attachment, canonical reports, per-case packs, duplicate conflict fixture, fingerprint mismatch fixture, and committed golden outputs are still pending.
- The intentionally omitted `skipped-coverage/graphify-plus/attempt-001` cell is documented in the benchmark plan but not yet surfaced as `missing_cells` because package-level merge/coverage expectation logic is future work.
- No roadmap amendment was required.

## 2026-06-18 - Increment 1 foundation: LORQ v1-alpha package export

### Roadmap position

Current increment: Increment 1, Python frozen conformance baseline. This session advanced the fixture foundation by adding a migration-only exporter from Python v0 results into the planned LORQ run-shard package shape.

### Added

- Added Python v0 `--export-lorq-shard` with `--lorq-shard-id` and `--lorq-package-id` to export existing run results into a v1-alpha LORQ run-shard package without invoking agents or judges.
- Added `eval_runner/lorq_package.py` to normalize Python v0 run records into `experiment.yaml`, `runs/<shard>/cells/`, `.lorq/cells/`, `.lorq/coverage.json`, `.lorq/fingerprints.json`, `.lorq/integrity.json`, and `.lorq/merge-log.json`.
- Added a v1-alpha `lorq.cell-evidence.v1alpha1` schema file under `schemas/`.
- Added fixture definition files under `fixtures/conformance/deterministic-orchestration/` for the frozen deterministic orchestration benchmark shape.
- Added Python tests for LORQ run-shard package export and missing-final-answer integrity warnings.
- Added Python v0 package export documentation.

### Changed

- Updated Python v0 docs index and README to include the migration-only LORQ package export path.
- Added LORQ v1-alpha migration fixture constants to Python v0 contracts.
- Imported `sys` in `eval_runner/agents.py` so the existing `{python}` command placeholder is resolvable when used.
- Updated fixture documentation to clarify the deterministic orchestration conformance directory.

### Validation

Executed during this increment:

- `cd python && python -m pytest tests/test_lorq_package.py -q` -> 2 passed.
- Python fake-agent smoke run plus `--export-lorq-shard` -> produced a v1-alpha package with 1 cell.

Full-suite validation is expected before packaging this artifact.

### Known limitations

- This is not the complete frozen benchmark yet. It does not implement two-shard merge, deterministic fake judge, judge fixtures, canonical `report.json`, Markdown rendering, per-case review packs, duplicate conflict fixtures, or fingerprint mismatch fixtures.
- Exported adapter timing still reflects the source Python v0 run; deterministic timing requires the next fake adapter increment.
- No roadmap amendment was required.

## 2026-06-18 - Increment 1 preparation: repo boundary and agent operating rules

### Roadmap position

Current increment: preparing for Increment 1, the Python frozen conformance baseline. This session preserved direction and repository hygiene before benchmark implementation.

### Added

- Added `.agents/` with reusable LORQ agent operating rules for roadmap alignment, source-control boundaries, validation, and deterministic benchmark discipline.
- Added `.agents/README.md` to explain why the rules are source-controlled and where session-specific material belongs.
- Added a session handoff under `internal/handoffs/` for non-source-controlled workspace continuity.

### Changed

- Documented `.agents/` in the root repository layout in `README.md`.

### Validation

Executed during this increment:

- `cd python && python -m pytest -q` -> 71 passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --suite-root .. --validate-config` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --run-conformance` -> passed.

### Known limitations

- No deterministic orchestration benchmark, fake agent adapter, fake judge adapter, or canonical v1 package exporter was added in this increment.
- No roadmap amendment was required.

## 2026-06-18 - Increment 0: repository bootstrap artifact

### Roadmap position

Current increment: Python v0 baseline reorganization and repository initialization.

### Changed

- Created the first clean LORQ repository candidate under `lorq/`.
- Separated source-controlled repo material from agent/session material using the `lorq/` and `internal/` boundary.
- Moved shared product assets to the repository root: `cases/`, `modes/`, `pricing/`, `execution/`, `schemas/`, `prompt_styles/`, `rubrics/`, `repositories/`, and `eval.config.yaml`.
- Moved Python v0 implementation and tests under `python/`.
- Added .NET v1 skeleton under `dotnet/`.
- Added roadmap and operating documents under `docs/`.
- Preserved generated Python v0 example results intentionally under `fixtures/python-v0-generated-results/`.
- Patched Python v0 conformance fixture lookup so it can resolve shared root-level examples in the monorepo layout.

### Validation

Executed during artifact creation:

- `cd python && python -m pytest -q` -> 71 passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --suite-root .. --validate-config` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --run-conformance` -> passed.

### Next increment

- Freeze a simple deterministic orchestration benchmark using fake agent and fake judge adapters.
- Create root-level conformance fixtures that can be consumed by both Python v0 and future .NET v1.
