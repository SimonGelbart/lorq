# LORQ documentation

The docs are organized by reader need rather than by work session. This follows the Diátaxis distinction between how-to guides, reference, explanation, and tutorials.

## How-to guides

Use these when you want to complete a task.

- `how-to/dotnet-deterministic-loop.md` — run the deterministic .NET package loop.

## Reference

Use these when you need precise contracts and command behavior.

- `reference/cli.md` — current CLI commands and options.
- `reference/package-validation.md` — validation scope and stable error codes.
- `reference/file-adapter-protocol.md` — file-based adapter evidence contract.

## Explanation

Use these when you need background, boundaries, and design rationale.

- `explanation/product-direction.md` — product thesis and non-goals.
- `dotnet/docs/cli-architecture.md` — .NET CLI composition and command-handler boundary.
- `dotnet/docs/engineering-guidelines.md` — .NET engineering rules.
- `dotnet/docs/full-loop-parity.md` — deterministic parity model.

## Decisions

Architecture Decision Records live in `decisions/`. Each ADR should describe context, decision, consequences, and status. Keep ADRs concise and about durable choices, not transient session notes.

## Roadmap

Roadmap files are retained as product planning documents:

- `roadmap/LORQ_PRACTICAL_ROADMAP.md`
- `roadmap/LORQ_ROADMAP_GOAL_REFINEMENT.md`

Session handoffs, local packaging rules, validation transcripts, and extracted artifact notes do not belong in this directory. Keep them in the sibling `internal/` workspace used by local delivery packages.
