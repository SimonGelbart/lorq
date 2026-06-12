# Code Structure Quick Reference

Extract an outline before reading a large source file. Stop as soon as one tool answers the question.

Install Universal Ctags when the `ctags` command is unavailable. Some operating systems ship a different, older `ctags` implementation with fewer language parsers.

## Universal Fallback

```bash
ctags -f - --fields=+n FILE
ctags -f - --fields=+n FILE | rg -v '^!'
ctags -R -f .tags src
ctags -f - --fields=+n FILE | rg $'\t(f|m|c|i|t)\t'
```

Useful symbol kinds commonly include `f` functions, `m` methods, `c` classes, `i` interfaces, and `t` types.

## Targeted ast-grep

```bash
ast-grep -l typescript -p 'export function $NAME($$$ARGS)' file.ts
ast-grep -l typescript -p 'export class $NAME { $$$BODY }' file.ts
ast-grep -l typescript -p 'interface $NAME { $$$BODY }' file.ts
ast-grep -l dart -p 'class $NAME { $$$BODY }' file.dart
ast-grep -l dart -p 'Widget build(BuildContext context) { $$$BODY }' file.dart
ast-grep -l csharp -p 'class $NAME { $$$BODY }' file.cs
ast-grep -l python -p 'def $NAME($$$ARGS): $$$BODY' file.py
```

Use `ast-grep` when the desired declaration shape is known. Use `ctags` for a broad compact outline.

## Language Tooling

Prefer project-native symbol tooling when it is already available:

- Dart: use analyzer tooling for semantic diagnostics; use `ctags` or `ast-grep` for outlines.
- TypeScript/JavaScript: use language-server symbols when exposed; otherwise use `ctags` or targeted `ast-grep`.
- C#: prefer IDE or language-server symbols when exposed; use `ctags`, `rg`, or targeted `ast-grep` for compact exploration.
- Other languages: try language-server symbols, then `ctags`.

## Follow-Up

After finding a symbol:

```bash
rg -n -C 2 -F 'symbolName' src
bat --line-range START:END FILE
```

Use `rg` for references and selective reads for implementation details.
