# Configuration

The default config file is `eval.config.yaml`.

Key sections:

```yaml
default_prompt_style: neutral

repositories:
  example-local:
    type: local
    path: /absolute/path/to/repo
    ref: HEAD

worktrees:
  root: /tmp/agent-eval-worktrees
  strategy: git-worktree
  cleanup: never
  dirty_policy: warn

output:
  root: ./results

agent:
  profile: codex

agent_profiles:
  codex:
    backend: codex
    command: codex
    args: [exec, --json]
    input_mode: stdin
    output_format: codex-jsonl
```

## Repositories

Local repo:

```yaml
repositories:
  my-repo:
    type: local
    path: /path/to/repo
    ref: HEAD
```

Git repo:

```yaml
repositories:
  my-repo:
    type: git
    url: https://github.com/org/repo.git
    ref: main
```

CLI `--repo` overrides case-level and config repositories.

## Dirty repo policy

Git worktrees evaluate committed `HEAD`; uncommitted changes are not included. Use:

```bash
agent-eval --dirty-policy fail
```

for formal runs.

Options:

- `warn`: warn and continue
- `fail`: stop before the run
- `allow`: proceed without warning

## Worktree strategies

- `git-worktree`: preferred for local Git repos
- `copy`: works for non-Git folders
- `clone`: useful for remote Git URLs or CI
