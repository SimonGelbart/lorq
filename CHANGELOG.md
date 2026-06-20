# Changelog

All notable changes to LORQ should be documented here.

## 2026-06-20 - Refactor: run orchestration services

### Roadmap position

Current branch: `refactor/dotnet-core-cleanup`. This is commit 4 of the remaining refactoring batch and keeps `run --no-judge` behavior unchanged while extracting orchestration responsibilities from the deterministic run application.

### Changed

- Reworked `DeterministicRunShardApplication` into an injected orchestration service instead of a static procedural coordinator.
- Extracted adapter creation/profile application, per-cell adapter execution, prompt construction, cell-evidence mapping, and shard result writing into focused internal runtime services.
- Registered the run orchestration services through CLI host composition so command dispatch still uses the same hosted path.
- Preserved deterministic fake adapter behavior, external file-adapter behavior, Codex profile wrapper behavior, workspace layout, package output, and CLI JSON summary shape.

### Validation

Executed during this increment:

- `dotnet restore dotnet/Lorq.slnx --configfile /mnt/data/nuget-local/tracepack-nuget-bundle/NuGet.config -p:Platform="Any CPU" --disable-parallel --ignore-failed-sources` -> passed.
- `dotnet build dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU" --nologo -v:minimal` -> passed.
- `dotnet test --solution dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU" --disable-logo` -> 42 passed.
- `dotnet build dotnet/Lorq.slnx --configuration Release --no-restore -p:Platform="Any CPU" --nologo -v:minimal` -> passed.
- `dotnet test --test-modules "dotnet/tests/**/bin/Release/net10.0/*.Tests.dll" --root-directory . --results-directory ../internal/generated/refactor-run-orchestration-test-results -- --report-trx` -> 42 passed.

### Known limitations

- This completes the current four-commit refactoring batch. The package is still local-only; no branch was pushed and no PR was opened.

## 2026-06-20 - Refactor: report rendering pipeline

### Roadmap position

Current branch: `refactor/dotnet-core-cleanup`. This is commit 3 of the remaining refactoring batch and keeps report output unchanged while splitting report rendering into smaller pipeline components.

### Changed

- Kept `LorqPackageReportRenderer` as the public static facade.
- Extracted report rendering orchestration, package report source reading, report document construction, case review pack construction, markdown rendering, JSON/file writing, and result construction into focused internal reporting components.
- Preserved report JSON, report markdown, case-review JSON, case-review markdown, and `.lorq/report.json` output shape.

### Validation

Executed during this increment:

- `dotnet restore dotnet/Lorq.slnx --configfile /mnt/data/nuget-local/NuGet.absolute.config -p:Platform="Any CPU" --disable-parallel` -> passed.
- `dotnet build dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU" --nologo -v:minimal` -> passed.
- `dotnet test --solution dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU" --disable-logo` -> 42 passed.
- `dotnet build dotnet/Lorq.slnx --configuration Release --no-restore -p:Platform="Any CPU" --nologo -v:minimal` -> passed.
- `dotnet test --test-modules "dotnet/tests/**/bin/Release/net10.0/*.Tests.dll" --root-directory . --results-directory ../internal/generated/refactor-report-rendering-pipeline-test-results -- --report-trx` -> 42 passed.

### Known limitations

- This commit intentionally does not move CLI run orchestration; that remains the final commit in the current refactoring batch.

## 2026-06-20 - Refactor: package domain identifiers

### Roadmap position

Current branch: `refactor/dotnet-core-cleanup`. This is commit 2 of the remaining refactoring batch and keeps package validation behavior unchanged while introducing small internal identifiers for package validation concepts.

### Changed

- Added internal package validation identifiers for package ids, package kinds, schema versions, shard ids, and cell ids.
- Updated package manifest, coverage, merge-log, shard, and report validation internals to pass identifier types across validation boundaries.
- Converted identifiers back to the existing public DTO string/int shapes at the package validation facade boundary.

### Validation

Executed during this increment:

- `dotnet restore dotnet/Lorq.slnx --configfile /mnt/data/nuget-config/NuGet.config -p:Platform="Any CPU" --disable-parallel` -> passed.
- `dotnet build dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU" --nologo -v:minimal` -> passed.
- `dotnet test --solution dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU" --disable-logo` -> 42 passed.
- `dotnet build dotnet/Lorq.slnx --configuration Release --no-restore -p:Platform="Any CPU" --nologo -v:minimal` -> passed.
- `dotnet test --test-modules "dotnet/tests/**/bin/Release/net10.0/*.Tests.dll" --root-directory . --results-directory ../internal/generated/refactor-package-domain-identifiers-test-results -- --report-trx` -> 42 passed.

### Known limitations

- Public validation result records intentionally still expose primitive JSON-friendly values so CLI and report output shape remain unchanged.
- Report rendering and run orchestration extraction remain later commits in this batch.

## 2026-06-20 - Refactor: package validation components

### Roadmap position

Current branch: `refactor/dotnet-core-cleanup`. This step keeps package validation behavior unchanged while extracting the large validator into smaller internal validation components.

### Changed

