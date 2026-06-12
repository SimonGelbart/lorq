---
name: analyzing-code
description: Analyze repository composition and code statistics with tokei. Use for language breakdowns, line counts, scoped metrics, or measuring refactor impact without reading source files.
---

# Analyze Code with tokei

Start with a scoped summary:

```bash
tokei [path]
tokei src tests -t Rust,Python
tokei -o json | jaq -c '.Total'
```

Useful flags: `-t TYPES`, `-e PATTERN`, `--files`, `--hidden`, `-s COLUMN`, and `-o json|yaml`.

Prefer summary output first.
Request file-level statistics only when the summary shows where a deeper look is needed.
When reporting metrics, include the command scope and avoid implying full-repo coverage unless the command actually covered the full repo.

Read [the tokei reference](./references/tokei-guide.md) for exclusions, serialization, and CI examples.
