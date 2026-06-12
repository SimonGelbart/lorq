---
name: fuzzy-selecting
description: Select interactively from files, search results, processes, Git output, or any line-oriented list with fzf. Use only when a human will interact with the terminal or when composing a reusable interactive command.
---

# Select Interactively with fzf

Reserve `fzf` for human-driven terminal workflows. Do not launch it during unattended agent execution.

```bash
fd -t f | fzf --preview 'bat --color=always --style=numbers {}'
rg -n 'pattern' | fzf
```

Useful flags: `-m`, `--preview CMD`, `--scheme path|history`, `--delimiter STR`, `--nth RANGE`, and `--bind KEY:ACTION`.

Read [the fzf reference](./reference/fzf-guide.md) for key bindings, previews, and shell integration.
