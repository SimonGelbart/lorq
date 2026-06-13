from __future__ import annotations

import asyncio
import importlib.util
import json
import os
import shlex
import shutil
import time
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

from .utils import run_command, write_json, write_text
from .events import canonical_event_type, summarize_normalized_events, normalize_copilot_sdk_jsonl, write_jsonl
from .pricing import normalize_usage


CODEX_JSONL_FORMATS = {"codex-jsonl", "jsonl", "codex"}
TEXT_FORMATS = {"text", "plain", "stdout"}
COPILOT_SDK_FORMATS = {"copilot-sdk-events", "copilot-sdk-jsonl", "github-copilot-sdk"}


def _walk_json(value: Any):
    if isinstance(value, dict):
        yield value
        for child in value.values():
            yield from _walk_json(child)
    elif isinstance(value, list):
        for child in value:
            yield from _walk_json(child)


def _jsonable(value: Any, *, depth: int = 0) -> Any:
    """Best-effort JSON serialization for SDK event objects.

    The GitHub Copilot Python SDK uses typed event/data objects. Their exact
    classes can evolve, so we avoid relying on a fixed schema and preserve any
    public attributes that are JSON-like. This keeps trace validators useful when
    events contain tool-call arguments such as shell commands.
    """
    if depth > 4:
        return repr(value)
    if value is None or isinstance(value, (str, int, float, bool)):
        return value
    if isinstance(value, Path):
        return str(value)
    if isinstance(value, dict):
        return {str(k): _jsonable(v, depth=depth + 1) for k, v in value.items()}
    if isinstance(value, (list, tuple, set)):
        return [_jsonable(v, depth=depth + 1) for v in value]
    if hasattr(value, "model_dump"):
        try:
            return _jsonable(value.model_dump(), depth=depth + 1)
        except Exception:
            pass
    if hasattr(value, "dict"):
        try:
            return _jsonable(value.dict(), depth=depth + 1)
        except Exception:
            pass
    attrs: dict[str, Any] = {}
    if hasattr(value, "__dict__"):
        for key, item in vars(value).items():
            if not key.startswith("_"):
                attrs[key] = _jsonable(item, depth=depth + 1)
    if attrs:
        attrs.setdefault("_repr", repr(value))
        return attrs
    return repr(value)


def _first_text_from_value(value: Any) -> str | None:
    """Find likely assistant text fields in an SDK event payload."""
    if isinstance(value, str):
        return value if value.strip() else None
    if isinstance(value, dict):
        for key in ("content", "text", "message", "markdown", "value"):
            item = value.get(key)
            if isinstance(item, str) and item.strip():
                return item
        for child in value.values():
            found = _first_text_from_value(child)
            if found:
                return found
    if isinstance(value, list):
        parts = [part for part in (_first_text_from_value(item) for item in value) if part]
        if parts:
            return "".join(parts)
    return None


def extract_codex_answer(stdout: str) -> str:
    """Best-effort extraction from `codex exec --json`.

    Codex JSON event shapes have changed over time, so this intentionally accepts
    several known variants and falls back to raw stdout when needed.
    """
    messages: list[str] = []
    raw_non_json: list[str] = []

    for line in stdout.splitlines():
        if not line.strip():
            continue
        try:
            event = json.loads(line)
        except json.JSONDecodeError:
            raw_non_json.append(line)
            continue

        for obj in _walk_json(event):
            typ = str(obj.get("type") or obj.get("item_type") or "")
            if typ in {"agent_message", "assistant_message", "message", "final_answer"}:
                for key in ("message", "text", "content", "final_answer", "answer"):
                    val = obj.get(key)
                    if isinstance(val, str) and val.strip():
                        messages.append(val.strip())
                content = obj.get("content")
                if isinstance(content, list):
                    chunks = []
                    for item in content:
                        if isinstance(item, dict):
                            text = item.get("text") or item.get("content")
                            if isinstance(text, str):
                                chunks.append(text)
                        elif isinstance(item, str):
                            chunks.append(item)
                    if chunks:
                        messages.append("".join(chunks).strip())

    if messages:
        return messages[-1].strip() + "\n"
    if raw_non_json:
        return "\n".join(raw_non_json).strip() + "\n"
    return stdout.strip() + "\n"