- Kept `LorqPackageValidator` as the public static facade for package and merge-input validation.
- Extracted package file discovery, manifest reading, coverage/fingerprint/integrity/merge-log validation, run shard/cell reading, judgement validation, report reference validation, and merge input validation into internal package-validation components.
- Preserved existing diagnostics, validation result records, and CLI JSON output shape.

### Validation

Executed during this increment:

- `dotnet restore dotnet/Lorq.slnx --configfile /mnt/data/nuget-config/NuGet.config -p:Platform="Any CPU" --disable-parallel` -> passed.
- `dotnet build dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU" --nologo -v:minimal` -> passed.
- `dotnet test --solution dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU" --disable-logo` -> 42 passed.
- `dotnet build dotnet/Lorq.slnx --configuration Release --no-restore -p:Platform="Any CPU" --nologo -v:minimal` -> passed.
- `dotnet test --test-modules "dotnet/tests/**/bin/Release/net10.0/*.Tests.dll" --root-directory . --results-directory ../internal/generated/refactor-package-validation-test-results -- --report-trx` -> 42 passed.

### Known limitations

- This commit intentionally does not introduce broader package domain identifiers or value objects; those remain for the next refactoring commit.

## 2026-06-20 - Refactor: CLI namespace organization

### Roadmap position

Current branch: `refactor/dotnet-hosted-cli-composition`. This step keeps behavior unchanged and organizes the CLI project into folder-backed namespaces after introducing hosted composition.

### Changed

- Moved hosted composition types under `Lorq.Cli.Hosting`.
- Split CLI command implementation into `Commands`, `Commands.Parsing`, `Commands.Handlers`, and `Commands.Results`.
- Moved deterministic run/workspace orchestration helpers under `Lorq.Cli.Runtime`.
- Updated CLI tests to reference the new namespaces without changing command behavior.

### Validation

Executed during this increment:

- `dotnet restore dotnet/Lorq.slnx --configfile /mnt/data/nuget-local/tracepack-nuget-bundle/NuGet.config -p:Platform="Any CPU"` -> passed.
- `dotnet build dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU" --nologo -v:minimal` -> passed.
- `dotnet test --solution dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU" --disable-logo --minimum-expected-tests 42` -> 42 passed.
- `dotnet build dotnet/Lorq.slnx --no-restore --configuration Release -p:Platform="Any CPU" --nologo -v:minimal` -> passed.
- `dotnet test --test-modules "dotnet/tests/**/bin/Release/net10.0/*.Tests.dll" --root-directory . --results-directory ../internal/generated/refactor-cli-namespace-test-results -- --report-trx` -> 42 passed.

### Known limitations

- This is a namespace/folder organization commit only; package validation, reporting, and run orchestration internals remain static/procedural and are still candidates for later extraction.

## 2026-06-20 - Refactor: hosted CLI composition

### Roadmap position

Current branch: `refactor/dotnet-hosted-cli-composition`. This step keeps CLI behavior unchanged while moving command construction behind .NET host and dependency-injection composition.

### Changed

- Added hosted CLI composition via `LorqCliHost` and `AddLorqCli`.
- Registered command handlers and command catalog through dependency injection instead of constructing the default catalog inside the application.
- Kept `LorqCliApplication.RunAsync` as a test-friendly compatibility entry point backed by the same host composition used by `Program.cs`.
- Added TUnit coverage proving the host registers `LorqCliApplication`, `LorqCommandCatalog`, and executable command handlers.

### Validation

Executed during this increment:

- `dotnet restore dotnet/Lorq.slnx --configfile /mnt/data/nuget-local/tracepack-nuget-bundle/NuGet.config -p:Platform="Any CPU"` -> passed.
- `dotnet build dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU" --nologo -v:minimal` -> passed.
- `dotnet test --solution dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU" --disable-logo --minimum-expected-tests 42` -> 42 passed.
- `dotnet build dotnet/Lorq.slnx --no-restore --configuration Release -p:Platform="Any CPU" --nologo -v:minimal` -> passed.
- `dotnet test --test-modules "dotnet/tests/**/bin/Release/net10.0/*.Tests.dll" --root-directory . --results-directory ../internal/generated/refactor-hosted-cli-test-results -- --report-trx` -> 42 passed.

### Known limitations

- Command-line parsing is still hand-written and remains a follow-up candidate for `System.CommandLine`.
- Core package operations are still mostly static procedural services; this step only changes CLI composition.

## 2026-06-19 - Increment 3 runtime: workspace materialization planning

### Roadmap position

Current increment: Increment 3, .NET run and merge loop. This session adds deterministic workspace and materialization planning to `run --no-judge` before introducing general repository orchestration or real LLM adapters.

### Added

- Added per-cell disposable workspace planning for `run --no-judge`.
- Added local repository source resolution from case `repo:` metadata and suite `eval.config.yaml`.
- Added repository copy materialization into the adapter workspace before each cell run.
- Added support for mode `materialize.copy` entries so future modes can copy files or directories into the per-cell workspace.
- Added `--work-root` to place materialized workspaces outside the run package root.
- Added TUnit coverage proving deterministic runs materialize `fake_project` workspaces and preserve valid run-shard output.

### Validation

Executed during this increment:

