# AGENTS.md

This repository uses these rules for automated and assisted coding agents.

The goal is to make repository changes reproducible, reviewable, well-documented, and safe to maintain.

## Mandatory read order

Before changing code, read this file.

For implementation work, also read:

1. `docs/reference/project-profile.md`
2. `docs/reference/repository-layout.md`
3. `docs/reference/git-workflow.md`
4. `docs/reference/validation.md`
5. `docs/reference/documentation-standards.md`
6. `docs/reference/architecture/architecture-principles.md`
7. `docs/reference/architecture/boundaries.md`
8. `docs/reference/architecture/dependency-policy.md`
9. `docs/reference/architecture/design-patterns.md`
10. `docs/reference/architecture/testing-strategy.md`
11. the relevant language profile under `docs/reference/coding/`
12. `docs/adr/README.md`
13. any accepted ADR relevant to the area being changed

## Rule precedence

Use this precedence order:

1. Direct maintainer instruction for the current task
2. This `AGENTS.md` file
3. Accepted ADRs in `docs/adr/`
4. Reference documentation in `docs/reference/`
5. Explanation documentation in `docs/explanation/`
6. How-to documentation in `docs/how-to/`
7. Existing code conventions

If a maintainer instruction contradicts an accepted ADR, address the contradiction explicitly. Adapt the implementation to follow the ADR or create a new ADR that supersedes or amends the previous one.

## Non-negotiable rules

- Do not push to a remote unless the maintainer explicitly asks.
- Do not open a pull request unless the maintainer explicitly asks.
- Do not claim validation passed unless it was actually run and passed.
- Do not hide failed validation.
- Do not claim remote repository state changed unless the operation was confirmed.
- Do not commit transient artifacts, validation logs, generated test output, local transcripts, temporary manifests, or scratch files.
- Keep committed repository documentation under `docs/` and follow the Diataxis structure.
- Follow accepted ADRs. If a change contradicts an ADR, create a new ADR to explain the change.
- Avoid duplicating documentation or writing details that will drift quickly.
- Prefer focused commits and Conventional Commit messages.
- Every meaningful implementation increment should include tests and documentation, or a clear explanation of why they are not needed.
- Follow applicable architecture and language-specific coding standards under `docs/reference/`.

## Standard repository shape

```text
lorq/
  .git/
  AGENTS.md
  CHANGELOG.md
  LICENSE
  docs/
    README.md
    tutorials/
    how-to/
    reference/
      architecture/
      coding/
    explanation/
    adr/
```

## Documentation standard

Documentation follows Diataxis:

- `docs/tutorials/` for learning-oriented walkthroughs
- `docs/how-to/` for task-oriented procedures
- `docs/reference/` for stable technical reference
- `docs/explanation/` for background, rationale, and concepts
- `docs/adr/` for Architecture Decision Records

Avoid ad-hoc documentation folders unless a repository-specific reason is documented in `docs/reference/repository-layout.md`.

## Honesty standard

Be explicit about:

- what was done
- what was not done
- what was changed
- what was not changed
- what was validated
- what could not be validated
- what failed validation
- whether anything was committed
- whether a branch was created
- whether remote repository state was changed
