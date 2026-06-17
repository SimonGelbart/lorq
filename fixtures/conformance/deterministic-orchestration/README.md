# Deterministic orchestration conformance fixture

This directory defines the source-controlled fixture shape for the Python frozen baseline in practical roadmap Increment 1.

The fixture is intentionally about orchestration, package integrity, adapter evidence, merge behavior, judgement attachment, and report rendering. It must not depend on real LLM intelligence.

## Fixture rules

- Agent outputs come from fake deterministic adapters.
- Judge outputs come from fake deterministic judge fixtures.
- Every adapter result must produce a full cell evidence contract:
  - final answer presence and answer artifact reference
  - adapter status, exit/timing data, and failure category
  - deterministic usage/cost-like counters
  - deterministic trace/event references
  - validation summary
  - integrity-relevant artifacts
- Reports are generated from canonical JSON first; Markdown is a rendering.

## Current status

This increment adds the fixture definition and Python v0 export path. The actual golden exported two-shard package will be added after fake agent and fake judge adapters are wired into the benchmark.
