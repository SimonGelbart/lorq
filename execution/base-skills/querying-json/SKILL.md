---
name: querying-json
description: Query, filter, or transform JSON with jaq. Use to extract fields from JSON files or command output, inspect nested data selectively, or reduce large JSON before loading it into context.
---

# Query JSON with jaq

Use `jaq` as the default JSON processor. It is largely `jq` compatible, so familiar filters usually work unchanged.
Use `jq` only when `jaq` is unavailable or when compatibility behavior is required, and state the fallback reason.

```bash
jaq -r '.version' package.json
jaq -r '.dependencies | keys[]' package.json
jaq -c '.items[] | select(.active)' data.json
```

Choose output flags deliberately:

- Use `-r` for strings consumed by shell commands.
- Use `-c` for compact JSON and lower output volume.
- Add `-e` when null or false should fail a script.
- Query only the fields needed; avoid printing a whole large document.

Do not parse large JSON manually when a query can extract only the needed fields.
Read [the jaq reference](./reference/jaq-guide.md) for common filters and edge cases.
