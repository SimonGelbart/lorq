# sd Quick Reference

## Preview and Apply

```bash
sd --preview 'old' 'new' file.txt
sd 'old' 'new' file.txt
sd -F --preview 'literal.text' 'replacement' file.txt
printf '%s\n' 'before' | sd 'before' 'after'
```

## Captures

```bash
sd '(\w+)' '${1}_suffix' file.txt
sd '(?P<name>\w+)' '${name}_suffix' file.txt
sd '\s+$' '' file.txt
```

## Batch Use

Review matches before applying:

```bash
rg -l -F 'old'
fd -e js -x sd --preview -F 'old' 'new'
rg -l -0 -F 'old' docs | xargs -0 sd --preview -F 'old' 'new'
```

Use `--` before a pattern that begins with `-`. Use null-delimited filenames for external batch pipelines. Use `ast-grep` instead when the replacement depends on code structure.
