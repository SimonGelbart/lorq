# File-based one-shot adapter protocol

Increment 3 starts with the adapter boundary instead of broad runtime implementation.

The first external adapter protocol is file-based and one-shot:

1. LORQ materializes an isolated cell workspace.
2. LORQ writes `adapter-request.json` into an exchange directory.
3. LORQ launches the adapter process with that exchange directory.
4. The adapter writes `adapter-evidence.json` and referenced artifacts.
5. LORQ validates the evidence and turns it into a run-shard cell package.

## Request contract

Schema: `schemas/lorq-file-adapter-request.v1alpha.schema.json`

Required top-level fields:

- `schema_version`
- `contract_version`
- `cell`
- `workspace`
- `task`
- `limits`
- `expected_output`

The request gives the adapter enough context to do exactly one cell. It does not give the adapter authority to decide merge, judgement, or reporting behavior.

## Evidence contract

Schema: `schemas/lorq-file-adapter-evidence.v1alpha.schema.json`

The evidence file must be a full evidence contract, not just a final answer. It must include:

- adapter identity and version
- status/failure class
- final answer presence and path
- token/cost usage when available
- timing and timeout state
- process exit code and raw stdout/stderr paths
- normalized trace events
- artifact references with checksums when available
- integrity warnings
- diagnostics

## Scope boundary

This protocol is for deterministic fake adapters, Codex process adapters, and external one-shot adapters. Copilot SDK remains a first-class industrial adapter target, but it should produce the same evidence shape after normalization.

This increment does not implement `lorq run` yet. It only freezes the process boundary that future run orchestration must satisfy.
