# Package validation

The .NET validator checks deterministic LORQ run-shard and experiment packages against the canonical package shape.

## Scope

The validator reads package manifests, run shards, coverage indexes, fingerprint indexes, integrity indexes, merge logs, judgement indexes, report references, and per-cell evidence references. It is used by:

- `validate-package`
- `validate-merge-inputs`
- merge conflict checks
- parity tests against golden fixtures

The validator does not call adapters, judges, Codex, Copilot, or any real LLM.

## Commands

Validate a package:

```bash
cd dotnet
dotnet run --project src/Lorq.Cli -- validate-package ../fixtures/golden/deterministic-orchestration/experiment-001
```

Validate merge inputs without writing an experiment package:

```bash
cd dotnet
dotnet run --project src/Lorq.Cli -- validate-merge-inputs \
  ../fixtures/conformance/deterministic-orchestration/edge-fixtures/duplicate-cell-conflict/shard-a \
  ../fixtures/conformance/deterministic-orchestration/edge-fixtures/duplicate-cell-conflict/shard-b
```

Rebuild indexes into a target root:

```bash
cd dotnet
dotnet run --project src/Lorq.Cli -- rebuild-indexes \
  ../fixtures/golden/deterministic-orchestration/experiment-001 \
  ../results/dotnet-index-rebuild/experiment-001
```

## Stable validation codes

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
| `LORQ310` | Missing deterministic judgement fixture entries. |
| `LORQ900`-`LORQ901` | General package format or JSON parse failure. |

## Fixture expectations

Valid fixtures:

- `fixtures/golden/deterministic-orchestration/shard-001`
- `fixtures/golden/deterministic-orchestration/shard-002`
- `fixtures/golden/deterministic-orchestration/experiment-001`

Known invalid merge fixtures:

- duplicate-cell fixture -> `LORQ210`
- fingerprint-mismatch fixture -> `LORQ220`
