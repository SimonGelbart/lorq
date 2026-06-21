# 0004 — CLI command model and compatibility aliases

## Status

Accepted.

## Context

The .NET CLI grew from implementation-specific commands such as `merge-shards`, `judge-package`, `report-package`, and `validate-package`. The product roadmap uses a simpler product loop: `run`, `merge`, `judge`, `report`, and `validate`. Adapter lifecycle commands also need room for grouped diagnostics such as `adapter conformance`.

Without a decision record, roadmap examples, how-to docs, and compatibility aliases can drift from the intended user-facing command model.

## Decision

Primary product-loop commands should be flat and named after user tasks:

- `run`
- `merge`
- `judge`
- `report`
- `validate`

Lifecycle, diagnostics, and administrative surfaces may use command groups when grouping improves discoverability:

- `adapter conformance`
- `experiment validate`
- `experiment inspect`
- `doctor`

Implementation-oriented command names may remain as compatibility aliases during migration. Documentation should prefer the canonical command once it exists, while reference docs may list aliases explicitly.

## Consequences

- New user-facing commands should use product vocabulary rather than internal package implementation names.
- Compatibility aliases can reduce churn but should not become the preferred documentation path.
- CLI reference docs are the source of truth for currently implemented commands and aliases.
- Roadmap examples may use target canonical commands, but how-to docs must use commands that exist today.
