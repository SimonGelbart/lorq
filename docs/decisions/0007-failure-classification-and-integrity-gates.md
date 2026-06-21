# 0007 — Failure classification and integrity gates

## Status

Accepted.

## Context

LORQ evaluates both answer quality and whether execution evidence can be trusted. Adapter conformance and package validation expose failures such as missing final answers, timeouts, adapter process failures, permission issues, and invalid artifacts. Roadmap text described these as execution or integrity gates, but the classification policy was not captured as a durable decision.

## Decision

Execution and integrity findings are validity gates, not quality scores. LORQ should classify failures before judgement and reporting decide how those failures affect confidence, rerun requirements, or adoption verdicts.

Stable product-facing failure classes should be explicit and few. The target taxonomy is:

- `no_final_answer`
- `timeout`
- `setup_failure`
- `adapter_failed`
- `permission_denied`
- `invalid_artifact`

`adapter_failed` is the stable product term for adapter process crashes, missing evidence after a failed process, or equivalent adapter failures. Documentation may mention adapter crashes as examples, but status values should use `adapter_failed` unless a future schema version intentionally renames the class.

Severity is determined by trust impact:

- Critical findings can make a package invalid or require rerun.
- Moderate findings can lower confidence or require caution.
- Minor findings are warnings that remain visible but do not block the loop.

## Consequences

- Quality judges should not silently score cells whose execution evidence is invalid.
- Reports should distinguish answer quality from validity/integrity risk.
- Adapter conformance should include deterministic coverage for failure classes before real runtime smoke tests become important.
- Reference docs and schemas should use stable status values; roadmap prose should avoid inventing alternate status names.
