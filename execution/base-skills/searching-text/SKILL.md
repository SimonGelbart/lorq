---
name: searching-text
description: Search text patterns across files with ripgrep. Use for locating code, finding references, listing matching files, counting matches, or returning line-numbered context without reading whole files.
---

# Search Text with rg

For repository text search, use one targeted `rg -n` call by default, scoped by path or glob when possible.
Use another search method only when `rg` is unavailable, unsuitable, or the user explicitly requests it.
When using another method, state the reason in one line.

## Boundary

Own literal, regex, reference, comment, documentation, and config-file search.
Hand off to `analyzing-code-structure` when the request depends on syntax shape, nested code patterns, call signatures, imports, declarations, or formatting-independent refactors.

```bash
rg -n -C 2 -e 'pattern' [path]
```

Do not start with repo-wide generic terms if a path, extension, type, or symbol is known.
A first search must include at least one narrowing mechanism: path, glob, file type, fixed string, word boundary, or max-count.

Narrow output early:

- Add `-F` for literal text, `-w` for whole words, and `-i` for case-insensitive matching.
- Add `-t TYPE` or `-g GLOB` before searching large trees.
- Use `-l` for filenames only, `-c` for counts, and `--max-count N` when sampling is enough.
- Use `finding-files` for path discovery; `rg --files` is appropriate for repo-local file lists.
- Use structural search instead for formatting-independent code patterns.

Read [the rg reference](./reference/ripgrep-guide.md) only for less common flags or pipeline patterns.
