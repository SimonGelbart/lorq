#!/usr/bin/env python3
"""Minimal LORQ one-shot file adapter example.

The adapter reads the LORQ request path from LORQ_ADAPTER_REQUEST and writes the
expected evidence contract to LORQ_ADAPTER_EVIDENCE. It performs no LLM calls.
"""

from __future__ import annotations

import hashlib
import json
import os
from pathlib import Path


def main() -> int:
    request_path = Path(require_env("LORQ_ADAPTER_REQUEST"))
    evidence_path = Path(require_env("LORQ_ADAPTER_EVIDENCE"))
    request = json.loads(request_path.read_text(encoding="utf-8"))
    exchange_dir = Path(request["workspace"]["evidence_directory"])
    exchange_dir.mkdir(parents=True, exist_ok=True)

    answer_path = exchange_dir / request["expected_output"]["final_answer_path"]
    stdout_path = exchange_dir / "stdout.raw.txt"
    stderr_path = exchange_dir / "stderr.txt"

    answer_text = f"Sample adapter answer for {request['cell']['cell_id']}\n"
    answer_path.write_text(answer_text, encoding="utf-8")
    stdout_path.write_text("sample adapter stdout\n", encoding="utf-8")
    stderr_path.write_text("", encoding="utf-8")

    evidence = {
        "schema_version": "lorq.file-adapter-evidence.v1alpha1",
        "contract_version": request["contract_version"],
        "cell_id": request["cell"]["cell_id"],
        "adapter": {
            "id": "sample-file-adapter",
            "kind": "file-adapter",
            "version": "v1alpha1",
        },
        "status": "completed",
        "final_answer": {
            "present": True,
            "path": request["expected_output"]["final_answer_path"],
            "summary": "Deterministic sample adapter answer.",
        },
        "usage": {
            "input_tokens": 0,
            "cached_input_tokens": 0,
            "output_tokens": 0,
            "reasoning_output_tokens": 0,
            "estimated_cost_usd": 0,
        },
        "counts": {
            "tool_call_count": 0,
            "artifact_count": 1,
            "trace_event_count": 1,
        },
        "timing": {
            "elapsed_milliseconds": 1,
            "timed_out": False,
        },
        "process": {
            "exit_code": 0,
            "stdout_path": "stdout.raw.txt",
            "stderr_path": "stderr.txt",
        },
        "trace": [
            {
                "kind": "adapter.message",
                "message": "sample adapter generated deterministic answer",
                "path": request["expected_output"]["final_answer_path"],
            }
        ],
        "artifacts": [
            {
                "kind": "answer",
                "path": request["expected_output"]["final_answer_path"],
                "sha256": sha256(answer_path),
            }
        ],
        "integrity_warnings": [],
        "diagnostics": [],
    }
    evidence_path.write_text(json.dumps(evidence, indent=2) + "\n", encoding="utf-8")
    return 0


def require_env(name: str) -> str:
    value = os.environ.get(name)
    if not value:
        raise RuntimeError(f"{name} is required")
    return value


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    digest.update(path.read_bytes())
    return digest.hexdigest()


if __name__ == "__main__":
    raise SystemExit(main())
