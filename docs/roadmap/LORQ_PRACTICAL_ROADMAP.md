# LORQ — Practical Delivery Roadmap

Status: practical roadmap derived from the consolidated LORQ product refinement  
Product: **LORQ — Ledger for Orchestrated Run Quality**  
Goal: convert the strategic roadmap into realistic, quantifiable increments with alignment reviews

---

## 1. Where we are

LORQ is currently at the **refined prototype / pre-product planning stage**.

The current Python project, still allowed to keep the temporary `agent-eval` name, has already proven enough of the evaluation-runner direction to be useful as a behavioral reference. It should not become the long-term product core.

Current state:

- A Python v0 runner exists as the experimental/prototype foundation.
- The product thesis has been refined into **orchestration and evidence**, not LLM intelligence benchmarking.
- The target product name is **LORQ**.
- The target v1 implementation language is **.NET**.
- The target architecture is SDK-independent, with pluggable agent-runtime and judge adapters.
- The first real product loop is clear: `run`, `merge`, `judge`, `report`.
- The most important migration requirement is clear: Python must create a frozen, deterministic, comparable baseline before the .NET implementation starts in earnest.

Current risk:

- The roadmap is strategically clear, but implementation could still drift into one of three wrong directions:
  1. Over-testing LLM intelligence instead of orchestration.
  2. Overbuilding future runtime comparison too early.
  3. Letting the Python prototype shape leak into the final .NET product.

---

## 2. Where we are going

LORQ should become a **shard-safe orchestration ledger** for agent, tool, and skill evaluation workflows.

The v1 product should let a user run this loop reliably:

```bash
lorq run --no-judge --out shard-001
lorq run --no-judge --out shard-002

lorq merge shard-001 shard-002 --out experiment-001

lorq judge --input experiment-001 --name judge-primary

lorq report \
  --input experiment-001 \
  --primary-judgement judge-primary
```

The output should be one reviewable experiment package:

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

The v1 report must answer:

- Which mode should we adopt, if any?
- Is the recommendation driven by quality gain, cost saving, time saving, or risk?
- Can we trust the comparison?
- Which cases drove the decision?
- What failed, timed out, or was skipped?
- Where are the full answers, traces, artifacts, and judge outputs?

---

## 3. Roadmap structure

This roadmap is intentionally organized into **large practical increments**, not tiny tickets.

Each increment includes:

- outcome
- scope
- concrete deliverables
- quantifiable exit criteria
- alignment review, when needed

Alignment reviews are explicit gates. Their job is to confirm that the implementation is still heading toward the defined product outcomes before the next stage compounds mistakes.

---

## 4. Increment 0 — Product and architecture baseline

### Outcome

Freeze the product direction tightly enough that implementation can proceed without reopening the core thesis every week.

### Scope

This is a planning and alignment increment. It does not need to produce final code, but it must produce stable implementation intent.

### Deliverables

- LORQ product name and positioning.
- v1 product promise.
- v1 non-goals.
- canonical command loop.
- canonical package shape.
- agent adapter contract draft.
- judge adapter contract draft.
- Python-to-.NET migration strategy.
- list of alignment review gates.

### Current branch status

Increment 1 is frozen on `feat/migration`. Increment 2 has started with a .NET package validation foundation that reads the committed Python golden fixtures, validates package/index references, and rejects duplicate-cell and fingerprint-mismatch merge inputs with stable error codes.

### Quantifiable exit criteria

- 1 canonical product goal statement exists.
- 1 canonical CLI loop is documented.
- 1 canonical package layout is documented.
- 1 v1 non-goals list is documented.
- At least 10 locked product principles are written down.
- No open P0 naming, language, migration, or architecture question remains.

### Alignment Review 0 — Goal confirmation

Review question:

> Are we still building an orchestration/evidence ledger, not a generic LLM benchmark?

Required attendees or reviewers:

- product owner
- technical owner
- at least one future user/reviewer of reports, if available

Review must confirm:

- LORQ is the final product identity.
- `.NET` is the final product core direction.
- Python v0 is a fixture/protocol baseline, not the final product.
- v1 focuses on mode/tool/skill comparison within one runtime.
- runtime comparison remains future scope.

Decision output:

```text
GO / REWORK
```

---

