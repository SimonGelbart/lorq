from __future__ import annotations

import json
import shutil
from pathlib import Path
from typing import Any

import yaml

from . import __version__
from .contracts import (
    LORQ_CELL_EVIDENCE_SCHEMA_VERSION,
    LORQ_CELL_JUDGEMENT_SCHEMA_VERSION,
    LORQ_CONTRACT_VERSION,
    LORQ_JUDGEMENT_PASS_SCHEMA_VERSION,
    LORQ_PACKAGE_SCHEMA_VERSION,
    LORQ_REPORT_SCHEMA_VERSION,
    LORQ_CASE_REVIEW_PACK_SCHEMA_VERSION,
)
from .lifecycle import load_run_records
from .utils import ensure_dir, read_json, read_text, rm_rf, slug, write_json, write_text


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


class LorqPackageError(ValueError):
    """Raised when a migration-only LORQ package operation is invalid."""


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


def cell_id_for_parts(case_id: str, mode_id: str, attempt: int | str = 1) -> str:
    if isinstance(attempt, int):
        attempt_id = f"attempt-{attempt:03d}"
    else:
        attempt_text = str(attempt)
        attempt_id = attempt_text if attempt_text.startswith("attempt-") else f"attempt-{slug(attempt_text)}"
    return f"{slug(str(case_id))}__{slug(str(mode_id))}__{attempt_id}"


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


def _load_adapter_integrity_warnings(package_root: Path, cell: dict[str, Any]) -> list[str]:
    cell_dir = package_root / str((cell.get("evidence_refs") or {}).get("cell_dir") or "")
    adapter_evidence = read_json(cell_dir / "adapter.evidence.json", default={}) or {}
    warnings = adapter_evidence.get("integrity_warnings") or []
    if isinstance(warnings, str):
        return [warnings]
    if isinstance(warnings, list):
        return [str(item) for item in warnings if str(item).strip()]
    return []


def _coverage(cells: list[dict[str, Any]], *, expected_cell_ids: list[str] | None = None) -> dict[str, Any]:
    status_counts: dict[str, int] = {}
    cases = sorted({str(cell.get("case_id")) for cell in cells})
    modes = sorted({str(cell.get("mode_id")) for cell in cells})
    attempts = sorted({str(cell.get("attempt_id")) for cell in cells})
    present_cell_ids = sorted(str(cell.get("cell_id")) for cell in cells)
    expected = sorted(set(expected_cell_ids or present_cell_ids))
    present = set(present_cell_ids)
    for cell in cells:
        status = str(cell.get("status") or "unknown")
        status_counts[status] = status_counts.get(status, 0) + 1
    return {
        "schema_version": "lorq.coverage.v1alpha1",
        "contract_version": LORQ_CONTRACT_VERSION,
        "cell_count": len(cells),
        "expected_cell_count": len(expected),
        "cases": cases,
        "modes": modes,
        "attempts": attempts,
        "present_cell_ids": present_cell_ids,
        "expected_cell_ids": expected,
        "status_counts": status_counts,
        "missing_cells": [cell_id for cell_id in expected if cell_id not in present],
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
        "unique_fingerprint_count": len(unique),
        "unique_fingerprints": [
            {"fingerprint": json.loads(key), "cell_ids": sorted(cell_ids)}
            for key, cell_ids in sorted(unique.items())
        ],
    }


def _integrity(
    cells: list[dict[str, Any]],
    *,
    package_root: Path | None = None,
    missing_cells: list[str] | None = None,
    duplicate_cells: list[dict[str, Any]] | None = None,
    fingerprint_mismatch: bool = False,
    shard_warnings: list[dict[str, Any]] | None = None,
) -> dict[str, Any]:
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
        if package_root is not None:
            for message in _load_adapter_integrity_warnings(package_root, cell):
                warnings.append({
                    "type": "adapter_integrity_warning",
                    "cell_id": cell_id,
                    "message": message,
                    "severity": "warning",
                })
    for cell_id in missing_cells or []:
        warnings.append({"type": "missing_expected_cell", "cell_id": cell_id, "severity": "warning"})
    for duplicate in duplicate_cells or []:
        warnings.append({"type": "duplicate_cell", **duplicate, "severity": "error"})
    if fingerprint_mismatch:
        warnings.append({"type": "fingerprint_mismatch", "severity": "error"})
    for warning in shard_warnings or []:
        preserved = {"type": "source_shard_integrity_warning", "severity": warning.get("severity", "warning")}
        preserved.update(warning)
        warnings.append(preserved)
    return {
        "schema_version": "lorq.integrity.v1alpha1",
        "contract_version": LORQ_CONTRACT_VERSION,
        "ok": not any(w.get("severity") == "error" for w in warnings),
        "warnings": warnings,
    }


