# Generic Agent Eval Runner v1.0

A Python evaluator for comparing AI agent modes, skills, tool preparation flows, prompt styles, and agent backends in isolated repository worktrees.

The runner is designed for fair skill/tool evaluation: change the **environment** through a mode, keep the task prompt neutral, run deterministic setup before the AI starts, then validate both the final answer and the behavior trace.

```text
fresh worktree per run
→ materialize selected mode files/skills
→ run pre-agent setup commands in that worktree
→ launch the agent with a neutral task prompt
→ normalize backend traces into common events
→ validate answer quality, evidence, and behavior
→ write reports and diagnostics
```

Generated tool artifacts such as `graphify-out/` are **not copied by default**. Generate them in each disposable worktree using mode-level `pre_agent.commands`.

## Features

- Declarative YAML modes and eval cases
- Clean worktree per run by default
- Python-run pre-agent setup commands
- Codex CLI backend via `codex exec --json`
- GitHub Copilot SDK backend via `github-copilot-sdk`
- Generic CLI backend for local/fake agents
- Neutral, weak-hint, and forced prompt styles
- Deterministic validators for symbols, files, forbidden claims, and evidence
- Backend-normalized event traces for behavior validation
- Optional LLM judge, disabled by default
- Repetitions and aggregate reports
- Safe cleanup using generated-folder markers
- Strict YAML schema validation
- Dirty-repo policy controls

## Install

```bash
cd generic-agent-eval-runner-v1.0
python -m venv .venv
source .venv/bin/activate
pip install -e .
```

For GitHub Copilot SDK support:

```bash
pip install -e '.[copilot]'
```

For development tests:

```bash
pip install -e '.[dev]'
pytest
```

## Quickstart

Validate the bundled suite configuration:

```bash
agent-eval --validate-config
```

List modes and cases:

```bash
agent-eval --list-modes
agent-eval --list-cases
```

Check your selected agent backend:

```bash
agent-eval --check-agent --agent-profile codex
agent-eval --check-agent --agent-profile copilot-sdk
```

Run a neutral evaluation:

```bash
agent-eval \
  --repo /path/to/your/repo \
  --modes no-skill \
  --cases admin-permissions \
  --prompt-style neutral \
  --dirty-policy fail \
  --require-agent-available \
  --out ./results/smoke
```

Regenerate reports from existing run artifacts:

```bash
agent-eval --report-only --out ./results/smoke
```

Explain a single run:

```bash
agent-eval --explain-run ./results/smoke/runs/no-skill/neutral/admin-permissions/r1
```

## Graphify smoke example

A Graphify mode should copy skills but generate Graphify artifacts fresh before the agent starts:

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

Run only two targeted cases first:

```bash
agent-eval \
  --repo /Users/simon/projects/nopCommerce \
  --modes default-graphify,default-graphify-plus \
  --cases admin-permissions,inactive-customers-csv \
  --prompt-style neutral \
  --dirty-policy fail \
  --out ./results/graphify-plus-smoke
```

## Important outputs

Each run writes artifacts under:

```text
results/runs/<mode>/<prompt-style>/<case>/r<repetition>/
```

Important run files:

```text
prompt.txt
answer.md
stdout.raw.jsonl / stdout.raw.txt
stderr.txt
agent.summary.json
validation.json
events.normalized.jsonl
events.summary.json
summary.json
run.manifest.json
environment.json
environment.txt
snapshots/
```

Top-level reports:

```text
summary.md
case_comparison.md
mode_summary.md
fairness_warnings.md
failed_runs.md
scorecard.csv
aggregate_scorecard.csv
summary.json
aggregates.json
```

## Documentation

- [Quickstart](docs/quickstart.md)
- [Configuration](docs/configuration.md)
- [Modes](docs/modes.md)
- [Cases and validation](docs/cases.md)
- [Agent backends](docs/backends.md)
- [Graphify example](docs/graphify.md)
- [Reports](docs/reports.md)
- [Troubleshooting](docs/troubleshooting.md)

## Bias-control rules

1. Neutral prompts should not mention skill names or specific tools.
2. The baseline should remove unavailable tools/skills, not ask the agent to ignore them.
3. Setup cost and agent cost are reported separately.
4. Forced prompts are ceiling tests and should not be mixed with neutral benchmark conclusions.
5. Formal local Git runs should use `--dirty-policy fail` so uncommitted source changes do not silently disappear from Git worktrees.

## Safety notes

- Run agents only in disposable worktrees.
- Use `permission_policy: approve_all` for Copilot SDK only in isolated eval worktrees.
- Cleanup commands only remove generated folders containing `.agent-eval-generated.json` markers.
- Logs are redacted for common token/key/secret patterns, but avoid putting secrets in prompts or setup commands.
