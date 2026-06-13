# LORQ Agent Work Session Handoff

## Files to give the AI agent

Give the agent these three inputs:

1. `agent-eval-runner-handoff-2026-06-17.zip`
   - Current Python v0 project.
   - This is the implementation baseline and should be treated as the prototype/specimen to inspect.

2. `LORQ_ROADMAP_GOAL_REFINEMENT.md`
   - Product identity and strategic goal refinement.
   - This is the product compass.

3. `LORQ_PRACTICAL_ROADMAP.md`
   - Practical staged roadmap with increments, exit criteria, alignment reviews, and anti-goals.
   - This is the delivery source of truth.

Do not give older roadmap drafts unless the agent explicitly needs historical context. The practical roadmap supersedes earlier notes.

---

## Session objective

The agent is working on LORQ: Ledger for Orchestrated Run Quality.

LORQ is not primarily an LLM intelligence benchmark. It is a shard-safe orchestration and evidence harness for agent/tool/skill evaluations.

The long-term target workflow is:

```bash
lorq run --no-judge --out shard-001
lorq run --no-judge --out shard-002

lorq merge shard-001 shard-002 --out experiment-001

lorq judge --input experiment-001 --name judge-primary

lorq report \
  --input experiment-001 \
  --primary-judgement judge-primary
```

The immediate work should advance the practical roadmap toward a frozen Python v0 orchestration baseline, then a .NET v1 implementation.

---

## First actions required from the agent

Before changing code, the agent must:

1. Inspect the current project structure.
2. Read both roadmap documents.
3. Identify which practical roadmap increment the project is currently in.
4. State the current increment, the intended next increment, and the proposed work for this session.
5. Confirm whether the proposed work fits the current roadmap.
6. If the proposed work changes the roadmap, amend the roadmap document before or alongside implementation.

The agent should not start coding until it can explain where the work sits in the roadmap.

---

## Operating rules to avoid drift

### 1. Roadmap alignment is mandatory

Every work session must begin and end with roadmap alignment.

At the start, the agent must report:

- Current roadmap increment.
- Target increment for this session.
- Relevant exit criteria.
- Risks or scope boundaries.

At the end, the agent must report:

- What changed.
- Which increment advanced.
- Which exit criteria are now satisfied.
- Which exit criteria remain incomplete.
- What the next increment should be.

If new increments are suggested, the agent must amend the roadmap instead of only mentioning them in chat.

### 2. Changelog updates are mandatory

Each increment must update `CHANGELOG.md`.

The changelog entry must include:

- Date.
- Increment name or number.
- Summary of changes.
- Added / changed / fixed / removed sections when relevant.
- Tests or validation performed.
- Known limitations.

If no `CHANGELOG.md` exists, the agent must create one.

### 3. Documentation must change with behavior

Any behavior, command, schema, package layout, adapter contract, judgement format, report format, or roadmap decision change must be documented.

Documentation may live in:

- `README.md`
- `docs/`
- roadmap documents
- schema comments/examples
- fixture documentation

No behavior change should be delivered without corresponding documentation.

### 4. Keep the package clean

The agent must avoid leaving clutter in the repository.

Do not commit or keep:

- temporary run outputs unless they are named fixtures
- local caches
- debug scratch files
- generated files outside approved fixture/output locations
- duplicate roadmap drafts
- obsolete artifacts without explanation
- environment-specific files

If generated artifacts are needed, they must go into a deliberate location such as:

```text
fixtures/
examples/
testdata/
artifacts/
```

and must be documented.

### 5. Python v0 is a fixture/spec phase, not the final product

The Python project may be improved only to create a frozen, comparable orchestration baseline.

Allowed Python v0 work:

- canonical package export
- deterministic fake agent adapter
- deterministic fake judge
- golden fixtures
- merge/judge/report conformance behavior
- basic docs needed for migration

Avoid turning Python v0 into the final product.

Once the frozen baseline exists, new product feature work should move to .NET.

### 6. Orchestration first; LLM intelligence second

The migration benchmark must not depend on Codex/Copilot/LLM intelligence.

The required benchmark should use:

- fake deterministic agent adapter
- fake deterministic judge
- canned answers
- canned traces
- canned cost/time
- controlled failures
- controlled integrity warnings

Real Codex/Copilot runs are adapter smoke tests after the orchestration gate, not the migration gate.

### 7. Adapter contracts must be explicit

Every agent adapter must produce a full cell evidence contract, not just a final answer.

A cell output should include:

```text
cell/
  final_answer.md
  cell_result.json
  artifacts/
  trace/
```

The adapter protocol is file-based:

```bash
adapter run-cell --input input.json --output output-dir
```

Stdout and stderr are logs only, not canonical protocol data.

### 8. Judgement is separate from execution

The agent must preserve the core product architecture:

```text
run -> merge -> judge -> report
```

Rules:

- `run` creates immutable execution evidence.
- `merge` creates the canonical experiment package.
- `judge` creates named judgement passes without rerunning agents.
- `report` renders decision outputs from experiment evidence plus selected judgement passes.

Do not collapse execution and judging back into one inseparable workflow.

### 9. Reports must be JSON-canonical and Markdown-rendered

Report data must be represented in JSON first.

Markdown is a rendering of JSON, not the only source of truth.

Expected report outputs:

```text
reports/
  report.json
  report.md
  cases/
    case-a.json
    case-a.md
```

### 10. Scope changes require roadmap amendments

If the agent discovers that the roadmap needs to change, it must:

1. Propose the change.
2. Explain why it is needed.
3. Update the practical roadmap.
4. Update the changelog.
5. Clearly state the amended next increments.

No silent roadmap drift.

---

## Session completion checklist

At the end of each work session, the agent must provide:

- Current roadmap increment.
- Work completed.
- Files changed.
- Tests run and results.
- Changelog entry summary.
- Documentation updated.
- Package cleanliness check.
- Remaining risks.
- Next practical increment.
- Whether the roadmap was amended.

If the roadmap was amended, the agent must include the amended roadmap file.

---

## Recommended first session goal

The first agent session should probably not start by rewriting the product in .NET.

Recommended first session:

1. Inspect current Python v0 project.
2. Compare it to `LORQ_PRACTICAL_ROADMAP.md`.
3. Add or update `CHANGELOG.md`.
4. Add a `docs/lifecycle.md` or equivalent explaining `run -> merge -> judge -> report`.
5. Identify the smallest Python v0 change needed to begin the frozen orchestration benchmark.
6. If needed, amend the practical roadmap with a precise first increment.

The first real implementation increment should likely be:

```text
Python v0 deterministic orchestration fixture foundation
```

with exit criteria around fake adapter/fake judge shape, package export structure, and fixture locations.
