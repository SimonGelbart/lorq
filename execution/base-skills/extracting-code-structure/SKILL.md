---
name: extracting-code-structure
description: Extract a compact outline of functions, methods, classes, exports, or symbols before reading source files. Use while exploring unfamiliar code, inspecting a large file, or deciding which lines to read selectively.
---

# Extract Code Structure

Get an outline before reading a large or unfamiliar source file. Use the first available approach that answers the question:

1. Use a language-specific symbol or analyzer command when available.
2. Use `ctags -f - FILE` for a compact cross-language outline.
3. Use targeted `ast-grep` patterns for specific declarations.
4. Read selected line ranges only after narrowing the target.

```bash
ctags -f - --fields=+n FILE
ast-grep -l typescript -p 'export function $NAME($$$ARGS)' FILE
```

Use structural extraction before reading implementation bodies in unfamiliar or large source files.
Use `rg` instead when locating references or a known symbol.
Read [the structure reference](./reference/code-structure-guide.md) for Dart, TypeScript, and fallback examples.
