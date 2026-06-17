# Python v0 LORQ package export

Python v0 remains a prototype and fixture generator. It is not the final LORQ product core.

This export path exists only to freeze deterministic orchestration behavior before the .NET v1 migration. It converts existing Python v0 `agent-eval` run results into the first v1-alpha LORQ run-shard package shape.

## Command

Run Python v0 normally, preferably with fake deterministic adapters and `--no-judge`, then export the result folder:

```bash
cd python
PYTHONPATH=. python -m eval_runner.cli \
  --suite-root .. \
  --out /path/to/python-v0-results \
  --export-lorq-shard /path/to/shard-001 \
  --lorq-shard-id shard-001 \
  --lorq-package-id experiment-001
```

The exporter reads the existing `--out` directory. It does not invoke an agent, judge, Copilot, Codex, or another LLM.

## Package layout written today

```text
shard-001/
  experiment.yaml
  runs/
    shard-001/
      shard.manifest.json
      cells/
        <cell-id>/
          cell_result.json
          prompt.txt
          answer.md
          validation.json
          events.normalized.jsonl
          events.summary.json
          stdout.raw.jsonl
          stdout.raw.txt
          stderr.txt
  judgements/
  reports/
    cases/
  .lorq/
    coverage.json
    fingerprints.json
    integrity.json
    merge-log.json
    cells/
      <cell-id>.json
```

This is a run-shard package, not a fully merged judged experiment. The future frozen baseline still needs two shards, merge, deterministic fake judgement, JSON report, Markdown rendering, and per-case packs.

## Cell identity

Python v0 still calls independent executions `repetition`. The exporter maps that field to the LORQ product term `attempt`:

```text
cell_id = <case_id>__<mode_id>__attempt-001
```

The normalized cell evidence includes both product identity fields and source provenance.

## v1-alpha cell evidence contract

Each `cell_result.json` and each `.lorq/cells/<cell-id>.json` contains the same full cell evidence object:

```json
{
  "schema_version": "lorq.cell-evidence.v1alpha1",
  "contract_version": "lorq.contract.v1alpha1",
  "cell_id": "case-a__mode-a__attempt-001",
  "case_id": "case-a",
  "mode_id": "mode-a",
  "attempt_id": "attempt-001",
  "shard_id": "shard-001",
  "status": "completed",
  "fingerprint": {
    "repo": "demo",
    "repo_type": "local",
    "ref": "HEAD",
    "commit": "abc123",
    "dirty": false,
    "is_git_repo": true
  },
  "adapter_output": {
    "status": "completed",
    "final_answer_present": true,
    "final_answer_chars": 123,
    "adapter": {
      "id": "fake-deterministic",
      "backend": "fake-deterministic",
      "output_format": "text",
      "input_mode": "stdin",
      "exit_code": 0,
      "timed_out": false,
      "ok": true,
      "error_category": null
    },
    "usage": {},
    "counts": {},
    "timing": {},
    "trace": {},
    "validation": {},
    "artifacts": []
  },
  "evidence_refs": {
    "cell_dir": "runs/shard-001/cells/case-a__mode-a__attempt-001",
    "final_answer": "runs/shard-001/cells/case-a__mode-a__attempt-001/answer.md",
    "cell_result": "runs/shard-001/cells/case-a__mode-a__attempt-001/cell_result.json"
  }
}
```

Adapter output is intentionally more than a final answer. It carries answer presence, adapter status, exit/timing data, usage/counts, trace summary references, validation summary, and file references.

## Integrity stance

The exporter records integrity warnings in `.lorq/integrity.json` for trust-relevant conditions such as missing final answers or non-completed cells. It does not use real LLM quality as the migration gate.

## Current limitations

This increment only establishes the export path and evidence shape. It does not yet implement:

- deterministic fake judge adapter
- fake judge fixture format
- two-shard merge
- duplicate-cell conflict fixture
- fingerprint mismatch fixture
- `reports/report.json`
- `reports/report.md`
- per-case review packs
- golden committed exported package