def _portable_path(path: Path, base: Path) -> str:
    try:
        return path.resolve().relative_to(base.resolve()).as_posix()
    except ValueError:
        try:
            return path.resolve().relative_to(base.resolve().parent).as_posix()
        except ValueError:
            return path.name


def _experiment_yaml(*, package_id: str, package_kind: str, shard_ids: list[str], cell_count: int) -> str:
    rendered_shards = "\n".join(f"  - {shard_id}" for shard_id in shard_ids)
    return f"""package_schema_version: {LORQ_PACKAGE_SCHEMA_VERSION}\npackage_kind: {package_kind}\npackage_id: {package_id}\ncreated_by:\n  name: agent-eval python-v0\n  implementation: python\n  version: {__version__}\nshards:\n{rendered_shards}\ncell_count: {cell_count}\n"""


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
    write_text(package_root / "experiment.yaml", _experiment_yaml(package_id=package_id, package_kind="run_shard", shard_ids=[shard_id], cell_count=len(cells)))
    coverage = _coverage(cells)
    write_json(package_root / ".lorq" / "coverage.json", coverage)
    write_json(package_root / ".lorq" / "fingerprints.json", _fingerprints(cells))
    write_json(package_root / ".lorq" / "integrity.json", _integrity(cells, package_root=package_root, missing_cells=coverage["missing_cells"]))
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


def _load_package_cells(package_root: Path) -> list[dict[str, Any]]:
    cells_dir = package_root / ".lorq" / "cells"
    if not cells_dir.exists():
        raise LorqPackageError(f"Missing LORQ cell index: {cells_dir}")
    cells = [read_json(path) for path in sorted(cells_dir.glob("*.json"))]
    return [cell for cell in cells if isinstance(cell, dict)]


def _read_shard_id(package_root: Path, cells: list[dict[str, Any]]) -> str:
    shard_ids = sorted({str(cell.get("shard_id")) for cell in cells if cell.get("shard_id")})
    if len(shard_ids) != 1:
        raise LorqPackageError(f"Expected exactly one shard id in {package_root}, found {shard_ids}")
    return shard_ids[0]


def expected_cells_from_benchmark(benchmark_file: Path) -> list[str]:
    """Read deterministic benchmark shape and return expected v1-alpha cell ids."""
    data = yaml.safe_load(benchmark_file.read_text(encoding="utf-8")) or {}
    cases = [str(item["id"] if isinstance(item, dict) else item) for item in data.get("cases", [])]
    modes = [str(item["id"] if isinstance(item, dict) else item) for item in data.get("modes", [])]
    attempts = int((data.get("shape") or {}).get("attempts_per_case_mode") or 1)
    expected: list[str] = []
    for case_id in cases:
        for mode_id in modes:
            for attempt in range(1, attempts + 1):
                expected.append(cell_id_for_parts(case_id, mode_id, attempt))
    return sorted(expected)


def _copy_shard_payload(src_root: Path, dst_root: Path, shard_id: str) -> None:
    src_run = src_root / "runs" / shard_id
    if not src_run.exists():
        raise LorqPackageError(f"Missing run shard payload: {src_run}")
    dst_run = dst_root / "runs" / shard_id
    rm_rf(dst_run)
    ensure_dir(dst_run.parent)
    shutil.copytree(src_run, dst_run)


def _duplicate_cell_errors(cell_sources: dict[str, list[str]]) -> list[dict[str, Any]]:
    return [
        {"cell_id": cell_id, "source_shards": sources}
        for cell_id, sources in sorted(cell_sources.items())
        if len(sources) > 1
    ]


def _fingerprint_mismatch(fingerprints: dict[str, Any]) -> bool:
    return int(fingerprints.get("unique_fingerprint_count") or 0) > 1


