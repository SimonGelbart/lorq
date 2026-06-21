# General Coding Standards

Follow the repository's established style first.

## General rules

- Prefer clear, readable code over clever code.
- Use meaningful names.
- Keep functions focused.
- Keep modules cohesive.
- Avoid unrelated changes.
- Preserve public behavior unless explicitly changing it.
- Include tests for new behavior.
- Add regression tests for bug fixes.
- Avoid broad rewrites when a focused change is safer.
- Do not add abstractions without a concrete need.
- Keep generated or temporary files out of source control.

## Error handling

- Validate inputs at boundaries.
- Fail clearly when required data is missing or invalid.
- Preserve useful diagnostics.
- Do not leak secrets or unstable machine-local paths in user-facing errors.

## Refactoring

Refactoring commits should be behavior-preserving unless explicitly stated otherwise.

If behavior changes during refactoring, call that out in the commit message, changelog, documentation, and review summary.
