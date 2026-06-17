from __future__ import annotations

from typing import Any

CONTRACT_VERSION = "agent-eval.contract.v1"
RESULT_SCHEMA_VERSION = "agent-eval.result.v1"
RUN_MANIFEST_SCHEMA_VERSION = "agent-eval.run-manifest.v1"
VALIDATION_RESULT_SCHEMA_VERSION = "agent-eval.validation-result.v1"
NORMALIZED_EVENT_SCHEMA_VERSION = "agent-eval.normalized-event.v1"
EVENT_SUMMARY_SCHEMA_VERSION = "agent-eval.event-summary.v1"
AGGREGATE_SUMMARY_SCHEMA_VERSION = "agent-eval.aggregate-summary.v1"
SUMMARY_SCHEMA_VERSION = "agent-eval.summary.v1"


def with_schema(data: dict[str, Any], schema_version: str) -> dict[str, Any]:
    out = dict(data)
    out.setdefault("schema_version", schema_version)
    out.setdefault("contract_version", CONTRACT_VERSION)
    return out
# LORQ v1-alpha migration fixture constants. These describe the future package
# contract used by the Python frozen baseline; they do not make Python v0 the
# long-term product core.
LORQ_CONTRACT_VERSION = "lorq.contract.v1alpha1"
LORQ_PACKAGE_SCHEMA_VERSION = 1
LORQ_CELL_EVIDENCE_SCHEMA_VERSION = "lorq.cell-evidence.v1alpha1"

