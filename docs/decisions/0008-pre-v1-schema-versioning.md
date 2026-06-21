# 0008 — Pre-v1 schema versioning stance

## Status

Accepted.

## Context

The package, adapter, judgement, and report contracts are still evolving while LORQ moves from deterministic fixtures toward a local-first v1. Roadmap text already states that pre-v1 schemas are disposable and that migration commands should wait until real users exist.

## Decision

Before .NET v1, schemas may change freely when needed to clarify the product model. Fixtures may be regenerated, and Python v0 exports may be updated to follow the current canonical contract.

A `package_schema_version` field remains useful, but `package_schema_version: 1` only becomes the first stable supported package format at v1. Do not build a general schema migration system before v1 unless a concrete user need appears.

Schema changes should still be explicit in changelog or ADRs when they alter durable concepts. Reference docs remain the source of truth for current contract fields.

## Consequences

- The team can simplify contracts before v1 without carrying premature compatibility layers.
- Golden fixtures are allowed to move during pre-v1 development.
- Post-v1 compatibility work should start from the v1 contract, not from every pre-v1 experiment.
- Documentation should avoid promising backward compatibility for pre-v1 package artifacts.