## 5. Increment 1 — Python frozen conformance baseline

### Outcome

Create a frozen, simple, deterministic baseline that proves the full orchestration loop before migration to .NET.

This is the most important bridge between the prototype and the final product.

### Scope

Python v0 should be used to generate fixtures and expected behavior. It should not receive broad new product features beyond what is required to freeze the comparable baseline.

### Deliverables

- canonical v1 package exporter from Python.
- scenario-driven fake agent adapter.
- deterministic fake judge adapter.
- fake judge fixture format.
- small deterministic benchmark definition.
- two run shards generated from that benchmark.
- one merged experiment package.
- one named judgement pass.
- `report.json` and `report.md`.
- per-case review packs.
- golden expected outputs.
- hand-authored edge-case fixtures.

### Required frozen benchmark shape

The migration benchmark should be deliberately small:

```text
2–3 cases
2–3 modes
1 attempt per case/mode
2 shards
1 merged experiment package
1 named judgement pass
1 top-level report
per-case review packs
```

It must include at least:

```text
1 successful comparison
1 timeout or no_final_answer case
1 intentionally skipped cell or incomplete coverage fixture
1 integrity warning
1 duplicate cell conflict fixture
1 fingerprint mismatch fixture
1 missing final answer / invalid artifact fixture
```

### Current branch status

Increment 1 is frozen on `feat/migration`. Increment 2 has started with a .NET package validation foundation that reads the committed Python golden fixtures, validates package/index references, and rejects duplicate-cell and fingerprint-mismatch merge inputs with stable error codes.

### Quantifiable exit criteria

- Python exports 1 complete canonical LORQ experiment package.
- The exported package uses the planned public layout and `.lorq/` indexes.
- The frozen benchmark can be regenerated with deterministic outputs.
- 100% of expected golden files are committed.
- 100% of hand-authored edge-case fixtures are documented.
- The benchmark can be manually reviewed in under 30 minutes by reading `report.md` and the case packs.
- No real LLM call is required for the migration gate.

### Alignment Review 1 — Migration baseline review

Review question:

> Is this frozen benchmark a good enough comparable point to start the .NET migration?

Review must confirm:

- The benchmark tests orchestration, not LLM intelligence.
- The fake adapter returns deterministic answers, traces, costs, timings, and controlled failures.
- The fake judge returns deterministic structured judgements.
- The exported package format is the intended future LORQ shape, not just the current Python shape.
- The edge-case fixtures cover the main trust risks.

Decision output:

```text
GO TO .NET FOUNDATION / FIX BASELINE FIRST
```

---

## 6. Increment 2 — .NET foundation and package model

### Outcome

Create the .NET LORQ core and prove it can read, validate, and reason about the frozen package format.

### Scope

This increment is about domain modeling, package IO, validation, and fixture compatibility. It should not yet chase Codex/Copilot integration.

### Deliverables

- .NET solution skeleton.
- `lorq` CLI skeleton.
- core domain model:
  - `ExperimentPackage`
  - `RunShard`
  - `RunCell`
  - `Attempt`
  - `Mode`
  - `Fingerprint`
  - `JudgementPass`
  - `Report`
- package reader.
- package writer.
- package schema validation.
- coverage index builder.
- fingerprint index builder.
- integrity index builder.
- golden fixture test harness.
- basic command summaries as JSON.

### Current branch status

Increment 1 is frozen on `feat/migration`. Increment 2 has started with a .NET package validation foundation that reads the committed Python golden fixtures, validates package/index references, and rejects duplicate-cell and fingerprint-mismatch merge inputs with stable error codes.

### Quantifiable exit criteria

- .NET loads 100% of Python frozen package fixtures.
- .NET validates 100% of valid fixtures successfully.
- .NET rejects 100% of invalid hand-authored fixtures with stable error codes.
- Package validation checks at least:
  - required files
  - package schema version
  - run shard references
  - cell references
  - coverage index
  - fingerprint index
  - judgement references
  - report references
- No Python runtime is required by the .NET validation test suite.

### Alignment Review 2 — Package model review

Review question:

> Does the .NET core model the product correctly before we build more behavior on top of it?

Review must confirm:

