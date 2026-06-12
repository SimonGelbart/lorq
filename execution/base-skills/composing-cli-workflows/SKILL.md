---
name: composing-cli-workflows
description: Compose minimal-context CLI workflows across repository discovery, search, selective viewing, structured data queries, analysis, refactoring, and .NET verification. Use when a task needs two or more CLI skills, when planning an exploration pipeline, or when choosing an efficient sequence of fd, rg, bat, jaq, yq, sd, ast-grep, ctags, tokei, dotnet, fzf, and xargs commands.
---

# Compose CLI Workflows

## Boundary

Own sequencing multiple tools into an investigation, edit, or verification pipeline with bounded output at each step.
Use `choosing-cli-tools` first when the main question is which single tool to use, or when choosing between defaults, fallbacks, add-on tools, IDE features, MCP tools, and built-in capabilities.

Choose the shortest sequence that answers the task:

1. Choose tools with `choosing-cli-tools` when fallback or custom tooling is possible.
2. Discover paths with `fd` or `rg --files`.
3. Narrow results with `rg`, `jaq`, or `yq`.
4. Inspect only relevant lines or symbols with `bat`, `sed`, `ctags`, or `ast-grep`.
5. Preview transformations with `sd` or `ast-grep`.
6. Apply changes and verify with the narrowest relevant query.

Before running a pipeline, state the intended output shape: filenames, line ranges, symbols, counts, or changed files.
Do not run a pipeline that produces unbounded source output.

Use non-interactive commands during agent execution. Use `fzf` only when generating a command for a human-driven terminal workflow.

Skip steps that add no value. Avoid loading whole files, printing large trees, or adding pipeline stages merely because a tool exists.

Read [the workflow recipes](./reference/workflow-recipes.md) when selecting a multi-tool pipeline or restoring a less common combination.
