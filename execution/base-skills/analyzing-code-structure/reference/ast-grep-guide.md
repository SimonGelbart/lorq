# ast-grep Quick Reference

Use actual source syntax plus metavariables:

- `$NODE` matches one AST node.
- `$$$NODES` matches zero or more nodes.
- Set `-l LANG` explicitly.

## Search

```bash
ast-grep -l typescript -p 'oldFunction($$$ARGS)' src
ast-grep -l javascript -p '$OBJ.oldMethod($$$ARGS)' src
ast-grep -l python -p 'def $NAME($$$ARGS): $$$BODY' src
ast-grep -l go -p 'func $NAME($$$ARGS) $RET { $$$BODY }' .
ast-grep -l rust -p 'fn $NAME($$$ARGS) -> $RET { $$$BODY }' src
ast-grep -l typescript -p 'console.log($$$ARGS)' --json=compact src
```

## Replace

Preview before applying:

```bash
ast-grep -l typescript -p 'oldFunction($$$ARGS)' -r 'newFunction($$$ARGS)' src
ast-grep -l typescript -p 'oldFunction($$$ARGS)' -r 'newFunction($$$ARGS)' --update-all src
```

## Reusable Rules

Use a rule file when a query needs constraints, relational matching, or repeated use:

```yaml
id: no-console-log
language: TypeScript
rule:
  pattern: console.log($$$ARGS)
```

```bash
ast-grep scan --rule rules/no-console-log.yml src
```

Rule files can combine `pattern`, `kind`, `regex`, `inside`, `has`, `precedes`, and `follows`. Start with a plain pattern and add relational constraints only when matches are too broad.

## Workflow

1. Search with an explicit language and narrow path.
2. Review every match or a representative bounded sample.
3. Preview the replacement without `--update-all`.
4. Apply only when the scope is correct.
5. Search for old and new patterns, then run project checks.

## Boundaries

- Use `rg` for comments, prose, config, and exact strings.
- Start with a specific pattern and broaden only if needed.
- Use `--json=compact` when another command needs structured matches.
- Move complex or repeated queries into versioned rule files.
- Avoid interactive modes during unattended agent runs.