- The package model preserves run shards rather than rewriting evidence.
- `.lorq/` indexes are machine-owned and reproducible.
- The domain model is SDK-independent.
- Versioning is not overengineered before v1.
- Runtime comparison metadata is stored but not treated as official v1 reporting scope.

Decision output:

```text
GO TO RUN/MERGE / REWORK DOMAIN MODEL
```

---

## 7. Increment 3 — .NET run and merge loop

### Outcome

Make the shard-safe execution model work in .NET using the deterministic fake adapter.

### Scope

This increment implements the first half of the core product loop:

```bash
lorq run --no-judge
lorq merge
```

It should still avoid real LLM/runtime integration. The goal is orchestration correctness.

### Deliverables

- `lorq run --no-judge` using fake adapter.
- ad-hoc run auto-promoted into an experiment manifest.
- run shard package creation.
- full cell result contract:
  - `cell_result.json`
  - `final_answer.md`
  - `artifacts/`
  - `trace/`
- adapter stdout/stderr capture.
- adapter exit-code capture.
- `lorq merge`.
- duplicate cell conflict detection.
- fingerprint mismatch detection.
- intentionally skipped cell handling.
- merge log generation.
- coverage/fingerprint/integrity index generation.
- materialized package option, if inexpensive.

### Current branch status

Increment 1 is frozen on `feat/migration`. Increment 2 has started with a .NET package validation foundation that reads the committed Python golden fixtures, validates package/index references, and rejects duplicate-cell and fingerprint-mismatch merge inputs with stable error codes.

### Quantifiable exit criteria

- The .NET fake run reproduces the frozen benchmark shape.
- 2 manually split shards merge into 1 valid experiment package.
- Duplicate cell fixture fails with a stable conflict error.
- Fingerprint mismatch fixture fails or warns according to configured severity.
- Merge produces deterministic `.lorq/coverage.json`, `.lorq/fingerprints.json`, `.lorq/merge-log.json`, and `.lorq/integrity.json`.
- Re-running the same fake benchmark produces byte-stable canonical JSON where timestamps are intentionally excluded or normalized.
- At least 6 failure/status classes are represented:
  - `success`
  - `timeout`
  - `no_final_answer`
  - `adapter_crash`
  - `permission_denied`
  - `invalid_artifact`

### Alignment Review 3 — Shard-safe orchestration review

Review question:

> Can LORQ now reliably split execution and recombine it into trustworthy evidence?

Review must confirm:

- Merge is first-class, not a copy helper.
- Shards remain canonical execution evidence.
- Merged packages become canonical evaluation evidence.
- Conflict behavior is strict by default.
- The adapter output contract is rich enough for reporting and debugging.

Decision output:

```text
GO TO JUDGE/REPORT / FIX ORCHESTRATION
```

---

## 8. Increment 4 — .NET judgement and report loop

### Outcome

Complete the deterministic v1 product loop with fake judgement and decision-grade reporting.

### Scope

This increment implements:

```bash
lorq judge
lorq report
```

It must keep quality judgement separate from execution/integrity.

### Deliverables

- fake deterministic judge adapter.
- named judgement pass creation.
- auto-generated judgement names.
- explicit `--primary-judgement` behavior when multiple judgements exist.
- quality judgement input generation from final answers only.
- execution/integrity validity gate integration.
- failure classification handling.
- baseline-first quality comparison for 3+ modes.
- targeted all-pairs support, if simple.
- value verdict model:
  - quality gain
  - roughly equal quality with cost/time saving
  - cost/time regression
  - invalid/rerun required
- raw and normalized cost/time metrics.
- canonical `report.json`.
- default `report.md` renderer.
- per-case `case.json` and `case.md` review packs.
- stable artifact links to full answers, traces, and judge JSON.

### Current branch status

Increment 1 is frozen on `feat/migration`. Increment 2 has started with a .NET package validation foundation that reads the committed Python golden fixtures, validates package/index references, and rejects duplicate-cell and fingerprint-mismatch merge inputs with stable error codes.

### Quantifiable exit criteria

- .NET reproduces the frozen Python end-to-end benchmark.
- One command sequence produces a valid report:

```bash
lorq run --no-judge --out shard-001
lorq run --no-judge --out shard-002
lorq merge shard-001 shard-002 --out experiment-001
lorq judge --input experiment-001 --name judge-primary
lorq report --input experiment-001 --primary-judgement judge-primary
```

