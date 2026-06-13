# LORQ Artifact and Repository Boundary Rules

## Purpose

This document defines the boundary between files that belong in the GitHub repository and files that are only useful to an AI agent work session.

The rule is simple:

```text
lorq/     = candidate GitHub repository
internal/ = agent/session workspace, not source-controlled by default
```

## Required artifact shape

Any zip or handoff artifact produced by an AI agent should have this top-level shape:

```text
artifact.zip
  lorq/
    ...source-controlled project files...

  internal/
    ...handoffs, scripts, scratch files, logs, generated outputs, notes...
```

The `lorq/` directory must be clean enough to commit.

The `internal/` directory may contain useful work-session material, but it is not part of the repository unless something is explicitly promoted.

## What belongs in `lorq/`

Only put files in `lorq/` if they should be source-controlled.

Examples:

- source code
- tests
- schemas
- shared fixtures
- product documentation
- roadmap documents
- architecture decisions
- README files
- changelog files
- package manifests
- build configuration
- deterministic conformance fixtures

## What belongs in `internal/`

Put work-session material in `internal/`.

Examples:

```text
internal/
  handoffs/
  scripts/
  scratch/
  generated/
  logs/
  extracted-zips/
  session-notes/
```

Use `internal/` for:

- AI agent handoff notes
- temporary scripts
- exploratory analysis
- extracted original handoff zips
- generated reports not yet promoted to fixtures
- local run outputs
- debug logs
- session summaries
- TODO dumps
- discarded drafts

## Promotion rule

A file may move from `internal/` to `lorq/` only when it becomes intentional product material.

Before promotion, verify:

1. It has a stable purpose.
2. It is referenced by the roadmap, docs, tests, or code.
3. It contains no credentials, local paths, scratch notes, or accidental generated state.
4. It is documented if it changes behavior, schema, or workflow.
5. The root `CHANGELOG.md` is updated if the promotion is part of an increment.

## Anti-drift rules

Every increment must follow these rules:

1. Start by reading the current roadmap and changelog.
2. State the current roadmap increment before changing files.
3. Keep `lorq/` source-control clean.
4. Put scratch work under `internal/`.
5. Update `lorq/CHANGELOG.md` for every increment.
6. Update documentation for every behavior, schema, CLI, package, adapter, or report change.
7. If a new increment is suggested, amend the roadmap instead of leaving the change only in chat.
8. End by listing the current increment, completed changes, validation performed, and next increments.

## Required end-of-increment checklist

At the end of every AI agent work session, produce a summary under:

```text
internal/handoffs/
```

The summary must include:

```markdown
# Session Summary

## Roadmap position
Current increment: ...
Next increment: ...

## Files changed in lorq/
- ...

## Files created in internal/
- ...

## Changelog update
- Updated: yes/no
- Location: lorq/CHANGELOG.md

## Documentation update
- Updated: yes/no
- Files: ...

## Validation performed
- ...

## Package cleanliness
- Confirmed no scratch files left in lorq/: yes/no

## Next recommended increment
- ...
```

## Forbidden by default in `lorq/`

Do not leave these in `lorq/`:

- temporary scripts
- local debug logs
- generated ad-hoc results
- extracted zip folders
- duplicate roadmap drafts
- copied chat transcripts
- `.env` files
- credentials
- Copilot or Codex auth state
- cache directories
- build output
- unreviewed AI-generated scratch notes

## Clean package expectation

A delivered artifact should be reviewable like this:

```text
artifact.zip
  lorq/
    README.md
    CHANGELOG.md
    ROADMAP.md
    docs/
    fixtures/
    python/
    dotnet/

  internal/
    handoffs/
    scripts/
    scratch/
    generated/
    logs/
```

If a reviewer deletes `internal/`, the `lorq/` repository should still make sense and remain buildable/testable.