- `dotnet test --solution dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU" --disable-logo --minimum-expected-tests 40` -> 40 passed.
- `dotnet run --project dotnet/src/Lorq.Cli/Lorq.Cli.csproj --no-restore --property:Platform="Any CPU" -- run --no-judge --suite-root fixtures/conformance/deterministic-orchestration --out ../internal/generated/dotnet-materialized-run/shard-001 --work-root <absolute-work-root>` -> passed.

### Known limitations

- Workspace materialization currently supports local repository copy and mode copy entries only.
- Git worktree/clone checkout, dirty policy enforcement, setup command execution, direct Codex, and Copilot SDK runtime behavior remain future work.

## 2026-06-18 - Increment 3 adapters: Codex file adapter profile

### Roadmap position

Current increment: Increment 3, .NET run and merge loop. This session adds a Codex-oriented process profile on top of the file-adapter protocol without invoking a real Codex runtime.

### Added

- Added `CodexFileAdapterProfile` as a built-in profile for external one-shot Codex wrapper processes.
- Added profile environment variables for Codex command, Codex arguments, output format, and one-shot invocation mode.
- Added `--adapter-profile codex-cli`, `--codex-command`, and repeated `--codex-arg` to `run --no-judge`.
- Added deterministic TUnit coverage proving the Codex profile is passed to the wrapper process without real Codex execution.
- Added `dotnet/docs/adapters/codex-file-adapter-profile.md`.

### Validation

Executed during this increment:

- `dotnet restore dotnet/Lorq.slnx --source <local package cache> -p:Platform="Any CPU"` -> passed.
- `dotnet build dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU"` -> passed.
- `dotnet test --solution dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU" --disable-logo --minimum-expected-tests 39` -> 39 passed.

### Known limitations

- `codex-cli` currently configures a wrapper process only. LORQ does not call Codex directly and does not use real LLM output as a migration gate.

## 2026-06-18 - Increment 3 runtime: external file adapter process

### Roadmap position

Current increment: Increment 3, .NET run and merge loop. This session adds external one-shot process invocation behind the file-adapter protocol while keeping deterministic fake adapters as the no-token baseline.

### Added

- Added `IFileAdapter` so deterministic and external adapters share the same execution boundary.
- Added `ExternalFileAdapterProcess` to write `adapter-request.json`, launch a configured process, capture process stdout/stderr, and read/validate `adapter-evidence.json`.
- Added stable protocol diagnostics for process start failures, timeouts, missing evidence, and malformed evidence.
- Added `--adapter-command`, repeated `--adapter-arg`, and `--adapter-working-directory` to `run --no-judge`.
- Added `Lorq.Adapter.TestHost` for no-token external process tests.
- Added TUnit coverage for external adapter invocation, missing evidence failure, CLI parsing, and `run --no-judge` with an external file adapter.

### Validation

Executed during this increment:

- `dotnet build dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU"` -> passed.
- `dotnet test --solution dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU" --disable-logo --minimum-expected-tests 34` -> 34 passed.

### Known limitations

- External file adapters are available only through the deterministic planned-shard `run --no-judge` path.
- General workspace materialization, Codex CLI adapter behavior, and Copilot SDK runtime integration remain future work.

## 2026-06-18 - Increment 3 runtime: .NET deterministic run shard

### Roadmap position

Current increment: Increment 3, .NET run and merge loop. This session adds the first narrow `run --no-judge` runtime slice using the deterministic file adapter and frozen benchmark shard plan.

### Added

- Added `run --no-judge` to `Lorq.Cli` with typed command options and handler dispatch.
- Added deterministic benchmark shard planning from `benchmark.yaml`.
- Added deterministic fake file adapter execution from `fixtures/fake-agent.yaml`.
- Added a .NET run-shard package writer that writes cell evidence, adapter evidence, shard manifests, and rebuilt `.lorq` indexes.
- Added `dotnet/docs/run-no-judge.md` to document the current runtime boundary.
- Added TUnit coverage for the deterministic adapter and CLI run command.

### Validation

Executed during this increment:

- `dotnet restore dotnet/Lorq.slnx --source <local package cache> -p:Platform="Any CPU"` -> passed.
- `dotnet build dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU"` -> passed.
- `dotnet test --solution dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU" --disable-logo --minimum-expected-tests 30` -> 30 passed.
- `dotnet run --project dotnet/src/Lorq.Cli/Lorq.Cli.csproj --no-restore --property:Platform="Any CPU" -- run --no-judge --suite-root fixtures/conformance/deterministic-orchestration --out ../internal/generated/dotnet-run-shard/shard-001` -> passed.
- `dotnet run --project dotnet/src/Lorq.Cli/Lorq.Cli.csproj --no-restore --property:Platform="Any CPU" -- validate-package ../internal/generated/dotnet-run-shard/shard-001` -> passed.

### Known limitations

- `run` supports only deterministic `--no-judge` fixture orchestration.
- General workspace materialization, external process launch, Codex, and Copilot runtime integration remain future work.

## 2026-06-18 - Increment 3 quality gate: .NET CLI command architecture

### Roadmap position

Current increment: Increment 3, .NET run and merge loop. This session intentionally paused new runtime behavior to clean up the .NET CLI boundary before adding `lorq run --no-judge`.

### Changed

