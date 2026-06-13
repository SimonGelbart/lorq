# Changelog

All notable changes to LORQ should be documented here.

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
