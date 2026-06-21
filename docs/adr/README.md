# Architecture Decision Records

This directory contains Architecture Decision Records for LORQ.

ADRs capture durable architectural, technical, product-shaping, or compatibility decisions that future contributors and maintainers must respect.

## ADR precedence

Accepted ADRs are binding repository guidance.

Before making architectural, structural, or contract-level changes, check whether an existing ADR applies.

If a requested change contradicts an accepted ADR, do not ignore the contradiction. Do one of the following:

1. adapt the implementation so it remains compatible with the ADR;
2. explain clearly that the request conflicts with the ADR; or
3. create a new ADR that explicitly supersedes or amends the previous ADR.

Do not rewrite historical ADRs to hide a change in direction. Old ADRs may be corrected for typos or formatting, but substantive decision changes should be made through a new ADR.

## When to create an ADR

Create or update an ADR when work introduces or changes a durable decision about:

- architecture;
- module boundaries;
- public APIs;
- CLI contracts;
- package structure;
- persistence format;
- adapter contracts;
- external integrations;
- security model;
- testing strategy;
- migration strategy;
- dependency policy;
- compatibility guarantees.

## Naming pattern

Use monotonically increasing numbers:

```text
0001-short-decision-title.md
0002-short-decision-title.md
0003-short-decision-title.md
```

Use `0000-adr-template.md` as the template.

## Status values

Use one of:

```text
Proposed
Accepted
Superseded
Deprecated
```

## Current ADRs

- `0001-documentation-structure.md` — source documentation structure.
- `0002-file-adapter-conformance.md` — adapter conformance command and runner.
- `0003-package-lifecycle-and-evidence-model.md` — run, merge, judge, report, and validation package lifecycle.
- `0004-cli-command-model.md` — canonical command shape and compatibility aliases.
- `0005-sdk-independent-adapter-architecture.md` — adapter boundary and SDK independence.
- `0006-report-data-and-rendering-boundary.md` — canonical report data and renderers.
- `0007-failure-classification-and-integrity-gates.md` — failure classes and validity gates.
- `0008-pre-v1-schema-versioning.md` — schema evolution before v1.
