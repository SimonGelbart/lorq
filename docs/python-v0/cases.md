# Cases and validation

A case defines the hidden task and validation expectations.

Example:

```yaml
id: admin-permissions
title: Admin permissions exploration
category: authorization
difficulty: medium
repo: nopcommerce

task: |
  Explain how admin permission checks work for controller actions.
  Identify the main attributes, services, permission records, and registration points.
  Include source-backed evidence.

validation:
  required_symbols:
    - PermissionService
    - AuthorizeAdminAttribute
    - symbol: CheckPermissionAttribute
      must_be_near_file_reference: true

  required_files:
    - src/Presentation/Nop.Web.Framework/Mvc/Filters/AuthorizeAdminAttribute.cs

  required_evidence:
    min_existing_file_references: 1
    min_source_files: 1
    max_missing_cited_files: 0
    require_symbol_near_file_reference: true

  forbidden_claims:
    - "All admin permissions are hardcoded directly in controllers."

rubric: authorization
```

## Good case design

The task should describe the work, not the method.

Good:

```text
Explain how admin permission checks work.
```

Avoid:

```text
Use Graphify to explain how admin permission checks work.
```

## Validation types

- `required_symbols`: strings or objects with `symbol`
- `required_files`: expected repository file paths
- `expected_concepts`: concepts the answer should cover
- `forbidden_claims`: claims that should not appear
- `required_evidence`: source-grounding thresholds

Validation is deterministic. The optional LLM judge is separate and disabled by default.
