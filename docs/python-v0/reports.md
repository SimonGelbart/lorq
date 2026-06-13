# Reports

Top-level reports:

```text
summary.md
case_comparison.md
mode_summary.md
fairness_warnings.md
failed_runs.md
scorecard.csv
aggregate_scorecard.csv
summary.json
aggregates.json
```

## `summary.md`

High-level run table and aggregate overview.

## `case_comparison.md`

Mode-by-mode comparison for each case.

## `mode_summary.md`

Aggregated view by mode.

## `fairness_warnings.md`

Flags comparison risks such as:

- forced and neutral prompt styles mixed together
- multiple agent backends in one comparison
- dirty source repository
- setup failures
- text-only traces where command metrics are best-effort

## `failed_runs.md`

Quick diagnosis table for failed or incomplete runs.

## CSV files

- `scorecard.csv`: one row per run
- `aggregate_scorecard.csv`: one row per mode/case/prompt-style aggregate

## Explain a run

```bash
agent-eval --explain-run ./results/foo/runs/mode/neutral/case/r1
```

## Compare result folders

```bash
agent-eval --compare-results ./results/a ./results/b
```
