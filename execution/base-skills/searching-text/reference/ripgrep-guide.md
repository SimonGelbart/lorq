# rg Quick Reference

## Default Shape

```bash
rg -n -C 2 -e 'pattern' [path]
```

## Narrow Early

```bash
rg -F -n -C 2 -e 'literal.text' src
rg -w -n -C 2 -e 'word' src
rg -i -n -e 'pattern' src
rg -n -t ts -t js -e 'pattern' src
rg -n -e 'TODO' -e 'FIXME' src
rg -n -g '*.test.ts' -e 'pattern'
rg -n -g '!vendor/**' -e 'pattern'
rg -uuu -n -e 'pattern' .              # include hidden and ignored files only when needed
```

## Control Output

```bash
rg -l -e 'pattern'                 # filenames only
rg -c -e 'pattern'                 # count by file
rg -o -e 'pattern'                 # matching text only
rg -L -e 'pattern'                 # files without a match
rg --max-count 5 -n -e 'pattern'   # sample matches
rg --files -g '*.md'               # discover paths
rg --files -0 | xargs -0 COMMAND   # preserve spaces in filenames
```

## Compose Carefully

Limit high-volume output before it enters context:

```bash
rg -l -e 'pattern' | head -n 20
rg -o -e 'import .*' src | sort -u
rg -n -e 'function' src | rg -F 'export'
```

## Decision Rules

- Add `-F` instead of escaping regex metacharacters for literal strings.
- Add a path, type, or glob before increasing context lines.
- Use multiple `-e` flags for OR searches and `-L` for files missing a required marker.
- Prefer null-delimited output with `-0` before passing filenames to `xargs`.
- Add `-u`, `-uu`, or `-uuu` incrementally when ignored or hidden content is intentionally in scope.
- Use `ast-grep` for syntax-aware queries.
- Use `rg --files` as a built-in path-discovery fallback.
