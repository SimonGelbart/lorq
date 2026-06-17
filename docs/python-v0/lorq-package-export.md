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

This is a run-shard package, not a fully judged experiment. Python v0 now also has a migration-only merge path for run shards; deterministic fake judgement attachment, JSON report, Markdown rendering, and per-case packs still remain future increments.


## Migration-only shard merge

Python v0 can merge exported run-shard packages into a v1-alpha merged experiment package without invoking an agent, judge, Copilot, Codex, or another LLM:

```bash
cd python
PYTHONPATH=. python -m eval_runner.cli \
  --merge-lorq-shards /path/to/shard-001 /path/to/shard-002 \
  --lorq-merge-out /path/to/experiment-001 \
  --lorq-package-id experiment-001 \
  --lorq-benchmark ../fixtures/conformance/deterministic-orchestration/benchmark.yaml
```

The merge copies each shard payload under `runs/<shard-id>/`, rebuilds `.lorq/cells/`, writes merged `coverage.json`, `fingerprints.json`, `integrity.json`, and records a `merge-log.json`.

By default, merge fails on duplicate cell IDs or incompatible repository fingerprints. Missing expected cells are warnings rather than hard failures because partial coverage is an intentional benchmark condition. Use `--lorq-allow-incompatible` only for diagnostic edge fixtures where the merged package should be written with integrity errors instead of stopping.

When `--lorq-benchmark` points to a deterministic benchmark shape, expected cells are computed as the case × mode × attempt matrix. This exposes intentionally omitted cells such as `skipped-coverage__graphify-plus__attempt-001`.

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


## Deterministic fake fixture slice

`fixtures/conformance/deterministic-orchestration/` now contains the runnable no-LLM benchmark slice for Python v0:

- `fixtures/fake-agent.yaml` drives deterministic adapter output for each case/mode/attempt.
- `fixtures/fake-judge.yaml` drives deterministic quality scores without real LLM calls.
- `cases/`, `modes/`, `prompt_styles/`, `rubrics/`, and `fake_project/` make the fixture self-contained.

The session workflow can generate two candidate run shards under `internal/generated/deterministic-benchmark/`. The generated outputs remain outside source control until the benchmark is fully frozen and promoted to committed golden fixtures.

## Current limitations

The current Python v0 migration slice establishes run-shard export, deterministic fake adapters, and package-level shard merge. It does not yet implement:

- duplicate-cell conflict fixture
- fingerprint mismatch fixture
- `reports/report.json`
- `reports/report.md`
- per-case review packs
- golden committed exported package
