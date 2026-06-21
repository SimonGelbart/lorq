# LORQ — Consolidated Product Roadmap

Status: consolidated from refinement session  
Product name: **LORQ — Ledger for Orchestrated Run Quality**  
Primary theme: shard-safe, evidence-grade orchestration for evaluating agent/tool/skill modes  
Target final implementation: .NET core with pluggable agent-runtime and judge adapters  
Legacy/prototype name: current Python v0 may remain `agent-eval`; .NET v1 product is `lorq`.

---

## 1. Product thesis

**LORQ** stands for **Ledger for Orchestrated Run Quality**. The name is intentionally not "agent eval": the product is a ledger of orchestrated execution evidence and judgement results, not a generic intelligence benchmark.

LORQ is not primarily a benchmark of LLM intelligence. It is an orchestration and evidence harness.

Its job is to let a user run agent evaluations across multiple token-limited sessions, merge those execution shards into one canonical experiment package, run one or more judgement passes later, and produce a final decision report comparing multiple modes by answer quality, execution validity, time, and price.

The core product promise:

> Users can split agent evaluation runs across multiple sessions, merge them into one trustworthy package, judge them later, and make an adoption decision from reproducible evidence.

The product should optimize for these jobs:

1. Run use-case executions independently from judging.
2. Support partial/sharded execution because token/time/budget limits are real.
3. Merge many shards into one canonical experiment package.
4. Run one or more named judgement passes after execution, without rerunning agents.
5. Compare 3+ modes as the normal case, for example `baseline` vs `graphify` vs `graphify-plus`.
6. Keep quality judgement independent from how the answer was produced.
7. Use execution/integrity evidence as a validity gate, not as a quality score.
8. Produce adoption-oriented reports using quality, time, and price.
9. Keep the core SDK-independent through adapter contracts.
10. Migrate from Python prototype to .NET product using deterministic conformance fixtures.

---

## 2. Locked product principles

### 2.1 Runs and judgements are separate phases

`run` creates execution evidence.  
`judge` evaluates existing evidence.  
`report` turns raw execution plus judgement passes into a decision surface.

Canonical workflow:

```bash
lorq run --no-judge --out shard-001
lorq run --no-judge --out shard-002

lorq merge shard-001 shard-002 --out experiment-001

lorq judge --input experiment-001 --name judge-primary
lorq judge --input experiment-001 --name judge-strict-v2

lorq report \
  --input experiment-001 \
  --primary-judgement judge-primary \
  --secondary-judgement judge-strict-v2
```

### 2.2 Two canonical evidence objects

Both run shards and merged experiments are canonical, but they have different roles.

- A **run shard** is canonical execution evidence.
- A **merged experiment package** is canonical evaluation evidence.

Run shards must remain useful on their own. Merged packages become the expected input for official judging, reporting, archiving, and comparison.

### 2.3 Merge is first-class

Merge is not a file convenience. It is a product step that validates whether split execution can be treated as one experiment.

Default merge behavior:

- Creates a manifest with checksums and references.
- Preserves original shard contents publicly.
- Builds normalized cell indexes internally.
- Fails by default on duplicate cells or incompatible fingerprints.
- Supports explicit conflict policies for advanced recovery.
- Supports materialization/portable export when needed.

Example:

```bash
lorq merge shard-001 shard-002 --out experiment-001
lorq merge shard-001 shard-002 --out experiment-001 --materialize
lorq merge shard-001 shard-002 --out experiment-001 --on-conflict rename-attempt
```

### 2.4 Auto-merge is convenience, not the canonical path

Official workflow uses explicit `merge`.

Convenience flow may exist:

```bash
lorq judge \
  --input shard-001 \
  --input shard-002 \
  --auto-merge \
  --out experiment-001
```

Auto-merge writes a real experiment package by default. A `--temp` mode can exist for throwaway local use.

---

## 3. Core concepts and terminology

### 3.1 Attempt, not repetition

Use **attempt** in product UX instead of repetition.

