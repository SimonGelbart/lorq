# File-based one-shot adapter protocol

The canonical adapter protocol reference lives at `../../../docs/reference/file-adapter-protocol.md`. Keep request/evidence fields, diagnostic codes, and conformance command behavior documented there to avoid drift.

This directory only contains adapter implementation notes that are specific to the .NET tree:

- `codex-file-adapter-profile.md` — metadata injected when `--adapter-profile codex-cli` is used with an external one-shot adapter.

Use `lorq adapter conformance` for new examples. The legacy `adapter-conformance` alias remains available for existing automation and is listed in `../../../docs/reference/cli.md`.
