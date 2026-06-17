# Frozen deterministic orchestration golden outputs

This directory is the Python v0 frozen migration baseline for LORQ Increment 1.

It contains the full no-LLM product loop snapshot:

```text
shard-001/
shard-002/
experiment-001/
  judgements/judge-primary/
  reports/report.json
  reports/report.md
  reports/cases/*/case-review.json
  reports/cases/*/case-review.md
```

The package was produced from `fixtures/conformance/deterministic-orchestration/` using the deterministic fake agent and deterministic fake judge. It verifies orchestration, shard export, merge, judgement attachment, reporting, coverage gaps, and integrity warnings. It is not an LLM intelligence benchmark.

The expected missing cell is:

```text
skipped-coverage__graphify-plus__attempt-001
```

Golden files are intentionally source-controlled. Session logs, worktrees, raw generation folders, and scratch commands remain outside the repo under `internal/`.