An attempt is one independent execution of the same case under the same mode.

Cell identity:

```text
cell_id = case_id + mode_id + attempt_id
```

Execution fingerprint:

```text
fingerprint = repo_revision
            + backend/runtime profile
            + adapter version
            + agent version
            + skill/tool config hash
            + prompt/config hashes
            + permission policy
```

The logical matrix stays understandable, while fingerprints protect against hidden apples-to-oranges merges.

### 3.2 Paired attempts

Attempts are paired when possible, but the product records whether the pairing is real or nominal.

Reports should distinguish:

```text
paired comparison:
  baseline attempt 1 vs graphify attempt 1

unpaired comparison:
  baseline attempt 1 vs graphify attempt 3
```

Default judging compares matching attempts. Optional advanced mode can compare all-vs-all attempts.

### 3.3 Modes

3+ modes are normal.

Example:

```yaml
modes:
  - baseline
  - graphify
  - graphify-plus
```

Global modes are the default. Per-case skips/overrides are allowed but advanced:

```yaml
cases:
  case-b:
    skip_modes:
      - graphify-plus
```

Reports must distinguish intentionally skipped cells from missing cells.

---

## 4. Experiment package model

### 4.1 Public layout

Canonical package shape:

```text
experiment/
  experiment.yaml
  runs/
    shard-001/
    shard-002/
  judgements/
    judge-primary/
    judge-strict-v2/
  reports/
    report.json
    report.md
    cases/
      case-a.json
      case-a.md
      case-b.json
      case-b.md
  .lorq/
    coverage.json
    fingerprints.json
    merge-log.json
    integrity.json
    cells/
```

Public folders remain easy to browse. `.lorq/` contains machine indexes and provenance.

### 4.2 Preserve shards; index cells

Merged packages preserve original shards under `runs/`. The tool builds normalized cell indexes internally rather than rewriting raw evidence into a new shape.

```text
runs/
  shard-001/
  shard-002/

.lorq/
  cells/
  coverage.json
  fingerprints.json
```

### 4.3 Schema versioning stance

Keep a package schema version, but do not overbuild backward compatibility before real users exist.

```yaml
package_schema_version: 1
created_by:
  name: lorq
  cli_version: 1.0.0
  implementation: dotnet
```

Pre-v1 schemas are disposable.

Before .NET v1:

- Schemas may change freely.
- Fixtures may be regenerated.
- Python exporter follows the current canonical format.

At .NET v1:

- `package_schema_version: 1` becomes the first stable supported package format.

Do not build migration commands yet.

---

## 5. CLI shape

### 5.1 Primary commands stay flat

Use flat primary commands for the main product loop:

```bash
lorq run
lorq merge
lorq judge
lorq report
```

Lifecycle and diagnostics can live under subcommands:

```bash
lorq experiment inspect
lorq experiment validate
lorq experiment coverage
lorq experiment repair
lorq adapter conformance
lorq doctor
```

### 5.2 Ad-hoc runs become experiments automatically

`run` should not require a manifest for simple local use.

A quick run like:

```bash
lorq run --cases case-a --modes baseline,graphify --out shard-001
```

should auto-create or attach to an `experiment.yaml`.

### 5.3 Planned shards later

v1 supports manual shard selection first:

```bash
lorq run --cases case-a,case-b --modes baseline,graphify --attempts 1 --out shard-001
lorq run --cases case-c,case-d --modes baseline,graphify --attempts 1 --out shard-002
```

Later milestone adds planner-generated shards:

```bash
lorq plan --cases all --modes baseline,graphify,graphify-plus --attempts 3 --shard-size 5
lorq run --shard shard-001
```

---

## 6. Judgement model

### 6.1 Judgements are named passes

Judgement passes are attached to the experiment package but separate from immutable raw execution evidence.

They live under:

```text
judgements/
  2026-06-18T14-32-10-gpt55/
  judge-strict-v2/
```

Names are auto-generated by default and can be explicit:

```bash
lorq judge --input experiment-001
lorq judge --input experiment-001 --name judge-gpt55-strict
```

