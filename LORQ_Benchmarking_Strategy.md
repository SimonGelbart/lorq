# LORQ Benchmarking Strategy

## Purpose

This benchmark compares four LORQ execution modes on six nopCommerce code-understanding tasks. The goal is not to prove an absolute winner from a single run, but to understand the practical tradeoffs between answer quality, model cost, prompt execution time, setup overhead, and tool behavior.

The final comparison uses the corrected **default Graphify** runs, not the earlier Graphify-plus runs.

## Compared modes

The benchmark compares four modes:

| Short name | LORQ mode | Description |
|---|---|---|
| `base` | `base-only` | Plain agent run without Graphify or Code Review Graph assistance. |
| `graphify` | `base-default-graphify` | Agent run with default Graphify context/tooling enabled. |
| `crg` | `base-default-code-review-graph` | Agent run with Code Review Graph available through its out-of-box MCP-style workflow. |
| `graphify+crg` | `base-default-graphify-code-review-graph` | Combined mode with both Graphify and Code Review Graph setup. |

The CRG mode is intentionally treated as an out-of-box tool mode. It is not penalized for using many tools; any overhead is captured only through the statistical and balanced-value metrics.

## Use cases

The benchmark uses six use cases. They are split into two classes for global weighting.

### Everyday cases — 70% of global weighting

These cases represent the kind of daily repository navigation work where a developer wants a useful answer quickly.

| Case | Intent |
|---|---|
| `admin-product-edit-field` | Locate the files and flow needed to add a new field to the admin product create/edit experience. |
| `admin-permissions` | Explain how admin permission checks are wired and where permission records/checks are defined. |
| `inactive-customers-csv` | Trace the inactive-customer export path through admin/customer/export-related code. |

Each everyday case contributes `70% / 3 = 23.33%` to the weighted global score.

### Hard graph-friendly cases — 30% of global weighting

These cases are expected to benefit more from structural search, graph context, relationship tracing, or broader impact analysis.

| Case | Intent |
|---|---|
| `auth-discovery` | Discover the major customer authentication, registration, password recovery, MFA, and external-auth paths. |
| `order-total-impact` | Identify likely blast radius of changing the order total calculation contract. |
| `cart-to-order-flow` | Trace how shopping-cart state becomes order/order-item persistence through checkout and cleanup. |

Each hard case contributes `30% / 3 = 10%` to the weighted global score.

## Prompt strategy

All runs use the same case task definition and a neutral prompt style. The benchmark focuses on comparing tool/mode effects, not prompt wording variations.

The prompts are designed to require source-backed repository understanding rather than generic explanation. A good answer should:

- identify relevant files and symbols;
- explain relationships between those files;
- distinguish confirmed source evidence from inferred next steps;
- remain useful as a developer handoff;
- avoid unsupported claims;
- avoid excessive wandering beyond the task.

The prompt style is deliberately neutral so that Graphify, CRG, combined, and base modes all receive a fair task framing.

## Sandboxed execution environment

Each benchmark cell is run in an isolated sandbox/worktree. This protects the target repository and prevents one run from contaminating another run.

The sandboxing strategy means that setup work is repeated for every run, including graph/index generation where applicable. This is useful for reproducible benchmarking, but it is not the same as normal steady-state developer usage.

Important distinction:

| Time concept | Meaning | Used in final value score? |
|---|---|---|
| Setup time | Cold-start setup such as graph generation, CRG build, MCP config, AGENTS/skill injection, or mode-specific preparation. | No, reported separately. |
| Prompt execution time | The actual agent/prompt execution time once setup is complete. | Yes. |
| Total cold-start time | Setup time + prompt execution time. | Reported as benchmark/cold-start overhead only. |

The final balanced recommendation uses **prompt execution time only**, because in real usage the complete graph/index would normally not be regenerated for every prompt.

## Setup strategy by mode

### Base

Base mode performs no graph setup. It runs the agent directly against the isolated repository worktree.

### Graphify

Graphify mode prepares default Graphify context/tooling for the run. In the sandbox benchmark, Graphify setup is repeated per run. In real use, this should be interpreted as a cold-start/indexing cost rather than a per-prompt cost.

### Code Review Graph

CRG mode builds and serves Code Review Graph context for the isolated worktree. The setup uses the out-of-box CRG approach rather than a narrowly customized tool allow-list.

The benchmark records whether CRG tool calls appear in normalized events, but content quality is judged independently of tool count.

### Graphify + CRG

Combined mode performs both Graphify and CRG setup. This mode is expected to have the highest setup overhead and should only win if it produces enough quality lift to justify the extra complexity.

## Captured run artifacts

Each run emits a structured set of artifacts. The analysis uses the following file types:

