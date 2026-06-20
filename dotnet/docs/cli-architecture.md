# .NET CLI architecture

The .NET CLI is intentionally thin. It translates command-line arguments into command option objects, delegates execution to command handlers, and renders canonical JSON summaries.

```text
Program
  -> LorqCliHost
  -> Microsoft.Extensions.Hosting / dependency injection
  -> LorqCliApplication
  -> LorqCommandCatalog
  -> CommandDefinition<TOptions>
  -> ICommandHandler<TOptions>
  -> Core/Reporting service
```

## Boundaries

- `Lorq.Cli` owns host composition, parsing, command dispatch, and console rendering.
- `Lorq.Core` owns package validation, index rebuild, merge, and deterministic judgement package operations.
- `Lorq.Reporting` owns deterministic package report rendering.
- `Lorq.Adapters.Process` owns file-based adapter protocol contracts.

The CLI must not become the product core. Any behavior that changes package state should live in a service that can be tested without invoking a process.

## Command handler pattern

Every command should have:

- a `CommandOptions` record;
- an `ICommandHandler<TOptions>` implementation;
- a parser entry in `LorqCommandOptionsParser`;
- a catalog entry in `LorqCommandCatalogFactory`;
- a DI registration in `AddLorqCli`;
- TUnit coverage for option parsing, host registration, or application dispatch.

Current commands:

- `run`
- `validate-package`
- `validate-merge-inputs`
- `rebuild-indexes`
- `merge-shards`
- `judge-package`
- `report-package`

## Namespace layout

The CLI project is organized around folder-backed namespaces:

```text
Lorq.Cli
  Application entry and command dispatch
Lorq.Cli.Hosting
  Generic Host and dependency-injection composition
Lorq.Cli.Commands
  Command definition and catalog abstractions
Lorq.Cli.Commands.Parsing
  Typed command option records and argument parsing
Lorq.Cli.Commands.Handlers
  One handler per CLI command
Lorq.Cli.Commands.Results
  Command result and console JSON rendering
Lorq.Cli.Runtime
  Deterministic run/workspace orchestration helpers
```

Keep future namespace changes behavior-preserving. Conceptual extractions in `Core`, `Reporting`, or runtime orchestration should happen in separate commits so review diffs remain readable.

## Quality rules

Domain/package behavior should stay small and testable. CLI classes may be pragmatic DTO/handler code, but new CLI behavior should still use meaningful names, file-scoped namespaces, sealed classes by default, guard clauses, and one type per file.

Do not add a new top-level switch branch in `Program.cs`. Add a typed command instead.


## Hosted composition

`Program.cs` builds a `LorqCliHost` and resolves `LorqCliApplication` from dependency injection. This keeps the CLI entry point thin while making command handlers replaceable and testable through composition.

`LorqCliApplication.RunAsync` remains as a compatibility helper for tests and programmatic smoke calls. It uses the same host-backed composition path as `Program.cs`.

This is an intermediate step. Argument parsing is still intentionally unchanged and can be migrated to `System.CommandLine` in a later refactoring PR.