Judgements are immutable by default. Local iteration may support explicit overwrite:

```bash
lorq judge --input experiment-001 --name scratch --overwrite
```

### 6.2 Multiple judgements are allowed

A package may contain many judgement passes.

Reports use one primary judgement for the headline decision and optional secondary judgements for disagreement analysis.

```bash
lorq report \
  --input experiment-001 \
  --primary-judgement judge-primary \
  --secondary-judgement judge-strict-v2
```

If exactly one judgement exists, report generation may auto-select it. If multiple exist, `--primary-judgement` is required.

Secondary judgements are explicit by default. Add convenience flag later:

```bash
lorq report --input experiment-001 --primary-judgement judge-primary --include-all-judgements
```

### 6.3 Quality judgement is separate from execution/integrity grading

There are two different grading systems:

1. **Quality judgement**  
   Evaluates final answers only. It should not care whether the answer came from baseline, Graphify, Graphify-plus, Codex, Copilot, or another runtime.

2. **Execution/integrity evaluation**  
   Evaluates whether the run evidence can be trusted: traces, artifacts, fingerprints, setup, leakage, coverage, and comparability.

Final product verdict combines:

```text
quality result + time + price + validity gates
```

Quality is not linked to how the answer was produced. Execution/integrity can block or downgrade confidence in the decision.

### 6.4 Quality comparisons

For 3+ modes, default judging should be baseline-first, then targeted all-pairs.

Example:

```text
Required:
  baseline vs graphify
  baseline vs graphify-plus

Targeted/optional:
  graphify vs graphify-plus
```

This controls judge cost while still answering whether the top candidates differ from each other.

### 6.5 Judge rationale

Case review packs include structured rationale summaries inline and link to full judge output.

Inline:

```text
winner
confidence
why_winner_won
main_missing_points
notable_disagreements
quality_risks
```

Linked:

```text
full judge JSON
raw comparison input
full final answers
raw traces/artifacts
```

---

## 7. Value verdict model

### 7.1 Verdict is value-based, not quality-only

A mode can win because it is better quality at acceptable cost, or because it has roughly equal quality at materially lower cost/time.

Supported adoption reasons:

```text
1. Better quality at acceptable extra cost/time.
2. Similar quality at materially lower cost/time.
```

### 7.2 Official verdict labels

Use adoption labels plus reason tags.

Verdicts:

```yaml
verdict:
  - adopt
  - adopt_selectively
  - keep_current
  - reject
  - rerun_required
  - invalid
```

Reason tags:

```yaml
reason_tags:
  - quality_gain
  - cost_saving
  - time_saving
  - quality_regression
  - too_expensive
  - integrity_warning
  - incomplete_coverage
```

### 7.3 Raw and normalized cost/time

Reports show both raw totals and normalized comparison metrics.

Budget view:

```text
total tokens
total price
total wall-clock time
```

Comparison view:

```text
cost per completed cell
cost per successful answer
time per completed attempt
time per accepted-quality answer
```

Verdicts should use normalized metrics, not raw totals, so a mode does not appear cheaper just because it failed or skipped more cells.

### 7.4 Execution/integrity gates

Execution/integrity is a validity gate, not a quality score.

Severity model:

```text
Critical issue:
  invalid / rerun_required

Moderate issue:
  adopt_with_caution / lower confidence

Minor issue:
  warning only
```

Examples:

Critical:

```text
incomparable repo revision
missing final answer artifact
judge input mismatch
leaked global skill/tool
invalid artifact
```

Warning/moderate:

```text
partial trace missing
cost estimate approximate
non-paired attempts
incomplete matrix
```

### 7.5 Failure classification

Failed cells are classified before deciding how they affect judgement and reporting.

Failure categories:

```yaml
failure_type:
  - no_final_answer
  - timeout
  - setup_failure
  - adapter_failed
  - permission_denied
  - invalid_artifact
```

Rules:

