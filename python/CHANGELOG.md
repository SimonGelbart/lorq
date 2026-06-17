# Changelog

## 1.2.8-lorq-package-judgement

- Add migration-only `--judge-lorq-package` for attaching deterministic fake judgement passes to v1-alpha LORQ packages.
- Write `judgements/<name>/judgement.manifest.json`, `judgements/<name>/judgement.summary.json`, per-cell judgement JSON, and `.lorq/judgements/<name>.json`.
- Add judgement contract constants and v1-alpha schemas for cell judgements and judgement pass manifests.
- Fail by default when a present cell has no deterministic fixture judgement.

## 1.2.7-lorq-merge

- Add migration-only `--merge-lorq-shards` and `--lorq-merge-out` for v1-alpha LORQ run-shard packages.
- Add expected-cell coverage from deterministic `benchmark.yaml` via `--lorq-benchmark`.
- Fail merge by default on duplicate cell IDs and repository fingerprint mismatches.
- Preserve adapter-level integrity warnings from `adapter.evidence.json` in package integrity output.
- Add tests for merge success, missing coverage, duplicate cells, and fingerprint mismatch.

## 1.2.6-lorq-deterministic-fixtures

- Add `backend: deterministic-fake` for no-LLM migration benchmark cells.
- Add deterministic fake judge support with `judge.backend: deterministic-fake` and `judge.fixture_file`.
- Write `adapter.evidence.json` for deterministic fake runs and include it in LORQ shard export.
- Add self-contained deterministic orchestration benchmark fixture under `fixtures/conformance/deterministic-orchestration/`.
- Add schemas and tests for deterministic fake agent/judge fixtures.

## 1.2.5-lorq-migration

- Add migration-only `--export-lorq-shard` to export existing Python v0 results into the v1-alpha LORQ run-shard package layout.
- Add `eval_runner.lorq_package` for cell evidence normalization, coverage, fingerprints, integrity, and single-shard export metadata.
- Add LORQ v1-alpha migration contract constants.
- Add tests for package export and missing-final-answer integrity warnings.
- Fix the existing `{python}` agent command placeholder by importing `sys` in `eval_runner.agents`.

## 1.2.4

- Preserve token/count metrics during redaction; `input_tokens`, `cached_input_tokens`, `output_tokens`, `reasoning_output_tokens`, pass counters, and cost metrics are no longer redacted.
- Normalize usage with `cached_input_tokens`, `uncached_input_tokens`, `cache_hit_rate`, and `total_tokens`.
- Add optional pricing estimates using configurable per-1M-token rates.
- Add `--pricing-model`, `--pricing-file`, and `--no-pricing`.
- Add pricing columns to `scorecard.csv` and aggregate cost fields.
- Add `pricing/openai-pricing.example.yaml` and `docs/pricing.md`.

## 1.2.3

- Improved setup-only failure diagnostics.

## 2026-06-18 - Package reporting

- Added `--report-lorq-package` with `--primary-judgement` for migration-only deterministic package reports.
- Added report rendering tests.

## 2026-06-18 - Negative merge fixtures

- Added tests against committed duplicate-cell and fingerprint-mismatch LORQ fixture packages.

## 2026-06-18 - Frozen deterministic golden baseline

- Added tests for the committed full-loop deterministic golden package.
- Kept Python v0 as the migration baseline generator, not the final product core.
