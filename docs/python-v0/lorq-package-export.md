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

This section describes a run-shard package. Python v0 also contains migration-only merge, deterministic fake judgement attachment, JSON report, Markdown rendering, and per-case review pack paths used to create the frozen migration baseline.


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



## Migration-only deterministic judgement attachment

Python v0 can attach a named deterministic fake judgement pass to an already-merged package without invoking Codex, Copilot, or any LLM judge:

```bash
cd python
PYTHONPATH=. python -m eval_runner.cli \
  --judge-lorq-package /path/to/experiment-001 \
  --lorq-judge-name judge-primary \
  --suite-root ../fixtures/conformance/deterministic-orchestration \
  --lorq-judge-fixture fixtures/fake-judge.yaml
```

The command reads `.lorq/cells/*.json`, matches each present cell against the deterministic fake judge fixture, and writes:

```text
experiment-001/
  judgements/
    judge-primary/
      judgement.manifest.json
      judgement.summary.json
      cells/
        <cell-id>.json
  .lorq/
    judgements/
      judge-primary.json
```

A missing expected coverage cell is recorded in the judgement manifest as `missing_expected_cell_ids`; it is not judged because no cell evidence exists. A missing fixture entry for a present cell fails by default because every evaluated cell must have an explicit deterministic quality record.

Each cell judgement records `source.real_llm_used: false` and references the input cell evidence rather than re-running an agent or judge.

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

The local workflow can generate two candidate run shards under `results/deterministic-benchmark/`. The generated outputs remain outside source control until the benchmark is fully frozen and promoted to committed golden fixtures.

## Current limitations

The current Python v0 migration slice establishes run-shard export, deterministic fake adapters, package-level shard merge, and package-level deterministic judgement attachment. It does not yet implement:

- duplicate-cell conflict fixture
- fingerprint mismatch fixture
- `reports/report.json`
- `reports/report.md`
- per-case review packs
- golden committed exported package

## Migration-only package reporting

After deterministic judgement attachment, Python v0 can render the canonical report slice used by the migration benchmark:

```bash
PYTHONPATH=. python -m eval_runner.cli \
  --report-lorq-package ../../results/deterministic-benchmark/experiment-001 \
  --primary-judgement judge-primary
```

This writes:

```text
reports/report.json
reports/report.md
reports/cases/<case-id>/case-review.json
reports/cases/<case-id>/case-review.md
.lorq/report.json
```

The report renderer reads merged package evidence and an existing deterministic judgement pass. It does not call a judge LLM and it does not mutate source run evidence.
