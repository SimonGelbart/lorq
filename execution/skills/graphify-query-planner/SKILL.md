---
name: graphify-query-planner
description: Lightweight companion override for the regular Graphify skill. Use when graphify-out/graph.json exists and the user asks about code discovery, call flow, impact, dependencies, permissions, auth, routing, services, controllers, plugins, or architecture. Forces exact source anchors before Graphify queries and blocks broad generic graph searches.
---

# Graphify Query Planner

This skill controls **how to use `graphify query`** when the regular Graphify skill is also relevant. It does not replace Graphify build/update/extraction flows.

## Priority rule

If both this skill and the regular Graphify skill apply, follow this skill for query planning.

Do not let Graphify discover the vocabulary. Use source search to find exact names; use Graphify to connect those names.

## Fast flow

1. **Gate Graphify.**
   - Use early for call flow, impact/blast radius, dependencies, permissions, auth/routing, plugins, and exact symbol exploration.
   - Delay for broad feature discovery until exact symbols are found.
   - Usually skip for high-level architecture or negative-control/source-only questions unless a specific relationship needs proof.

2. **Anchor first.**
   Before the first `graphify query`, run 1-3 cheap probes with `rg`, `fd`, or equivalent.
   Prefer declarations, filenames, controllers, actions, interfaces, methods, attributes, providers, routes, models, factories, plugins, and config names.

3. **Query exact names only.**
   Build the first query from source anchors.
   - Good: `OrderTotalCalculationService GetShoppingCartTotalAsync IOrderTotalCalculationService PlaceOrderAsync`
   - Bad: `order total cart checkout service`

4. **Block generic-only queries.**
   Do not run `graphify query` with only terms like `Admin`, `Order`, `Cart`, `Route`, `Plugin`, `Service`, `Permission`, `Controller`, `Customer`, `Authentication`, or `Architecture`.
   If only generic nouns are known, do another source-anchor probe first.

5. **Limit graph attempts.**
   Run at most 2 graph queries before switching to source inspection, unless the graph clearly returns exact symbols or new relationship evidence.

6. **Relevance check after each graph query.**
   Continue using Graphify only if it returns exact symbols, likely owners, callers, callees, implementations, related files, or relationship candidates not already found.
   Otherwise stop graph queries and use source search/read.

7. **Verify in source.**
   Final claims must cite file paths and symbol names from source files. Treat Graphify as navigation, not proof.

## Query shapes

- Call flow: `<entry action> <service method> <domain method> <terminal side effect>`
- Impact: `<interface> <implementation> <method> callers consumers`
- Permissions/auth: `<attribute> <permission provider> <permission service> <target action>`
- Plugin/routing: `<plugin/provider> <route provider> <controller> <view/component>`
- Data/export: `<admin controller> <search model> <factory> <export manager> <writer>`

## Final answer discipline

Separate:
- source-confirmed facts,
- Graphify-located candidates,
- uncertain or unverified relationships.
