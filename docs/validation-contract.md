# Validation contract

Validation results are written to `validation.json` and follow `schemas/validation-result.schema.json`.

Validation combines:

- answer checks
- source evidence checks
- behavior checks over normalized events
- optional judge results

Hard checks determine `validation.ok`. Soft checks are reported but do not fail the run.

Stable top-level fields:

```json
{
  "schema_version": "agent-eval.validation-result.v1",
  "contract_version": "agent-eval.contract.v1",
  "ok": true,
  "hard_passed": 3,
  "hard_total": 3,
  "soft_passed": 1,
  "soft_total": 2,
  "answer": {},
  "behavior": {},
  "checks": []
}
```
