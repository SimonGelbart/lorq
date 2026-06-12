# fd Quick Reference

## Find

```bash
fd 'pattern' [path]
fd -e js -e ts src
fd -t f -d 3
fd -H -I -E node_modules
fd --size '+1m'
fd --changed-within 1d
fd -0 -e md docs | xargs -0 COMMAND
```

## Execute

Review the file list before batch operations:

```bash
fd -e json
fd -e json -X jaq -r '.version'
fd -e js -x sd --preview 'old' 'new'
fd -e csproj -x sh -c 'printf "%s\n" "$1"' sh {}
```

- `-x` runs once per result.
- `-X` runs once with all results.
- `{}`, `{.}`, `{/}`, and `{//}` are placeholders.
- Prefer `-0` with `xargs -0` when filenames leave `fd` control.

Prefer `rg --files` when `fd` is unavailable and only repository paths are needed.
