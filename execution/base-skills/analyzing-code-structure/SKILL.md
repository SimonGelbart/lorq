---
name: analyzing-code-structure
description: Search or refactor code structurally with ast-grep. Use when text matching is ambiguous, formatting varies, or changes target calls, declarations, signatures, imports, methods, classes, or nested AST patterns.
---

# Search Code Structure with ast-grep

## Boundary

Own AST-shaped search, structural matching, syntax-aware refactors, and code changes where formatting or nesting makes literal text unreliable.
Hand off to `searching-text` for literal strings, references in prose, comments, documentation, config files, and unique verified text matches.

Use AST matching for code shape, not plain text:

```bash
ast-grep -l typescript -p 'oldFunction($$$ARGS)' src
ast-grep -l typescript -p 'oldFunction($$$ARGS)' -r 'newFunction($$$ARGS)' src
```

Always set the language, inspect matches, preview replacements, and validate after applying. Use `--update-all` only after reviewing the replacement output.

Use structural search before raw replacement when changing calls, imports, declarations, signatures, class members, or nested code patterns.
Raw string replacement is allowed only when the match is unique, literal, and verified with `rg`.

Use `rg` for comments, documentation, config files, or a simple unique string. Read [the ast-grep reference](./reference/ast-grep-guide.md) for metavariables and language examples.
