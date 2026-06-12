# Changelog

## v1.2.0

Controlled base-skills and Codex isolation release for Simon's Graphify test.

Added:

- `execution/base-skills/` populated from the uploaded base skill bundle
- actual default `graphify` skill and `graphify-query-planner` skill under `execution/skills/`
- base-aware modes: `base-only`, `base-default-graphify`, and `base-default-graphify-plus`
- Codex CLI per-run `HOME` / `CODEX_HOME` isolation via agent profile settings
- `--isolate-agent-home` and `--no-isolate-agent-home` overrides
- `active-skills.json` per run to inspect the materialized workspace skill set

Recommended first run uses `base-default-graphify,base-default-graphify-plus` with `--no-judge`.

## v1.1.0

Portability contract release.

Added:

- JSON schema files under `schemas/`
- contract docs for architecture, results, normalized events, validation, backends, and portability
- schema-versioned machine-readable outputs
- `--run-conformance` no-token fixture
- bundled golden fixture expectations
- sample result folder generated from the conformance example

This release is intended to make a future .NET port safer by freezing the external contract.

## v1.0.0

First stable release of the generic agent evaluation runner.

Stable capabilities:

- Fairness-first clean execution model: fresh worktree per run
- Declarative YAML modes, cases, repositories, rubrics, prompt styles, and agent profiles
- Pre-agent setup commands executed by Python before the AI starts
- Codex CLI, GitHub Copilot SDK, Copilot CLI fallback, and generic CLI agent profiles
- Strict schema validation and `--validate-config`
- Backend availability checks with `--check-agent` / `--check-all-agents`
- Normalized backend events in `events.normalized.jsonl`
- Behavior validation over normalized events
- Source-grounded deterministic validation
- Optional LLM judge, disabled by default
- Repetition support and aggregate scorecards
- Human-readable reports: `summary.md`, `case_comparison.md`, `mode_summary.md`, `fairness_warnings.md`, and `failed_runs.md`
- Safe generated-folder cleanup guarded by `.agent-eval-generated.json`
- Dirty-repo policy controls
- Secret redaction for common token/key/secret patterns
- Final docs and examples

## v0.15.0

Pre-v1 correctness and safety hardening.

## v0.14.0

Reporting and UX polish.

## v0.13.0

Backend hardening.

## v0.12.0

Source-grounded validation hardening.

## v0.11.0

Normalized event model.

## v0.10.0

Schema validation.