def merge_lorq_run_shards(
    shard_roots: list[Path],
    output_root: Path,
    *,
    package_id: str | None = None,
    benchmark_file: Path | None = None,
    strict: bool = True,
) -> dict[str, Any]:
    """Merge v1-alpha LORQ run-shard packages into an experiment package.

    This is migration-only Python v0 scaffolding for the frozen conformance
    benchmark. It verifies orchestration/package semantics and intentionally
    fails by default on duplicate cell ids or incompatible source fingerprints.
    """
    if len(shard_roots) < 1:
        raise LorqPackageError("At least one LORQ run-shard package is required")

    normalized_shards = [Path(root).expanduser().resolve() for root in shard_roots]
    output_root = output_root.expanduser().resolve()
    package_id = package_id or output_root.name or "experiment"
    expected_cell_ids = expected_cells_from_benchmark(benchmark_file.expanduser().resolve()) if benchmark_file else None

    inputs: list[dict[str, Any]] = []
    cells: list[dict[str, Any]] = []
    cell_sources: dict[str, list[str]] = {}
    shard_ids: list[str] = []
    source_shard_warnings: list[dict[str, Any]] = []

    for shard_root in normalized_shards:
        shard_cells = _load_package_cells(shard_root)
        shard_id = _read_shard_id(shard_root, shard_cells)
        shard_ids.append(shard_id)
        inputs.append({"kind": "lorq-run-shard", "path": _portable_path(shard_root, output_root), "shard_id": shard_id, "cell_count": len(shard_cells)})
        shard_integrity = read_json(shard_root / ".lorq" / "integrity.json", default={}) or {}
        for warning in shard_integrity.get("warnings") or []:
            if isinstance(warning, dict):
                source_shard_warnings.append({"source_shard": shard_id, **warning})
        for cell in shard_cells:
            copied = dict(cell)
            copied["source_shard_id"] = shard_id
            cells.append(copied)
            cell_sources.setdefault(str(cell.get("cell_id")), []).append(shard_id)

    duplicate_cells = _duplicate_cell_errors(cell_sources)
    if duplicate_cells and strict:
        raise LorqPackageError(f"Duplicate LORQ cell ids while merging shards: {duplicate_cells}")

    unique_cells: list[dict[str, Any]] = []
    seen: set[str] = set()
    for cell in cells:
        cell_id = str(cell.get("cell_id"))
        if cell_id in seen:
            continue
        seen.add(cell_id)
        unique_cells.append(cell)

    coverage = _coverage(unique_cells, expected_cell_ids=expected_cell_ids)
    fingerprints = _fingerprints(unique_cells)
    fingerprint_mismatch = _fingerprint_mismatch(fingerprints)
    if fingerprint_mismatch and strict:
        raise LorqPackageError("Input shards contain incompatible repository fingerprints")

    rm_rf(output_root)
    ensure_dir(output_root / "runs")
    ensure_dir(output_root / "judgements")
    ensure_dir(output_root / "reports" / "cases")
    ensure_dir(output_root / ".lorq" / "cells")

    for shard_root, shard_id in zip(normalized_shards, shard_ids):
        _copy_shard_payload(shard_root, output_root, shard_id)

    for cell in unique_cells:
        cell_id = str(cell["cell_id"])
        write_json(output_root / ".lorq" / "cells" / f"{cell_id}.json", cell)

    integrity = _integrity(
        unique_cells,
        package_root=output_root,
        missing_cells=coverage["missing_cells"],
        duplicate_cells=duplicate_cells,
        fingerprint_mismatch=fingerprint_mismatch,
        shard_warnings=source_shard_warnings,
    )

    write_text(output_root / "experiment.yaml", _experiment_yaml(package_id=package_id, package_kind="merged_experiment", shard_ids=sorted(shard_ids), cell_count=len(unique_cells)))
    write_json(output_root / ".lorq" / "coverage.json", coverage)
    write_json(output_root / ".lorq" / "fingerprints.json", fingerprints)
    write_json(output_root / ".lorq" / "integrity.json", integrity)
    write_json(output_root / ".lorq" / "merge-log.json", {
        "schema_version": "lorq.merge-log.v1alpha1",
        "contract_version": LORQ_CONTRACT_VERSION,
        "operation": "python-v0-merge-run-shards",
        "inputs": inputs,
        "outputs": [{"kind": "lorq-merged-experiment", "package_id": package_id, "path": "."}],
        "strict": strict,
        "cell_count": len(unique_cells),
        "expected_cell_count": coverage["expected_cell_count"],
        "missing_cell_count": len(coverage["missing_cells"]),
        "duplicate_cell_count": len(duplicate_cells),
        "fingerprint_mismatch": fingerprint_mismatch,
    })

    return {
        "schema_version": "lorq.package-merge-result.v1alpha1",
        "contract_version": LORQ_CONTRACT_VERSION,
        "ok": integrity["ok"],
        "package_root": str(output_root),
        "package_id": package_id,
        "shard_ids": sorted(shard_ids),
        "cell_count": len(unique_cells),
        "expected_cell_count": coverage["expected_cell_count"],
        "missing_cell_ids": coverage["missing_cells"],
        "duplicate_cell_ids": [item["cell_id"] for item in duplicate_cells],
        "fingerprint_mismatch": fingerprint_mismatch,
    }