def extract_text_answer(stdout: str, stderr: str = "") -> str:
    """Extract an answer from text-oriented CLIs such as GitHub Copilot CLI.

    We keep this intentionally conservative: the evaluator should preserve the
    raw transcript and treat stdout as the answer unless the CLI writes nothing.
    """
    if stdout.strip():
        return stdout.strip() + "\n"
    if stderr.strip():
        return stderr.strip() + "\n"
    return ""


def extract_copilot_sdk_answer(stdout: str, stderr: str = "") -> str:
    """Extract final assistant text from GitHub Copilot SDK JSONL events.

    The SDK streaming-events docs state that `assistant.message` is the complete
    response and is emitted regardless of streaming. Prefer that final event to
    avoid duplicating `assistant.message_delta` chunks.
    """
    final_messages: list[str] = []
    delta_parts: list[str] = []
    for line in stdout.splitlines():
        try:
            event = json.loads(line)
        except json.JSONDecodeError:
            continue
        event_type = canonical_event_type(event.get("type")).lower()
        data_type = str(event.get("data_type") or "")
        data = event.get("data")
        text = event.get("assistant_text") or _first_text_from_value(data)
        if not isinstance(text, str) or not text.strip():
            continue
        if event_type.endswith("assistant.message") or data_type == "AssistantMessageData":
            final_messages.append(text.strip())
        elif event_type.endswith("assistant.message_delta") or data_type == "AssistantMessageDeltaData":
            delta_parts.append(text)
    if final_messages:
        return "\n".join(final_messages).strip() + "\n"
    if delta_parts:
        return "".join(delta_parts).strip() + "\n"
    return extract_text_answer(stdout, stderr)


def extract_answer(stdout: str, stderr: str = "", output_format: str = "codex-jsonl") -> str:
    fmt = (output_format or "text").lower()
    if fmt in CODEX_JSONL_FORMATS:
        return extract_codex_answer(stdout)
    if fmt in COPILOT_SDK_FORMATS:
        return extract_copilot_sdk_answer(stdout, stderr)
    if fmt in TEXT_FORMATS:
        return extract_text_answer(stdout, stderr)
    # Unknown formats fall back to text to avoid losing output.
    return extract_text_answer(stdout, stderr)


def extract_usage_and_counts(stdout: str, output_format: str = "codex-jsonl") -> dict[str, Any]:
    usage: dict[str, Any] = {}
    counts = {"json_events": 0, "tool_events": 0, "command_events": 0}

    if (output_format or "").lower() not in (CODEX_JSONL_FORMATS | COPILOT_SDK_FORMATS):
        return {"usage": normalize_usage(usage), "counts": counts}

    def update_usage(obj: dict[str, Any]) -> None:
        keys = set(obj.keys())
        if {"input_tokens", "output_tokens"} & keys or "total_tokens" in keys:
            for key in ("input_tokens", "output_tokens", "total_tokens", "cached_input_tokens", "reasoning_output_tokens"):
                if key in obj and isinstance(obj[key], int):
                    usage[key] = obj[key]

    for line in stdout.splitlines():
        try:
            event = json.loads(line)
        except json.JSONDecodeError:
            continue
        counts["json_events"] += 1
        for obj in _walk_json(event):
            if not isinstance(obj, dict):
                continue
            typ = str(obj.get("type") or obj.get("item_type") or obj.get("data_type") or "").lower()
            if "tool" in typ:
                counts["tool_events"] += 1
            if "command" in typ or "exec" in typ or obj.get("cmd") or obj.get("command"):
                counts["command_events"] += 1
            update_usage(obj)
    return {"usage": usage, "counts": counts}


def split_args(value: str | list[str] | None, default: list[str]) -> list[str]:
    if value is None:
        return list(default)
    if isinstance(value, list):
        return [str(item) for item in value]
    if not value.strip():
        return list(default)
    return shlex.split(value)



def classify_process_result(result: dict[str, Any]) -> str | None:
    """Return a stable failure category for process-like results."""
    if result.get("timed_out"):
        return "timeout"
    exit_code = result.get("exit_code")
    if exit_code in {None, 0}:
        return None
    if exit_code == 127:
        return "command_not_found"
    return "nonzero_exit"