| File | Purpose |
|---|---|
| `answer.md` | Final answer produced by the agent. Used for content judging. |
| `agent.summary.json` | Agent status, runtime, token usage, pricing summary, and execution metadata. |
| `events.summary.json` | High-level event/tool/message summary. |
| `events.normalized.jsonl` | Normalized event stream used to count tools, turns, shell commands, MCP calls, and graph-related signals. |
| `validation.json` | Deterministic validation output. Used as a diagnostic signal, not as the final quality score. |
| setup logs | Used to identify setup commands, setup duration, and whether graph/CRG setup occurred. |

## Statistical metrics

The statistical pass extracts metrics for every run.

### Token metrics

The benchmark tracks:

- input tokens;
- cached input tokens;
- uncached input tokens;
- output tokens;
- reasoning output tokens;
- total tokens;
- cache hit rate.

Derived fields:

```text
uncached_input_tokens = input_tokens - cached_input_tokens
cache_hit_rate = cached_input_tokens / input_tokens
```

Cost interpretation focuses especially on uncached input and output tokens, because high total token volume can be less expensive when most input is cached.

### Cost metrics

Estimated model cost is read from the LORQ/agent summary and reflects the configured pricing model for the run. Conceptually:

```text
estimated_cost = uncached_input_cost + cached_input_cost + output_cost
```

The benchmark does not manually override the price calculation; it uses the pricing emitted by the run summaries.

### Time metrics

The analysis keeps three time measures separate:

```text
prompt_execution_time_s = agent_elapsed_s
setup_time_s = setup_elapsed_s
total_cold_start_time_s = setup_elapsed_s + agent_elapsed_s
```

The final value score uses `prompt_execution_time_s`. Setup time is still included in the artifact as a cold-start/onboarding/CI overhead caveat.

### Tool and turn metrics

Tool usage is derived from normalized events and summaries.

Tracked counts include:

- normalized tool call count;
- assistant/message turn signals;
- shell/tool commands;
- Graphify shell/query signals;
- CRG MCP tool calls;
- Code Review Graph shell/`uvx` setup signals.

Tool count is not directly judged as good or bad. It is used to explain differences in cost, latency, and answer behavior.

## Content judging strategy

The content pass evaluates the final answer itself.

### Blind judging

Answers are judged blind to mode first. The judge sees the case/task and the answer, but avoids using the mode identity when assigning content scores. The mode is unblinded only after scoring for interpretation.

This reduces bias such as assuming graph-based modes should be better.

### Strict rubric

The final re-judging uses a stricter, presentation-grade rubric. Under this rubric, “mostly useful” is not automatically excellent.

Score bands:

| Score range | Interpretation |
|---:|---|
| 85–89 | Excellent / near presentation-ready. |
| 80–84 | Strong, but with identifiable gaps. |
| 70–79 | Usable, but needs review or rework. |
| 60–69 | Partial or weak for the task. |
| <60 | Not reliable. |

No run is expected to score 90+ unless it is both highly complete and highly precise.

### Content dimensions

Each answer is scored on six dimensions:

| Dimension | Weight | Meaning |
|---|---:|---|
| Correctness | 30% | Are the claims accurate and consistent with the source? |
| Coverage | 20% | Does the answer cover the required files, symbols, and relationships? |
| Evidence quality | 20% | Are source references precise and useful? |
| Task focus | 15% | Does the answer stay on the requested workflow rather than drifting? |
| Actionability | 10% | Could a developer use this answer for the next step? |
| Caveats / uncertainty discipline | 5% | Does the answer avoid overclaiming and distinguish facts from inference? |

Weighted score:

```text
content_score_0_100 =
  100 * (
    0.30 * correctness_0_5 +
    0.20 * coverage_0_5 +
    0.20 * evidence_quality_0_5 +
    0.15 * task_focus_0_5 +
    0.10 * actionability_0_5 +
    0.05 * caveats_0_5
  ) / 5
```

### Deterministic validation

`validation.json` is used only as a diagnostic signal. It is not the final quality score.

Reason: deterministic validators can fail when an answer is useful but uses path formatting or evidence formatting that the extractor does not recognize. Conversely, passing validation does not guarantee that the answer is the best developer handoff.

## Balanced-value calculation

Balanced value combines strict content quality with price and prompt execution time.

The goal is not to reward cheap but weak answers. A lower-cost run only wins if the answer remains sufficiently useful.

### Per-case normalization

For each case, identify:

```text
case_min_cost = minimum estimated_cost_usd across modes for that case
case_min_prompt_time = minimum prompt_execution_time_s across modes for that case
case_best_content = maximum strict_content_score_0_100 across modes for that case
```

For each run:

```text
cost_ratio_to_case_min = estimated_cost_usd / case_min_cost
prompt_time_ratio_to_case_min = prompt_execution_time_s / case_min_prompt_time
score_gap_to_best = case_best_content - strict_content_score_0_100
```

