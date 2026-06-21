# 0005 — SDK-independent adapter architecture

## Status

Accepted.

## Context

LORQ needs to support deterministic fake adapters, external one-shot file adapters, Codex wrappers, Copilot SDK integration, and future runtimes without turning the core product into a wrapper around one SDK. The roadmap and adapter docs describe this boundary in several places, but the broader architecture decision should be durable and easy to find.

## Decision

LORQ core package, validation, merge, judgement attachment, and reporting logic must remain SDK-independent. Core code should not depend directly on Codex, Copilot, OpenAI, Anthropic, or another provider SDK.

Runtime-specific behavior belongs behind adapter contracts. The current plugin boundary is the external one-shot file adapter protocol. Built-in adapters may be added later, but they must normalize their outputs into the same evidence model before package logic consumes them.

Agent-runtime adapters and judge adapters use separate business contracts even if they share lower-level provider plumbing such as authentication, retry, transcript capture, or usage accounting.

## Consequences

- Real runtime integrations are adapter work, not package/report logic work.
- Deterministic gates can use fake adapters and conformance checks without real LLM calls.
- Provider-specific metadata belongs in namespaced extension blocks or adapter metadata, not in core package assumptions.
- ADR 0002 governs file-adapter conformance; this ADR governs the broader SDK-independence boundary.
