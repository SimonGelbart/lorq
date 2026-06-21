# File-based one-shot adapter protocol

Increment 3 starts with the adapter boundary instead of broad runtime implementation.

The first external adapter protocol is file-based and one-shot:

1. LORQ materializes an isolated cell workspace.
2. LORQ writes `adapter-request.json` into an exchange directory.
3. LORQ launches the adapter process with request/evidence paths exposed through environment variables.
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

## Process invocation

The .NET process adapter writes `adapter-request.json`, then launches the configured adapter executable without shell expansion. The process receives:

- `LORQ_ADAPTER_REQUEST`: absolute path to `adapter-request.json`
- `LORQ_ADAPTER_EVIDENCE`: absolute path where `adapter-evidence.json` must be written
- `LORQ_ADAPTER_EXCHANGE_DIR`: directory shared for request, evidence, and raw adapter files
- `LORQ_ADAPTER_WORKSPACE_ROOT`: workspace root visible to the adapter

The runner captures process stdout/stderr into `adapter-process.stdout.txt` and `adapter-process.stderr.txt` for diagnostics, but the adapter remains responsible for writing the full evidence contract. If no evidence file is produced, the invocation fails with a stable protocol error.


## Conformance command

Use `adapter conformance` to check a local adapter wrapper before using it in `run --no-judge`:

```bash
lorq adapter conformance \
  --adapter-command <adapter-executable> \
  --adapter-arg <argument-if-needed> \
  --out ../internal/generated/adapter-conformance
```

The legacy `adapter-conformance` command remains available as an alias.

The command currently runs deterministic `basic-exchange`, `metadata-capture`, and `artifact-reference` scenarios. It writes request contracts, launches the adapter without shell expansion, reads evidence contracts, verifies required protocol fields, and checks that referenced output files and metadata exist.

A failure returns exit code `1` with a stable diagnostic code such as:

- `LORQ-ADAPTER-PROCESS-START` — adapter process could not be started.
- `LORQ-ADAPTER-PROCESS-TIMEOUT` — adapter exceeded the request timeout.
- `LORQ-ADAPTER-EVIDENCE-MISSING` — no `adapter-evidence.json` was produced.
- `LORQ-ADAPTER-EVIDENCE-INVALID` — evidence JSON could not be parsed.
- `LORQ-ADAPTER-EVIDENCE-FINAL-ANSWER` — final answer metadata is missing.
- `LORQ-ADAPTER-EVIDENCE-USAGE` — usage metadata is missing.
- `LORQ-ADAPTER-CONFORMANCE-FILES` — evidence references an output file that does not exist.

Generated exchange directories are local run artifacts. Keep them outside the source tree, usually under the sibling `internal/generated/` workspace used by handoff packages. See `docs/how-to/write-file-adapter.md` and `examples/adapters/file-adapter-sample/` for a minimal adapter-author workflow.

## Scope boundary

This protocol is for deterministic fake adapters, Codex process adapters, and external one-shot adapters. Copilot SDK remains a first-class industrial adapter target, but it should produce the same evidence shape after normalization.

The current .NET `run --no-judge` path can use this process adapter for deterministic planned shards. Setup command execution, Git worktree/clone orchestration, direct Codex runtime behavior, and Copilot SDK runtime behavior remain future adapter/runtime work.


## Built-in process profiles

The first built-in process profile is `codex-cli`. It injects Codex wrapper metadata into an external one-shot adapter process while preserving the same request/evidence contract. See `codex-file-adapter-profile.md`.
