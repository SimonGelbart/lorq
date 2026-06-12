---
name: replacing-text
description: Replace text with sd. Use for previewable literal or regex substitutions in files, batch text transformations, or clearer replacement syntax than sed. Use structural tools for code-aware refactors.
---

# Replace Text with sd

Search first when the replacement scope is uncertain. Preview before changing multiple files:

```bash
sd --preview 'OLD' 'NEW' FILE...
sd -F --preview 'literal text' 'replacement' FILE...
```

Use `-F` for literals and `${name}` where a capture boundary is ambiguous.
For multi-file replacements, review the candidate file list or count before applying.
After applying, verify with `rg`.
Use `ast-grep` for formatting-independent code changes.

Read [the sd reference](./reference/sd-guide.md) for captures, escaping, and batch examples.
