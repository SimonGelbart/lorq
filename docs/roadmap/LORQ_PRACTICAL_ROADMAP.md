# LORQ practical delivery roadmap

Product: **LORQ — Ledger for Orchestrated Run Quality**

This roadmap tracks product increments, not work-session handoffs. Session notes, validation transcripts, and package manifests belong outside the source tree in the local `internal/` workspace.

## Product goal

LORQ is a shard-safe orchestration ledger for agent, tool, and skill evaluations. It should let a user split runs, merge execution shards into a canonical experiment package, attach judgement passes later, and review a decision-grade report from reproducible evidence.

LORQ is not primarily an LLM intelligence benchmark.

## Current state

Completed baseline and deterministic .NET package work:

1. Python v0 produced frozen deterministic conformance fixtures.
2. .NET validates valid package fixtures and rejects known invalid fixtures with stable codes.
3. .NET rebuilds package indexes from evidence.
4. .NET merges deterministic run shards.
5. .NET attaches deterministic fixture-backed judgement passes.
6. .NET renders deterministic report artifacts and per-case review packs.
7. .NET executes deterministic `run --no-judge` shards through fake and external file-adapter boundaries.
8. The CLI uses hosted composition and typed command handlers.
9. Recent refactoring extracted package validation components, package identifiers, report rendering components, and run orchestration services without changing public output shapes.

Current product boundary:

- The deterministic gate uses fake/no-token adapters.
- `--adapter-command` supports external one-shot file adapters.
- `--adapter-profile codex-cli` configures wrapper metadata only; LORQ does not call Codex directly.
- Copilot SDK integration remains a future adapter increment.
- Setup command execution, Git checkout/worktree orchestration, and dirty-worktree policy remain future runtime work.

## Canonical loop

Durable lifecycle decisions are recorded in `../decisions/0003-package-lifecycle-and-evidence-model.md`. This roadmap only tracks sequencing and remaining work.

```bash
lorq run --no-judge --out shard-001
lorq run --no-judge --out shard-002
lorq merge shard-001 shard-002 --out experiment-001
lorq judge --input experiment-001 --name judge-primary
lorq report --input experiment-001 --primary-judgement judge-primary
```

The current CLI command names are still implementation-oriented (`merge-shards`, `judge-package`, `report-package`, `validate-package`) and can be normalized later when the CLI surface is hardened.

## Canonical package shape

```text
experiment-001/
  experiment.yaml
  runs/
  judgements/
  reports/
    report.json
    report.md
    cases/
  .lorq/
    coverage.json
    fingerprints.json
    merge-log.json
    integrity.json
    cells/
```

Public folders remain browseable. `.lorq/` contains machine-owned indexes and provenance.

## Remaining increments

### Increment A — Adapter conformance

Adapter architecture decisions are recorded in `../decisions/0005-sdk-independent-adapter-architecture.md`; file-adapter conformance is recorded in `../decisions/0002-file-adapter-conformance.md`.

Outcome: make pluggability testable without coupling the core to one SDK.

Deliverables:

- `lorq adapter conformance` command-group test runner, with `adapter-conformance` retained as a compatibility alias.
- Adapter input and output JSON contract checks.
- Deterministic conformance scenarios and negative tests covering success, timeout, no final answer, adapter failure, permission denied, process start failure, invalid artifact, artifact checksums, integrity warnings, usage metadata, timing metadata, trace output, stdout/stderr capture, and exit-code consistency.
- A sample external adapter outside the core project.

Exit criteria:

- A well-formed external adapter passes conformance.
- A malformed adapter fails with actionable diagnostic codes and ADR 0007 failure classes.
- Core domain code still has no direct dependency on Codex or Copilot SDK types.

### Increment B — Real runtime smoke tests

Outcome: prove real runtime integrations can produce LORQ-compliant evidence without becoming the deterministic gate.

Deliverables:

- Codex wrapper smoke run.
- Copilot SDK smoke run.
- Runtime metadata capture.
- Provider extension block examples.

Exit criteria:

- Each smoke path can produce at least one valid cell evidence file.
- Real smoke runs do not change deterministic fixtures or migration gates.
- Adapter-specific issues remain isolated from package/report logic.

### Increment C — Local-first v1 hardening

Outcome: make the working deterministic loop usable as a local-first product.

Deliverables:

- Stable CLI help.
- Stable command exit codes.
- Machine-readable command summaries.
- `lorq doctor` or equivalent environment diagnostics.
- Installation/package instructions.
- Quickstart using the fake adapter.
- Quickstart using one real adapter smoke path.

Exit criteria:

- A fresh checkout can run the fake quickstart end-to-end from documented commands.
- A user can produce a deterministic sample report without internal knowledge.
- Documentation covers the canonical loop in under 10 minutes of reading.

### Increment D — Post-v1 review and automation surfaces

Outcome: improve review and CI workflows without changing the evidence model.

Candidate deliverables:

- HTML report renderer generated from `report.json`.
- Report diff view.
- CI gate summaries.
- Experiment comparison command.
- Shard planning and partial workflow support.

## Anti-goals for v1

Schema versioning is recorded in `../decisions/0008-pre-v1-schema-versioning.md`; report data/rendering decisions are recorded in `../decisions/0006-report-data-and-rendering-boundary.md`; failure classification is recorded in `../decisions/0007-failure-classification-and-integrity-gates.md`.

Avoid these until the deterministic product loop is trusted:

- Full runtime comparison verdicts.
- Persistent adapter service protocol.
- HTML dashboard as the canonical report.
- Advanced statistical all-vs-all engine.
- Complex CI threshold language.
- Schema migration system.
- LLM intelligence benchmark positioning.

## Readiness statement

The first shippable product is successful when a user can split runs, merge them, judge later, and review a decision-grade package without depending on LLM nondeterminism to prove that orchestration works.
