# Project Profile

This file contains public, durable facts about LORQ.

Do not put private, personal, environment-specific, or task-specific information in this file.

## Project identity

Project name:

```text
LORQ
```

Short description:

```text
Ledger for Orchestrated Run Quality: a deterministic package ledger for agent, tool, and skill evaluation runs.
```

Repository slug:

```text
lorq
```

Public repository:

```text
SimonGelbart/lorq
```

License:

```text
MIT
```

Project status:

```text
Experimental
```

## Supported stacks

Primary platform:

```text
.NET 10 / C#
```

Secondary stacks and baselines:

```text
Python v0 deterministic baseline, JSON schemas, shell-based local workflows, optional process adapters.
```

Supported targets:

```text
Local developer machines and CI runners capable of running the .NET SDK and Python baseline tools.
```

## Public interfaces

Durable interfaces include:

- the `lorq` CLI command surface;
- file-adapter request and evidence JSON contracts;
- package validation error codes;
- run-shard, experiment-package, report, and index file layouts;
- JSON schemas under `schemas/`;
- committed deterministic fixtures under `fixtures/`;
- optional adapter examples under `examples/adapters/`.

## Current direction

LORQ prioritizes deterministic orchestration evidence over model-intelligence benchmarking. The product direction is to make split runs mergeable, auditable, judgeable after execution, and reportable from canonical package evidence.

Real runtime integrations such as Codex and Copilot should remain behind file-adapter or SDK-independent boundaries. Deterministic validation must not depend on external LLM calls.

## Canonical documentation

Use these documents as canonical sources of truth:

- Documentation index: `docs/README.md`
- Repository layout: `docs/reference/repository-layout.md`
- Validation reference: `docs/reference/validation.md`
- Documentation standards: `docs/reference/documentation-standards.md`
- Architecture principles: `docs/reference/architecture/architecture-principles.md`
- Architecture decisions: `docs/adr/README.md`
- Product direction: `docs/explanation/product-direction.md`
- Practical roadmap: `docs/roadmap/LORQ_PRACTICAL_ROADMAP.md`
