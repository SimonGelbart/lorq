# TypeScript Coding Standards

LORQ does not currently have a primary TypeScript implementation. Use this profile if TypeScript tooling, examples, or future UI work are added.

## Style

- Keep `strict` TypeScript enabled.
- Avoid `any` unless there is a documented reason.
- Prefer explicit domain types over loosely shaped objects.
- Keep side effects at boundaries.
- Avoid hidden global state.
- Use meaningful names.

## Architecture

- Separate UI, application logic, domain logic, and infrastructure.
- Put HTTP clients, storage, analytics, auth, and external SDKs behind adapters.
- Do not couple package validation or domain behavior to UI frameworks.

## Testing

- Use unit tests for pure logic.
- Use integration tests for framework behavior.
- Mock external services at adapter boundaries.

## Validation

Add and document TypeScript validation commands in `docs/reference/validation.md` if TypeScript becomes part of the maintained stack.
