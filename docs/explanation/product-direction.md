# Product direction

LORQ is a ledger for orchestrated run quality. Its value is not in claiming that one model is smarter than another. Its value is in proving that an evaluation run was complete, comparable, reproducible, and reviewable.

## Product thesis

Users should be able to split agent/tool/skill evaluations across shards, merge those shards into a trustworthy experiment package, attach one or more judgement passes later, and review a decision-grade report.

The core loop is:

```bash
lorq run --no-judge --out shard-001
lorq run --no-judge --out shard-002
lorq merge shard-001 shard-002 --out experiment-001
lorq judge --input experiment-001 --name judge-primary
lorq report --input experiment-001 --primary-judgement judge-primary
```

## Non-goals for v1

- LLM intelligence benchmarking as the primary product identity.
- Runtime comparison verdicts across unrelated providers.
- Persistent adapter services.
- HTML dashboards as the canonical report.
- Real LLM calls as a deterministic migration gate.

## Design principles

- Evidence is canonical; summaries are derived.
- JSON reports are canonical; Markdown reports are renderings.
- Shards remain preserved after merge.
- `.lorq/` indexes are machine-owned and reproducible.
- Quality judgement is separate from execution integrity.
- Adapter-specific behavior belongs behind adapter contracts, not in the core package model.


## See also

- `../decisions/0003-package-lifecycle-and-evidence-model.md` records the durable package lifecycle and evidence model.
- `../reference/cli.md` describes the current command surface.
- `../reference/package-validation.md` describes current validation behavior and stable error codes.
- `../decisions/0006-report-data-and-rendering-boundary.md` records the canonical report JSON and rendering boundary.
- `../decisions/0007-failure-classification-and-integrity-gates.md` records how execution failures affect validity and confidence.
- `../decisions/0008-pre-v1-schema-versioning.md` records the pre-v1 schema compatibility stance.