The prompt-time burden index combines cost and prompt latency:

```text
prompt_burden_index = (cost_ratio_to_case_min + prompt_time_ratio_to_case_min) / 2
```

A run at the case minimum for both cost and prompt time has a burden index of `1.0`.

### Balanced score formula

The prompt-time balanced score is:

```text
prompt_time_balanced_score_0_100 =
  strict_content_score_0_100
  - 0.35 * score_gap_to_best
  - 2.0 * (prompt_burden_index - 1.0)
```

Interpretation:

- content is primary;
- falling behind the best answer for the same case is penalized;
- cost/time overhead is penalized, but not enough to let a weak cheap answer beat a much better answer automatically;
- setup time is not included in this formula.

### Quality per dollar and quality per minute

Additional diagnostic ratios are calculated:

```text
quality_per_dollar = strict_content_score_0_100 / estimated_cost_usd
quality_per_prompt_minute = strict_content_score_0_100 / (prompt_execution_time_s / 60)
cost_per_quality_point = estimated_cost_usd / strict_content_score_0_100
prompt_time_per_quality_point_s = prompt_execution_time_s / strict_content_score_0_100
```

These ratios help explain tradeoffs but are not used alone as the final recommendation, because ratio metrics can over-reward cheap but mediocre answers.

## Global weighting

Global mode scores are weighted by case class:

```text
everyday cases = 70% total
hard graph-friendly cases = 30% total
```

Per-case weights:

```text
admin-product-edit-field = 23.33%
admin-permissions = 23.33%
inactive-customers-csv = 23.33%
auth-discovery = 10.00%
order-total-impact = 10.00%
cart-to-order-flow = 10.00%
```

For each mode:

```text
weighted_content_score = sum(case_weight * strict_content_score_0_100)
weighted_cost = sum(case_weight * estimated_cost_usd)
weighted_prompt_time = sum(case_weight * prompt_execution_time_s)
weighted_setup_time = sum(case_weight * setup_elapsed_s)
weighted_balanced_score = sum(case_weight * prompt_time_balanced_score_0_100)
```

## Pareto and winner analysis

For each case, the benchmark identifies:

- quality winner;
- value winner;
- cost winner;
- prompt-time winner;
- dominated modes.

A mode is considered dominated when another mode is both better in quality and no worse in the relevant cost/time dimensions.

The final recommendation uses both:

1. per-case winners, because different tasks benefit from different context strategies;
2. global weighted winner, because the user cares more about everyday workflow performance than hard graph-heavy cases.

## Interaction-effect analysis

The four-mode matrix allows a rough interaction check between Graphify and CRG.

For each case, content interaction is computed as:

```text
interaction_effect =
  score(graphify+crg)
  - score(graphify)
  - score(crg)
  + score(base)
```

Interpretation:

| Interaction value | Meaning |
|---:|---|
| Positive | Graphify and CRG complement each other. |
| Near zero | Combined mode adds little beyond the individual modes. |
| Negative | Combined mode may be redundant, noisy, or less focused. |

This is descriptive, not a causal proof, because each cell has only one repetition.

## Important interpretation caveats

### One repetition per cell

Each case/mode combination was run once. This is enough to identify directional patterns but not enough for strong statistical claims.

Recommended follow-up: repeat the closest or most important cells, especially where the content score difference is small.

### Setup time is benchmark overhead

Because each run is sandboxed, setup is repeated more often than it would be in real usage. Graph/index generation should be interpreted as cold-start overhead, not per-prompt latency.

### Graphify did not reduce model cost in this run

Graphify improved strict content score and prompt-time balanced value, but it did not reduce model cost. It often improved navigation and answer quality while still causing substantial source reading and fresh context usage.

Presentation-safe phrasing:

```text
Graphify improved answer quality and steady-state prompt performance in this run, but it should not be presented as a guaranteed token-saving mechanism.
```

### CRG tool availability/use must be interpreted carefully

CRG is judged by answer quality, not by whether it used a particular number of tools. However, CRG tool-call counts are still useful diagnostically. Some runs may not demonstrate the full potential of CRG if the agent did not actually use CRG tools meaningfully.

### Deterministic validation is not the judge

Validation outcomes are helpful for debugging and consistency checks, but strict content judging is the primary quality assessment.

## Final benchmark interpretation

Under the corrected full six-case matrix, strict re-judging, and prompt-time-only value calculation:

- **Graphify** is the strongest steady-state default recommendation.
- **Base** remains a strong low-complexity fallback.
- **CRG-only** is not compelling as a default in this run.
- **Graphify + CRG** is useful for specific hard discovery scenarios, especially when higher cost/complexity is acceptable, but is not justified as the default daily mode.

The most accurate high-level conclusion is:

```text
Graphify is a quality and workflow-navigation improvement in this benchmark, not a guaranteed token-cost optimization.
```
