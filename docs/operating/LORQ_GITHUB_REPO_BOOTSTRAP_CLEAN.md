# LORQ GitHub Repository Bootstrap

## Purpose

This document explains how to initialize the LORQ GitHub repository from the current project handoff.

LORQ stands for **Ledger for Orchestrated Run Quality**.

The product is not primarily an LLM intelligence benchmark. It is an orchestration and evidence harness for agent, tool, and skill evaluations.

Its core product loop is:

```bash
lorq run --no-judge --out shard-001
lorq run --no-judge --out shard-002
lorq merge shard-001 shard-002 --out experiment-001
lorq judge --input experiment-001 --name judge-primary
lorq report --input experiment-001 --primary-judgement judge-primary
```

## Recommended repository shape

Use a monorepo with separate areas for the Python v0 baseline and the future .NET v1 product.

```text
lorq/
  README.md
  CHANGELOG.md
  ROADMAP.md
  .gitignore
  LICENSE

  docs/
    roadmap/
      LORQ_ROADMAP_GOAL_REFINEMENT.md
      LORQ_PRACTICAL_ROADMAP.md
      LORQ_AGENT_WORK_SESSION_HANDOFF.md
    architecture/
    decisions/

  fixtures/
    conformance/
    golden/
    smoke/

  python/
    README.md
    CHANGELOG.md
    pyproject.toml
    eval_runner/
    tests/
    schemas/
    cases/
    modes/
    rubrics/
    prompt_styles/
    examples/
    execution/
    pricing/

  dotnet/
    Lorq.sln
    src/
      Lorq.Cli/
      Lorq.Core/
      Lorq.Adapters.Copilot/
      Lorq.Adapters.Process/
      Lorq.Reporting/
    tests/
      Lorq.Core.Tests/
      Lorq.Cli.Tests/
      Lorq.Adapters.Tests/
```

## What belongs at the repository root

The root should contain product-level material shared by Python and .NET:

- `README.md`
- `CHANGELOG.md`
- `ROADMAP.md`
- `docs/`
- `fixtures/`
- `.gitignore`
- license and governance files

The root should not contain implementation-specific package files unless they apply to the whole repository.

## What belongs in `python/`

The `python/` directory contains the current Python v0 implementation.

Its purpose is to produce a frozen, comparable baseline for the .NET migration.

Keep these there:

- Python package files
- Python tests
- current eval runner implementation
- Python-specific schemas if not yet promoted to shared fixtures
- current cases, modes, rubrics, prompt styles, examples, execution helpers, and pricing files

Do not let Python become the long-term product after the frozen baseline exists.

## What belongs in `dotnet/`

The `dotnet/` directory contains the future LORQ v1 product implementation.

The .NET implementation should become the final product after Python v0 produces the frozen conformance baseline.

Planned projects:

- `Lorq.Cli`
- `Lorq.Core`
- `Lorq.Adapters.Copilot`
- `Lorq.Adapters.Process`
- `Lorq.Reporting`

## What belongs in `fixtures/`

The `fixtures/` directory is shared between Python and .NET.

It defines the compatibility contract between Python v0 and .NET v1.

Expected fixture types:

- deterministic fake adapter run shards
- merged experiment package
- coverage index
- fingerprint index
- fake judge input and output
- report JSON and Markdown
- conflict and error cases
- adapter conformance examples

## What should not be committed

Do not commit:

- virtual environments
- cache directories
- scratch files
- extracted zip artifacts
- local logs
- ad-hoc generated results
- credentials
- `.env` files
- Codex or Copilot auth state
- temporary handoff files unless intentionally promoted to documentation

Recommended `.gitignore` entries:

```gitignore
# Python
__pycache__/
*.py[cod]
.pytest_cache/
.venv/
venv/
.env

# .NET
bin/
obj/
TestResults/
*.user
*.suo

# Local artifacts
internal/
*.zip
*.tar.gz
*.log
*.tmp
.DS_Store

# Generated eval outputs unless promoted to fixtures
results/
runs/
experiments/
judgements/
reports/generated/
```

## Changelog rule

Every increment must update the root `CHANGELOG.md`.

Use this structure:

```markdown
## YYYY-MM-DD - Increment N: Name

### Roadmap position
Current increment: ...
Next increment: ...

### Changed
- ...

### Documentation
- ...

### Validation
- ...

### Package cleanliness
- Confirmed no scratch/generated files were left in `lorq/`.

### Next increments
- ...
```

Implementation-specific changelogs may also exist:

```text
python/CHANGELOG.md
dotnet/CHANGELOG.md
```

But the root changelog is mandatory.

## First commit recommendation

After extracting the source-controlled content into `lorq/`:

```bash
git init
cd lorq
git add .
git commit -m "chore: import Python v0 baseline and LORQ roadmap"
git tag python-v0-baseline-2026-06-18
```

## First development increment

The first increment should not start by adding new product features.

It should:

1. Verify the Python v0 project still runs from `python/`.
2. Add or update the root `CHANGELOG.md`.
3. Add the root `ROADMAP.md` summary.
4. Confirm repository cleanliness.
5. Identify the smallest deterministic fake-adapter benchmark needed for the Python v0 freeze.
