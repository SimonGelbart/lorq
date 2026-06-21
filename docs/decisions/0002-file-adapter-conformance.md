# 0002 — File adapter conformance checks

## Status

Accepted.

## Context

LORQ adapters are external processes that receive `adapter-request.json` and write `adapter-evidence.json` plus referenced artifacts. The protocol is intentionally file-based so deterministic fake adapters, Codex wrappers, Copilot wrappers, and other local runtimes can share the same evidence boundary. ADR 0005 records the broader SDK-independent adapter architecture; this ADR records the conformance decision for the file-adapter boundary.

Before adding real runtime integrations, LORQ needs a deterministic way to check whether an adapter process can complete a valid one-shot exchange. This check must not call real LLMs and must produce machine-readable diagnostics that are stable enough for local development and CI.

## Decision

Add a first-class `adapter conformance` CLI command group backed by a reusable .NET conformance runner. Keep `adapter-conformance` as a compatibility alias while docs use the command-group form.

The conformance command runs `basic-exchange`, `metadata-capture`, and `artifact-reference` scenarios. It writes canonical requests, starts the supplied adapter command without shell expansion, reads evidence contracts, checks stable protocol requirements, validates shallow JSON contract shape, verifies referenced output files and artifact checksums, preserves integrity warnings as observations, and maps failures to ADR 0007 classes.

The conformance runner reports JSON with:

- protocol contract/schema versions;
- total, passed, and failed scenario counts;
- per-scenario status, adapter id, diagnostic code, failure class, diagnostic message, exchange directory, and observations.

Negative protocol scenarios remain deterministic tests around the conformance runner and the test adapter host. They cover setup failure, timeout, adapter failure, permission denial, missing final answers, invalid artifacts, warning preservation, exit-code consistency, and malformed or incomplete evidence. Real adapters are expected to pass the same conformance boundary before they are used in product smoke paths.

## Consequences

- Adapter authors get a local command to validate protocol wiring before running a full benchmark shard.
- Future Codex and Copilot smoke adapters can be gated by the same file-adapter conformance boundary.
- Additional conformance scenarios can be added without changing the external request/evidence contract.
- Adapter authors have a minimal sample process under `examples/adapters/file-adapter-sample/`.
- The command writes generated exchange files to an explicit `--out` directory so source-controlled docs and fixtures stay clean.