- Refactored `Lorq.Cli` from a procedural top-level switch into typed command options, command definitions, and `ICommandHandler<TOptions>` handlers.
- Kept the existing CLI behavior and JSON summaries for validation, merge, index rebuild, judgement, and report commands.
- Added `Lorq.Cli.Tests` with TUnit coverage for command option parsing and application dispatch.
- Added `dotnet/docs/cli-architecture.md` to define the CLI/application/core split and the command handler pattern.

### Validation

Executed during this increment:

- `dotnet restore dotnet/Lorq.slnx --source <local package cache> -p:Platform="Any CPU"` -> passed.
- `dotnet build dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU"` -> passed.
- `dotnet test --solution dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU" --disable-logo --minimum-expected-tests 27` -> 27 passed.

### Known limitations

- The CLI is cleaner, but this is still not a full DI/hosting architecture.
- `lorq run --no-judge` remains the next runtime increment.

## 2026-06-18 - Increment 3 start: file adapter contract

### Roadmap position

Current increment: Increment 3, .NET run and merge loop. This session started Increment 3 by defining the file-based one-shot adapter protocol before implementing `lorq run`.

### Added

- Added `Lorq.Adapters.Process` with request/evidence contract records and protocol constants.
- Added committed JSON schemas for file adapter requests and full adapter evidence.
- Added `dotnet/docs/adapters/file-adapter-protocol.md`.
- Added TUnit coverage proving protocol constants match schemas and evidence contains more than a final answer.

### Validation

Executed during this increment:

- `dotnet restore dotnet/Lorq.slnx --source <local package cache> -p:Platform="Any CPU"` -> passed.
- `dotnet build dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU"` -> passed.
- `dotnet test --solution dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU" --disable-logo --minimum-expected-tests 22` -> passed.

### Known limitations

- The adapter protocol is defined but not yet executed by `lorq run`.
- Codex and Copilot runtime integrations remain future increments.

## 2026-06-18 - Increment 2 complete: .NET deterministic parity loop

### Roadmap position

Current increment: Increment 2, .NET foundation and package model. This session froze the .NET package-only loop against the Python v0 deterministic migration baseline.

### Added

- Added a TUnit full-loop parity test for .NET `merge-shards -> judge-package -> report-package -> validate-package`.
- Added complete byte-level comparison of the generated .NET experiment package against `fixtures/golden/deterministic-orchestration/experiment-001`.
- Added documentation for the .NET deterministic full-loop parity boundary.

### Changed

- Marked Increment 2 complete: .NET can read, validate, rebuild indexes, merge deterministic shards, attach deterministic judgements, and render deterministic reports against the frozen package shape.

### Validation

Executed during this increment:

- `dotnet build dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU"` -> passed.
- `dotnet test --solution dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU" --disable-logo --minimum-expected-tests 18` -> 18 passed.
- .NET full-loop parity test compared the complete generated package with the frozen Python golden experiment package -> passed.

### Known limitations

- .NET `run` and external adapters remain future Increment 3 work.
- Real Codex and Copilot runtime behavior remain future work behind adapter contracts.

## 2026-06-18 - Increment 2 foundation: .NET deterministic judgement

### Roadmap position

Current increment: Increment 2, .NET foundation and package model. This session added .NET deterministic judgement attachment for merged packages, still without implementing .NET run, report, Codex, or Copilot runtime behavior.

### Added

- Added `LorqDeterministicPackageJudge` and the `judge-package` CLI command.
- Added fixture-backed deterministic fake judge parsing for `fixtures/fake-judge.yaml` without using any real LLM.
- Added writing of `judgements/<name>/judgement.manifest.json`, `judgement.summary.json`, per-cell judgement files, and `.lorq/judgements/<name>.json`.
- Added TUnit tests proving judgement output is byte-stable against the frozen Python golden baseline.
- Added strict missing-fixture diagnostics with stable code `LORQ310`.

### Validation

Executed during this increment:

