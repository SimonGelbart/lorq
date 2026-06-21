# Sample file adapter

This example is a deterministic one-shot adapter for the LORQ file protocol. It is intended for conformance checks and local development; it does not call a real LLM.

Run it from the repository root after building the .NET CLI:

```bash
lorq adapter conformance \
  --adapter-command python3 \
  --adapter-arg examples/adapters/file-adapter-sample/sample_file_adapter.py \
  --adapter-working-directory . \
  --out ../internal/generated/sample-file-adapter-conformance
```

A passing run proves that the adapter can read `adapter-request.json`, write `adapter-evidence.json`, and produce referenced answer/stdout/stderr files for the conformance scenarios.