```text
no_final_answer / timeout:
  quality loss if comparable modes answered

setup_failure / adapter_failed:
  execution validity issue; may require rerun

permission_denied:
  configuration issue

invalid_artifact:
  integrity issue; may block judgement/report
```

---

## 8. Report model

### 8.1 JSON canonical, Markdown default rendering

Canonical data:

```text
reports/report.json
```

Default human artifact:

```text
reports/report.md
```

Future renderer:

```text
reports/report.html
```

Rule:

> Markdown is a rendering of `report.json`. It must not contain decision data missing from `report.json`.

### 8.2 Summary plus per-case review packs

Reports include a top-level summary plus per-case review packs by default.

```text
reports/
  report.json
  report.md
  cases/
    case-a.json
    case-a.md
    case-b.json
    case-b.md
```

Top-level report answers:

```text
What should we adopt?
Why?
What changed in quality, time, and price?
Can we trust the comparison?
Where are the biggest disagreements or risks?
```

Case packs answer:

```text
What was the task?
What did each mode answer?
Which mode won?
What evidence did the judge use?
What were the cost/time differences?
Were there execution/integrity warnings?
```

### 8.3 Case pack answer handling

Case packs include inline summaries plus stable links to full answer artifacts.

They should not inline huge final answers by default.

```text
case-a.md
  task
  modes compared
  judge result
  answer summaries
  cost/time table
  integrity warnings
  links:
    baseline full answer
    graphify full answer
    graphify-plus full answer
    raw traces
    full judge JSON
```

---

## 9. Adapter architecture

### 9.1 Core is SDK-independent

The core product should not be an LLM SDK wrapper. It should be an agent-runtime evaluation harness.

Core domain concepts:

```text
Experiment
RunShard
RunCell
Attempt
Mode
AgentRuntime
JudgeBackend
ArtifactStore
Trace
CostUsage
Fingerprint
JudgementPass
Report
```

The core should not depend directly on Codex, Copilot, OpenAI, Anthropic, or any one SDK.

### 9.2 Final implementation language

Target final core: **.NET**.

Rationale:

- Strong typed domain model.
- Good CLI and cross-platform story.
- Good fit for enterprise/industrial use.
- First-class path for Copilot SDK integration.
- Still supports provider-neutral architecture through adapters.

Strategic backend roles:

```text
Heavy/local experimental runs:
  Codex-first

Industrial/company adoption path:
  Copilot-first

Final product core:
  .NET
```

Adapter priority:

```text
1. Copilot SDK adapter
2. Codex process/CLI adapter
3. External one-shot adapter protocol
4. Fake deterministic adapter for conformance and migration
```

### 9.3 Built-in adapters plus external process escape hatch

v1 should ship built-in adapters first and an external one-shot adapter protocol as the plugin escape hatch.

Built-in:

```text
copilot
codex
fake
shell/mock
```

External:

```text
any executable that implements the lorq adapter protocol
```

### 9.4 Generic contract plus provider-specific extension blocks

Core fields stay stable. Provider-specific config lives under namespaced extension blocks.

Example:

```yaml
backend:
  type: codex
  model: gpt-5.5
  permission_profile: workspace-write
  timeout_seconds: 1800
  extensions:
    codex:
      approval_policy: on-request
      workspace_policy: workspace-write
```

Example:

```yaml
backend:
  type: copilot
  model: auto
  permission_profile: workspace-write
  timeout_seconds: 1800
  extensions:
    copilot:
      cli_path: auto
      mcp_config: ./mcp.json
```

### 9.5 Agent adapters and judge adapters use separate contracts

Reuse provider plumbing where possible, but keep business contracts separate.

```text
AgentRuntimeAdapter.run_cell(...)
JudgeAdapter.evaluate_comparison(...)
```

Shared lower-level plumbing may include:

```text
ProviderClient
AuthConfig
ModelConfig
UsageCapture
RetryPolicy
RateLimitPolicy
TranscriptLogger
```

### 9.6 External adapter protocol

Start with one-shot external adapters. Persistent/long-running adapters can come later.

