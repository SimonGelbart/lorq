# tokei Quick Reference

## Scope and Filter

```bash
tokei
tokei src tests
tokei -t Rust,Python src
tokei -e node_modules -e target
tokei --hidden
```

## Output

```bash
tokei --files
tokei -s code
tokei -o json
tokei -o json | jaq -c '.Total'
tokei src tests -o json | jaq -c '{total: .Total, languages: keys - ["Total"]}'
```

Use `--files` only when file-level detail is needed. JSON and YAML output require a `tokei` build with serialization features.

For refactor impact, capture summaries before and after the edit and compare the scoped totals. Keep generated reports outside the source tree unless the repository intentionally tracks them.

## Useful Flags

- `-t TYPES`: include languages.
- `-e PATTERN`: exclude paths.
- `--hidden`: include hidden files.
- `--no-ignore`: bypass ignore files.
- `-s files|lines|blanks|code|comments`: sort output.
- `-o json|yaml|cbor`: emit structured output.
