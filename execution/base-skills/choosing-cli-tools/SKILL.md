---
name: choosing-cli-tools
description: Enforce strict terminal tool selection for repository exploration, selective reading, transformations, and verification. Use before choosing fallback, custom, IDE, MCP, or built-in search/read/edit tools, or when deciding between fd, rg, bat, sed, jaq, jq, yq, ast-grep, ctags, sd, tokei, fzf, RTK, and add-on CLI tools.
---

# Choose CLI Tools

Terminal-native tools are authoritative for repository CLI work.

## Boundary

Own selecting the right command or deciding whether a default, fallback, add-on CLI tool, IDE feature, MCP tool, or built-in capability should be used.
Hand off to `composing-cli-workflows` when the task needs a sequence of discovery, search, inspection, edit, and verification steps.

## Default Tools

Use these defaults unless an exception applies:

1. File discovery: `fd`; use `rg --files` for repo-local file lists.
2. Text search: `rg`.
3. Bounded reads: `sed -n` or `bat --line-range`.
4. JSON: `jaq`; use `jq` only for compatibility or availability.
5. YAML: `yq`.
6. Code structure: `ast-grep`, `ctags`, or language-native symbol tools.
7. Text replacement: `sd --preview`; use `apply_patch` for precise manual edits.
8. Metrics: `tokei`.
9. .NET verification: `dotnet build` and `dotnet test` at the narrowest affected scope.
10. Interactive selection: `fzf` only for human-driven terminal flows.

RTK may wrap supported verbose commands when filtered output preserves the signal. Use raw commands for exact logs, audits, unsupported commands, or cases where filtering could hide the issue.

## Exceptions

Use non-default repo tools only when one is true:

1. The default tool is unavailable.
2. The default tool cannot express the operation safely.
3. The non-default tool is strictly better for the current file type or analysis.
4. The user explicitly requested the tool.

When taking an exception, record the skipped default, reason, and output-bounding safeguard.
