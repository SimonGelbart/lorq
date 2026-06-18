# .NET CLI architecture

The .NET CLI is intentionally thin. It translates command-line arguments into command option objects, delegates execution to command handlers, and renders canonical JSON summaries.

```text
Program
  -> LorqCliApplication
  -> LorqCommandCatalog
  -> CommandDefinition<TOptions>
  -> ICommandHandler<TOptions>
  -> Core/Reporting service
```

## Boundaries

- `Lorq.Cli` owns parsing, command dispatch, and console rendering.
- `Lorq.Core` owns package validation, index rebuild, merge, and deterministic judgement package operations.
- `Lorq.Reporting` owns deterministic package report rendering.
- `Lorq.Adapters.Process` owns file-based adapter protocol contracts.

The CLI must not become the product core. Any behavior that changes package state should live in a service that can be tested without invoking a process.

## Command handler pattern

Every command should have:

- a `CommandOptions` record;
- an `ICommandHandler<TOptions>` implementation;
- a parser entry in `LorqCommandOptionsParser`;
- a catalog entry in `LorqCommandCatalog`;
- TUnit coverage for option parsing or application dispatch.

Current commands:

- `validate-package`
- `validate-merge-inputs`
- `rebuild-indexes`
- `merge-shards`
- `judge-package`
- `report-package`

## Quality rules

Domain/package behavior should stay small and testable. CLI classes may be pragmatic DTO/handler code, but new CLI behavior should still use meaningful names, file-scoped namespaces, sealed classes by default, guard clauses, and one type per file.

Do not add a new top-level switch branch in `Program.cs`. Add a typed command instead.
