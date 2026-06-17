# LORQ .NET engineering rules

These rules apply to .NET v1 work under `dotnet/`.

## Architecture

- Keep the product core clean and independent of CLI, adapters, file-system details, and external SDKs.
- Use `Lorq.Core` for domain concepts and use cases.
- Use `Lorq.Cli` only for command-line parsing and command dispatch.
- Use adapter projects for process, Codex, Copilot, and other external integrations.
- Prefer ports/interfaces at the boundary between core behavior and infrastructure.
- Do not let Python v0 internals leak into the .NET product model; consume the frozen package contract instead.

## Domain design

- Prefer small value objects and records for package identifiers, cell identifiers, shard identifiers, diagnostics, and canonical report concepts.
- Apply object-calisthenics discipline strictly in domain code and pragmatically elsewhere.
- Use guard clauses and early returns; avoid deeply nested methods.
- Keep domain classes small and behavior-focused.
- Avoid primitive obsession in domain APIs when a value object clarifies intent.
- Avoid exposing mutable collections from domain objects.

## C# style

- Use file-scoped namespaces for new files.
- Use sealed classes by default.
- Use clear names; do not abbreviate domain concepts.
- Use `nameof` in exceptions.
- Prefer `var` when the type is obvious and explicit types when clarity improves.
- Prefer modern C#/.NET features when they make the code clearer or safer: records, pattern matching, spans, `ValueTask`, async streams, and collection expressions.
- Do not use modern features only for novelty.

## Testing

- Use TUnit for .NET tests.
- Use Microsoft.Testing.Platform mode for `dotnet test` on .NET 10.
- Prefer async-first TUnit assertions.
- Use TUnit.Mocks when mocking is genuinely needed; prefer simple fakes for deterministic package/adapter fixtures.
- Tests may use readable data-carrier records and direct property assertions; object-calisthenics rules are strict for domain code, not test scaffolding.

## Patterns

Use design patterns only when they simplify the product model or isolate a boundary:

- Command pattern for CLI commands and use-case orchestration.
- Factory pattern for complex package readers/writers or adapter construction.
- Strategy/provider pattern for fake, process, Codex, and Copilot adapters.
- Repository pattern only for durable storage boundaries, not for in-memory package traversal.