def _which(command: str) -> str | None:
    """Resolve an executable command without invoking a shell."""
    if not command:
        return None
    parts = shlex.split(command) if isinstance(command, str) else [str(command)]
    executable = parts[0] if parts else command
    if any(sep in executable for sep in ("/", "\\")):
        candidate = Path(executable).expanduser()
        return str(candidate) if candidate.exists() else None
    return shutil.which(executable)


@dataclass
class CliAgent:
    """Generic local CLI agent adapter.

    input_mode:
      - stdin: pass the rendered prompt on stdin.
      - argument: place the prompt into args via {prompt}; if no placeholder is
        present, append prompt_arg and the prompt, or append the prompt directly.
      - none: do not pass the prompt automatically.

    output_format:
      - codex-jsonl: parse Codex JSONL events and token usage.
      - text: preserve stdout as answer.
    """

    backend_id: str = "generic"
    command: str = "codex"
    args: list[str] = field(default_factory=lambda: ["exec", "--json"])
    timeout_seconds: int | None = None
    input_mode: str = "stdin"
    prompt_arg: str | None = None
    output_format: str = "codex-jsonl"
    shell: bool = False
    env: dict[str, str] | None = None
    availability_args: list[str] = field(default_factory=lambda: ["--version"])
    availability_timeout_seconds: int = 10

    def check_availability(self, cwd: Path | None = None) -> dict[str, Any]:
        """Check whether the local CLI backend is present and minimally invokable.

        This is intentionally lightweight and does not authenticate or run an eval.
        A version probe failure is reported separately from executable discovery.
        """
        resolved_path = _which(self.command)
        payload: dict[str, Any] = {
            "backend": self.backend_id,
            "command": self.command,
            "resolved_path": resolved_path,
            "available": bool(resolved_path),
            "ok": bool(resolved_path),
            "version_check": None,
        }
        if not resolved_path:
            payload["error_category"] = "command_not_found"
            payload["message"] = f"Command not found on PATH: {self.command}"
            return payload
        if self.availability_args:
            check_cmd: list[str] = [self.command, *self.availability_args]
            result = run_command(
                check_cmd,
                cwd=cwd or Path.cwd(),
                timeout_seconds=self.availability_timeout_seconds,
                shell=False,
                env=self.env,
            )
            payload["version_check"] = {
                "command": check_cmd,
                "exit_code": result.get("exit_code"),
                "timed_out": result.get("timed_out"),
                "elapsed_ms": result.get("elapsed_ms"),
                "stdout_preview": (result.get("stdout") or "")[:500],
                "stderr_preview": (result.get("stderr") or "")[:500],
            }
            failure = classify_process_result(result)
            # Some CLIs return non-zero for --version when auth is absent or the
            # command shape differs. Treat discovery as available but surface the
            # probe failure so users can adjust availability_args.
            payload["version_check"]["ok"] = failure is None
            payload["version_check"]["error_category"] = failure
        return payload

    def _render_command(self, prompt: str) -> tuple[list[str] | str, str | None, list[str] | str]:
        input_mode = (self.input_mode or "stdin").lower()
        args = list(self.args)
        summary_args = list(self.args)
        input_text: str | None = None

        if input_mode in {"stdin", "pipe"}:
            input_text = prompt
        elif input_mode in {"argument", "arg", "prompt-arg"}:
            inserted = False
            rendered_args: list[str] = []
            rendered_summary: list[str] = []
            for arg in args:
                if "{prompt}" in arg:
                    rendered_args.append(arg.replace("{prompt}", prompt))
                    rendered_summary.append(arg.replace("{prompt}", "<PROMPT>"))
                    inserted = True
                else:
                    rendered_args.append(arg)
                    rendered_summary.append(arg)
            if not inserted:
                if self.prompt_arg:
                    rendered_args.extend([self.prompt_arg, prompt])
                    rendered_summary.extend([self.prompt_arg, "<PROMPT>"])
                else:
                    rendered_args.append(prompt)
                    rendered_summary.append("<PROMPT>")
            args = rendered_args
            summary_args = rendered_summary
        elif input_mode == "none":
            pass
        else:
            raise ValueError(f"Unsupported input_mode: {self.input_mode!r}")

        cmd: list[str] | str
        summary_cmd: list[str] | str
        if self.shell:
            cmd = " ".join([shlex.quote(self.command), *(shlex.quote(part) for part in args)])
            summary_cmd = " ".join([shlex.quote(self.command), *(shlex.quote(part) for part in summary_args)])
        else:
            cmd = [self.command, *args]
            summary_cmd = [self.command, *summary_args]
        return cmd, input_text, summary_cmd

    def run(self, worktree: Path, prompt: str, output_dir: Path) -> dict[str, Any]:
        write_text(output_dir / "prompt.txt", prompt)
        cmd, input_text, summary_cmd = self._render_command(prompt)
        start = time.time()
        result = run_command(
            cmd,
            cwd=worktree,
            timeout_seconds=self.timeout_seconds,
            input_text=input_text,
            shell=self.shell,
            env=self.env,
        )
        elapsed_ms = int((time.time() - start) * 1000)

        stdout = result["stdout"]
        stderr = result["stderr"]
        answer = extract_answer(stdout, stderr, self.output_format)
        usage_counts = extract_usage_and_counts(stdout, self.output_format)

        # Keep both names so existing trace/report code can continue to read
        # stdout.raw.jsonl while text-oriented agents remain easy to inspect.
        write_text(output_dir / "stdout.raw.jsonl", stdout)
        write_text(output_dir / "stdout.raw.txt", stdout)
        write_text(output_dir / "stderr.txt", stderr)
        write_text(output_dir / "answer.md", answer)

        failure_category = classify_process_result(result)
        summary = {
            "agent": self.backend_id,
            "backend": self.backend_id,
            "command": summary_cmd,
            "input_mode": self.input_mode,
            "output_format": self.output_format,
            "exit_code": result["exit_code"],
            "timed_out": result["timed_out"],
            "elapsed_ms": elapsed_ms,
            "ok": failure_category is None,
            "error_category": failure_category,
            "usage": usage_counts["usage"],
            "counts": usage_counts["counts"],
        }
        write_json(output_dir / "agent.summary.json", summary)
        return summary


