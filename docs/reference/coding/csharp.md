# C# and .NET Coding Standards

This file complements `dotnet/docs/engineering-guidelines.md` and the architecture references under `docs/reference/architecture/`.

## Baseline style

- Use file-scoped namespaces for new files.
- Use sealed classes by default unless inheritance is intentional.
- Use clear names and avoid abbreviating domain concepts.
- Use `ArgumentNullException.ThrowIfNull` for null guards.
- Use `nameof` in exceptions.
- Prefer `var` when the type is obvious; use explicit types when clarity improves.
- Use modern C# features when they improve clarity or safety, not for novelty.

## Domain and application code

- Keep package, validation, merge, report, and adapter concepts explicit.
- Prefer records and value objects when they clarify identifiers or immutable data.
- Avoid exposing mutable collections from domain objects.
- Keep behavior that changes package state testable without invoking the CLI process.

## Infrastructure and adapters

- Keep external process and provider behavior in adapter projects.
- Preserve stdout, stderr, exit codes, timing, diagnostics, and artifact references when they are part of evidence.
- Keep Codex, Copilot, and future provider SDK types outside core package and reporting assemblies.
- Use async APIs for I/O when practical.

## Testing

- Use TUnit for .NET tests.
- Prefer deterministic fake adapters and fixtures over real LLM calls.
- Use contract tests for CLI JSON summaries, adapter evidence, and validation codes.
- Real runtime smoke paths should be optional local checks.

## Validation

Run repository-specific commands from `docs/reference/validation.md`.
