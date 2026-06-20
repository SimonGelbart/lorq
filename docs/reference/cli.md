# CLI reference

The .NET CLI currently exposes deterministic package and adapter-boundary commands.

## `run`

Produces a deterministic no-judge run shard.

```bash
lorq run --no-judge --suite-root <suite-root> --out <shard-root>
```

Common options:

- `--no-judge` — required for the current deterministic runtime slice.
- `--suite-root <path>` — deterministic benchmark suite root.
- `--out <path>` — run-shard package output path.
- `--work-root <path>` — optional workspace root for materialized per-cell workspaces.
- `--adapter-command <path>` — optional external one-shot file adapter.
- `--adapter-arg <value>` — repeatable external adapter argument.
- `--adapter-profile codex-cli` — passes Codex wrapper metadata to an external adapter process; LORQ itself still does not call Codex.

## `merge-shards`

Merges run shards into an experiment package.

```bash
lorq merge-shards <shard-root>... --out <experiment-root> --package-id <id> --benchmark <benchmark.yaml>
```

Rejects duplicate cell IDs and repository fingerprint mismatches with stable validation codes.

## `judge-package`

Attaches a deterministic fixture-backed judgement pass.

```bash
lorq judge-package <experiment-root> --name <judgement-name> --fixture <fake-judge.yaml>
```

The command records `real_llm_used: false` for the deterministic judgement fixture.

## `report-package`

Renders canonical report artifacts from a judged experiment package.

```bash
lorq report-package <experiment-root> --primary-judgement <judgement-name>
```

`report.json` is the canonical report. Markdown files are renderings of canonical data.

## `validate-package`

Validates an experiment or run-shard package.

```bash
lorq validate-package <package-root>
```

Outputs a machine-readable JSON summary.

## `validate-merge-inputs`

Checks merge-input compatibility without writing an experiment package.

```bash
lorq validate-merge-inputs <shard-root>...
```

## `rebuild-indexes`

Rebuilds `.lorq/` indexes from package evidence into a target root.

```bash
lorq rebuild-indexes <source-root> <target-root>
```