def _load_yaml_or_json(path: Path) -> dict[str, Any]:
    if not path.exists():
        raise LorqPackageError(f"Missing deterministic judge fixture: {path}")
    text = path.read_text(encoding="utf-8")
    data = json.loads(text or "{}") if path.suffix.lower() == ".json" else (yaml.safe_load(text) or {})
    if not isinstance(data, dict):
        raise LorqPackageError(f"Expected mapping in deterministic judge fixture: {path}")
    return data


def _normalise_fixture_judgements(data: dict[str, Any]) -> dict[str, dict[str, Any]]:
    raw = data.get("judgements") or data.get("judgments") or {}
    judgements: dict[str, dict[str, Any]] = {}
    if isinstance(raw, dict):
        for key, value in raw.items():
            if isinstance(value, dict):
                judgements[str(key)] = dict(value)
        return judgements
    if isinstance(raw, list):
        for item in raw:
            if not isinstance(item, dict):
                continue
            case_id = item.get("case") or item.get("case_id")
            mode_id = item.get("mode") or item.get("mode_id")
            attempt = item.get("attempt") or item.get("repetition") or 1
            judgements[f"{case_id}|{mode_id}|{attempt}"] = dict(item)
    return judgements


def _attempt_number(attempt_id: Any) -> int | None:
    text = str(attempt_id or "")
    if text.startswith("attempt-"):
        text = text.removeprefix("attempt-")
    try:
        return int(text)
    except ValueError:
        return None


def _cell_fixture_keys(cell: dict[str, Any]) -> list[str]:
    case_id = str(cell.get("case_id") or "")
    mode_id = str(cell.get("mode_id") or "")
    attempt_id = cell.get("attempt_id") or "attempt-001"
    attempt_number = _attempt_number(attempt_id)
    keys: list[str] = []
    if attempt_number is not None:
        keys.append(f"{case_id}|{mode_id}|{attempt_number}")
    keys.append(f"{case_id}|{mode_id}|{attempt_id}")
    # Preserve deterministic fixture compatibility for single-attempt cells.
    if attempt_number != 1:
        keys.append(f"{case_id}|{mode_id}|1")
    return keys


def _quality_from_fixture_payload(payload: dict[str, Any]) -> dict[str, Any]:
    dimensions = payload.get("dimensions") if isinstance(payload.get("dimensions"), dict) else {}
    scores: list[float] = []
    for value in dimensions.values():
        if isinstance(value, dict):
            try:
                scores.append(float(value.get("score")))
            except (TypeError, ValueError):
                continue
    overall = payload.get("overall_score")
    try:
        overall_score = float(overall)
    except (TypeError, ValueError):
        overall_score = round(sum(scores) / len(scores), 3) if scores else None
    return {
        "ok": bool(payload.get("ok", overall_score is not None)),
        "overall_score": overall_score,
        "confidence": payload.get("confidence"),
        "dimensions": dimensions,
        "strengths": payload.get("strengths") or [],
        "weaknesses": payload.get("weaknesses") or [],
        "missing_or_questionable": payload.get("missing_or_questionable") or [],
        "summary": payload.get("summary") or "",
    }


def _score_summary(cell_judgements: list[dict[str, Any]]) -> dict[str, Any]:
    scores: list[float] = []
    by_mode: dict[str, list[float]] = {}
    by_case: dict[str, list[float]] = {}
    for judgement in cell_judgements:
        score = ((judgement.get("quality") or {}).get("overall_score"))
        if score is None:
            continue
        score_value = float(score)
        scores.append(score_value)
        by_mode.setdefault(str(judgement.get("mode_id")), []).append(score_value)
        by_case.setdefault(str(judgement.get("case_id")), []).append(score_value)

    def _average(values: list[float]) -> float | None:
        return round(sum(values) / len(values), 3) if values else None

    return {
        "overall_average": _average(scores),
        "overall_min": min(scores) if scores else None,
        "overall_max": max(scores) if scores else None,
        "by_mode": {key: _average(values) for key, values in sorted(by_mode.items())},
        "by_case": {key: _average(values) for key, values in sorted(by_case.items())},
    }