- `report.json` contains every decision field shown in `report.md`.
- `report.md` contains no decision data missing from `report.json`.
- 100% of cases have a case review pack.
- Case packs include:
  - task
  - modes compared
  - structured judge summary
  - answer summaries
  - cost/time table
  - integrity warnings
  - links to full answers
  - links to full judge JSON
- Multiple judgement passes are supported.
- If multiple judgement passes exist, report generation requires explicit primary judgement.
- At least 6 official verdict/reason paths are covered by fixtures:
  - `adopt` with `quality_gain`
  - `adopt` with `cost_saving`
  - `adopt_selectively`
  - `keep_current`
  - `reject`
  - `rerun_required` or `invalid`

### Alignment Review 4 — Decision report review

Review question:

> Does the report actually support an adoption decision from reproducible evidence?

Review must confirm:

- Quality judgement is based on final answers only.
- Execution/integrity acts as a validity gate, not a quality score.
- Cost/time normalization prevents false wins from skipped or failed work.
- Reports are understandable without opening raw artifacts.
- Case packs make disagreements and failures reviewable.

Decision output:

```text
GO TO ADAPTER SYSTEM / FIX DECISION SURFACE
```

---

## 9. Increment 5 — Adapter protocol and conformance

### Outcome

Make pluggability real without coupling the product core to one SDK.

### Scope

This increment formalizes the adapter system and conformance suite. It does not require full industrial Codex/Copilot maturity yet.

### Deliverables

- `AgentRuntimeAdapter` contract.
- `JudgeAdapter` contract.
- file-based one-shot external adapter protocol.
- adapter input schema.
- adapter output schema.
- full cell result contract validation.
- `lorq adapter conformance`.
- sample external fake adapter executable.
- shell/mock adapter, if useful.
- Codex process/CLI adapter skeleton.
- Copilot SDK adapter skeleton.

### Adapter protocol target

```bash
adapter run-cell --input input.json --output output-dir
```

Required output:

```text
output-dir/
  cell_result.json
  final_answer.md
  artifacts/
  trace/
```

Runner-captured logs:

```text
adapter.stdout.log
adapter.stderr.log
adapter.exit_code
```

### Current branch status

Increment 1 is frozen on `feat/migration`. Increment 2 has started with a .NET package validation foundation that reads the committed Python golden fixtures, validates package/index references, and rejects duplicate-cell and fingerprint-mismatch merge inputs with stable error codes.

### Quantifiable exit criteria

- `lorq adapter conformance` runs at least 10 protocol scenarios.
- Conformance scenarios cover:
  - success result
  - timeout result
  - no final answer
  - adapter crash
  - permission denied
  - invalid artifact
  - usage/cost metadata
  - timing metadata
  - trace output
  - stdout/stderr capture
- A sample external adapter written outside the core project passes conformance.
- A malformed adapter fails conformance with actionable errors.
- Core domain model still has no direct dependency on Codex or Copilot SDK types.

### Alignment Review 5 — Adapter architecture review

Review question:

> Can LORQ support Codex, Copilot, fake adapters, and future runtimes without redesigning the core?

Review must confirm:

- Agent adapters and judge adapters use separate business contracts.
- Provider-specific options live in namespaced extension blocks.
- External adapters can be written in any language.
- One-shot file protocol is sufficient for v1.
- Persistent adapter services remain future scope.

Decision output:

```text
GO TO REAL RUNTIME SMOKE / FIX ADAPTER BOUNDARY
```

---

## 10. Increment 6 — Real runtime smoke tests

### Outcome

Validate that real runtime adapters can produce LORQ-compliant evidence without making real LLM behavior a migration blocker.

### Scope

This increment validates integration paths. It should not turn into broad model-quality benchmarking.

### Deliverables

- Codex process/CLI smoke run.
- Copilot SDK smoke run.
- optional real judge smoke run.
- runtime metadata capture.
- adapter version capture.
- permission profile capture.
- provider extension block examples.
- one single-runtime official report from a real adapter, if stable.

### Current branch status

Increment 1 is frozen on `feat/migration`. Increment 2 has started with a .NET package validation foundation that reads the committed Python golden fixtures, validates package/index references, and rejects duplicate-cell and fingerprint-mismatch merge inputs with stable error codes.

