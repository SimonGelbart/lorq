# Testing Strategy

LORQ validation should prove package behavior without requiring real LLM calls.

## Defaults

- Prefer deterministic tests and fixtures.
- Use fake adapters and fake judges for core package behavior.
- Keep real Codex/Copilot checks as optional local smoke tests.
- Add regression tests for bug fixes.
- Do not delete or weaken tests just to make validation pass.

## Test types

| Type | Purpose |
| --- | --- |
| Unit tests | Pure logic and small domain behavior. |
| Application tests | Use case orchestration and command handlers. |
| Contract tests | CLI summaries, adapter contracts, validation codes, schema shapes. |
| Fixture parity tests | Deterministic migration and golden fixture behavior. |
| Optional smoke tests | Local real-runtime wrapper checks, not deterministic CI gates. |

## Golden fixtures

Golden fixtures are source-controlled contracts. Update them only from deterministic workflows and include documentation for why the new output is expected.

## External services

Tests must not require network services, provider credentials, real LLM calls, or local authenticated tools unless they are explicitly marked as optional integration or smoke checks.
