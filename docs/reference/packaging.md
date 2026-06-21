# Packaging Reference

This file describes durable source repository packaging boundaries.

It does not describe maintainer-specific delivery mechanics, local execution environments, or temporary task files.

## Source repository contents

The repository should contain:

- source code;
- tests;
- schemas;
- deterministic fixtures and golden outputs;
- examples intended for maintainers and users;
- durable documentation;
- CI and project configuration.

The repository should not contain:

- transient validation logs;
- local command transcripts;
- generated run outputs;
- downloaded SDKs or caches;
- temporary package manifests;
- scratch scripts;
- machine-local paths;
- secrets or credentials.

## Generated outputs

Use ignored local output directories such as:

```text
results/
worktrees/
.lorq/tmp/
```

A generated output may be promoted into `fixtures/` only when it is deterministic, reviewed, documented, and intended to become a stable project asset.

## Package contracts

LORQ product package contracts are documented in:

- `docs/reference/package-validation.md`
- `docs/reference/file-adapter-protocol.md`
- schemas under `schemas/`
- accepted ADRs under `docs/adr/`