Canonical command shape:

```bash
adapter run-cell --input input.json --output output-dir
```

Adapter output is file-based. Stdout/stderr are logs, not canonical result data.

```text
output-dir/
  cell_result.json
  final_answer.md
  artifacts/
  trace/

runner-captured:
  adapter.stdout.log
  adapter.stderr.log
  adapter.exit_code
```

### 9.7 Full cell result contract

Every agent adapter must produce a full cell result contract, not just a final answer.

```text
cell/
  final_answer.md
  cell_result.json
  artifacts/
  trace/
```

Example `cell_result.json` shape:

```yaml
status: completed | timeout | no_final_answer | adapter_failed | permission_denied | invalid_artifact
final_answer_path: final_answer.md
usage:
  input_tokens: 1234
  output_tokens: 567
  cost_usd: 0.08
timing:
  started_at: "2026-06-18T10:00:00Z"
  completed_at: "2026-06-18T10:00:50Z"
  duration_seconds: 50
artifacts:
  - path: artifacts/result.json
trace:
  - path: trace/events.jsonl
warnings:
  - code: partial_trace
    message: trace was truncated
```

---

## 10. Runtime comparison scope

Runtime comparison is secondary and not part of the original core roadmap.

Primary v1 product:

```text
Compare modes/tools/skills within one agent runtime.
```

Secondary/future product:

```text
Compare agent runtimes.
Compare tool × runtime matrices.
```

v1 should record runtime metadata and comparison type, but official v1 reports should support single-runtime mode comparisons only.

Allowed schema fields:

```yaml
comparison:
  type: tool_mode_within_runtime
  baseline_mode: baseline

runtime:
  id: codex
  adapter: codex-cli
  adapter_version: ...
  model: ...
  permissions: ...
```

Official v1 report rule:

```text
Official verdicts require a single runtime.
Runtime-mixed reporting is not supported in v1.
```

Future comparison types can be reserved:

```yaml
comparison:
  type: agent_runtime
```

```yaml
comparison:
  type: tool_runtime_matrix
```

---

## 11. Validation and trust commands

Validation is part of the orchestration product, not a nice-to-have.

Because the product is an evidence harness, users need to trust packages, adapters, merges, fingerprints, coverage, judgements, and reports.

Roadmap commands:

```bash
lorq doctor
lorq experiment validate
lorq adapter conformance
```

v1.0 should include at least basic validation for:

```text
experiment package structure
coverage index
fingerprints
merge integrity
required cell artifacts
judgement/report references
```

v1.x should expand into:

```text
adapter conformance suite
environment doctor
repair suggestions
CI-safe validation output
```

Adapter conformance should test:

```text
success result
timeout result
no_final_answer result
adapter_failed result
permission_denied result
invalid_artifact result
usage/cost metadata
timing metadata
trace output
artifact references and checksums
warnings
stdout/stderr capture
exit-code consistency
```

---

## 12. Python v0 to .NET v1 migration plan

### 12.1 Migration strategy

Use Python to define fixtures/protocols. Implement the final core in .NET.

Python v0:

```text
proves behavior
generates canonical sample packages
defines fixture expectations
exports canonical v1-compatible packages
```

.NET v1:

```text
implements final product architecture
passes conformance fixtures from Python v0
continues product improvements after migration
```

### 12.2 Frozen comparable baseline gate

Start .NET migration only after Python v0 can produce one frozen, simple, deterministic, end-to-end benchmark.

The benchmark must prove orchestration, not LLM intelligence.

Gate requirements:

```text
small fixture setup
2–3 cases
2–3 modes
1 attempt each
split across 2 shards
merged into 1 experiment package
1 named judgement pass
report.json + report.md
per-case review packs
raw + normalized cost/time
at least one integrity warning fixture
at least one conflict/error fixture
golden expected outputs
```

### 12.3 Deterministic fake adapter and fake judge

The migration gate uses deterministic fake agent and fake judge adapters.

Fake agent adapter:

