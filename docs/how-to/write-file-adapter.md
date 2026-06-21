# Write a file adapter

Use a file adapter when a runtime can run as a one-shot process. The adapter reads one request, writes one evidence contract, and exits.

## 1. Read the request path

LORQ sets these environment variables before starting the process:

- `LORQ_ADAPTER_REQUEST` — absolute path to `adapter-request.json`.
- `LORQ_ADAPTER_EVIDENCE` — absolute path where `adapter-evidence.json` must be written.
- `LORQ_ADAPTER_EXCHANGE_DIR` — directory for request, evidence, and local adapter files.
- `LORQ_ADAPTER_WORKSPACE_ROOT` — materialized cell workspace root.

Read `LORQ_ADAPTER_REQUEST` first. Do not infer paths from the current working directory.

## 2. Produce the answer and support files

Write the final answer and raw process files inside the exchange directory. Evidence paths must be relative to that directory.

Minimum useful files:

```text
adapter-evidence.json
answer.md
stdout.raw.txt
stderr.txt
```

## 3. Write full evidence

The evidence contract is not just the answer. It must include:

- schema and contract versions;
- matching `cell_id`;
- adapter id/kind/version;
- final answer presence and path;
- usage metadata, even when token counts are zero or unknown;
- timing metadata;
- process stdout/stderr paths;
- trace events;
- artifact references with SHA-256 checksums;
- diagnostics and integrity warnings arrays.

See `examples/adapters/file-adapter-sample/sample_file_adapter.py` for a minimal deterministic implementation.

## 4. Run conformance

Use the canonical command group:

```bash
lorq adapter conformance \
  --adapter-command python3 \
  --adapter-arg examples/adapters/file-adapter-sample/sample_file_adapter.py \
  --adapter-working-directory . \
  --out ../results/sample-file-adapter-conformance
```

The legacy `adapter-conformance` alias remains available for existing automation, but new docs should use `adapter conformance`.

A passing adapter currently completes `basic-exchange`, `metadata-capture`, and `artifact-reference` scenarios. Failures return stable diagnostic codes plus product-facing failure classes. Integrity warnings are preserved as observations; missing files, checksum problems, malformed contracts, timeouts, permission denial, and adapter failures fail conformance. Generated exchange files are written under the requested `--out` directory.

## 5. Use the adapter in a run shard

After conformance passes, use the same command with `run --no-judge`:

```bash
lorq run --no-judge \
  --suite-root fixtures/conformance/deterministic-orchestration \
  --out ../results/sample-adapter-shard \
  --adapter-command python3 \
  --adapter-arg examples/adapters/file-adapter-sample/sample_file_adapter.py \
  --adapter-working-directory .
```

Keep generated outputs under an ignored local output directory such as `results/`, not in the source tree.
