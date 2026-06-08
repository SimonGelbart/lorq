# Graphify example

The recommended Graphify benchmark is fairness-first:

```text
fresh worktree per run
→ copy Graphify-related skills
→ run graphify build in that worktree
→ launch the agent with a neutral prompt
```

Do not copy `graphify-out/` by default. Generate it with a setup command.

## Mode: default Graphify

```yaml
id: default-graphify
materialize:
  copy:
    - from: execution/skills/graphify
      to: .agents/skills/graphify
pre_agent:
  setup_scope: per-run
  commands:
    - id: graphify-build
      argv: [graphify, build]
      cwd: .
      timeout_seconds: 600
      required: true
```

## Mode: default Graphify plus query planner

```yaml
id: default-graphify-plus
materialize:
  copy:
    - from: execution/skills/graphify
      to: .agents/skills/graphify
    - from: execution/skills/graphify-query-planner
      to: .agents/skills/graphify-query-planner
pre_agent:
  setup_scope: per-run
  commands:
    - id: graphify-build
      argv: [graphify, build]
      cwd: .
      timeout_seconds: 600
      required: true
```

## Suggested first run

```bash
agent-eval \
  --repo /Users/simon/projects/nopCommerce \
  --modes default-graphify,default-graphify-plus \
  --cases admin-permissions,inactive-customers-csv \
  --prompt-style neutral \
  --dirty-policy fail \
  --out ./results/graphify-plus-smoke
```

Interpret `default-graphify-plus` as a naturalistic availability test: the prompt does not tell the agent to use Graphify; the mode makes the skills and graph artifacts available.