- `dotnet restore dotnet/Lorq.slnx --source <local package cache> -p:Platform="Any CPU"` -> passed.
- `dotnet build dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU"` -> passed.
- `dotnet test --solution dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU" --disable-logo --minimum-expected-tests 14` -> 14 passed.
- `dotnet run --project dotnet/src/Lorq.Cli/Lorq.Cli.csproj --no-restore --property:Platform="Any CPU" -- judge-package <temp>/experiment-001 --name judge-primary --fixture fixtures/conformance/deterministic-orchestration/fixtures/fake-judge.yaml` -> passed.
- `cd python && python -m pytest -q` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --suite-root .. --validate-config` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --suite-root ../fixtures/conformance/deterministic-orchestration --validate-config` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --run-conformance` -> passed.

### Known limitations

- .NET report rendering is not implemented yet.
- .NET `run`, Codex, and Copilot runtime behavior remain future increments.

## 2026-06-18 - Increment 2 foundation: .NET merge writer

### Roadmap position

Current increment: Increment 2, .NET foundation and package model. This session added the first real .NET merge writer for frozen deterministic run-shard packages, still without implementing .NET run, judge, report, Codex, or Copilot runtime behavior.

### Added

- Added `LorqPackageMerger` and the `merge-shards` CLI command.
- Added deterministic benchmark expected-cell expansion from `benchmark.yaml` for .NET merge coverage.
- Added merge writing of `experiment.yaml`, copied shard run evidence, `.lorq/merge-log.json`, and rebuilt `.lorq` cell/coverage/fingerprint/integrity indexes.
- Added TUnit tests proving merged core indexes are byte-stable against the frozen Python golden baseline.
- Added merge-writer tests proving duplicate-cell and fingerprint-mismatch fixtures are rejected by default with stable diagnostics.

### Validation

Executed during this increment:

- `dotnet restore dotnet/Lorq.slnx --source <local package cache> -p:Platform="Any CPU"` -> passed.
- `dotnet build dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU"` -> passed.
- `dotnet test --solution dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU" --disable-logo --minimum-expected-tests 11` -> 11 passed.
- `dotnet run --project dotnet/src/Lorq.Cli/Lorq.Cli.csproj --no-restore --property:Platform="Any CPU" -- merge-shards <shard-001> <shard-002> --out <temp>/experiment-001 --package-id deterministic-benchmark --benchmark fixtures/conformance/deterministic-orchestration/benchmark.yaml` -> passed.
- `dotnet run --project dotnet/src/Lorq.Cli/Lorq.Cli.csproj --no-restore --property:Platform="Any CPU" -- validate-package <temp>/experiment-001` -> passed with the expected warning that no report is attached yet.
- `cd python && python -m pytest -q` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --suite-root .. --validate-config` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --suite-root ../fixtures/conformance/deterministic-orchestration --validate-config` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --run-conformance` -> passed.

### Known limitations

- The .NET merge writer does not attach judgements or render reports yet.
- .NET `run`, `judge`, and `report` remain future increments.

## 2026-06-18 - Increment 2 foundation: .NET index rebuild writer

### Roadmap position

Current increment: Increment 2, .NET foundation and package model. This session added deterministic package index writing for already-loaded packages, still without implementing .NET run, merge, judge, report, Codex, or Copilot runtime behavior.

### Added

- Added `LorqPackageIndexRebuilder` to rebuild `.lorq` package indexes from frozen package evidence and run shard manifests.
- Added the validation-only CLI command `rebuild-indexes <package-root> <target-root>`.
- Added TUnit coverage proving regenerated `.lorq` indexes are byte-stable against the frozen Python golden experiment package.
- Added overlay validation proving rebuilt indexes can replace the package `.lorq` directory while preserving package validity.

### Validation

Executed during this increment:

- `dotnet restore dotnet/Lorq.slnx --source <local package cache> -p:Platform="Any CPU"` -> passed.
- `dotnet build dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU"` -> passed.
- `dotnet test --solution dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU" --disable-logo --minimum-expected-tests 7` -> 7 passed.
- `dotnet run --project dotnet/src/Lorq.Cli/Lorq.Cli.csproj --no-restore --property:Platform="Any CPU" -- rebuild-indexes fixtures/golden/deterministic-orchestration/experiment-001 <temp>` -> generated byte-stable indexes.
- `dotnet run --project dotnet/src/Lorq.Cli/Lorq.Cli.csproj --no-restore --property:Platform="Any CPU" -- validate-package fixtures/golden/deterministic-orchestration/experiment-001` -> passed.
- `cd python && python -m pytest -q` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --suite-root .. --validate-config` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --suite-root ../fixtures/conformance/deterministic-orchestration --validate-config` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --run-conformance` -> passed.

### Known limitations

- The .NET writer currently rebuilds indexes for existing package evidence only; it does not yet create run shards, merge shards, attach judgements, or render reports.
- Source-shard integrity warnings in merged packages are preserved from package metadata until .NET merge owns shard input composition.

## 2026-06-18 - Increment 2: .NET deterministic report rendering

### Roadmap position

Current increment: Increment 2, .NET foundation and package model. This session added deterministic package report rendering for judged merged packages.

### Added

- Added .NET `report-package` CLI support with `--primary-judgement`.
- Added `LorqPackageReportRenderer` to write canonical `reports/report.json`, `reports/report.md`, per-case review packs, and `.lorq/report.json`.
- Added TUnit tests proving .NET report outputs are byte-stable against the frozen Python golden baseline.

### Changed

- Updated .NET docs and command summaries to include deterministic report rendering.
- Advanced the practical roadmap status for Increment 2 from judgement attachment to report rendering.

### Validation

Executed during this increment:

- `dotnet restore dotnet/Lorq.slnx --source <local package cache> -p:Platform="Any CPU"` -> passed.
- `dotnet build dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU"` -> passed.
- `dotnet test --solution dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU" --disable-logo --minimum-expected-tests 16` -> 16 passed.
- .NET merge -> judge -> report -> validate smoke under `internal/generated/dotnet-report-package/` -> passed.

### Known limitations

- .NET full-loop parity freeze is still pending.
- .NET `run`, production/external `judge`, Codex, and Copilot runtime behavior remain future increments.

## 2026-06-18 - Increment 2 setup: .NET engineering and TUnit standard

### Roadmap position

Current increment: Increment 2, .NET foundation and package model. This session consolidated the .NET coding guidelines before adding more product behavior.

### Added

