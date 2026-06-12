# Modes

A mode defines the environment the agent receives.

Modes should be declarative and should avoid prompt injection.

Example:

```yaml
id: default-graphify-plus
description: Default Graphify plus query-planner skill.

materialize:
  copy:
    - from: execution/skills/graphify
      to: .agents/skills/graphify
    - from: execution/skills/graphify-query-planner
      to: .agents/skills/graphify-query-planner
    - from: execution/configs/graphify/.graphifyignore
      to: .graphifyignore

pre_agent:
  setup_scope: per-run
  commands:
    - id: graphify-codex-install
      argv: [graphify, codex, install]
      cwd: .
      timeout_seconds: 300
      required: true
    - id: graphify-generate
      argv: [graphify, .]
      cwd: .
      timeout_seconds: 900
      required: true

expectations:
  should_verify_with_source: true
  should_avoid_generic_graph_queries: true
  max_graph_queries_before_source_fallback: 2
```

## Materialization

`materialize.copy` copies files from the suite root into the run worktree.

Destination paths must stay inside the worktree.

Absolute `from` paths are rejected unless explicitly allowed:

```yaml
- from: /opt/shared/tool
  to: tools/tool
  allow_absolute_from: true
```

## Setup commands

Preferred command syntax:

```yaml
commands:
  - id: build-index
    argv: [tool, build]
    cwd: .
    timeout_seconds: 600
    required: true
```

Shell syntax is opt-in:

```yaml
commands:
  - id: pipeline
    run: "tool-a | tool-b > out.txt"
    shell: true
```

Required setup failures stop subsequent setup commands unless `continue_on_failure: true` is set.
