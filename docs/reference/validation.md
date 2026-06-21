# Validation Reference

Before claiming completion, run the strongest reasonable validation available for the change.

Do not claim validation passed unless it was actually run and passed. Report failed, skipped, or incomplete validation honestly.

## .NET validation

From the repository root:

```bash
dotnet restore dotnet/Lorq.slnx -p:Platform="Any CPU"
dotnet build dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU"
dotnet test --solution dotnet/Lorq.slnx --no-restore -p:Platform="Any CPU" --disable-logo
```

When validating package behavior manually, prefer commands documented in:

- `docs/how-to/dotnet-deterministic-loop.md`
- `docs/reference/cli.md`
- `docs/reference/package-validation.md`
- `docs/reference/file-adapter-protocol.md`

## Python baseline validation

The Python v0 baseline is retained for deterministic migration reference. Use the commands in `python/README.md` and `docs/python-v0/` when changing Python baseline behavior or fixtures.

## Runtime smoke validation

Real runtime smoke paths for Codex or Copilot are optional local checks. They must not be required for deterministic CI or for validating unrelated changes.

See `docs/how-to/run-runtime-smoke.md`.

## Validation record

For each validation command, record:

- command;
- working directory;
- result;
- important output summary;
- failure reason, if any.

Use clear statuses:

```text
Passed
Failed
Not run
Not completed
```

Examples:

```text
Not run: required SDK version is not installed.
Failed: 3 tests failed in <test-suite>.
Not completed: command exceeded the available execution window.
Passed: 128 tests passed.
```

## Logs and artifacts

Generated validation logs and test output should not be committed unless they are intentional stable fixtures.

Prefer ignored local output directories such as `results/`, `worktrees/`, or `.lorq/tmp/` for generated artifacts.