- Added source-controlled .NET engineering rules for clean architecture boundaries, object-calisthenics domain discipline, modern C# usage, deliberate design pattern use, and TUnit testing.
- Added `dotnet/docs/engineering-guidelines.md` and `.agents/LORQ_DOTNET_ENGINEERING_RULES.md`.
- Added root `global.json` to opt `dotnet test` into Microsoft.Testing.Platform mode for .NET 10.
- Added central TUnit package version management under `dotnet/Directory.Packages.props`.

### Changed

- Migrated `Lorq.Core.Tests` from a custom console assertion harness to TUnit tests.
- Updated `.gitignore` to exclude .NET `TestResults/`.
- Updated .NET README validation commands to use `dotnet test`.

### Validation

Executed during this increment:

- `dotnet restore dotnet/Lorq.slnx --source <local package cache> -p:Platform="Any CPU"` -> passed.
- `dotnet build dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU"` -> passed.
- `dotnet test --solution dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU" --disable-logo --minimum-expected-tests 5` -> 5 passed.
- `cd python && python -m pytest -q` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --suite-root .. --validate-config` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --suite-root ../fixtures/conformance/deterministic-orchestration --validate-config` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --run-conformance` -> passed.

### Next increment

- Add .NET package writer/index rebuild support for already-loaded packages, then validate stable regenerated `.lorq` indexes against the frozen Python golden package.

## 2026-06-18 - Increment 2 foundation: .NET package validation

### Roadmap position

Current increment: Increment 2, .NET foundation and package model. This session started .NET v1 after the Python frozen conformance baseline by adding package IO/domain validation only. It did not add real agent, judge, merge, report, Codex, or Copilot runtime behavior.

### Added

- Added `dotnet/Lorq.slnx` with `Lorq.Core`, `Lorq.Reporting`, `Lorq.Cli`, and a no-dependency `Lorq.Core.Tests` console test harness.
- Added .NET domain records for experiment packages, run shards, run cells, judgement passes, report references, diagnostics, and merge-input validation results.
- Added package validation for the frozen LORQ v1-alpha package shape, including required files, package schema version, run shard references, cell references, coverage, fingerprints, integrity, judgement references, and report references.
- Added merge-input validation for deterministic negative fixtures with stable error codes for duplicate cells and fingerprint mismatches.
- Added validation-only CLI commands: `validate-package` and `validate-merge-inputs`, both returning JSON summaries.
- Documented .NET package validation scope and stable validation codes under `dotnet/docs/package-validation.md`.

### Validation

Executed during this increment:

- `cd dotnet && dotnet build Lorq.slnx -p:Platform="Any CPU"` -> passed.
- `cd lorq && dotnet run --project dotnet/tests/Lorq.Core.Tests/Lorq.Core.Tests.csproj --property:Platform="Any CPU"` -> passed.
- `cd lorq && dotnet run --project dotnet/src/Lorq.Cli/Lorq.Cli.csproj --property:Platform="Any CPU" -- validate-package fixtures/golden/deterministic-orchestration/experiment-001` -> passed.
- `cd lorq && dotnet run --project dotnet/src/Lorq.Cli/Lorq.Cli.csproj --property:Platform="Any CPU" -- validate-merge-inputs fixtures/conformance/deterministic-orchestration/edge-fixtures/duplicate-cell-conflict/shard-a fixtures/conformance/deterministic-orchestration/edge-fixtures/duplicate-cell-conflict/shard-b` -> failed intentionally with `LORQ210`.
- `cd python && python -m pytest -q` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --suite-root .. --validate-config` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --suite-root ../fixtures/conformance/deterministic-orchestration --validate-config` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --run-conformance` -> passed.

### Known limitations

- .NET does not yet write packages or rebuild indexes.
- .NET `run`, `merge`, `judge`, and `report` remain future increments.
- This increment originally used a temporary no-dependency console assertion harness; a later Increment 2 setup change migrates the tests to TUnit.

## 2026-06-18 - Increment 1 fixture: deterministic package judgement

### Roadmap position

Current increment: Increment 1, Python frozen conformance baseline. This session added migration-only package-level deterministic judgement attachment for merged experiment packages.

### Added

- Added Python v0 `--judge-lorq-package` with `--lorq-judge-name` and `--lorq-judge-fixture` to attach a named no-LLM judgement pass to an existing v1-alpha LORQ package.
- Added `attach_lorq_deterministic_judgement` to read `.lorq/cells/*.json`, match present cells to `fixtures/fake-judge.yaml`, and write `judgements/<name>/` plus `.lorq/judgements/<name>.json`.
- Added v1-alpha cell judgement and judgement pass schemas under `schemas/`.
- Added tests for successful package judgement attachment and missing deterministic fixture entries.

### Changed

- Updated deterministic benchmark metadata and Python v0 package docs to show package-level judgement attachment as implemented.
- Added LORQ v1-alpha judgement contract constants to Python v0 contracts.

### Validation

Executed during this increment:

