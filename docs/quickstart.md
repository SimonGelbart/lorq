# Quickstart

## 1. Install

```bash
python -m venv .venv
source .venv/bin/activate
pip install -e .
```

Optional Copilot SDK support:

```bash
pip install -e '.[copilot]'
```

## 2. Validate the suite

```bash
agent-eval --validate-config
```

## 3. Check the agent backend

```bash
agent-eval --check-agent --agent-profile codex
```

For Copilot SDK:

```bash
agent-eval --check-agent --agent-profile copilot-sdk
```

## 4. Run a small eval

```bash
agent-eval \
  --repo /path/to/repo \
  --modes no-skill \
  --cases admin-permissions \
  --prompt-style neutral \
  --dirty-policy fail \
  --require-agent-available \
  --out ./results/smoke
```

## 5. Read reports

```text
results/smoke/summary.md
results/smoke/case_comparison.md
results/smoke/mode_summary.md
results/smoke/fairness_warnings.md
results/smoke/failed_runs.md
```

## 6. Explain a run

```bash
agent-eval --explain-run ./results/smoke/runs/no-skill/neutral/admin-permissions/r1
```

## No-token conformance check

Before running expensive agents, verify the portability contract using the bundled fake-agent fixture:

```bash
agent-eval --run-conformance
```

The command should return a JSON object with `ok: true`. It creates a temporary result folder and verifies that the run produced schema-versioned summary, validation, manifest, and normalized-events artifacts.
