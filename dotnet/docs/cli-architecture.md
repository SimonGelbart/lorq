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
  -> Core/Reporting/Adapter service
```

## Boundaries

- `Lorq.Cli` owns host composition, parsing, command dispatch, and console rendering.
- `Lorq.Core` owns package validation, index rebuilds, merge, deterministic judgement attachment, and deterministic run package services.
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
  Runtime service registrations and CLI-side runtime helpers
```

## Hosted composition

`Program.cs` builds a `LorqCliHost` and resolves `LorqCliApplication` from dependency injection. This keeps the entry point small and makes handlers and orchestration services replaceable in tests.

`LorqCliApplication.RunAsync` remains as a compatibility helper for tests and programmatic smoke calls. It uses the same host-backed composition path as `Program.cs`.
