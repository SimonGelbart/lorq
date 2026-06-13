# Simon Graphify smoke test

This package is prepared for the next controlled Graphify smoke test.

It includes:

- your uploaded base skills in `execution/base-skills/`
- the default Graphify skill in `execution/skills/graphify/`
- the query-planner companion skill in `execution/skills/graphify-query-planner/`
- base-aware modes:
  - `base-only`
  - `base-default-graphify`
  - `base-default-graphify-plus`
- Codex CLI per-run `HOME` / `CODEX_HOME` isolation by default for the `codex` profile
- Graphify modes that copy `.graphifyignore`, run `graphify codex install`, then run `graphify .`

## First checks

```bash
agent-eval --validate-config
agent-eval --check-agent --agent-profile codex
agent-eval --run-conformance
```

If `--check-agent` fails because your Codex auth is stored in your normal Codex home, diagnose with:

```bash
agent-eval --check-agent --agent-profile codex --no-isolate-agent-home
```

For formal runs, prefer keeping isolation enabled and authenticating through environment variables where possible.

## Recommended first real run

No LLM judge yet:

```bash
agent-eval \
  --repo /path/to/nopCommerce \
  --modes base-default-graphify,base-default-graphify-plus \
  --cases admin-permissions,inactive-customers-csv \
  --prompt-style neutral \
  --dirty-policy fail \
  --require-agent-available \
  --no-judge \
  --out ./results/graphify-plus-smoke-codex
```

Optional baseline-on-your-normal-stack comparison:

```bash
agent-eval \
  --repo /path/to/nopCommerce \
  --modes base-only,base-default-graphify,base-default-graphify-plus \
  --cases admin-permissions,inactive-customers-csv \
  --prompt-style neutral \
  --dirty-policy fail \
  --require-agent-available \
  --no-judge \
  --out ./results/graphify-plus-smoke-codex-with-base-baseline
```

## What to inspect

Start with:

```text
results/.../summary.md
results/.../case_comparison.md
results/.../fairness_warnings.md
results/.../failed_runs.md
results/.../scorecard.csv
```

For each run, inspect:

```text
active-skills.json
agent.summary.json
validation.json
events.summary.json
```

`active-skills.json` confirms the workspace skill set materialized by the evaluator. It does not include Codex system-bundled skills.

## Graphify setup in this package

The Graphify modes now prepare each fresh worktree with:

```text
copy execution/configs/graphify/.graphifyignore -> .graphifyignore
graphify codex install
graphify .
```

This replaces the earlier invalid `graphify build` setup command.

## Optional cost estimate with cached tokens

Token accounting now includes cached input tokens. To estimate cost, pass a pricing model configured in `eval.config.yaml` or `pricing/openai-pricing.example.yaml`:

```bash
python3 -m eval_runner.cli \
  --repo /home/simon/repos/to-check/misc/nopCommerce/ \
  --modes base-only,base-default-graphify,base-default-graphify-plus \
  --cases admin-permissions,inactive-customers-csv \
  --prompt-style neutral \
  --dirty-policy fail \
  --require-agent-available \
  --no-judge \
  --no-isolate-agent-home \
  --pricing-model gpt-5.5 \
  --out ./results/graphify-plus-smoke-codex-with-base-baseline
```

Or regenerate reports for an existing run if raw `stdout.raw.jsonl` files are still present:

```bash
python3 -m eval_runner.cli \
  --out ./results/graphify-plus-smoke-codex-with-base-baseline \
  --report-only \
  --pricing-model gpt-5.5
```

The estimator uses:

```text
uncached_input = input_tokens - cached_input_tokens
cost = uncached_input * input_rate + cached_input * cached_rate + output * output_rate
```

All rates are per 1M tokens.
