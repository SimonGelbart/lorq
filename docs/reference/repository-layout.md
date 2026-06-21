# Repository Layout

The repository root is the Git checkout.

```text
lorq/
  AGENTS.md
  CHANGELOG.md
  LICENSE
  README.md
  docs/
  dotnet/
  python/
  schemas/
  fixtures/
  examples/
```

## Root directories

| Path | Purpose |
| --- | --- |
| `docs/` | Canonical documentation root. |
| `dotnet/` | .NET product implementation. |
| `python/` | Python v0 baseline and migration reference. |
| `schemas/` | Shared JSON schemas. |
| `fixtures/` | Intentional fixtures, golden outputs, and conformance packages. |
| `examples/` | Example suites, fixture repositories, and adapter samples. |
| `execution/` | Shared execution assets and skill/tool materialization inputs. |
| `cases/`, `modes/`, `pricing/`, `prompt_styles/`, `rubrics/`, `repositories/` | Shared benchmark and evaluation definitions. |

## Documentation layout

LORQ follows the Diataxis structure:

```text
docs/tutorials/
docs/how-to/
docs/reference/
docs/explanation/
docs/adr/
```

Project-specific documentation folders retained for now:

| Path | Reason |
| --- | --- |
| `docs/roadmap/` | Active product planning documents used by the maintainer. |
| `docs/python-v0/` | Legacy Python baseline documentation used for migration and parity checks. |
| `dotnet/docs/` | .NET implementation notes retained while canonical references are consolidated under `docs/reference/`. |

Avoid new ad-hoc documentation folders unless the reason is documented here.

## Generated output

Do not commit transient logs, local command transcripts, generated validation output, downloaded tools, scratch files, or local run outputs.

Use ignored local output directories such as `results/`, `worktrees/`, or `.lorq/tmp/` for generated data unless the file is an intentional fixture or golden output.

Committed golden fixtures must be deterministic, documented, and covered by tests.
