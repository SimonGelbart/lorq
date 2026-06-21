# LORQ documentation

The docs are organized by reader need rather than by work session. This follows the Diátaxis distinction between how-to guides, reference, explanation, and tutorials.

## How-to guides

Use these when you want to complete a task.

- `how-to/dotnet-deterministic-loop.md` — run the deterministic .NET package loop.
- `how-to/write-file-adapter.md` — write and conformance-check a one-shot file adapter.

## Reference

Use these when you need precise contracts and command behavior.

- `reference/cli.md` — current CLI commands and options.
- `reference/package-validation.md` — validation scope and stable error codes.
- `reference/file-adapter-protocol.md` — file-based adapter evidence contract and conformance command.

## Explanation

Use these when you need background, boundaries, and design rationale.

- `explanation/product-direction.md` — product thesis and non-goals.
- `dotnet/docs/cli-architecture.md` — .NET CLI composition and command-handler boundary.
- `dotnet/docs/engineering-guidelines.md` — .NET engineering rules.
- `dotnet/docs/full-loop-parity.md` — deterministic parity model.

## Decisions

Architecture Decision Records live in `decisions/`. Each ADR should describe context, decision, consequences, and status. Keep ADRs concise and about durable choices, not transient session notes.

Current ADRs:

- `0001-documentation-structure.md` — source documentation structure.
- `0002-file-adapter-conformance.md` — adapter conformance command and runner.
- `0003-package-lifecycle-and-evidence-model.md` — run, merge, judge, report, and validation package lifecycle.
- `0004-cli-command-model.md` — canonical command shape and compatibility aliases.
- `0005-sdk-independent-adapter-architecture.md` — adapter boundary and SDK independence.
- `0006-report-data-and-rendering-boundary.md` — canonical report data and renderers.
- `0007-failure-classification-and-integrity-gates.md` — failure classes and validity gates.
- `0008-pre-v1-schema-versioning.md` — schema evolution before v1.

## Roadmap

Roadmap files are retained as product planning documents:

- `roadmap/LORQ_PRACTICAL_ROADMAP.md`
- `roadmap/LORQ_ROADMAP_GOAL_REFINEMENT.md`

Session handoffs, local packaging rules, validation transcripts, and extracted artifact notes do not belong in this directory. Keep them in the sibling `internal/` workspace used by local delivery packages.
