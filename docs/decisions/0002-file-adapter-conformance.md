# 0002 — File adapter conformance checks

## Status

Accepted.

## Context

LORQ adapters are external processes that receive `adapter-request.json` and write `adapter-evidence.json` plus referenced artifacts. The protocol is intentionally file-based so deterministic fake adapters, Codex wrappers, Copilot wrappers, and other local runtimes can share the same evidence boundary.

Before adding real runtime integrations, LORQ needs a deterministic way to check whether an adapter process can complete a valid one-shot exchange. This check must not call real LLMs and must produce machine-readable diagnostics that are stable enough for local development and CI.

## Decision

Add a first-class `adapter-conformance` CLI command backed by a reusable .NET conformance runner.

The initial conformance command runs a `basic-exchange` scenario. It writes a canonical request, starts the supplied adapter command without shell expansion, reads the evidence contract, checks stable protocol requirements, and verifies that referenced output files exist in the exchange directory.

The conformance runner reports JSON with:

- protocol contract/schema versions;
- total, passed, and failed scenario counts;
- per-scenario status, adapter id, diagnostic code, diagnostic message, exchange directory, and observations.

Negative protocol scenarios remain deterministic tests around the conformance runner and the test adapter host. Real adapters are expected to pass the same basic exchange before they are used in product smoke paths.

## Consequences

- Adapter authors get a local command to validate protocol wiring before running a full benchmark shard.
- Future Codex and Copilot smoke adapters can be gated by the same file-adapter conformance boundary.
- The first command validates one canonical scenario; additional conformance scenarios can be added without changing the external request/evidence contract.
- The command writes generated exchange files to an explicit `--out` directory so source-controlled docs and fixtures stay clean.