def _input_refs_for_cell(cell: dict[str, Any]) -> dict[str, Any]:
    refs = cell.get("evidence_refs") if isinstance(cell.get("evidence_refs"), dict) else {}
    return {
        "cell_result": refs.get("cell_result"),
        "cell_dir": refs.get("cell_dir"),
        "final_answer": refs.get("final_answer"),
        "validation": refs.get("validation"),
        "trace": refs.get("trace"),
    }


def attach_lorq_deterministic_judgement(
    package_root: Path,
    *,
    judge_name: str = "judge-primary",
    fixture_file: Path,
    strict: bool = True,
) -> dict[str, Any]:
    """Attach a deterministic fake judgement pass to a merged LORQ package.

    This is migration-only Python v0 scaffolding. It reads cell evidence from a
    merged package and fixture scores from a deterministic YAML/JSON file; it
    does not call Codex, Copilot, a judge LLM, or any external service.
    """
    package_root = package_root.expanduser().resolve()
    fixture_file = fixture_file.expanduser().resolve()
    cells = _load_package_cells(package_root)
    if not cells:
        raise LorqPackageError(f"No LORQ cells found in package: {package_root}")

    fixture = _load_yaml_or_json(fixture_file)
    fixture_judgements = _normalise_fixture_judgements(fixture)
    if not fixture_judgements:
        raise LorqPackageError(f"No deterministic judgements found in fixture: {fixture_file}")

    missing_fixture_cell_ids: list[str] = []
    cell_judgements: list[dict[str, Any]] = []
    for cell in sorted(cells, key=lambda item: str(item.get("cell_id"))):
        cell_id = str(cell.get("cell_id"))
        payload = None
        matched_key = None
        for key in _cell_fixture_keys(cell):
            if key in fixture_judgements:
                payload = fixture_judgements[key]
                matched_key = key
                break
        if payload is None:
            missing_fixture_cell_ids.append(cell_id)
            continue
        judgement = {
            "schema_version": LORQ_CELL_JUDGEMENT_SCHEMA_VERSION,
            "contract_version": LORQ_CONTRACT_VERSION,
            "judgement_name": judge_name,
            "cell_id": cell_id,
            "case_id": cell.get("case_id"),
            "mode_id": cell.get("mode_id"),
            "attempt_id": cell.get("attempt_id"),
            "cell_status": cell.get("status"),
            "status": "judged",
            "source": {
                "backend": "deterministic-fake",
                "fixture_schema_version": fixture.get("schema_version"),
                "fixture_file": _portable_path(fixture_file, package_root),
                "fixture_key": matched_key,
                "real_llm_used": False,
            },
            "quality": _quality_from_fixture_payload(payload),
            "input_refs": _input_refs_for_cell(cell),
        }
        cell_judgements.append(judgement)

    if missing_fixture_cell_ids and strict:
        raise LorqPackageError(f"Missing deterministic judgement fixture entries for cells: {missing_fixture_cell_ids}")

    coverage = read_json(package_root / ".lorq" / "coverage.json", default={}) or {}
    missing_expected = coverage.get("missing_cells") or []
    if not isinstance(missing_expected, list):
        missing_expected = []

    judgement_dir = package_root / "judgements" / judge_name
    cells_dir = judgement_dir / "cells"
    ensure_dir(cells_dir)
    ensure_dir(package_root / ".lorq" / "judgements")

    for judgement in cell_judgements:
        write_json(cells_dir / f"{judgement['cell_id']}.json", judgement)

    manifest = {
        "schema_version": LORQ_JUDGEMENT_PASS_SCHEMA_VERSION,
        "contract_version": LORQ_CONTRACT_VERSION,
        "judgement_name": judge_name,
        "package_root": ".",
        "backend": "deterministic-fake",
        "source": {
            "fixture_schema_version": fixture.get("schema_version"),
            "fixture_file": _portable_path(fixture_file, package_root),
            "real_llm_used": False,
        },
        "cell_count": len(cells),
        "judged_cell_count": len(cell_judgements),
        "missing_fixture_cell_ids": missing_fixture_cell_ids,
        "missing_expected_cell_ids": [str(item) for item in missing_expected],
        "score_summary": _score_summary(cell_judgements),
        "cell_judgement_refs": [
            {"cell_id": item["cell_id"], "path": f"judgements/{judge_name}/cells/{item['cell_id']}.json"}
            for item in cell_judgements
        ],
    }
    write_json(judgement_dir / "judgement.manifest.json", manifest)
    write_json(judgement_dir / "judgement.summary.json", {
        "schema_version": "lorq.judgement-summary.v1alpha1",
        "contract_version": LORQ_CONTRACT_VERSION,
        "judgement_name": judge_name,
        "cell_count": len(cells),
        "judged_cell_count": len(cell_judgements),
        "missing_fixture_cell_count": len(missing_fixture_cell_ids),
        "missing_expected_cell_count": len(missing_expected),
        "score_summary": manifest["score_summary"],
    })
    write_json(package_root / ".lorq" / "judgements" / f"{judge_name}.json", manifest)

    return {
        "schema_version": "lorq.judgement-attach-result.v1alpha1",
        "contract_version": LORQ_CONTRACT_VERSION,
        "ok": not missing_fixture_cell_ids,
        "package_root": str(package_root),
        "judgement_name": judge_name,
        "backend": "deterministic-fake",
        "cell_count": len(cells),
        "judged_cell_count": len(cell_judgements),
        "missing_fixture_cell_ids": missing_fixture_cell_ids,
        "missing_expected_cell_ids": [str(item) for item in missing_expected],
        "score_summary": manifest["score_summary"],
    }


