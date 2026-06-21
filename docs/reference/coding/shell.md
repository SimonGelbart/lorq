# Shell Coding Standards

Use these standards for shell scripts unless stronger local conventions exist.

## Style

- Prefer POSIX shell for portability unless Bash features are required.
- Use `set -euo pipefail` in Bash scripts when appropriate.
- Quote variables unless word splitting is intentional.
- Check that required commands exist before using them.
- Avoid destructive commands unless the target path is validated.
- Keep scripts small and focused.

## Safety

- Avoid `rm -rf` on computed or empty paths without guard checks.
- Prefer explicit paths.
- Print useful diagnostics.
- Fail clearly on missing prerequisites.

## Validation

Run repository-specific shell or script checks from `docs/reference/validation.md` when scripts change.
