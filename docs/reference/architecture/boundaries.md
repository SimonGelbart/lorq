# Architecture Boundaries

Use boundaries that make package behavior testable and provider-independent.

## CLI boundary

`Lorq.Cli` owns host composition, command parsing, command dispatch, and console rendering.

It should translate command-line input into typed options and delegate behavior to command handlers or services.

## Core package boundary

`Lorq.Core` owns deterministic package behavior:

- run-shard package writing;
- merge input validation and merge output writing;
- package validation;
- index rebuilding;
- deterministic judgement attachment;
- package lifecycle concepts.

Core should not depend on Codex, Copilot, process-specific APIs, or CLI parsing.

## Reporting boundary

`Lorq.Reporting` owns canonical report data and renderers.

JSON report data is canonical. Markdown reports are renderings derived from that data.

## Adapter boundary

Adapter projects own external execution details.

File adapters communicate through request and evidence files. Provider-specific metadata may be preserved, but provider-specific SDK details must not become package validation assumptions.

## Python baseline boundary

`python/` remains a deterministic v0 baseline and migration reference. It should not be treated as the future architecture for the .NET product path.

## Fixtures boundary

Committed fixtures must be deterministic, reviewed, and intentionally part of the repository.

Generated local outputs belong in ignored output directories until they are promoted to fixtures.
