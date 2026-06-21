#!/usr/bin/env python3
"""LORQ Codex CLI file-adapter smoke wrapper.

This wrapper is intentionally small and local-only. It adapts a Codex CLI
one-shot invocation to LORQ's file adapter protocol without importing any LORQ
runtime code or making LORQ core depend on Codex.
"""

from __future__ import annotations

import hashlib
import json
import os
import pathlib
import subprocess
import sys
import time
from typing import Any


EVIDENCE_SCHEMA_VERSION = "lorq.file-adapter-evidence.v1alpha1"
CONTRACT_VERSION = "lorq.contract.v1alpha1"


def main() -> int:
    request_path = required_env("LORQ_ADAPTER_REQUEST")
    evidence_path = required_env("LORQ_ADAPTER_EVIDENCE")
    with open(request_path, "r", encoding="utf-8") as handle:
        request = json.load(handle)

    evidence_directory = pathlib.Path(request["workspace"]["evidence_directory"])
    evidence_directory.mkdir(parents=True, exist_ok=True)
    artifacts_directory = pathlib.Path(request["workspace"]["artifacts_directory"])
    artifacts_directory.mkdir(parents=True, exist_ok=True)

    started = time.monotonic()
    stdout_path = evidence_directory / "stdout.raw.txt"
    stderr_path = evidence_directory / "stderr.txt"
    answer_path = evidence_directory / request["expected_output"]["final_answer_path"]

    command = codex_command(request)
    process = subprocess.run(
        command,
        input=request["task"]["prompt_text"],
        text=True,
        capture_output=True,
        cwd=request["workspace"]["root"],
        check=False,
    )

    stdout_path.write_text(process.stdout, encoding="utf-8")
    stderr_path.write_text(process.stderr, encoding="utf-8")
    answer_text = process.stdout.strip()
    if answer_text:
        answer_path.write_text(answer_text + "\n", encoding="utf-8")

    elapsed = int((time.monotonic() - started) * 1000)
    evidence = evidence_for(request, process, answer_text, elapsed)
    with open(evidence_path, "w", encoding="utf-8") as handle:
        json.dump(evidence, handle, indent=2, sort_keys=True)
        handle.write("\n")

    return process.returncode


def required_env(name: str) -> str:
    value = os.environ.get(name)
    if not value:
        raise RuntimeError(f"{name} is required")
    return value


def codex_command(request: dict[str, Any]) -> list[str]:
    command = os.environ.get("LORQ_CODEX_COMMAND", "codex")
    arguments = os.environ.get("LORQ_CODEX_ARGUMENTS", "exec\n--json").splitlines()
    return [command, *[argument for argument in arguments if argument], request["task"]["prompt_path"]]


def evidence_for(request: dict[str, Any], process: subprocess.CompletedProcess[str], answer_text: str, elapsed_ms: int) -> dict[str, Any]:
    completed = process.returncode == 0 and bool(answer_text)
    status = "completed" if completed else "adapter_failed"
    answer_path = request["expected_output"]["final_answer_path"]
    return {
        "schema_version": EVIDENCE_SCHEMA_VERSION,
        "contract_version": CONTRACT_VERSION,
        "cell_id": request["cell"]["cell_id"],
        "adapter": {
            "id": "codex-cli-smoke-wrapper",
            "kind": "file-adapter",
            "version": "v1alpha1",
            "runtime": {
                "provider": "openai",
                "runtime": "codex-cli",
                "profile": os.environ.get("LORQ_ADAPTER_PROFILE", "codex-cli"),
                "command": os.environ.get("LORQ_CODEX_COMMAND", "codex"),
                "permission_profile": os.environ.get("LORQ_CODEX_PERMISSION_PROFILE", "local-smoke"),
                "output_format": os.environ.get("LORQ_CODEX_OUTPUT_FORMAT", "codex-jsonl"),
                "extensions": {},
            },
        },
        "status": status,
        "final_answer": {
            "present": completed,
            "path": answer_path,
            "summary": answer_text[:120],
        },
        "usage": {
            "input_tokens": 0,
            "cached_input_tokens": 0,
            "output_tokens": 0,
            "tool_call_count": 0,
            "estimated_cost_usd": 0,
        },
        "counts": {
            "tool_call_count": 0,
            "artifact_count": 1 if completed else 0,
            "trace_event_count": 1,
        },
        "timing": {
            "elapsed_milliseconds": elapsed_ms,
            "timed_out": False,
        },
        "process": {
            "exit_code": process.returncode,
            "stdout_path": "stdout.raw.txt",
            "stderr_path": "stderr.txt",
        },
        "trace": [
            {
                "kind": "runtime.command",
                "message": "codex cli smoke invocation",
                "path": None,
            }
        ],
        "artifacts": artifact_list(answer_path, answer_text, completed),
        "integrity_warnings": [],
        "diagnostics": diagnostics(process, completed),
    }


def artifact_list(answer_path: str, answer_text: str, completed: bool) -> list[dict[str, str]]:
    if not completed:
        return []
    return [{"kind": "answer", "path": answer_path, "sha256": hashlib.sha256((answer_text + "\n").encode("utf-8")).hexdigest()}]


def diagnostics(process: subprocess.CompletedProcess[str], completed: bool) -> list[dict[str, str]]:
    if completed:
        return []
    return [{"code": "LORQ-ADAPTER-FAILED", "severity": "critical", "message": f"Codex CLI wrapper exited with code {process.returncode}."}]


if __name__ == "__main__":
    sys.exit(main())
