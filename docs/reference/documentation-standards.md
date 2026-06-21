# Documentation Standards

Documentation must be durable, focused, and easy to keep correct.

LORQ follows the Diataxis pattern.

## Standard structure

```text
docs/
  README.md
  tutorials/
  how-to/
  reference/
  explanation/
  adr/
```

## Placement rules

Use `docs/tutorials/` for learning-oriented walkthroughs.

Use `docs/how-to/` for task-oriented procedures.

Use `docs/reference/` for exact facts, contracts, options, configuration keys, schemas, APIs, and file formats.

Use `docs/explanation/` for background, design rationale, architecture, trade-offs, and domain concepts.

Use `docs/adr/` for durable architecture, product, contract, or compatibility decisions.

## Avoid mixed documents

Avoid mixing documentation types in one file.

Examples:

- Do not hide reference tables inside tutorials.
- Do not put long conceptual explanations inside a how-to guide.
- Do not put step-by-step operational instructions inside an ADR.
- Do not put architectural decisions only in changelog entries.

## Avoid duplication and drift

Prefer one canonical source of truth and link to it from other documents.

Avoid long-lived documentation that includes:

- generated command output;
- temporary validation results;
- local machine paths;
- transient execution-environment paths;
- near-term guesses;
- CI run IDs;
- local delivery mechanics.

Prefer documenting:

- stable contracts;
- invariants;
- decision rationale;
- supported workflows;
- public interfaces;
- validation commands;
- where generated output should go;
- how to regenerate current information;
- known limitations that affect users or maintainers.

## Documentation update rules

Every meaningful implementation increment must update documentation unless there is a clear reason why no documentation change is needed.

At minimum, consider whether the change affects:

- `CHANGELOG.md`
- `docs/README.md`
- `docs/tutorials/`
- `docs/how-to/`
- `docs/reference/`
- `docs/explanation/`
- `docs/adr/`

Create a new ADR under `docs/adr/` when a durable decision is introduced, reversed, or materially amended.
