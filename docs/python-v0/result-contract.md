# Result contract

Every completed or failed run directory should contain the following stable files:

```text
prompt.txt
answer.md
stdout.raw.jsonl or stdout.raw.txt
stderr.txt
agent.summary.json
run.manifest.json
summary.json
validation.json
events.normalized.jsonl
events.summary.json
```

Result roots contain:

```text
summary.json
aggregates.json
scorecard.csv
aggregate_scorecard.csv
summary.md
case_comparison.md
mode_summary.md
fairness_warnings.md
failed_runs.md
```

## Schema versions

Machine-readable outputs include `schema_version` and `contract_version` where relevant.

Stable schema IDs:

```text
agent-eval.contract.v1
agent-eval.result.v1
agent-eval.summary.v1
agent-eval.aggregate-summary.v1
agent-eval.run-manifest.v1
agent-eval.validation-result.v1
agent-eval.normalized-event.v1
agent-eval.event-summary.v1
```

Future implementations, including a .NET port, should preserve these fields when targeting the v1 contract.