def _read_package_metadata(package_root: Path) -> dict[str, Any]:
    experiment_file = package_root / "experiment.yaml"
    if not experiment_file.exists():
        return {"package_id": package_root.name, "package_kind": "unknown", "shards": []}
    data = yaml.safe_load(experiment_file.read_text(encoding="utf-8")) or {}
    if not isinstance(data, dict):
        return {"package_id": package_root.name, "package_kind": "unknown", "shards": []}
    return data


def _load_primary_judgements(package_root: Path, primary_judgement: str) -> tuple[dict[str, Any], dict[str, dict[str, Any]]]:
    manifest = read_json(package_root / ".lorq" / "judgements" / f"{primary_judgement}.json", default=None)
    if manifest is None:
        manifest = read_json(package_root / "judgements" / primary_judgement / "judgement.manifest.json", default=None)
    if not isinstance(manifest, dict):
        raise LorqPackageError(f"Missing primary LORQ judgement pass: {primary_judgement}")
    judgements: dict[str, dict[str, Any]] = {}
    cells_dir = package_root / "judgements" / primary_judgement / "cells"
    for path in sorted(cells_dir.glob("*.json")):
        item = read_json(path, default={}) or {}
        if isinstance(item, dict) and item.get("cell_id"):
            judgements[str(item["cell_id"])] = item
    return manifest, judgements


def _status_counts(cells: list[dict[str, Any]]) -> dict[str, int]:
    counts: dict[str, int] = {}
    for cell in cells:
        status = str(cell.get("status") or "unknown")
        counts[status] = counts.get(status, 0) + 1
    return dict(sorted(counts.items()))


def _scores_by_cell(judgements: dict[str, dict[str, Any]]) -> dict[str, float | None]:
    scores: dict[str, float | None] = {}
    for cell_id, judgement in sorted(judgements.items()):
        score = (judgement.get("quality") or {}).get("overall_score")
        try:
            scores[cell_id] = float(score)
        except (TypeError, ValueError):
            scores[cell_id] = None
    return scores


def _case_pack_markdown(case_pack: dict[str, Any]) -> str:
    case_id = case_pack["case_id"]
    lines = [
        f"# LORQ case review: {case_id}",
        "",
        f"Cells: {case_pack['cell_count']}",
        f"Missing expected cells: {case_pack['missing_expected_cell_count']}",
        f"Average score: {case_pack['score_summary']['average']}",
        "",
        "| Cell | Mode | Status | Score | Final answer |",
        "| --- | --- | --- | ---: | --- |",
    ]
    for cell in case_pack["cells"]:
        lines.append(
            f"| `{cell['cell_id']}` | `{cell['mode_id']}` | {cell['status']} | "
            f"{cell.get('score')} | {cell.get('final_answer_present')} |"
        )
    if case_pack["missing_expected_cell_ids"]:
        lines.extend(["", "## Missing expected cells"])
        for cell_id in case_pack["missing_expected_cell_ids"]:
            lines.append(f"- `{cell_id}`")
    return "\n".join(lines) + "\n"


