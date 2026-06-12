# Portability guide

The Python implementation is the reference implementation for the v1 contract. A future .NET, Go, or Rust implementation should target the external contract, not Python internals.

## Stable

- YAML mode/case/config concepts
- result folder layout
- JSON schema IDs
- normalized event JSONL shape
- validation result shape
- run status values
- core scorecard columns

## Unstable

- exact Markdown wording
- raw Codex/Copilot trace files
- optional judge prompt wording
- diagnostic prose from `--explain-run`

## Conformance

Run:

```bash
agent-eval --run-conformance
```

A port should implement the same fixture and produce equivalent contract-level results:

- completed run
- validation ok
- schema-versioned summary/result/validation/manifest files
- normalized events file present
