# .NET engineering guidelines

LORQ v1 is the future product core. The .NET implementation must remain cleaner than the Python v0 migration baseline and must not inherit Python prototype structure by default.

## Clean architecture boundary

`Lorq.Core` owns domain concepts, use cases, validation rules, diagnostics, and package contracts. It must not depend on CLI parsing, process execution, Copilot SDKs, Codex, or the local file-system shape except through explicit package IO boundaries.

`Lorq.Cli` dispatches commands. It should stay thin: parse arguments, create command options, call application/core services, render exit codes.

Adapter projects own external process and SDK integration. They translate external protocols into LORQ evidence contracts; they do not decide product policy.

`Lorq.Reporting` owns report rendering and projection helpers. Canonical report JSON remains the source of truth; Markdown is a rendering.

## Object design

Apply object-calisthenics rules strictly in domain code and with judgment in application code:

- one indentation level per method where practical
- no `else` when a guard clause communicates intent better
- wrap domain primitives and strings in value objects when they become product concepts
- use first-class collections for meaningful package/cell/judgement sets
- avoid call chains that expose object internals
- do not abbreviate names
- keep entities and services small
- challenge classes with more than two instance variables
- avoid mutable getters/setters in domain objects

DTOs, CLI options, serializer models, and tests may use public get-only properties or records when that keeps binding/readability simple.

## Modern C# usage

Use recent C#/.NET features when they improve clarity, safety, or performance:

- records for immutable data carriers and value objects
- pattern matching for validation branches
- spans only when parsing or allocation pressure justifies them
- `ValueTask` only on hot async paths where it avoids meaningful allocations
- async streams when streaming package artifacts or adapter events
- collection expressions where readability improves

Avoid novelty-driven rewrites. A feature must make the product easier to understand, safer, faster, or easier to test.

## Design patterns

Use patterns deliberately:

- command handlers for CLI commands and application use cases
- factories for package readers/writers and adapter creation when construction becomes conditional
- strategy/provider abstractions for fake/process/Codex/Copilot adapters
- repository abstractions only for durable state boundaries, not for simple file traversal
- template methods only when shared orchestration flow would otherwise duplicate policy

Prefer small explicit objects over framework-heavy indirection.

## Testing standard

Use TUnit for .NET tests. The test project should run through Microsoft.Testing.Platform mode on .NET 10.

Typical validation from the repo root:

```bash
dotnet restore dotnet/Lorq.slnx -p:Platform="Any CPU"
dotnet build dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU"
dotnet test --solution dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU" --disable-logo --minimum-expected-tests 5
```

When mocks are useful, prefer `TUnit.Mocks`; otherwise use deterministic fakes so the migration gate remains independent of real LLM quality or external services.
