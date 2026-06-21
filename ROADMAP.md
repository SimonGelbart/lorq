# LORQ roadmap

LORQ is moving from a Python v0 baseline to a .NET product core through deterministic, no-token conformance fixtures.

## Current position

Completed:

1. Python v0 produced a frozen deterministic orchestration baseline.
2. .NET can validate the frozen package model and reject known invalid fixtures.
3. .NET can rebuild deterministic package indexes.
4. .NET can merge deterministic run shards into an experiment package.
5. .NET can attach deterministic fake judgement passes.
6. .NET can render deterministic report artifacts and case review packs.
7. .NET can execute deterministic `run --no-judge` shards through fake and external file-adapter boundaries.
8. Recent refactoring split package validation, package identifiers, report rendering, and run orchestration into smaller services without changing package or CLI output shape.
9. .NET can run a deterministic file-adapter conformance probe before using a wrapper in a shard run.

Current focus:

- Keep the deterministic package contract stable.
- Harden the .NET run, merge, judge, report, and validate loop.
- Formalize adapter conformance before relying on real Codex or Copilot runtime behavior.

## Next product increments

1. Finish adapter conformance: keep schema/scenario coverage current and use it as the gate before real runtime smoke tests.
2. Real runtime smoke tests: prove Codex and Copilot adapters can produce LORQ-compliant evidence without changing deterministic gates.
3. Local-first v1 hardening: stable CLI help, exit codes, `doctor`/validation, packaging, and user-facing quickstart docs.
4. Post-v1 review surfaces: richer HTML/diff reports and CI comparison gates.

The full roadmap remains in `docs/roadmap/LORQ_PRACTICAL_ROADMAP.md`.
