---
name: finding-files
description: Find files and directories with fd, or list repository files with rg --files. Use for name, extension, type, depth, hidden-file, ignored-file, size, modification-time filters, or building file lists for other commands.
---

# Find Files

Use `fd` for filesystem discovery by name, type, depth, size, time, hidden files, ignored files, or paths outside the current repo.
Use `rg --files` when the task only needs a repo-local tracked-and-unignored file list.
Use custom or built-in file search only when neither terminal path is safe or expressive enough, and state the reason in one line.

```bash
fd [pattern] [path]
fd -e ts -e tsx src
fd -t f -H -E node_modules
rg --files -g '*.cs'
```

Useful flags: `-e EXT`, `-t f|d`, `-H`, `-I`, `-d DEPTH`, `-E GLOB`, `-x COMMAND`, and `-X COMMAND`.

Keep destructive batch operations explicit and review the file list first.
Before destructive or multi-file operations, print the candidate file list or count and confirm the scope.
Read [the fd reference](./reference/fd-guide.md) for size, time, and execution patterns.
