# .NET engineering guidelines

These rules apply to .NET work under `dotnet/`.

## Architecture

- Keep the product core independent of CLI parsing, process execution, external SDKs, and provider-specific runtime details.
- Put package behavior, validation, merge, judgement attachment, and run package services in `Lorq.Core`.
- Put CLI parsing, host composition, command dispatch, and JSON console summaries in `Lorq.Cli`.
- Put report rendering in `Lorq.Reporting`.
- Put process/file adapter protocol behavior in adapter projects.

## Domain design

- Prefer small value objects and records where they clarify package identifiers, cell identifiers, shard identifiers, diagnostics, and report concepts.
- Apply object-calisthenics discipline strictly in domain code and pragmatically elsewhere.
- Use guard clauses and early returns.
- Keep domain classes small and behavior-focused.
- Avoid primitive obsession in domain APIs when a value object clarifies intent.
- Avoid exposing mutable collections from domain objects.

## C# style

- Use file-scoped namespaces for new files.
- Use sealed classes by default.
- Use clear names; do not abbreviate domain concepts.
- Use `ArgumentNullException.ThrowIfNull` for null guards.
- Use `nameof` in exceptions.
- Prefer `var` when the type is obvious and explicit types when clarity improves.
- Use modern C# features when they improve clarity or safety, not for novelty.

## Testing

- Use TUnit for .NET tests.
- Use Microsoft.Testing.Platform mode for `dotnet test` on .NET 10.
- Prefer deterministic fake adapters and fixtures over real LLM calls.
- Tests may use readable data-carrier records and direct property assertions.

## Patterns

Use design patterns only when they simplify the product model or isolate a boundary:

- Command pattern for CLI commands and use-case orchestration.
- Factory pattern for complex package readers/writers or adapter construction.
- Strategy/provider pattern for fake, process, Codex-wrapper, and Copilot adapters.
- Repository pattern only for durable storage boundaries, not for in-memory package traversal.
