from __future__ import annotations

import json
import shutil
from pathlib import Path
from typing import Any

from . import __version__
from .contracts import LORQ_CELL_EVIDENCE_SCHEMA_VERSION, LORQ_CONTRACT_VERSION, LORQ_PACKAGE_SCHEMA_VERSION
from .lifecycle import load_run_records
from .utils import ensure_dir, read_json, read_text, slug, write_json, write_text


COPY_EVIDENCE_FILES = (
    "prompt.txt",
    "answer.md",
    "validation.json",
    "events.normalized.jsonl",
    "events.summary.json",
    "stdout.raw.jsonl",
    "stdout.raw.txt",
    "stderr.txt",
    "active-skills.json",
    "adapter.evidence.json",
)


STATUS_MAP = {
    "completed": "completed",
    "setup_failed": "setup_failed",
    "agent_failed": "adapter_failed",
    "validation_failed": "invalid_artifact",
    "judge_failed": "judge_failed",
}


def attempt_id_for_record(record: dict[str, Any]) -> str:
    repetition = record.get("repetition", 1)
    try:
        number = int(repetition)
    except (TypeError, ValueError):
        return f"attempt-{slug(str(repetition))}"
    return f"attempt-{number:03d}"


def cell_id_for_record(record: dict[str, Any]) -> str:
    case_id = str(record.get("case") or "case")
    mode_id = str(record.get("mode") or "mode")
    return f"{slug(case_id)}__{slug(mode_id)}__{attempt_id_for_record(record)}"


def _safe_copy(src: Path, dst: Path) -> bool:
    if not src.exists() or not src.is_file():
        return False
    ensure_dir(dst.parent)
    shutil.copy2(src, dst)
    return True


def _repo_fingerprint(record: dict[str, Any]) -> dict[str, Any]:
    repo_status = record.get("repo_status") or {}
    return {
        "repo": record.get("repo"),
        "repo_type": repo_status.get("type"),
        "ref": repo_status.get("ref"),
        "commit": repo_status.get("commit"),
        "dirty": bool(repo_status.get("dirty", False)),
        "is_git_repo": repo_status.get("is_git_repo"),
    }


def _adapter_output(record: dict[str, Any], cell_public_dir: Path) -> dict[str, Any]:
    agent = record.get("agent_summary") or {}
    validation = record.get("validation") or {}
    run_dir = Path(str(record.get("run_dir") or ""))
    events_summary = read_json(run_dir / "events.summary.json", default={}) or {}
    answer = read_text(run_dir / "answer.md")
    prompt = read_text(run_dir / "prompt.txt")
    artifacts: list[dict[str, Any]] = []
    for name in COPY_EVIDENCE_FILES:
        if (cell_public_dir / name).exists():
            artifacts.append({"path": f"runs/{{shard_id}}/cells/{{cell_id}}/{name}", "kind": name})

    failure_category = agent.get("error_category")
    timed_out = bool(agent.get("timed_out", False))
    final_answer_present = bool(answer.strip())
    status = STATUS_MAP.get(str(record.get("run_status") or "unknown"), str(record.get("run_status") or "unknown"))
    if timed_out:
        status = "timeout"
    elif not final_answer_present and status == "completed":
        status = "no_final_answer"

    return {
        "status": status,
        "final_answer_present": final_answer_present,
        "final_answer_chars": len(answer),
        "prompt_chars": len(prompt),
        "adapter": {
            "id": agent.get("agent") or agent.get("backend"),
            "backend": agent.get("backend"),
            "output_format": agent.get("output_format"),
            "input_mode": agent.get("input_mode"),
            "exit_code": agent.get("exit_code"),
            "timed_out": timed_out,
            "ok": agent.get("ok"),
            "error_category": failure_category,
        },
        "usage": agent.get("usage") or {},
        "counts": agent.get("counts") or {},
        "timing": {
            "elapsed_ms": agent.get("elapsed_ms"),
            "setup_elapsed_ms": (((record.get("setup") or {}).get("setup") or {}).get("elapsed_ms")),
        },
        "trace": {
            "summary": events_summary,
            "normalized_events_path": "events.normalized.jsonl" if (cell_public_dir / "events.normalized.jsonl").exists() else None,
        },
        "validation": {
            "ok": validation.get("ok"),
            "hard_passed": validation.get("hard_passed"),
            "hard_total": validation.get("hard_total"),
            "soft_passed": validation.get("soft_passed"),
            "soft_total": validation.get("soft_total"),
        },
        "artifacts": artifacts,
    }


