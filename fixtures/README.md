# Fixtures

This directory contains source-controlled fixtures that define or verify LORQ behavior.

Keep only intentional fixtures here. Do not place ad-hoc generated outputs in this directory unless they are promoted to golden/conformance material and documented.

Current fixture areas:

- `python-v0-generated-results/` - preserved Python v0 example results from the imported handoff package.
- `conformance/` - deterministic orchestration conformance definitions and future generated package fixtures shared by Python and .NET.
- `golden/` - reserved for golden expected outputs.
- `smoke/` - reserved for small smoke fixtures.

`conformance/deterministic-orchestration/` currently defines the v1-alpha frozen benchmark shape. Golden generated outputs will be promoted only after deterministic fake agent and fake judge adapters are wired.
