# Dependency Policy

Dependencies should support deterministic evidence capture, package validation, and reviewable local development.

## Rules

- Keep third-party SDK types out of core package, validation, and reporting contracts.
- Keep real LLM provider dependencies out of deterministic validation.
- Prefer process/file boundaries for optional runtime integrations until an SDK dependency is justified by an ADR.
- Do not add a package dependency only for convenience when the same behavior can be implemented clearly with existing dependencies.
- Avoid dependency upgrades mixed with unrelated behavior changes.
- Document new dependencies when they affect setup, validation, licensing, runtime behavior, or public interfaces.

## External providers

Codex, Copilot, and future providers should be treated as infrastructure.

Provider wrappers may capture provider-specific metadata, but the canonical LORQ package model should preserve rather than interpret those details unless a later ADR changes that boundary.

## Licensing

The repository is MIT licensed. New dependencies should be compatible with the repository license and intended distribution model.
