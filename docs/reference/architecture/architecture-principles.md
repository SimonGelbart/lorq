# Architecture Principles

These principles apply across LORQ implementation tracks.

## Defaults

- Evidence is canonical; summaries and reports are derived.
- Keep entry points thin.
- Keep package, validation, merge, judgement, report, and adapter boundaries explicit.
- Keep deterministic behavior testable without real LLM calls.
- Put external systems behind adapters.
- Preserve enough evidence to debug, validate, and reproduce behavior.
- Prefer explicit contracts over hidden conventions.
- Avoid abstractions without a concrete need.
- Durable architectural choices require an ADR.

## Dependency direction

For non-trivial code, prefer this dependency pressure:

```text
CLI / Presentation -> Application -> Core package model
Infrastructure / Adapters -> Application or Core contracts
Reporting -> Canonical report data
```

The CLI must not become the product core. Adapter implementations must not leak provider-specific SDK types into package validation, reporting, or domain code.

## Determinism first

Deterministic package behavior is the default migration and validation gate.

Real runtime integrations such as Codex and Copilot are optional smoke paths. They must normalize into the same package and file-adapter evidence contracts.

## Public contracts

Treat these as compatibility-sensitive:

- CLI commands and JSON summaries;
- file-adapter request/evidence contracts;
- package validation codes;
- package and report JSON shapes;
- committed schemas;
- deterministic fixture shapes.

A breaking change to a durable contract should update reference docs, tests, and usually an ADR.