### Quantifiable exit criteria

- Codex adapter can produce at least 1 valid `cell_result.json` with final answer, usage/timing where available, trace/log capture, and artifacts.
- Copilot adapter can produce at least 1 valid `cell_result.json` with final answer, usage/timing where available, trace/log capture, and artifacts.
- Both adapters pass basic adapter conformance or a documented subset.
- Real smoke runs do not change the deterministic migration gate.
- Official v1 report remains single-runtime only.
- Runtime-mixed packages are either rejected by official report generation or marked unsupported with a stable error.

### Alignment Review 6 — Real adapter readiness review

Review question:

> Are real runtime integrations validating the architecture, or are they pulling the product away from the v1 scope?

Review must confirm:

- Codex remains the heavy/local experimental path.
- Copilot remains the industrial/company path.
- Runtime comparison remains secondary.
- Official v1 reporting remains mode/tool/skill comparison within one runtime.
- Adapter issues are isolated from core package/report logic.

Decision output:

```text
GO TO V1 HARDENING / CONTAIN ADAPTER SCOPE
```

---

## 11. Increment 7 — Local-first, CI-safe v1 hardening

### Outcome

Turn the working loop into a usable v1 product.

### Scope

This increment is about reliability, documentation, packaging, validation, and automation-safe output. It should not add major new analytical features.

### Deliverables

- stable CLI help.
- stable command exit codes.
- machine-readable command summaries.
- no hidden interactive prompts in CI/non-interactive mode.
- `lorq experiment validate`.
- basic `lorq doctor`.
- clean error messages.
- installation/package instructions.
- minimal user guide.
- quickstart using fake adapter.
- quickstart using one real adapter.
- v1 sample package.
- v1 sample report.

### Current branch status

Increment 1 is frozen on `feat/migration`. Increment 2 has started with a .NET package validation foundation that reads the committed Python golden fixtures, validates package/index references, and rejects duplicate-cell and fingerprint-mismatch merge inputs with stable error codes.

### Quantifiable exit criteria

- Fresh checkout/install can run the fake quickstart end-to-end.
- A new user can produce a deterministic sample report using documented commands only.
- All primary commands return stable exit codes.
- Each primary command can emit or write machine-readable JSON summary.
- `lorq experiment validate` succeeds on valid sample packages and fails invalid fixtures.
- `lorq doctor` catches at least:
  - missing runtime dependency
  - invalid workspace path
  - adapter not executable
  - unsupported package schema
- Documentation covers the canonical loop in under 10 minutes of reading.

### Alignment Review 7 — v1 readiness review

Review question:

> Is this ready to be called LORQ v1, or is it still an internal prototype?

Review must confirm:

- A user can complete the full v1 loop without internal knowledge.
- The product is local-first but CI-safe.
- The report is decision-grade enough for review.
- The package shape is stable enough to call `package_schema_version: 1`.
- Deferred features are not accidentally half-promised.

Decision output:

```text
SHIP V1 / HARDEN MORE
```

---

## 12. Post-v1 increments

Post-v1 work should only start after the v1 loop is trusted.

### Increment 8 — Shard planning and partial workflows

Outcome:

Make split execution easier and more deliberate.

Deliverables:

- `lorq plan`
- `lorq run --shard`
- generated shard plans
- matrix coverage planning
- optional `--allow-partial`
- strict/partial report modes

Quantifiable exit criteria:

- A benchmark matrix can be split into N planned shards.
- Coverage report distinguishes completed, missing, skipped, and intentionally deferred cells.
- Partial reports clearly mark confidence limitations.

### Increment 9 — CI gates and experiment comparison

Outcome:

Make LORQ useful in automated decision workflows.

Deliverables:

- `lorq report --ci`
- basic threshold gates
- compare two experiment packages
- regression summaries
- cost/time delta summaries

Quantifiable exit criteria:

- CI can fail on `invalid`, `rerun_required`, or configured value thresholds.
- Two experiment packages can be compared with stable JSON output.
- Regression report identifies changed cases and changed verdicts.

### Increment 10 — Better review surfaces

Outcome:

Improve human review without changing the evidence model.

Deliverables:

- HTML report renderer
- richer case navigation
- disagreement summaries across secondary judgements
- compact executive summary
- report diff view