- `cd python && python -m pytest -q` -> 82 passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --suite-root .. --validate-config` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --suite-root ../fixtures/conformance/deterministic-orchestration --validate-config` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --run-conformance` -> passed.
- Attached `judge-primary` to the deterministic merged package under `internal/generated/deterministic-benchmark/experiment-001`; the pass judged 8 present cells and preserved 1 missing expected cell.

### Known limitations

- Canonical `reports/report.json`, Markdown report rendering, per-case packs, duplicate/fingerprint edge fixture packages, and committed golden outputs are still pending.
- No roadmap amendment was required.

## 2026-06-18 - Increment 1 fixture: migration package merge

### Roadmap position

Current increment: Increment 1, Python frozen conformance baseline. This session added the migration-only package merge needed to combine deterministic run shards before package-level judgement and reporting.

### Added

- Added Python v0 `--merge-lorq-shards` with `--lorq-merge-out`, `--lorq-benchmark`, `--lorq-package-id`, and `--lorq-allow-incompatible` for v1-alpha run-shard merge.
- Added `merge_lorq_run_shards` to build a merged experiment package with copied shard payloads, rebuilt `.lorq/cells/`, merged coverage, fingerprint, integrity, and merge-log indexes.
- Added expected-cell coverage from deterministic `benchmark.yaml`, surfacing intentionally omitted cells as `missing_cells`.
- Added default merge failure behavior for duplicate cell IDs and incompatible repository fingerprints.
- Added merge-result schema under `schemas/lorq-package-merge-result.v1alpha.schema.json`.
- Added tests for successful merge coverage, duplicate-cell failure, and fingerprint-mismatch failure.

### Changed

- Extended LORQ package integrity output to preserve adapter-level integrity warnings from `adapter.evidence.json`.
- Updated deterministic fixture and Python package docs to include the shard merge command.

### Validation

Executed during this increment:

- `cd python && python -m pytest -q` -> 80 passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --suite-root .. --validate-config` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --suite-root ../fixtures/conformance/deterministic-orchestration --validate-config` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --run-conformance` -> passed.
- Merged deterministic `shard-001` and `shard-002` candidate packages into `internal/generated/deterministic-benchmark/experiment-001`; merge returned 8 present cells, 9 expected cells, and 1 missing expected cell.

### Known limitations

- Package-level deterministic judgement attachment, canonical `reports/report.json`, Markdown report rendering, per-case packs, duplicate/fingerprint edge fixture packages, and committed golden outputs are still pending.
- No roadmap amendment was required.

## 2026-06-18 - Increment 1 fixture: deterministic adapter and judge

### Roadmap position

Current increment: Increment 1, Python frozen conformance baseline. This session added the deterministic fake adapter and fake judge fixture pair needed to start generating the no-LLM orchestration benchmark shards.

### Added

- Added Python v0 `deterministic-fake` agent backend that reads per-cell fixture data and writes full evidence files, including `adapter.evidence.json`, normalized events, usage/counts, timing, artifact refs, and integrity warnings.
- Added Python v0 deterministic fake judge support through `judge.backend: deterministic-fake` and `judge.fixture_file`.
- Added self-contained deterministic orchestration benchmark material under `fixtures/conformance/deterministic-orchestration/`, including cases, modes, prompt style, rubric, tiny fake repository, fake agent fixture, and fake judge fixture.
- Added v1-alpha fake agent and fake judge fixture schemas under `schemas/`.
- Added Python tests for deterministic fake agent output, missing-final-answer modeling, and deterministic fake judge output.

### Changed

- Extended config validation and JSON schemas to accept deterministic fake agent and judge fixture settings.
- Extended LORQ package export to copy `adapter.evidence.json` into cell evidence directories when present.
- Documented deterministic fake adapters in Python v0 backend and package-export docs.

### Validation

Executed during this increment:

