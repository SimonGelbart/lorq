# 0003 — Package lifecycle and evidence model

## Status

Accepted.

## Context

LORQ must support split execution, later merge, later judgement, and later reporting without losing evidence or coupling quality judgement to how answers were produced. Roadmap and explanation documents already described this model, but the durable decision was scattered across planning prose.

## Decision

LORQ has an explicit package lifecycle:

1. `run` creates run-shard execution evidence.
2. `merge` validates compatible run shards and creates a merged experiment package.
3. `judge` attaches one or more named judgement passes to an existing package.
4. `report` derives decision artifacts from package evidence and selected judgement passes.
5. `validate` verifies package integrity and references.

A run shard is canonical execution evidence. A merged experiment package is canonical evaluation evidence. Merged packages preserve original shard contents under `runs/` and build machine-owned indexes under `.lorq/` instead of rewriting raw evidence into a different public shape.

Quality judgement and execution integrity remain separate concerns. Execution and integrity findings can block, warn, or reduce confidence in a report, but quality judgement should evaluate final answers independently from the runtime that produced them.

## Consequences

- Merge is a first-class product step, not a file-copy convenience.
- Judgement can be repeated or varied without rerunning agents.
- Reports must be derivable from persisted package evidence and judgement outputs.
- Package references, coverage, fingerprints, and integrity indexes are part of the trust model.
- Reference documents describe the current package fields and commands; this ADR describes the durable lifecycle decision.
