# Git Workflow

Repository changes should produce reviewable Git history.

## Branches

Create branches from the intended base branch, usually `main`.

Use descriptive slash-separated names:

```text
feat/<short-feature-name>
fix/<short-bug-name>
refactor/<short-area-name>
docs/<short-doc-topic>
ci/<short-ci-topic>
test/<short-test-topic>
```

## Commits

Use focused commits and Conventional Commit messages where practical:

```text
feat: add <capability>
fix: correct <bug>
refactor: simplify <area>
test: cover <behavior>
docs: update <topic>
ci: update <pipeline>
build: update <build-system>
chore: update <maintenance-task>
```

Use scopes when helpful:

```text
feat(cli): add <capability>
fix(adapter): correct <behavior>
refactor(package): extract <concept>
test(validation): cover <contract>
docs(architecture): document <decision>
```

Avoid mixing unrelated concerns in a single commit.

## Commit metadata

Use the Git identity configured in the local development environment.

Do not hard-code personal maintainer identities or private email addresses in committed repository documentation.

Commit dates should normally reflect the actual time the work was performed. Only set explicit author and committer dates when a maintainer or local workflow policy requires it.

If explicit dates are used, set both consistently:

```bash
GIT_AUTHOR_DATE="<DATE>" GIT_COMMITTER_DATE="<DATE>" git commit -m "<message>"
```

Do not falsify Git history or imply that work happened at a time it did not.

## Pull requests

Pull request descriptions should include:

- summary;
- changed areas;
- validation performed;
- known limitations;
- follow-up work, when relevant.

Branches should be comparable with the intended base branch. If a branch has unrelated history, create a clean branch from the intended base and replay the intended changes rather than hiding the problem.