class CodexCliAgent(CliAgent):
    def __init__(self, command: str = "codex", args: list[str] | None = None, timeout_seconds: int | None = None, availability_args: list[str] | None = None) -> None:
        super().__init__(
            backend_id="codex-cli",
            command=command,
            args=args or ["exec", "--json"],
            timeout_seconds=timeout_seconds,
            input_mode="stdin",
            output_format="codex-jsonl",
            availability_args=availability_args or ["--version"],
        )


class GitHubCopilotCliAgent(CliAgent):
    def __init__(
        self,
        command: str = "copilot",
        args: list[str] | None = None,
        timeout_seconds: int | None = None,
        input_mode: str = "argument",
        output_format: str = "text",
        env: dict[str, str] | None = None,
        availability_args: list[str] | None = None,
    ) -> None:
        super().__init__(
            backend_id="github-copilot-cli",
            command=command,
            args=args or ["--prompt", "{prompt}"],
            timeout_seconds=timeout_seconds,
            input_mode=input_mode,
            prompt_arg="--prompt",
            output_format=output_format,
            env=env,
            availability_args=availability_args or ["--version"],
        )


@dataclass
class GitHubCopilotSdkAgent:
    """GitHub Copilot Python SDK adapter.

    This backend uses `github-copilot-sdk`, which controls GitHub Copilot CLI via
    JSON-RPC. It is preferred over the plain text `copilot --prompt` profile when
    you want a programmatic session, event capture, model selection, and explicit
    permission handling.

    The dependency is optional and imported lazily so Codex-only users do not
    need it installed.
    """

    model: str = "gpt-5"
    reasoning_effort: str | None = None
    timeout_seconds: int | None = 1200
    permission_policy: str = "approve_all"
    base_directory: str | None = None
    github_token_env: str | None = "GITHUB_TOKEN"
    github_token: str | None = None
    use_logged_in_user: bool | None = None
    log_level: str = "info"
    session_idle_timeout_seconds: int | None = None
    output_format: str = "copilot-sdk-events"
    backend_id: str = "github-copilot-sdk"

    def check_availability(self, cwd: Path | None = None) -> dict[str, Any]:
        """Check that the optional Copilot SDK package is importable.

        Authentication and permission checks happen during a real session and are
        intentionally not performed here to keep `--check-agent` cheap.
        """
        spec = importlib.util.find_spec("copilot")
        payload: dict[str, Any] = {
            "backend": self.backend_id,
            "package": "github-copilot-sdk",
            "module": "copilot",
            "available": spec is not None,
            "ok": spec is not None,
            "model": self.model,
            "permission_policy": self.permission_policy,
        }
        if spec is None:
            payload["error_category"] = "missing_python_package"
            payload["message"] = "GitHub Copilot SDK is not installed. Install with: pip install github-copilot-sdk"
        else:
            payload["origin"] = spec.origin
        if self.github_token_env:
            payload["github_token_env"] = self.github_token_env
            payload["github_token_env_present"] = bool(os.environ.get(self.github_token_env))
        return payload

    def _format_path_template(self, template: str | None, worktree: Path, output_dir: Path) -> str | None:
        if not template:
            return None
        return template.format(
            worktree=str(worktree),
            run_dir=str(output_dir),
            output_dir=str(output_dir),
        )

    async def _run_async(self, worktree: Path, prompt: str, output_dir: Path) -> dict[str, Any]:
        try:
            from copilot import CopilotClient
            from copilot.session import PermissionHandler
        except ModuleNotFoundError as exc:  # pragma: no cover - depends on local install
            if exc.name == "copilot":
                raise RuntimeError(
                    "GitHub Copilot SDK is not installed. Install it with: "
                    "pip install github-copilot-sdk"
                ) from exc
            raise

        done = asyncio.Event()
        events: list[dict[str, Any]] = []
        final_messages: list[str] = []
        delta_parts: list[str] = []

        def on_event(event: Any) -> None:
            event_type = canonical_event_type(getattr(event, "type", type(event).__name__))
            data = getattr(event, "data", None)
            data_type = type(data).__name__ if data is not None else None
            data_json = _jsonable(data)
            assistant_text: str | None = None
            lower_type = event_type.lower()
            if data_type in {"AssistantMessageData", "AssistantMessageDeltaData", "AssistantReasoningData", "AssistantReasoningDeltaData"} or lower_type.startswith("assistant."):
                assistant_text = _first_text_from_value(data_json)
                if assistant_text and (lower_type.endswith("assistant.message") or data_type == "AssistantMessageData"):
                    final_messages.append(assistant_text)
                elif assistant_text and (lower_type.endswith("assistant.message_delta") or data_type == "AssistantMessageDeltaData"):
                    delta_parts.append(assistant_text)
            events.append({
                "type": event_type,
                "data_type": data_type,
                "id": getattr(event, "id", None),
                "timestamp": getattr(event, "timestamp", None),
                "parent_id": getattr(event, "parent_id", getattr(event, "parentId", None)),
                "ephemeral": getattr(event, "ephemeral", None),
                "data": data_json,
                "assistant_text": assistant_text,
            })
            if data_type == "SessionIdleData" or lower_type.endswith("session.idle") or "idle" in lower_type:
                done.set()

        base_directory = self._format_path_template(self.base_directory, worktree, output_dir)
        if not base_directory:
            base_directory = str(output_dir / ".copilot-home")
        Path(base_directory).mkdir(parents=True, exist_ok=True)

        client_kwargs: dict[str, Any] = {
            "working_directory": str(worktree),
            "base_directory": base_directory,
            "log_level": self.log_level,
        }
        token = self.github_token
        if not token and self.github_token_env:
            token = os.environ.get(self.github_token_env)
        if token:
            client_kwargs["github_token"] = token
        if self.use_logged_in_user is not None:
            client_kwargs["use_logged_in_user"] = self.use_logged_in_user
        if self.session_idle_timeout_seconds is not None:
            client_kwargs["session_idle_timeout_seconds"] = self.session_idle_timeout_seconds

        session_kwargs: dict[str, Any] = {}
        if self.model:
            session_kwargs["model"] = self.model
        if self.reasoning_effort:
            session_kwargs["reasoning_effort"] = self.reasoning_effort
        if (self.permission_policy or "").lower() == "approve_all":
            session_kwargs["on_permission_request"] = PermissionHandler.approve_all
        elif (self.permission_policy or "").lower() in {"none", "manual", "event"}:
            pass
        else:
            raise ValueError(
                f"Unsupported Copilot SDK permission_policy={self.permission_policy!r}. "
                "Use 'approve_all' for non-interactive evals or 'manual' to leave permission requests pending."
            )

        async with CopilotClient(**client_kwargs) as client:
            async with await client.create_session(**session_kwargs) as session:
                session.on(on_event)
                await session.send(prompt)
                await done.wait()

        return {
            "events": events,
            "assistant_text": ("\n".join(final_messages).strip() or "".join(delta_parts).strip()),
            "client": {
                "model": self.model,
                "reasoning_effort": self.reasoning_effort,
                "permission_policy": self.permission_policy,
                "base_directory": base_directory,
                "use_logged_in_user": self.use_logged_in_user,
            },
        }

    def run(self, worktree: Path, prompt: str, output_dir: Path) -> dict[str, Any]:
        write_text(output_dir / "prompt.txt", prompt)
        start = time.time()
        stdout = ""
        stderr = ""
        exit_code = 0
        timed_out = False
        payload: dict[str, Any] = {"events": [], "assistant_text": "", "client": {}}
        try:
            if self.timeout_seconds:
                payload = asyncio.run(asyncio.wait_for(self._run_async(worktree, prompt, output_dir), timeout=self.timeout_seconds))
            else:
                payload = asyncio.run(self._run_async(worktree, prompt, output_dir))
            stdout = "\n".join(json.dumps(event, ensure_ascii=False) for event in payload.get("events", [])) + "\n"
        except asyncio.TimeoutError:
            timed_out = True
            exit_code = 124
            stderr = f"GitHub Copilot SDK session timed out after {self.timeout_seconds} seconds.\n"
        except Exception as exc:  # pragma: no cover - depends on external SDK/auth/runtime
            exit_code = 127 if "github-copilot-sdk" in str(exc) or "Copilot SDK is not installed" in str(exc) else 1
            stderr = f"{type(exc).__name__}: {exc}\n"

        elapsed_ms = int((time.time() - start) * 1000)
        answer = payload.get("assistant_text") or extract_answer(stdout, stderr, self.output_format)
        if answer and not answer.endswith("\n"):
            answer += "\n"
        usage_counts = extract_usage_and_counts(stdout, self.output_format)
        failure_category = "timeout" if timed_out else (None if exit_code == 0 else ("missing_python_package" if exit_code == 127 else "runtime_error"))

        write_text(output_dir / "stdout.raw.jsonl", stdout)
        write_text(output_dir / "stdout.raw.txt", payload.get("assistant_text", "") or stdout)
        write_text(output_dir / "copilot.events.jsonl", stdout)
        normalized_events = normalize_copilot_sdk_jsonl(output_dir / "stdout.raw.jsonl")
        write_jsonl(output_dir / "events.normalized.jsonl", normalized_events)
        write_json(output_dir / "events.summary.json", summarize_normalized_events(normalized_events))
        write_text(output_dir / "stderr.txt", stderr)
        write_text(output_dir / "answer.md", answer)

        summary = {
            "agent": self.backend_id,
            "backend": self.backend_id,
            "command": "github-copilot-sdk CopilotClient.create_session",
            "input_mode": "sdk-session",
            "output_format": self.output_format,
            "exit_code": exit_code,
            "timed_out": timed_out,
            "elapsed_ms": elapsed_ms,
            "ok": failure_category is None,
            "error_category": failure_category,
            "usage": usage_counts["usage"],
            "counts": usage_counts["counts"],
            "copilot_sdk": payload.get("client", {}),
        }
        write_json(output_dir / "agent.summary.json", summary)
        return summary