```text
deterministic answers
deterministic traces
deterministic costs/timing
controlled failures
controlled integrity warnings
```

Fake judge adapter:

```text
deterministic structured quality judgements
deterministic confidence
deterministic rationale summaries
deterministic value/verdict inputs
```

Real LLM-backed flows are adapter smoke tests, not migration gates.

Post-gate:

```text
Codex smoke run
Copilot smoke run
real judge smoke run
```

### 12.4 Scenario-driven fake adapter

Each test case declares fake behavior explicitly.

Example:

```yaml
cases:
  case-a:
    fake_behavior:
      baseline:
        result: success
        final_answer: answers/baseline-case-a.md
        cost_usd: 0.12
        duration_seconds: 80
      graphify:
        result: success
        final_answer: answers/graphify-case-a.md
        cost_usd: 0.08
        duration_seconds: 50
      graphify_plus:
        result: timeout
```

### 12.5 Fake judge fixtures

Use authored fake judge fixtures with optional metadata hints.

Example:

```yaml
quality_hint:
  baseline: adequate
  graphify: better
  graphify_plus: timeout

expected_judgement:
  winner: graphify
  confidence: high
  why_winner_won: Graphify answer contains the required file and method.
  main_missing_points:
    baseline:
      - missed the exact implementation file
```

The fake judge should test the exact judgement/report contract, not infer quality through LLM behavior.

### 12.6 Golden fixture suite

Before the .NET rewrite, Python v0 must produce golden fixtures for:

```text
run shard package
merge result
coverage index
fingerprint index
judge input
judge output
report summary
per-case report packs
conflict/error cases
```

Use both:

```text
hand-authored edge-case fixtures
generated smoke fixtures
```

Hand-authored fixtures:

```text
duplicate cell conflict
fingerprint mismatch
incomplete coverage
multiple judgements requiring explicit primary
runtime-mixed official report rejection
missing final answer artifact
invalid judgement input
```

Generated smoke fixtures:

```text
small deterministic fixture
2–3 modes
1 attempt
split across 2 shards
merged into one experiment
judged once
reported once
```

---

## 13. v1 milestone: shard-safe decision loop

v1 should deliver the smallest reliable end-to-end product loop.

Must have:

```text
run --no-judge
merge with conflict detection
judge with named passes
report with primary judgement
quality separate from execution validity
raw + normalized cost/time
basic value verdict
summary report + per-case review packs
JSON canonical report + Markdown rendering
basic package validation
fake deterministic adapter and judge for conformance
```

v1 is not:

```text
a full runtime comparison product
a full HTML dashboard
a mature planner-generated shard system
a complete CI gate framework
a legacy schema migration system
an LLM intelligence benchmark
```

---

## 14. Suggested roadmap milestones

### Milestone 0 — Python freeze and conformance baseline

Goal: freeze a simple comparable point before .NET migration.

Deliverables:

```text
canonical v1 package export from Python
scenario-driven fake adapter
fake judge fixture support
golden fixture suite
simple deterministic end-to-end benchmark
edge-case fixtures
exported report.json/report.md/case packs
basic validation of exported package
```

Exit criteria:

```text
Python can produce the frozen benchmark package.
Expected outputs are committed as golden fixtures.
The benchmark is manually understandable.
No real LLM intelligence is required to pass the gate.
```

### Milestone 1 — .NET core package model

Goal: implement the domain model and package reader/writer in .NET.

Deliverables:

```text
ExperimentPackage model
RunShard model
RunCell model
Attempt/mode/case model
Fingerprint model
Coverage index builder
Integrity index builder
Package schema validation
Golden fixture reader compatibility
```

Exit criteria:

```text
.NET can load Python-exported fixture packages.
.NET can validate package structure, coverage, fingerprints, and references.
```

### Milestone 2 — .NET run and merge loop

Goal: reproduce shard-safe execution and merge behavior.

Deliverables:

```text
lorq run --no-judge with fake adapter
lorq merge
conflict detection
duplicate cell handling
fingerprint mismatch handling
materialized package option
merge-log generation
coverage/fingerprint/integrity indexes
```

