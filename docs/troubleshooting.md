# Troubleshooting

## `Configuration validation failed`

Run:

```bash
agent-eval --validate-config
```

The validator reports file and field paths. Unknown YAML keys are rejected.

## Agent command not found

Run:

```bash
agent-eval --check-agent --agent-profile codex
agent-eval --list-agent-profiles
```

Install/authenticate the selected backend or switch profiles with `--agent-profile`.

## Dirty repository warning

Git worktrees evaluate committed `HEAD`, not uncommitted local files.

Use:

```bash
agent-eval --dirty-policy fail
```

for formal benchmarks, or commit/stash local changes.

## Setup failed

Look under the run directory:

```text
setup/setup.log
setup/<command>.stdout.txt
setup/<command>.stderr.txt
setup/setup.summary.json
```

A required setup failure skips the agent run to avoid wasting tokens.

## Cleanup refuses to delete a folder

Cleanup only removes folders with `.agent-eval-generated.json` markers inside the configured worktree/results root.

## Copilot SDK missing

Install optional dependencies:

```bash
pip install -e '.[copilot]'
```

## Evidence validation fails after cleanup

Evidence validation runs before cleanup. If you want to manually inspect cited files after the run, use:

```bash
agent-eval --cleanup never
```