def _average_or_none(values: list[float]) -> float | None:
    return round(sum(values) / len(values), 3) if values else None


def _case_review_packs(
    cells: list[dict[str, Any]],
    judgements: dict[str, dict[str, Any]],
    missing_expected_cell_ids: list[str],
) -> list[dict[str, Any]]:
    cases = sorted({str(cell.get("case_id")) for cell in cells} | {str(item).split("__", 1)[0] for item in missing_expected_cell_ids})
    packs: list[dict[str, Any]] = []
    for case_id in cases:
        case_cells = [cell for cell in sorted(cells, key=lambda item: str(item.get("cell_id"))) if str(cell.get("case_id")) == case_id]
        case_missing = [cell_id for cell_id in missing_expected_cell_ids if cell_id.startswith(f"{case_id}__")]
        row_cells: list[dict[str, Any]] = []
        scores: list[float] = []
        for cell in case_cells:
            cell_id = str(cell.get("cell_id"))
            judgement = judgements.get(cell_id) or {}
            score = (judgement.get("quality") or {}).get("overall_score")
            try:
                score_value = float(score)
                scores.append(score_value)
            except (TypeError, ValueError):
                score_value = None
            adapter_output = cell.get("adapter_output") if isinstance(cell.get("adapter_output"), dict) else {}
            row_cells.append({
                "cell_id": cell_id,
                "mode_id": cell.get("mode_id"),
                "attempt_id": cell.get("attempt_id"),
                "status": cell.get("status"),
                "score": score_value,
                "final_answer_present": adapter_output.get("final_answer_present"),
                "evidence_refs": cell.get("evidence_refs") or {},
                "judgement_ref": f"judgements/{judgement.get('judgement_name', '')}/cells/{cell_id}.json" if judgement else None,
                "integrity_warnings": [],
            })
        pack = {
            "schema_version": LORQ_CASE_REVIEW_PACK_SCHEMA_VERSION,
            "contract_version": LORQ_CONTRACT_VERSION,
            "case_id": case_id,
            "cell_count": len(case_cells),
            "missing_expected_cell_count": len(case_missing),
            "missing_expected_cell_ids": case_missing,
            "score_summary": {
                "average": _average_or_none(scores),
                "min": min(scores) if scores else None,
                "max": max(scores) if scores else None,
            },
            "cells": row_cells,
        }
        packs.append(pack)
    return packs


def _report_markdown(report: dict[str, Any]) -> str:
    summary = report["summary"]
    package = report["package"]
    judgement = report["primary_judgement"]
    lines = [
        "# LORQ package report",
        "",
        f"Package: `{package['package_id']}`",
        f"Kind: `{package['package_kind']}`",
        f"Primary judgement: `{judgement['name']}`",
        f"Real LLM used for judgement: `{judgement['source'].get('real_llm_used')}`",
        "",
        "## Summary",
        "",
        f"- Cells: {summary['cell_count']} present / {summary['expected_cell_count']} expected",
        f"- Missing expected cells: {summary['missing_expected_cell_count']}",
        f"- Integrity OK: {summary['integrity_ok']}",
        f"- Warning count: {summary['warning_count']}",
        f"- Average score: {summary['score_summary'].get('overall_average')}",
        "",
        "## Status counts",
        "",
    ]
    for status, count in summary["status_counts"].items():
        lines.append(f"- `{status}`: {count}")
    if summary["missing_expected_cell_ids"]:
        lines.extend(["", "## Missing expected cells", ""])
        for cell_id in summary["missing_expected_cell_ids"]:
            lines.append(f"- `{cell_id}`")
    lines.extend(["", "## Cases", "", "| Case | Cells | Missing | Average score |", "| --- | ---: | ---: | ---: |"])
    for case in report["case_packs"]:
        lines.append(
            f"| `{case['case_id']}` | {case['cell_count']} | {case['missing_expected_cell_count']} | {case['score_summary']['average']} |"
        )
    return "\n".join(lines) + "\n"


