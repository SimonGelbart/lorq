# 0006 — Report data and rendering boundary

## Status

Accepted.

## Context

LORQ reports currently produce machine-readable JSON and human-readable Markdown. Future HTML or comparison views are likely, and roadmap documents already state that JSON is canonical. That decision needs a stable home so renderers do not become competing sources of business data.

## Decision

`reports/report.json` is the canonical report data artifact. Markdown and future HTML outputs are renderings derived from canonical report data and per-case review data.

A renderer must not introduce decision data that is absent from the canonical JSON model. Human artifacts may summarize, format, link, and explain data, but any value verdict, quality outcome, risk, cost/time measure, reference, or warning needed for automation must be represented in canonical JSON first.

Per-case review packs are derived report artifacts. They may include links to raw answers, traces, artifacts, and judge outputs, but the report model remains the source for decision-level data.

## Consequences

- Future renderers should consume report models rather than raw package folders independently.
- CI and automation should inspect JSON, not Markdown or HTML.
- Markdown output can change presentation without changing decision semantics.
- Report reference docs describe current fields; this ADR describes the canonical-data boundary.
