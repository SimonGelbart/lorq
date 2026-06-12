# bat Quick Reference

## Selective Reads

```bash
bat --line-range 10:30 --style=numbers file.py
bat -p --color=never file.txt
bat --diff file.rs
sed -n '10,30p' file.py
```

## Pipelines

```bash
git show HEAD:file.py | bat -l py -p
git diff -- path/to/file.py | bat -l diff -p
fd -t f | fzf --preview 'bat --color=always --style=numbers {}'
```

## Useful Flags

- `--line-range START:END`: limit output.
- `-p`: remove decorations.
- `--color never|always|auto`: control ANSI output.
- `--paging never|always|auto`: control paging.
- `-l LANG`: force syntax detection.
- `--diff`: show Git changes.

Prefer plain `sed -n 'START,ENDp' FILE` for compact agent-visible output. Use `bat` when highlighting, Git markers, or human-facing previews add value.
