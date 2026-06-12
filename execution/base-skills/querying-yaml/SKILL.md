---
name: querying-yaml
description: Query, filter, transform, or edit YAML with mikefarah/yq. Use for selective reads from Compose, GitHub Actions, Kubernetes, and other YAML files, especially when preserving YAML formatting, comments, anchors, or multi-document behavior matters.
---

# Query YAML with yq

Use `yq` for YAML-aware operations:

```bash
yq -r '.services | keys[]' docker-compose.yml
yq -o json '.jobs.build.steps' .github/workflows/ci.yml
yq -i '.spec.replicas = 3' deployment.yml
```

Choose the smallest output:

- Use `-r` for scalar strings and `-o json -I 0` for compact structured output.
- Query only the fields needed instead of printing the whole file.
- Use `-i` only after reviewing the expression; it edits in place.
- Keep `yq` as the YAML default, especially for YAML-preserving edits.

Do not parse large YAML manually when a query can extract only the needed fields.
Read [the yq reference](./reference/yq-guide.md) for multi-document YAML, conversions, and update patterns.