- `cd python && python -m pytest -q` -> 77 passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --suite-root .. --validate-config` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --suite-root ../fixtures/conformance/deterministic-orchestration --validate-config` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --run-conformance` -> passed.
- Generated two candidate deterministic run-shard packages under `internal/generated/deterministic-benchmark/`: `shard-001` with 3 cells and `shard-002` with 5 cells.
- Ran a deterministic fake judge smoke test with `--judge --judge-backend deterministic-fake`.

### Known limitations

- The two-shard merge command, package-level judge attachment, canonical reports, per-case packs, duplicate conflict fixture, fingerprint mismatch fixture, and committed golden outputs are still pending.
- The intentionally omitted `skipped-coverage/graphify-plus/attempt-001` cell is documented in the benchmark plan but not yet surfaced as `missing_cells` because package-level merge/coverage expectation logic is future work.
- No roadmap amendment was required.

## 2026-06-18 - Increment 1 foundation: LORQ v1-alpha package export

### Roadmap position

Current increment: Increment 1, Python frozen conformance baseline. This session advanced the fixture foundation by adding a migration-only exporter from Python v0 results into the planned LORQ run-shard package shape.

### Added

- Added Python v0 `--export-lorq-shard` with `--lorq-shard-id` and `--lorq-package-id` to export existing run results into a v1-alpha LORQ run-shard package without invoking agents or judges.
- Added `eval_runner/lorq_package.py` to normalize Python v0 run records into `experiment.yaml`, `runs/<shard>/cells/`, `.lorq/cells/`, `.lorq/coverage.json`, `.lorq/fingerprints.json`, `.lorq/integrity.json`, and `.lorq/merge-log.json`.
- Added a v1-alpha `lorq.cell-evidence.v1alpha1` schema file under `schemas/`.
- Added fixture definition files under `fixtures/conformance/deterministic-orchestration/` for the frozen deterministic orchestration benchmark shape.
- Added Python tests for LORQ run-shard package export and missing-final-answer integrity warnings.
- Added Python v0 package export documentation.

### Changed

- Updated Python v0 docs index and README to include the migration-only LORQ package export path.
- Added LORQ v1-alpha migration fixture constants to Python v0 contracts.
- Imported `sys` in `eval_runner/agents.py` so the existing `{python}` command placeholder is resolvable when used.
- Updated fixture documentation to clarify the deterministic orchestration conformance directory.

### Validation

Executed during this increment:

- `cd python && python -m pytest tests/test_lorq_package.py -q` -> 2 passed.
- Python fake-agent smoke run plus `--export-lorq-shard` -> produced a v1-alpha package with 1 cell.

Full-suite validation is expected before packaging this artifact.

### Known limitations

- This is not the complete frozen benchmark yet. It does not implement two-shard merge, deterministic fake judge, judge fixtures, canonical `report.json`, Markdown rendering, per-case review packs, duplicate conflict fixtures, or fingerprint mismatch fixtures.
- Exported adapter timing still reflects the source Python v0 run; deterministic timing requires the next fake adapter increment.
- No roadmap amendment was required.

## 2026-06-18 - Increment 1 preparation: repo boundary and agent operating rules

### Roadmap position

Current increment: preparing for Increment 1, the Python frozen conformance baseline. This session preserved direction and repository hygiene before benchmark implementation.

### Added

- Added `.agents/` with reusable LORQ agent operating rules for roadmap alignment, source-control boundaries, validation, and deterministic benchmark discipline.
- Added `.agents/README.md` to explain why the rules are source-controlled and where session-specific material belongs.
- Added a session handoff under `internal/handoffs/` for non-source-controlled workspace continuity.

### Changed

- Documented `.agents/` in the root repository layout in `README.md`.

### Validation

Executed during this increment:

- `cd python && python -m pytest -q` -> 71 passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --suite-root .. --validate-config` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --run-conformance` -> passed.

### Known limitations

- No deterministic orchestration benchmark, fake agent adapter, fake judge adapter, or canonical v1 package exporter was added in this increment.
- No roadmap amendment was required.

## 2026-06-18 - Increment 0: repository bootstrap artifact

### Roadmap position

Current increment: Python v0 baseline reorganization and repository initialization.

### Changed

- Created the first clean LORQ repository candidate under `lorq/`.
- Separated source-controlled repo material from agent/session material using the `lorq/` and `internal/` boundary.
- Moved shared product assets to the repository root: `cases/`, `modes/`, `pricing/`, `execution/`, `schemas/`, `prompt_styles/`, `rubrics/`, `repositories/`, and `eval.config.yaml`.
- Moved Python v0 implementation and tests under `python/`.
- Added .NET v1 skeleton under `dotnet/`.
- Added roadmap and operating documents under `docs/`.
- Preserved generated Python v0 example results intentionally under `fixtures/python-v0-generated-results/`.
- Patched Python v0 conformance fixture lookup so it can resolve shared root-level examples in the monorepo layout.

### Validation

Executed during artifact creation:

- `cd python && python -m pytest -q` -> 71 passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --suite-root .. --validate-config` -> passed.
- `cd python && PYTHONPATH=. python -m eval_runner.cli --run-conformance` -> passed.

### Next increment

- Freeze a simple deterministic orchestration benchmark using fake agent and fake judge adapters.
- Create root-level conformance fixtures that can be consumed by both Python v0 and future .NET v1.

## 2026-06-18 - Increment 1e: deterministic package reporting

### Roadmap position

Current increment: Python v0 frozen conformance baseline.

### Changed

- Added migration-only package reporting for judged LORQ experiment packages.
- Wrote canonical `reports/report.json`, Markdown `reports/report.md`, and per-case review packs under `reports/cases/`.
- Added v1-alpha schemas for package reports and case review packs.

### Validation

- Added tests for report rendering from a judged merged package.

## 2026-06-18 - Increment 1f: negative deterministic merge fixtures

### Roadmap position

Current increment: Python v0 frozen conformance baseline.

### Changed

- Added committed negative LORQ fixture packages for duplicate-cell conflicts and repository fingerprint mismatches.
- Updated the deterministic benchmark metadata to mark the merge edge fixtures as covered.
- Added tests proving both edge fixtures fail by default.

### Validation

- Added fixture-backed merge failure tests.

## 2026-06-18 - Increment 1g: frozen deterministic golden baseline

### Roadmap position

Current increment: Python v0 frozen conformance baseline.

### Changed

- Promoted the deterministic no-LLM full-loop package into `fixtures/golden/deterministic-orchestration/`.
- Added committed golden outputs for two run shards, one merged experiment, one deterministic judgement pass, canonical JSON/Markdown reports, and per-case review packs.
- Updated the practical roadmap to record Increment 1 as frozen on this branch once validation is green.
- Added a test that verifies the golden package shape and blocks sandbox-specific absolute paths.

### Validation

- Regenerated the full deterministic loop from the conformance fixture before promotion.
