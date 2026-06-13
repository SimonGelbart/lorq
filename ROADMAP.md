# LORQ roadmap

The roadmap source of truth is currently split into two documents:

- `docs/roadmap/LORQ_ROADMAP_GOAL_REFINEMENT.md`
- `docs/roadmap/LORQ_PRACTICAL_ROADMAP.md`

## Current practical direction

1. Keep Python v0 only long enough to freeze a simple deterministic orchestration benchmark.
2. Use the frozen Python benchmark as the comparable migration gate.
3. Implement LORQ v1 in .NET.
4. Keep shared product inputs at the repo root so both Python and .NET can consume the same case, mode, pricing, execution, schema, and fixture definitions.
5. Preserve the core product loop: `run -> merge -> judge -> report`.

Any changed scope or newly proposed increment must amend the practical roadmap rather than live only in a chat note.
