# Simple local no-token smoke example

This example shows how to run the evaluator with a fake local agent, without Codex or Copilot tokens.

From the package root:

```bash
agent-eval \
  --suite-root examples/simple-local-repo \
  --repo ./examples/simple-local-repo/fake_project \
  --modes local-fake \
  --cases explain-demo \
  --agent-profile fake \
  --out ./examples/simple-local-repo/results/smoke
```

The fake agent simply reads the prompt and emits a source-backed answer that satisfies the demo validation.
