# Graphify example suite

Copy these ideas into the root suite if you want a Graphify-specific benchmark.

Recommended first run:

```bash
agent-eval \
  --repo /Users/simon/projects/nopCommerce \
  --modes default-graphify,default-graphify-plus \
  --cases admin-permissions,inactive-customers-csv \
  --prompt-style neutral \
  --dirty-policy fail \
  --out ./results/graphify-plus-smoke
```

The default design is fresh setup per run. Do not copy `graphify-out/`; write `.graphifyignore`, run `graphify codex install`, then generate it with `graphify .` in `pre_agent.commands`.