Exit criteria:

```text
.NET run/merge outputs match golden behavior.
Manual split execution works across two shards.
```

### Milestone 3 — .NET judge and report loop

Goal: complete the deterministic product loop.

Deliverables:

```text
lorq judge with fake judge
named judgement passes
auto-generated judgement names
explicit primary judgement behavior
report.json canonical output
report.md renderer
per-case review packs
quality/value verdict logic
raw + normalized cost/time
failure classification handling
```

Exit criteria:

```text
.NET reproduces the frozen Python end-to-end benchmark.
Reports are reviewable without opening raw artifacts.
Case packs link to full answers and judge JSON.
```

### Milestone 4 — Adapter system and conformance

Goal: make SDK/runtime pluggability real.

Deliverables:

```text
AgentRuntimeAdapter contract
JudgeAdapter contract
external one-shot adapter protocol
file-based output contract
adapter stdout/stderr capture
lorq adapter conformance
fake adapter conformance fixtures
Codex process/CLI adapter skeleton
Copilot SDK adapter skeleton
```

Exit criteria:

```text
A third-party executable can implement the protocol.
Adapter conformance catches missing files, invalid statuses, bad metadata, permission denial, warning handling, exit-code mismatches, and trace/artifact issues.
```

### Milestone 5 — Real runtime smoke tests

Goal: validate real adapters without making them migration blockers.

Deliverables:

```text
Codex smoke run
Copilot smoke run
real judge smoke run
runtime metadata capture
permission profile capture
adapter version capture
provider extension blocks
```

Exit criteria:

```text
Real adapters can produce valid cell result contracts.
Reports remain single-runtime official only.
Runtime-mixed experiments are stored but not officially verdict-scored in v1.
```

### Milestone 6 — Local-first, CI-safe polish

Goal: make the product usable locally and safe for automation.

Deliverables:

```text
stable exit codes
machine-readable command summaries
report JSON suitable for CI inspection
no hidden prompts in CI mode
lorq experiment validate
lorq doctor basic checks
clear error messages and repair suggestions
```

Exit criteria:

```text
The tool feels good locally.
CI can consume outputs externally.
Built-in --ci gates can wait until v1.x.
```

---

## 15. Later roadmap

### v1.x

```text
planner-generated shards
run --shard
optional --allow-partial / strict matrix modes
basic --ci report gate
expanded adapter conformance
HTML report renderer
report comparison across experiments
repair suggestions
richer integrity severity rules
```

### v2

```text
runtime comparison reports
tool × runtime matrix analysis
persistent/long-running adapter protocol
multi-judge consensus reports
advanced value formula configuration
interactive dashboard
historical trend analysis
organization/team benchmark packs
```

---

## 16. Decisions explicitly deferred

Do not build these into v1 unless necessary:

```text
full runtime comparison verdicts
schema migration system
HTML dashboard
persistent adapter service protocol
full CI threshold language
advanced statistical all-vs-all comparison engine
large benchmark authoring suite
legacy compatibility promises
```

---

## 17. Final v1 definition

v1 succeeds when a user can run this loop reliably:

```bash
lorq run --no-judge --out shard-001
lorq run --no-judge --out shard-002

lorq merge shard-001 shard-002 --out experiment-001

lorq judge --input experiment-001 --name judge-primary

lorq report \
  --input experiment-001 \
  --primary-judgement judge-primary
```

And receive:

```text
experiment.yaml
runs/ with original shards
.lorq/ indexes
judgements/judge-primary/
reports/report.json
reports/report.md
reports/cases/*.json
reports/cases/*.md
```

The report must answer:

```text
Which mode should we adopt, if any?
Is the recommendation driven by quality gain, cost saving, time saving, or risk?
Can we trust the comparison?
Which cases drove the decision?
What failed, timed out, or was skipped?
Where are the full answers, traces, artifacts, and judge outputs?
```

That is the consolidated roadmap.
