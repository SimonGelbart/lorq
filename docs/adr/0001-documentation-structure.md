# 0001. Organize documentation by reader need

Status: accepted

Date: 2026-06-20

## Context

The repository had a mix of product documentation, roadmap notes, AI-session handoffs, packaging rules, validation transcripts, and implementation notes. Some of that material was useful during local packaging, but it made the source repository harder to read.

## Decision

Use a Diátaxis-inspired documentation structure:

- `docs/how-to/` for task-oriented procedures.
- `docs/reference/` for precise command, package, schema, and adapter contracts.
- `docs/explanation/` for product and architecture background.
- `docs/adr/` for durable architecture decisions.
- `docs/roadmap/` for product direction and delivery sequencing.

Keep session handoffs, packaging manifests, validation logs, extracted archives, and session-specific notes in the sibling `internal/` workspace instead of the source repository.

## Consequences

- Source-controlled docs should describe source code, product usage, architecture, contracts, or durable product direction.
- Local package metadata remains available in delivery artifacts but is not part of the repository documentation set.
- Future documentation cleanup should move transient material to `internal/` instead of deleting useful handoff context outright.