def create_agent(profile: dict[str, Any]) -> CliAgent | GitHubCopilotSdkAgent:
    backend = str(profile.get("backend") or profile.get("type") or "codex").lower()
    command = str(profile.get("command") or ("copilot" if backend in {"copilot", "github-copilot-cli"} else "codex"))
    # Special portable placeholder for bundled fake/local Python agents.
    # This avoids assuming a `python` executable exists on PATH; on many systems
    # the active interpreter is exposed as `python3` only.
    if command == "{python}":
        command = sys.executable
    args = split_args(profile.get("args"), ["exec", "--json"] if backend in {"codex", "codex-cli"} else ["--prompt", "{prompt}"])
    args = [sys.executable if arg == "{python}" else arg for arg in args]
    timeout = profile.get("timeout_seconds")
    timeout_seconds = int(timeout) if timeout is not None else None
    env = profile.get("env") if isinstance(profile.get("env"), dict) else None
    availability_args = split_args(profile.get("availability_args"), ["--version"])
    availability_timeout_seconds = int(profile.get("availability_timeout_seconds") or 10)

    if backend in {"codex", "codex-cli"}:
        return CliAgent(
            backend_id="codex-cli",
            command=command,
            args=args,
            timeout_seconds=timeout_seconds,
            input_mode=str(profile.get("input_mode") or "stdin"),
            output_format=str(profile.get("output_format") or "codex-jsonl"),
            env=env,
            availability_args=availability_args,
            availability_timeout_seconds=availability_timeout_seconds,
        )
    if backend in {"copilot", "github-copilot", "github-copilot-cli"}:
        agent = GitHubCopilotCliAgent(
            command=command,
            args=args,
            timeout_seconds=timeout_seconds,
            input_mode=str(profile.get("input_mode") or "argument"),
            output_format=str(profile.get("output_format") or "text"),
            env=env,
            availability_args=availability_args,
        )
        agent.availability_timeout_seconds = availability_timeout_seconds
        return agent
    if backend in {"copilot-sdk", "github-copilot-sdk", "github-copilot-python-sdk"}:
        return GitHubCopilotSdkAgent(
            model=str(profile.get("model") or "gpt-5"),
            reasoning_effort=profile.get("reasoning_effort"),
            timeout_seconds=timeout_seconds or 1200,
            permission_policy=str(profile.get("permission_policy") or "approve_all"),
            base_directory=profile.get("base_directory"),
            github_token_env=profile.get("github_token_env", "GITHUB_TOKEN"),
            github_token=profile.get("github_token"),
            use_logged_in_user=profile.get("use_logged_in_user"),
            log_level=str(profile.get("log_level") or "info"),
            session_idle_timeout_seconds=profile.get("session_idle_timeout_seconds"),
            output_format=str(profile.get("output_format") or "copilot-sdk-events"),
        )
    return CliAgent(
        backend_id=str(profile.get("id") or backend or "generic-cli"),
        command=command,
        args=args,
        timeout_seconds=timeout_seconds,
        input_mode=str(profile.get("input_mode") or "stdin"),
        prompt_arg=profile.get("prompt_arg"),
        output_format=str(profile.get("output_format") or "text"),
        shell=bool(profile.get("shell", False)),
        env=env,
        availability_args=availability_args,
        availability_timeout_seconds=availability_timeout_seconds,
    )



