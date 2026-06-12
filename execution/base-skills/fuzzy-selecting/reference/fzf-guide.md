# fzf Quick Reference

Use `fzf` only when a human can interact with the terminal.

## Selection

```bash
fd -t f | fzf --scheme path
rg -n 'pattern' | fzf
git branch --format='%(refname:short)' | fzf
git diff --name-only | fzf -m
```

## Preview and Multi-Select

```bash
fd -t f | fzf --preview 'bat --color=always --style=numbers {}'
fd -t f | fzf -m
rg -n 'pattern' | fzf --delimiter : --nth 1,3.. --preview 'bat --color=always --highlight-line {2} {1}'
```

## Useful Flags

- `-m`: select multiple entries.
- `--preview CMD`: render a preview.
- `--scheme path|history`: tune matching.
- `--delimiter STR`, `--nth RANGE`: search selected fields.
- `--bind KEY:ACTION`: add key bindings.
- `--height 40%`, `--layout reverse`: adjust display.

Generate `fzf` commands for a human to run; do not launch them during unattended agent execution. Avoid destructive actions directly inside `fzf`; inspect selected values first and quote filenames when passing them onward.