def render_lorq_package_report(package_root: Path, *, primary_judgement: str = "judge-primary") -> dict[str, Any]:
    """Render canonical JSON/Markdown reports and per-case review packs.

    This is migration-only Python v0 scaffolding for the frozen benchmark. It
    reads a merged package plus a named deterministic judgement pass and writes
    stable report artifacts without calling an LLM or mutating run evidence.
    """
    package_root = package_root.expanduser().resolve()
    cells = _load_package_cells(package_root)
    if not cells:
        raise LorqPackageError(f"No LORQ cells found in package: {package_root}")
    package = _read_package_metadata(package_root)
    coverage = read_json(package_root / ".lorq" / "coverage.json", default={}) or {}
    integrity = read_json(package_root / ".lorq" / "integrity.json", default={}) or {}
    fingerprint_index = read_json(package_root / ".lorq" / "fingerprints.json", default={}) or {}
    judgement_manifest, judgements = _load_primary_judgements(package_root, primary_judgement)

    missing_expected = [str(item) for item in (coverage.get("missing_cells") or [])]
    warning_items = integrity.get("warnings") if isinstance(integrity.get("warnings"), list) else []
    case_packs = _case_review_packs(cells, judgements, missing_expected)
    scores_by_cell = _scores_by_cell(judgements)

    report = {
        "schema_version": LORQ_REPORT_SCHEMA_VERSION,
        "contract_version": LORQ_CONTRACT_VERSION,
        "package": {
            "package_id": package.get("package_id") or package_root.name,
            "package_kind": package.get("package_kind") or "unknown",
            "schema_version": package.get("package_schema_version"),
            "shards": package.get("shards") or [],
            "package_root": ".",
        },
        "summary": {
            "cell_count": len(cells),
            "expected_cell_count": int(coverage.get("expected_cell_count") or len(cells)),
            "missing_expected_cell_count": len(missing_expected),
            "missing_expected_cell_ids": missing_expected,
            "status_counts": _status_counts(cells),
            "integrity_ok": bool(integrity.get("ok", False)),
            "warning_count": len(warning_items),
            "score_summary": judgement_manifest.get("score_summary") or {},
        },
        "primary_judgement": {
            "name": primary_judgement,
            "backend": judgement_manifest.get("backend"),
            "source": judgement_manifest.get("source") or {},
            "cell_count": judgement_manifest.get("cell_count"),
            "judged_cell_count": judgement_manifest.get("judged_cell_count"),
            "scores_by_cell": scores_by_cell,
        },
        "integrity": integrity,
        "coverage": coverage,
        "fingerprints": {
            "unique_fingerprint_count": fingerprint_index.get("unique_fingerprint_count"),
        },
        "case_packs": [
            {
                "case_id": item["case_id"],
                "path": f"reports/cases/{item['case_id']}/case-review.json",
                "markdown_path": f"reports/cases/{item['case_id']}/case-review.md",
                "cell_count": item["cell_count"],
                "missing_expected_cell_count": item["missing_expected_cell_count"],
                "score_summary": item["score_summary"],
            }
            for item in case_packs
        ],
    }

    reports_dir = package_root / "reports"
    ensure_dir(reports_dir / "cases")
    write_json(reports_dir / "report.json", report)
    write_text(reports_dir / "report.md", _report_markdown(report))
    for pack in case_packs:
        case_dir = reports_dir / "cases" / pack["case_id"]
        write_json(case_dir / "case-review.json", pack)
        write_text(case_dir / "case-review.md", _case_pack_markdown(pack))
    write_json(package_root / ".lorq" / "report.json", {
        "schema_version": LORQ_REPORT_SCHEMA_VERSION,
        "contract_version": LORQ_CONTRACT_VERSION,
        "report": "reports/report.json",
        "markdown": "reports/report.md",
        "primary_judgement": primary_judgement,
        "case_count": len(case_packs),
    })

    return {
        "schema_version": "lorq.report-render-result.v1alpha1",
        "contract_version": LORQ_CONTRACT_VERSION,
        "ok": True,
        "package_root": str(package_root),
        "primary_judgement": primary_judgement,
        "report_json": "reports/report.json",
        "report_markdown": "reports/report.md",
        "case_pack_count": len(case_packs),
        "missing_expected_cell_ids": missing_expected,
        "score_summary": judgement_manifest.get("score_summary") or {},
    }
