# .NET package validation foundation

This document describes the first .NET v1 foundation slice. It is intentionally limited to package IO, domain modeling, and validation against the frozen Python v0 migration fixtures.

## Scope

The current .NET code does **not** run agents, merge shards, attach judgements, or render reports. Those behaviors remain future increments. This slice proves that .NET can read and validate the canonical package shape produced by the frozen Python baseline.

## Projects

```text
Lorq.slnx
src/Lorq.Core/        Domain records and package validation.
src/Lorq.Reporting/   JSON summary shaping for CLI output.
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
| `LORQ220` | Repository fingerprint mismatch across merge inputs. |
| `LORQ900`-`LORQ901` | General package format or JSON parse failure. |

## Fixture expectations

The .NET validator must accept:

- `fixtures/golden/deterministic-orchestration/shard-001`
- `fixtures/golden/deterministic-orchestration/shard-002`
- `fixtures/golden/deterministic-orchestration/experiment-001`

It must reject the hand-authored negative merge fixtures with stable codes:

- duplicate-cell fixture -> `LORQ210`
- fingerprint-mismatch fixture -> `LORQ220`