def build_cell_evidence(record: dict[str, Any], *, shard_id: str, package_root: Path) -> dict[str, Any]:
    cell_id = cell_id_for_record(record)
    cell_public_dir = package_root / "runs" / shard_id / "cells" / cell_id
    run_dir = Path(str(record.get("run_dir") or ""))
    copied: list[str] = []
    for name in COPY_EVIDENCE_FILES:
        if _safe_copy(run_dir / name, cell_public_dir / name):
            copied.append(name)

    adapter_output = _adapter_output(record, cell_public_dir)
    adapter_output["artifacts"] = [
        {"path": f"runs/{shard_id}/cells/{cell_id}/{item}", "kind": item}
        for item in copied
    ]

    evidence = {
        "schema_version": LORQ_CELL_EVIDENCE_SCHEMA_VERSION,
        "contract_version": LORQ_CONTRACT_VERSION,
        "cell_id": cell_id,
        "case_id": record.get("case"),
        "mode_id": record.get("mode"),
        "attempt_id": attempt_id_for_record(record),
        "shard_id": shard_id,
        "prompt_style": record.get("prompt_style"),
        "category": record.get("category"),
        "status": adapter_output["status"],
        "source": {
            "implementation": "python-v0",
            "python_package": "generic-agent-eval-runner",
            "python_version": __version__,
            "source_schema_version": record.get("schema_version"),
        },
        "fingerprint": _repo_fingerprint(record),
        "adapter_output": adapter_output,
        "evidence_refs": {
            "cell_dir": f"runs/{shard_id}/cells/{cell_id}",
            "final_answer": f"runs/{shard_id}/cells/{cell_id}/answer.md" if "answer.md" in copied else None,
            "cell_result": f"runs/{shard_id}/cells/{cell_id}/cell_result.json",
            "prompt": f"runs/{shard_id}/cells/{cell_id}/prompt.txt" if "prompt.txt" in copied else None,
            "validation": f"runs/{shard_id}/cells/{cell_id}/validation.json" if "validation.json" in copied else None,
            "trace": f"runs/{shard_id}/cells/{cell_id}/events.normalized.jsonl" if "events.normalized.jsonl" in copied else None,
        },
    }
    write_json(cell_public_dir / "cell_result.json", evidence)
    return evidence


def _coverage(cells: list[dict[str, Any]]) -> dict[str, Any]:
    status_counts: dict[str, int] = {}
    cases = sorted({str(cell.get("case_id")) for cell in cells})
    modes = sorted({str(cell.get("mode_id")) for cell in cells})
    attempts = sorted({str(cell.get("attempt_id")) for cell in cells})
    present_cell_ids = sorted(str(cell.get("cell_id")) for cell in cells)
    for cell in cells:
        status = str(cell.get("status") or "unknown")
        status_counts[status] = status_counts.get(status, 0) + 1
    return {
        "schema_version": "lorq.coverage.v1alpha1",
        "contract_version": LORQ_CONTRACT_VERSION,
        "cell_count": len(cells),
        "cases": cases,
        "modes": modes,
        "attempts": attempts,
        "present_cell_ids": present_cell_ids,
        "status_counts": status_counts,
        "missing_cells": [],
        "skipped_cells": [cell["cell_id"] for cell in cells if cell.get("status") == "skipped"],
    }


def _fingerprints(cells: list[dict[str, Any]]) -> dict[str, Any]:
    by_cell = {str(cell.get("cell_id")): cell.get("fingerprint") or {} for cell in cells}
    unique: dict[str, list[str]] = {}
    for cell_id, fingerprint in by_cell.items():
        key = json.dumps(fingerprint, sort_keys=True, ensure_ascii=False)
        unique.setdefault(key, []).append(cell_id)
    return {
        "schema_version": "lorq.fingerprints.v1alpha1",
        "contract_version": LORQ_CONTRACT_VERSION,
        "by_cell": by_cell,
        "unique_fingerprints": [
            {"fingerprint": json.loads(key), "cell_ids": sorted(cell_ids)}
            for key, cell_ids in sorted(unique.items())
        ],
    }


