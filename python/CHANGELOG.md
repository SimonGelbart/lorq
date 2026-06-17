# Changelog

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
