# Python Coding Standards

Python code is currently retained as the v0 deterministic baseline and fixture-generation reference.

## Style

- Use type hints for public functions and important boundaries.
- Keep I/O separate from pure transformation logic.
- Avoid hidden global state.
- Prefer explicit exceptions over silent failure.
- Keep CLI parsing separate from application logic.

## Baseline stability

- Avoid changing Python v0 behavior unless the migration baseline or fixtures intentionally change.
- Keep deterministic fixture behavior independent from live external services.
- When changing golden fixture generation, update tests and documentation together.

## Testing

- Use pytest for Python baseline tests.
- Prefer deterministic fixtures.
- Do not require real Codex, Copilot, or provider credentials in default tests.

## Validation

Run repository-specific commands from `docs/reference/validation.md` and Python baseline docs when Python behavior changes.
