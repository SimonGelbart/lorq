# 0001. Organize documentation by reader need

Status: accepted

Date: 2026-06-20

## Context

The repository had a mix of product documentation, roadmap notes, task notes, and implementation notes. Some of that material was useful locally, but it made the source repository harder to read.

## Decision

Use a Diátaxis-inspired documentation structure:

- `docs/how-to/` for task-oriented procedures.
- `docs/reference/` for precise command, package, schema, and adapter contracts.
- `docs/explanation/` for product and architecture background.
- `docs/adr/` for durable architecture decisions.
- `docs/roadmap/` for product direction and delivery sequencing.

Keep task-specific notes, packaging manifests, validation logs, and extracted archives outside the source repository.

## Consequences

- Source-controlled docs should describe source code, product usage, architecture, contracts, or durable product direction.
- Temporary local metadata is not part of the repository documentation set.
- Future documentation cleanup should remove transient material from source documentation and keep durable guidance in the canonical docs tree.