def _integrity(cells: list[dict[str, Any]]) -> dict[str, Any]:
    warnings: list[dict[str, Any]] = []
    seen: set[str] = set()
    for cell in cells:
        cell_id = str(cell.get("cell_id"))
        if cell_id in seen:
            warnings.append({"type": "duplicate_cell", "cell_id": cell_id, "severity": "error"})
        seen.add(cell_id)
        adapter = (cell.get("adapter_output") or {})
        if not adapter.get("final_answer_present"):
            warnings.append({"type": "missing_final_answer", "cell_id": cell_id, "severity": "warning"})
        if cell.get("status") not in {"completed", "skipped"}:
            warnings.append({"type": "non_completed_cell", "cell_id": cell_id, "status": cell.get("status"), "severity": "warning"})
    return {
        "schema_version": "lorq.integrity.v1alpha1",
        "contract_version": LORQ_CONTRACT_VERSION,
        "ok": not any(w.get("severity") == "error" for w in warnings),
        "warnings": warnings,
    }


def _experiment_yaml(*, package_id: str, shard_id: str, cell_count: int) -> str:
    return f"""package_schema_version: {LORQ_PACKAGE_SCHEMA_VERSION}\npackage_kind: run_shard\npackage_id: {package_id}\ncreated_by:\n  name: agent-eval python-v0\n  implementation: python\n  version: {__version__}\nshards:\n  - {shard_id}\ncell_count: {cell_count}\n"""


def export_lorq_run_shard(results_root: Path, package_root: Path, *, shard_id: str = "shard-001", package_id: str | None = None) -> dict[str, Any]:
    """Export Python v0 results into the first v1-alpha LORQ run-shard layout.

    This is intentionally a migration/conformance bridge. It does not implement
    the future .NET product; it normalizes Python v0 records into the planned
    public package shape so fixture consumers can verify orchestration semantics.
    """
    results_root = results_root.expanduser().resolve()
    package_root = package_root.expanduser().resolve()
    records = load_run_records(results_root)
    if not records:
        raise ValueError(f"No Python v0 run records found under {results_root}")

    ensure_dir(package_root)
    ensure_dir(package_root / "runs" / shard_id / "cells")
    ensure_dir(package_root / "judgements")
    ensure_dir(package_root / "reports" / "cases")
    ensure_dir(package_root / ".lorq" / "cells")

    cells = [build_cell_evidence(record, shard_id=shard_id, package_root=package_root) for record in records]
    for cell in cells:
        write_json(package_root / ".lorq" / "cells" / f"{cell['cell_id']}.json", cell)

    package_id = package_id or package_root.name or shard_id
    write_text(package_root / "experiment.yaml", _experiment_yaml(package_id=package_id, shard_id=shard_id, cell_count=len(cells)))
    write_json(package_root / ".lorq" / "coverage.json", _coverage(cells))
    write_json(package_root / ".lorq" / "fingerprints.json", _fingerprints(cells))
    write_json(package_root / ".lorq" / "integrity.json", _integrity(cells))
    write_json(package_root / ".lorq" / "merge-log.json", {
        "schema_version": "lorq.merge-log.v1alpha1",
        "contract_version": LORQ_CONTRACT_VERSION,
        "operation": "python-v0-single-shard-export",
        "inputs": [{"kind": "python-v0-results", "label": results_root.name}],
        "outputs": [{"kind": "lorq-run-shard", "package_id": package_id, "shard_id": shard_id}],
        "cell_count": len(cells),
    })
    write_json(package_root / "runs" / shard_id / "shard.manifest.json", {
        "schema_version": "lorq.run-shard-manifest.v1alpha1",
        "contract_version": LORQ_CONTRACT_VERSION,
        "shard_id": shard_id,
        "cell_count": len(cells),
        "cell_ids": sorted(cell["cell_id"] for cell in cells),
        "source": {"kind": "python-v0-results", "label": results_root.name},
    })

    return {
        "schema_version": "lorq.package-export-result.v1alpha1",
        "contract_version": LORQ_CONTRACT_VERSION,
        "ok": True,
        "package_root": str(package_root),
        "package_id": package_id,
        "shard_id": shard_id,
        "cell_count": len(cells),
        "cell_ids": sorted(cell["cell_id"] for cell in cells),
    }