def check_agent_availability(profile: dict[str, Any], *, cwd: Path | None = None) -> dict[str, Any]:
    """Create an agent from a profile and run its cheap availability check."""
    agent = create_agent(profile)
    check = agent.check_availability(cwd=cwd) if hasattr(agent, "check_availability") else {"ok": True, "available": True, "backend": getattr(agent, "backend_id", "unknown")}
    check["profile_id"] = profile.get("id")
    check["backend_config"] = profile.get("backend") or profile.get("type")
    return check


def resolve_agent_profile(config: dict[str, Any], *, profile_name: str | None = None) -> tuple[str, dict[str, Any]]:
    profiles = config.get("agent_profiles") or config.get("agents") or {}
    agent_config = config.get("agent") or {}
    selected = profile_name or agent_config.get("profile")

    if selected:
        if selected not in profiles:
            raise KeyError(f"Unknown agent profile {selected!r}. Available: {', '.join(sorted(profiles))}")
        profile = dict(profiles[selected] or {})
        profile.setdefault("id", selected)
        return selected, profile

    # Backwards compatibility with v0.1-v0.5 configs.
    profile = {
        "id": "inline-agent",
        "backend": agent_config.get("backend") or "codex",
        "command": agent_config.get("command") or "codex",
        "args": agent_config.get("args") or ["exec", "--json"],
        "timeout_seconds": agent_config.get("timeout_seconds"),
        "input_mode": agent_config.get("input_mode") or "stdin",
        "output_format": agent_config.get("output_format") or "codex-jsonl",
    }
    return "inline-agent", profile
