# Changelog

All notable changes to LORQ should be documented here.

## 2026-06-21 - Runtime smoke: adapter scaffolds and metadata

### Added

- Added an optional Codex CLI file-adapter smoke wrapper example.
- Added provider runtime metadata to adapter evidence and run-shard adapter summaries.
- Added a Copilot SDK adapter profile scaffold without adding a core SDK dependency.
- Documented optional runtime smoke workflows and kept deterministic CI fake/no-token.

## 2026-06-21 - Adapter conformance: failure classes and readiness boundary

### Changed

- Added product-facing adapter conformance failure classes alongside low-level diagnostic codes.
- Added deterministic permission-denied, non-zero-exit, exit-code mismatch, artifact checksum, and integrity-warning conformance coverage.
- Documented the conformance readiness boundary across adapter protocol reference docs and ADRs without adding volatile test-count details.

## 2026-06-20 - Adapter conformance: command group and author workflow

### Changed

- Added `lorq adapter conformance` as the documented command-group form for file-adapter conformance while retaining `adapter-conformance` as a compatibility alias.
- Added shallow JSON contract checks before typed request/evidence binding.
- Expanded conformance scenarios to cover metadata and artifact references in addition to the basic exchange.
- Added a deterministic sample file adapter and adapter-author how-to guide.

## 2026-06-20 - Feature: adapter conformance command

### Added

- Added `adapter-conformance` to run a deterministic one-shot file-adapter protocol probe.
- Added a reusable .NET file-adapter conformance runner with stable JSON summary output.
- Added deterministic conformance coverage for valid adapters, malformed evidence, missing final answer metadata, missing referenced output files, adapter timeouts, and process start failures.
- Added an ADR documenting the adapter conformance boundary before real runtime smoke integrations.

### Changed

- Tightened external adapter evidence parsing so malformed evidence JSON is reported as `LORQ-ADAPTER-EVIDENCE-INVALID`.
- Updated CLI and file-adapter protocol documentation with the conformance command.

## 2026-06-20 - Docs: clean source documentation

### Changed

- Reorganized documentation around reader needs: how-to guides, reference, explanation, decisions, and roadmap material.
- Added a documentation-structure ADR.
- Updated top-level README, roadmap summary, .NET README, CLI architecture, run, package validation, parity, and engineering guidance docs to reflect the current deterministic .NET package loop.
- Removed transient task notes, repository bootstrap notes, and reusable agent prompts from source documentation.
- Removed local validation command transcripts and local branch/package notes from source-controlled changelog entries.

## 2026-06-20 - Refactor: run orchestration services

### Changed

- Reworked `DeterministicRunShardApplication` into injected orchestration services instead of a static procedural coordinator.
- Extracted adapter creation/profile application, per-cell adapter execution, prompt construction, cell-evidence mapping, and shard result writing into focused runtime services.
- Registered run orchestration services through CLI host composition.
- Preserved deterministic fake adapter behavior, external file-adapter behavior, Codex wrapper profile behavior, workspace layout, package output, and CLI JSON summary shape.

## 2026-06-20 - Refactor: report rendering pipeline

### Changed

- Kept `LorqPackageReportRenderer` as the public static facade.
- Extracted report orchestration, source reading, report document construction, case review pack construction, Markdown rendering, JSON/file writing, and result construction into internal reporting components.
- Preserved report JSON, report Markdown, case-review JSON, case-review Markdown, and `.lorq/report.json` output shape.

## 2026-06-20 - Refactor: package domain identifiers

### Changed

- Added internal package validation identifiers for package ids, package kinds, schema versions, shard ids, and cell ids.
- Updated package manifest, coverage, merge-log, shard, and report validation internals to pass identifier types across validation boundaries.
- Converted identifiers back to existing public DTO string/int shapes at the package validation facade boundary.

## 2026-06-20 - Refactor: package validation components

### Changed

- Kept `LorqPackageValidator` as the public static facade for package and merge-input validation.
- Extracted package file discovery, manifest reading, coverage/fingerprint/integrity/merge-log validation, run shard/cell reading, judgement validation, report reference validation, and merge input validation into internal package-validation components.
- Preserved diagnostics, validation result records, and CLI JSON output shape.

## 2026-06-20 - Refactor: CLI namespace organization

### Changed

- Moved hosted composition types under `Lorq.Cli.Hosting`.
- Split CLI command implementation into `Commands`, `Commands.Parsing`, `Commands.Handlers`, and `Commands.Results`.
- Moved deterministic run/workspace orchestration helpers under `Lorq.Cli.Runtime`.
- Updated CLI tests to reference the new namespaces without changing command behavior.

## 2026-06-20 - Refactor: hosted CLI composition

### Changed

- Added hosted CLI composition via `LorqCliHost` and `AddLorqCli`.
- Registered command handlers and command catalog through dependency injection.
- Kept `LorqCliApplication.RunAsync` as a test-friendly compatibility entry point backed by the same host composition used by `Program.cs`.
- Added TUnit coverage proving host registrations and command execution.

## 2026-06-19 - Increment 3 runtime: workspace materialization planning

### Added

- Added per-cell disposable workspace planning for `run --no-judge`.
- Added local repository source resolution from case `repo:` metadata and suite `eval.config.yaml`.
- Added repository copy materialization into the adapter workspace before each cell run.
- Added support for mode `materialize.copy` entries.
- Added `--work-root` to place materialized workspaces outside the run package root.

## 2026-06-18 - Increment 3 adapters: Codex file adapter profile

### Added

- Added `CodexFileAdapterProfile` as a built-in profile for external one-shot Codex wrapper processes.
- Added profile environment variables for Codex command, Codex arguments, output format, and one-shot invocation mode.
- Added `--adapter-profile codex-cli`, `--codex-command`, and repeated `--codex-arg` to `run --no-judge`.
- Added deterministic tests proving the profile is passed to the wrapper process without real Codex execution.
- Added `dotnet/docs/adapters/codex-file-adapter-profile.md`.

## 2026-06-18 - Increment 3 runtime: external file adapter process

### Added

- Added `IFileAdapter` so deterministic and external adapters share the same execution boundary.
- Added `ExternalFileAdapterProcess` to write adapter requests, launch a configured process, capture stdout/stderr, and read adapter evidence.
- Added stable protocol diagnostics for process start failures, timeouts, missing evidence, and malformed evidence.
- Added `--adapter-command`, repeated `--adapter-arg`, and `--adapter-working-directory` to `run --no-judge`.
- Added `Lorq.Adapter.TestHost` for no-token external process tests.

## 2026-06-18 - Increment 3 runtime: .NET deterministic run shard

### Added

- Added `run --no-judge` to `Lorq.Cli` with typed command options and handler dispatch.
- Added deterministic benchmark shard planning from `benchmark.yaml`.
- Added deterministic fake file adapter execution from `fixtures/fake-agent.yaml`.
- Added a .NET run-shard package writer and rebuilt `.lorq` indexes.
- Added `dotnet/docs/run-no-judge.md`.

## 2026-06-18 - Increment 3 CLI quality gate

### Changed

- Split CLI command parsing and execution into typed command options, command definitions, and command handlers.
- Added command-handler tests and kept command output JSON stable.

## 2026-06-18 - Increment 2 package full-loop parity

### Added

- Added package merge, deterministic judgement attachment, report rendering, validation, and full package parity tests against the frozen deterministic baseline.

## 2026-06-17 - Python deterministic baseline

### Added

- Added Python v0 deterministic package export, fake agent and judge fixtures, run shards, merged experiment package, report artifacts, and edge-case fixtures used as the .NET migration baseline.
