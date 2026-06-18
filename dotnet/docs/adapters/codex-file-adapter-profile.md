# Codex file adapter profile

The Codex profile is a wrapper contract on top of the file-based one-shot adapter protocol. It does not call Codex directly from `Lorq.Cli`.

The boundary stays:

```text
lorq run --no-judge
-> writes adapter-request.json
-> launches an external wrapper process
-> wrapper may call Codex CLI
-> wrapper writes adapter-evidence.json
-> LORQ validates and packages full evidence
```

## Why a wrapper profile

Codex CLI is an industrial runtime target, but the migration gate must remain deterministic and no-token. LORQ therefore models Codex through the same file-adapter evidence contract used by fake and external adapters.

The wrapper process is responsible for translating between Codex-specific streaming output and the LORQ evidence contract. Adapter output must include final answer, usage, timing, process, trace, artifacts, integrity warnings, and diagnostics.

## CLI shape

Use the profile with an explicit wrapper command:

```bash
lorq run \
  --no-judge \
  --suite-root fixtures/conformance/deterministic-orchestration \
  --out shard-001 \
  --adapter-command lorq-codex-file-adapter \
  --adapter-profile codex-cli \
  --codex-command codex \
  --codex-arg exec \
  --codex-arg --json
```

`--adapter-command` is the wrapper executable, not Codex itself. The wrapper receives the normal file-adapter environment plus Codex profile metadata.

## Profile environment

The process adapter injects:

- `LORQ_ADAPTER_PROFILE=codex-cli`
- `LORQ_CODEX_COMMAND`, defaulting to `codex`
- `LORQ_CODEX_ARGUMENTS`, newline-delimited, defaulting to `exec` and `--json`
- `LORQ_CODEX_OUTPUT_FORMAT=codex-jsonl`
- `LORQ_CODEX_INVOCATION=one-shot-file-adapter`

The standard file-adapter variables are still present:

- `LORQ_ADAPTER_REQUEST`
- `LORQ_ADAPTER_EVIDENCE`
- `LORQ_ADAPTER_EXCHANGE_DIR`
- `LORQ_ADAPTER_WORKSPACE_ROOT`

## Current implementation status

This increment adds the Codex-oriented profile and deterministic tests using `Lorq.Adapter.TestHost`. It intentionally does not run a real Codex process and does not make Codex a migration gate.
