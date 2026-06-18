# .NET package validation foundation

This document describes the first .NET v1 foundation slice. It is intentionally limited to package IO, domain modeling, and validation against the frozen Python v0 migration fixtures.

## Scope

The current .NET code does **not** run agents or render production/general reports. Those behaviors remain future increments. This slice proves that .NET can read, validate, rebuild indexes for, merge, attach deterministic judgements to, and render deterministic reports from the canonical package shape produced by the frozen Python baseline.

## Projects

```text
Lorq.slnx
src/Lorq.Core/        Domain records, package validation, index rebuilding, merge writing, and deterministic judgement attachment.
src/Lorq.Reporting/   JSON summary shaping plus deterministic package report rendering.
src/Lorq.Cli/         Minimal validation command surface.
tests/Lorq.Core.Tests TUnit fixture validation tests.
```

The test project uses TUnit through Microsoft.Testing.Platform on .NET 10.

## Commands

Validate a package:

```bash
cd dotnet
dotnet run --project src/Lorq.Cli -- validate-package ../fixtures/golden/deterministic-orchestration/experiment-001
```

Validate merge inputs without actually merging them:

```bash
cd dotnet
dotnet run --project src/Lorq.Cli -- validate-merge-inputs \
  ../fixtures/conformance/deterministic-orchestration/edge-fixtures/duplicate-cell-conflict/shard-a \
  ../fixtures/conformance/deterministic-orchestration/edge-fixtures/duplicate-cell-conflict/shard-b
```


Rebuild package indexes from already-existing package evidence:

```bash
cd dotnet
dotnet run --project src/Lorq.Cli -- rebuild-indexes \
  ../fixtures/golden/deterministic-orchestration/experiment-001 \
  ../internal/generated/dotnet-index-rebuild/experiment-001
```

The rebuild command writes `.lorq/cells/`, `coverage.json`, `fingerprints.json`, `integrity.json`, judgement indexes, `merge-log.json`, and `report.json` under the target root. It is intentionally not a .NET merge implementation yet; it proves .NET can write stable package indexes from the frozen Python evidence contract.


Merge run shards into a merged experiment package:

```bash
cd dotnet
dotnet run --project src/Lorq.Cli -- merge-shards \
  ../fixtures/golden/deterministic-orchestration/shard-001 \
  ../fixtures/golden/deterministic-orchestration/shard-002 \
  --out ../internal/generated/dotnet-merge-writer/experiment-001 \
  --package-id deterministic-benchmark \
  --benchmark ../fixtures/conformance/deterministic-orchestration/benchmark.yaml
```

The merge command copies run evidence into a new package, writes `experiment.yaml`, creates `.lorq/merge-log.json`, rebuilds coverage/fingerprint/integrity/cell indexes from package evidence, and rejects duplicate cell IDs or fingerprint mismatches by default.

Attach deterministic judgements from the frozen fake judge fixture:

```bash
cd dotnet
dotnet run --project src/Lorq.Cli -- judge-package \
  ../internal/generated/dotnet-merge-writer/experiment-001 \
  --name judge-primary \
  --fixture ../fixtures/conformance/deterministic-orchestration/fixtures/fake-judge.yaml
```

The judgement command writes `judgements/<name>/`, per-cell judgement files, and `.lorq/judgements/<name>.json`. It is fixture-backed and records `real_llm_used: false`; it intentionally does not call Codex, Copilot, or any judge LLM.


Render deterministic reports from a judged package:

```bash
cd dotnet
dotnet run --project src/Lorq.Cli -- report-package \
  ../internal/generated/dotnet-merge-writer/experiment-001 \
  --primary-judgement judge-primary
```

The report command writes `reports/report.json`, `reports/report.md`, `reports/cases/<case>/case-review.json`, `reports/cases/<case>/case-review.md`, and `.lorq/report.json`. It is deterministic and fixture-backed; it does not invoke any real LLM.

## Stable validation codes introduced

| Code | Meaning |
| --- | --- |
| `LORQ001` | Package or shard root does not exist. |
| `LORQ010`-`LORQ014` | Required package files are missing. |
| `LORQ020`-`LORQ021` | Unsupported package schema version or package kind. |
| `LORQ030`-`LORQ033` | Coverage index is missing, malformed, or inconsistent. |
| `LORQ040`-`LORQ041` | Fingerprint index is missing or malformed. |
| `LORQ050` | Integrity index schema is unexpected. |
| `LORQ060`-`LORQ061` | Merge log is malformed or inconsistent with package kind. |
| `LORQ070`-`LORQ071` | Cell evidence index is missing or inconsistent. |
| `LORQ080`-`LORQ081` | Run shard manifests are missing or inconsistent. |
| `LORQ090` | Coverage present cells do not match the `.lorq/cells` index. |
| `LORQ100` | A cell is missing from the fingerprint index. |
| `LORQ110`-`LORQ111` | Declared shard and run-cell references are inconsistent. |
| `LORQ120`-`LORQ123` | Judgement pass references are inconsistent. |
| `LORQ130`-`LORQ133` | Report references are missing or inconsistent. |
| `LORQ210` | Duplicate cell IDs across merge inputs. |
| `LORQ310` | Missing deterministic judgement fixture entries. |
| `LORQ220` | Repository fingerprint mismatch across merge inputs. |
| `LORQ900`-`LORQ901` | General package format or JSON parse failure. |

## Fixture expectations

The .NET validator must accept:

- `fixtures/golden/deterministic-orchestration/shard-001`
- `fixtures/golden/deterministic-orchestration/shard-002`
- `fixtures/golden/deterministic-orchestration/experiment-001`

Both merge-input validation and the merge writer must reject the hand-authored negative merge fixtures with stable codes:

- duplicate-cell fixture -> `LORQ210`
- fingerprint-mismatch fixture -> `LORQ220`
