# jaq Quick Reference

`jaq` is largely compatible with `jq`. Use `jq` only as a fallback when `jaq` is unavailable or a filter behaves differently.

## Output Control

```bash
jaq -r '.name' package.json       # raw string
jaq -c '.items[]' data.json       # compact JSON
jaq -e '.required' config.json    # fail for false or null
jaq -S '.' data.json              # sort object keys
printf '%s\n' '{"name":"demo"}' | jaq -r '.name'
```

## Common Filters

```bash
jaq -r '.version' package.json
jaq -r '.dependencies.react' package.json
jaq '{name, version}' package.json
jaq '.[0]' array.json
jaq -r '.items[].name' data.json
jaq -r '.dependencies | keys[]' package.json
jaq '.items[] | select(.active)' data.json
jaq '.items | length' data.json
jaq '.items | map(.name) | sort' data.json
jaq -r '.field // "default"' data.json
jaq -r '.items[]? // empty' data.json
jaq -r 'has("requiredField")' data.json
```

## Variables and Updates

Pass shell values as data instead of interpolating them into filters:

```bash
jaq --arg version "$VERSION" '.version = $version' package.json
jaq --argjson enabled true '.features.enabled = $enabled' config.json
jaq -s 'map(.items[]) | unique_by(.id)' shard-*.json
```

`jaq` prints transformed JSON; it does not edit files in place. Write to a temporary file, validate it, then replace the original with the repository's preferred editing workflow.

## Shell Composition

Prefer compact or raw output before piping:

```bash
jaq -r '.dependencies | keys[]' package.json | sort
jaq -r '.dependencies | keys[]' package.json | while read -r dep; do
  rg -l -F "$dep"
done
```

## Edge Cases

- Use `// empty` to suppress absent optional values.
- Use `has("field")` when absence differs from null.
- Use `-e` in scripts that must fail on missing required data.
- Use `--arg` and `--argjson` for shell-provided values.
- Use `-s` to slurp multiple inputs into one array before combining them.
- Test complex filters if exact `jq` compatibility matters.
- Check the installed `jaq` release before relying on optional non-JSON formats.
