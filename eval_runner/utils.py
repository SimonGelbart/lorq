from __future__ import annotations

import json
import os
import shutil
import subprocess
import time
from pathlib import Path
from typing import Any


def ensure_dir(path: Path) -> Path:
    path.mkdir(parents=True, exist_ok=True)
    return path


def rm_rf(path: Path) -> None:
    if not path.exists() and not path.is_symlink():
        return
    if path.is_symlink() or path.is_file():
        path.unlink()
    else:
        shutil.rmtree(path)




SENSITIVE_KEY_HINTS = ("TOKEN", "SECRET", "KEY", "PASSWORD", "PASS", "CREDENTIAL")

def _sensitive_env_values() -> list[str]:
    values: list[str] = []
    for key, value in os.environ.items():
        upper = key.upper()
        if value and len(value) >= 8 and any(hint in upper for hint in SENSITIVE_KEY_HINTS):
            values.append(value)
    # Longest first avoids partially redacting overlapping values.
    return sorted(set(values), key=len, reverse=True)

def redact_secrets(text: str | None) -> str:
    if text is None:
        return ""
    out = str(text)
    for value in _sensitive_env_values():
        out = out.replace(value, "[REDACTED]")
    # Redact common KEY=value / KEY: value fragments that may not match env values.
    import re
    out = re.sub(r'(?i)\b([A-Z0-9_]*(?:TOKEN|SECRET|PASSWORD|API_KEY|ACCESS_KEY|PRIVATE_KEY|CREDENTIAL)[A-Z0-9_]*)\s*[:=]\s*([^\s,;]+)', r'\1=[REDACTED]', out)
    return out

def redact_data(value: Any) -> Any:
    if isinstance(value, str):
        return redact_secrets(value)
    if isinstance(value, list):
        return [redact_data(item) for item in value]
    if isinstance(value, tuple):
        return [redact_data(item) for item in value]
    if isinstance(value, dict):
        out = {}
        for key, item in value.items():
            key_str = str(key)
            if any(hint in key_str.upper() for hint in SENSITIVE_KEY_HINTS):
                out[key] = "[REDACTED]" if item not in (None, "") else item
            else:
                out[key] = redact_data(item)
        return out
    return value

def ensure_relative_inside(base: Path, candidate: Path, *, label: str = "path") -> Path:
    base_resolved = base.resolve()
    resolved = candidate.resolve()
    try:
        resolved.relative_to(base_resolved)
    except ValueError as exc:
        raise ValueError(f"{label} escapes allowed root: {resolved} is not inside {base_resolved}") from exc
    return resolved


def read_text(path: Path, default: str = "") -> str:
    try:
        return path.read_text(encoding="utf-8")
    except FileNotFoundError:
        return default


def write_text(path: Path, text: str) -> None:
    ensure_dir(path.parent)
    path.write_text(redact_secrets(text), encoding="utf-8")



def read_json(path: Path, default: Any = None) -> Any:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError:
        return default


def write_json(path: Path, data: Any) -> None:
    ensure_dir(path.parent)
    path.write_text(json.dumps(redact_data(data), indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def slug(value: str) -> str:
    allowed = []
    for ch in value.strip().lower():
        if ch.isalnum():
            allowed.append(ch)
        elif ch in {"-", "_", "."}:
            allowed.append(ch)
        elif ch.isspace() or ch in {"/", ":", ","}:
            allowed.append("-")
    out = "".join(allowed).strip("-._")
    while "--" in out:
        out = out.replace("--", "-")
    return out or "run"


def run_command(
    command: str | list[str],
    cwd: Path,
    timeout_seconds: int | None = None,
    env: dict[str, str] | None = None,
    input_text: str | None = None,
    shell: bool | None = None,
) -> dict[str, Any]:
    start = time.time()
    merged_env = os.environ.copy()
    if env:
        merged_env.update({str(k): str(v) for k, v in env.items()})

    if shell is None:
        shell = isinstance(command, str)

    try:
        proc = subprocess.run(
            command,
            cwd=str(cwd),
            input=input_text,
            text=True,
            capture_output=True,
            timeout=timeout_seconds,
            shell=shell,
            env=merged_env,
        )
        timed_out = False
        return redact_data({
            "command": command,
            "cwd": str(cwd),
            "exit_code": proc.returncode,
            "timed_out": timed_out,
            "elapsed_ms": int((time.time() - start) * 1000),
            "stdout": proc.stdout,
            "stderr": proc.stderr,
        })
    except subprocess.TimeoutExpired as exc:
        return redact_data({
            "command": command,
            "cwd": str(cwd),
            "exit_code": None,
            "timed_out": True,
            "elapsed_ms": int((time.time() - start) * 1000),
            "stdout": exc.stdout or "",
            "stderr": exc.stderr or f"Command timed out after {timeout_seconds}s",
        })
    except FileNotFoundError as exc:
        return redact_data({
            "command": command,
            "cwd": str(cwd),
            "exit_code": 127,
            "timed_out": False,
            "elapsed_ms": int((time.time() - start) * 1000),
            "stdout": "",
            "stderr": str(exc),
        })


def copy_path(src: Path, dst: Path, *, overwrite: bool = True) -> None:
    if not src.exists():
        raise FileNotFoundError(f"Cannot copy missing path: {src}")
    if overwrite:
        rm_rf(dst)
    ensure_dir(dst.parent)
    if src.is_dir():
        shutil.copytree(src, dst)
    else:
        shutil.copy2(src, dst)


def is_git_repo(path: Path) -> bool:
    result = run_command(["git", "-C", str(path), "rev-parse", "--is-inside-work-tree"], cwd=path, shell=False)
    return result["exit_code"] == 0 and result["stdout"].strip() == "true"
