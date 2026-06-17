# Deterministic negative merge fixtures

These tiny LORQ v1-alpha packages are intentional source-controlled edge fixtures for the Python v0 migration benchmark. They are not generated session output.

- `duplicate-cell-conflict/` contains two shards with the same cell id and must fail by default during merge.
- `fingerprint-mismatch/` contains compatible cell ids from different repository fingerprints and must fail by default during merge.
