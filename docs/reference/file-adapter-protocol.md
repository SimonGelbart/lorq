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

- adapter identity, version, and optional provider runtime metadata
- status and product-facing failure class when the scenario fails
- final answer presence and path
- token/cost usage when available
- timing and timeout state
- process exit code and raw stdout/stderr paths
- normalized trace events
- artifact references with SHA-256 checksums
- integrity warnings that remain visible but do not block conformance by themselves
- diagnostics

## Process invocation

The .NET process adapter writes `adapter-request.json`, then launches the configured adapter executable without shell expansion. The process receives:

- `LORQ_ADAPTER_REQUEST`: absolute path to `adapter-request.json`
- `LORQ_ADAPTER_EVIDENCE`: absolute path where `adapter-evidence.json` must be written
- `LORQ_ADAPTER_EXCHANGE_DIR`: directory shared for request, evidence, and raw adapter files
- `LORQ_ADAPTER_WORKSPACE_ROOT`: workspace root visible to the adapter

The runner captures process stdout/stderr into `adapter-process.stdout.txt` and `adapter-process.stderr.txt` for diagnostics, but the adapter remains responsible for writing the full evidence contract. If no evidence file is produced, the invocation fails with a stable protocol error. If evidence is produced after a non-zero process exit, the evidence `process.exit_code` must match the observed process exit code and the evidence `status` determines the product-facing failure class.


## Conformance command

Use `adapter conformance` to check a local adapter wrapper before using it in `run --no-judge`:

```bash
lorq adapter conformance \
  --adapter-command <adapter-executable> \
  --adapter-arg <argument-if-needed> \
  --out ../internal/generated/adapter-conformance
```

The legacy `adapter-conformance` command remains available as an alias.

The command currently runs deterministic `basic-exchange`, `metadata-capture`, and `artifact-reference` scenarios. It writes request contracts, launches the adapter without shell expansion, reads evidence contracts, verifies required protocol fields, checks referenced output files, verifies artifact SHA-256 values, preserves integrity warnings as observations, and maps failures to the ADR 0007 taxonomy.

A failure returns exit code `1` with a stable diagnostic code and a `failure_class` such as:

- `LORQ-ADAPTER-PROCESS-START` / `setup_failure` — adapter process could not be started.
- `LORQ-ADAPTER-PROCESS-TIMEOUT` / `timeout` — adapter exceeded the request timeout.
- `LORQ-ADAPTER-EVIDENCE-MISSING` / `adapter_failed` — no `adapter-evidence.json` was produced.
- `LORQ-ADAPTER-EVIDENCE-INVALID` / `setup_failure` — evidence JSON could not be parsed.
- `LORQ-ADAPTER-EVIDENCE-FINAL-ANSWER` / `no_final_answer` — final answer metadata is missing.
- `LORQ-ADAPTER-EVIDENCE-STATUS` / evidence status class — evidence reported `timeout`, `no_final_answer`, `adapter_failed`, `permission_denied`, or `invalid_artifact`.
- `LORQ-ADAPTER-CONFORMANCE-FILES` / `invalid_artifact` — evidence references a missing output file or an artifact checksum is missing or invalid.

Generated exchange directories are local run artifacts. Keep them outside the source tree, usually under the sibling `internal/generated/` workspace used by handoff packages. See `docs/how-to/write-file-adapter.md` and `examples/adapters/file-adapter-sample/` for a minimal adapter-author workflow.

## Scope boundary

This protocol is for deterministic fake adapters, Codex process adapters, and external one-shot adapters. Copilot SDK remains a first-class industrial adapter target, but it should produce the same evidence shape after normalization.

The current .NET `run --no-judge` path can use this process adapter for deterministic planned shards and optional local runtime smoke wrappers. Setup command execution, Git worktree/clone orchestration, and dirty-worktree policy remain future adapter/runtime work.


## Built-in process profiles

Built-in process profiles include `codex-cli` and `copilot-sdk`. They inject wrapper metadata into an external one-shot adapter process while preserving the same request/evidence contract. See `../../dotnet/docs/adapters/codex-file-adapter-profile.md` and `../how-to/run-runtime-smoke.md`.

## Runtime metadata

Adapters may include optional `adapter.runtime` metadata in evidence. This metadata is preserved for inspection and reporting, but package validation should not interpret provider-specific details. Stable metadata fields are `provider`, `runtime`, `runtime_version`, `profile`, `command`, `permission_profile`, `output_format`, and provider-specific `extensions`.