Quantifiable exit criteria:

- HTML is generated from `report.json` only.
- Markdown and HTML agree on verdicts and reason tags.
- Reviewers can navigate from summary to case evidence in one click.

### Increment 11 — Runtime comparison and matrix analysis

Outcome:

Support runtime comparison as a deliberate advanced product, not accidental v1 scope.

Deliverables:

- official `agent_runtime` comparison type
- official `tool_runtime_matrix` comparison type
- runtime-normalized reporting
- runtime × mode heatmaps or tables
- stricter comparability rules

Quantifiable exit criteria:

- Runtime-mixed experiments receive explicit supported verdicts.
- Reports separate tool/mode effects from runtime effects.
- The product avoids claiming a tool improvement when the actual difference is runtime-driven.

---

## 13. Suggested sequencing summary

| Increment | Name | Main output | Alignment review |
|---:|---|---|---|
| 0 | Product and architecture baseline | confirmed LORQ direction | Review 0 |
| 1 | Python frozen conformance baseline | deterministic golden benchmark | Review 1 |
| 2 | .NET foundation and package model | package reader/validator | Review 2 |
| 3 | .NET run and merge loop | shard-safe execution/merge | Review 3 |
| 4 | .NET judgement and report loop | decision report and case packs | Review 4 |
| 5 | Adapter protocol and conformance | pluggable runtime boundary | Review 5 |
| 6 | Real runtime smoke tests | Codex/Copilot validation | Review 6 |
| 7 | Local-first, CI-safe v1 hardening | shippable v1 | Review 7 |
| 8 | Shard planning and partial workflows | planned split execution | later review |
| 9 | CI gates and experiment comparison | automation and regression use | later review |
| 10 | Better review surfaces | HTML/diff UX | later review |
| 11 | Runtime comparison and matrix analysis | runtime comparison product | later review |

---

## 14. Practical cut lines

### Must be true before starting .NET implementation seriously

```text
Python frozen deterministic benchmark exists.
Golden expected outputs exist.
Canonical package layout is exported.
Fake adapter and fake judge are deterministic.
```

### Must be true before building real adapters seriously

```text
.NET package model works.
.NET run/merge works with fake adapter.
.NET judge/report works with fake judge.
Adapter protocol is documented and validated.
```

### Must be true before calling it v1

```text
End-to-end loop works from documented commands.
Reports are generated from report.json.
Case packs exist by default.
Validation commands exist.
Stable exit codes exist.
At least one real adapter smoke path works.
The product still focuses on orchestration/evidence, not LLM intelligence.
```

---

## 15. Outcome metrics for v1

The v1 release should be judged by these measurable outcomes:

| Outcome | Target |
|---|---:|
| deterministic fake benchmark pass rate | 100% |
| golden fixture compatibility | 100% |
| valid package validation success | 100% |
| invalid fixture rejection | 100% for known invalid fixtures |
| primary CLI loop commands | 4: `run`, `merge`, `judge`, `report` |
| required package indexes | 4: coverage, fingerprints, merge log, integrity |
| report formats | 2: JSON canonical, Markdown default |
| case review pack coverage | 100% of cases |
| judgement pass support | multiple named passes |
| official runtime scope | single-runtime mode comparison only |
| real adapter smoke paths | at least Codex + Copilot skeleton/smoke |
| migration dependency on LLM intelligence | 0 required LLM calls |

---

## 16. Anti-goals for v1

The practical roadmap should actively resist these until after v1:

```text
full runtime comparison verdicts
persistent adapter service protocol
HTML dashboard as primary report
advanced statistical all-vs-all engine
complex CI threshold language
schema migration system
large benchmark authoring suite
multi-judge consensus as headline decision
LLM intelligence benchmark positioning
```

---

## 17. Final practical roadmap statement

LORQ should move from Python prototype to .NET product through one frozen deterministic bridge.

The roadmap is not:

> Add more evaluation features until it feels complete.

The roadmap is:

> Freeze the orchestration behavior in Python, reproduce it in .NET, validate pluggable adapters, then harden the local-first/CI-safe product loop.

The first shippable product is successful when a user can split runs, merge them, judge later, and review a decision-grade package without depending on LLM nondeterminism to prove that the orchestration works.
