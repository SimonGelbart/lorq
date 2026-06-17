from __future__ import annotations

import argparse
import sys
from pathlib import Path
from typing import Any

from .cases import load_cases, select_cases
from .config import load_config
from .agents import check_agent_availability, create_agent, resolve_agent_profile, split_args
from .modes import load_modes, select_modes
from .judge import CodexCliJudge, DeterministicFakeJudge
from .prompts import load_prompt_style, render_prompt, validate_prompt_styles_dir
from .reports import compare_result_sets, explain_run_markdown, load_run_record_from_path, write_reports
from .repositories import inspect_repository, load_repositories, resolve_repository
from .rubrics import load_rubrics, resolve_rubric
from .lorq_package import LorqPackageError, export_lorq_run_shard, merge_lorq_run_shards
from .lifecycle import clean_results_root, clean_worktree_root, list_generated_worktrees, load_run_records, prune_git_worktrees, remove_execution_path, write_generated_marker, write_lifecycle_event
from .metadata import write_environment_files, write_run_snapshots
from .utils import ensure_dir, read_json, slug, write_json
from .validators import run_validators
from .worktrees import WorktreeManager, materialize_mode, run_pre_agent_commands
from .schema import SchemaError
from . import __version__
from .contracts import RESULT_SCHEMA_VERSION, with_schema
from .pricing import resolve_pricing_config, estimate_cost


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Generic agent skill/tool eval runner")
    parser.add_argument("--suite-root", default=".", help="Path to eval-suite root")
    parser.add_argument("--config", default="eval.config.yaml", help="Config file name relative to suite root")
    parser.add_argument("--repo", help="Repository id from config or local path")
    parser.add_argument("--modes", help="Comma-separated mode ids")
    parser.add_argument("--cases", help="Comma-separated case ids")
    parser.add_argument("--categories", help="Comma-separated category ids")
    parser.add_argument("--prompt-style", help="Prompt style name, e.g. neutral")
    parser.add_argument("--pricing-model", help="Enable cost estimates using this pricing model id from config pricing.rates")
    parser.add_argument("--pricing-file", help="YAML/JSON pricing file with pricing.rates; prices are per 1M tokens")
    parser.add_argument("--no-pricing", action="store_true", help="Disable cost estimates even if pricing is enabled in config")
    parser.add_argument("--repetitions", type=int, default=1)
    parser.add_argument("--worktree-root", help="Override worktree root")
    parser.add_argument("--out", help="Override results output root")
    parser.add_argument("--worktree-strategy", choices=["git-worktree", "copy", "clone"], help="Override worktree strategy")
    parser.add_argument("--dirty-policy", choices=["warn", "fail", "allow"], help="How to handle dirty local Git repositories: warn (default), fail, or allow")
    parser.add_argument("--setup-scope", choices=["none", "per-run", "per-case", "per-mode"], help="Override setup scope. Default is per-run. per-case is an alias for per-run; per-mode is deprecated and rejected for real runs.")
    parser.add_argument("--agent-profile", default=None, help="Agent profile id from agent_profiles in config, e.g. codex or copilot")
    parser.add_argument("--agent-backend", choices=["codex", "copilot", "copilot-sdk", "generic", "deterministic-fake"], default=None, help="Override agent backend type")
    parser.add_argument("--agent-model", default=None, help="Override SDK/model name for agent profiles that support it")
    parser.add_argument("--agent-reasoning-effort", default=None, help="Override reasoning effort for agent profiles that support it")
    parser.add_argument("--agent-permission-policy", default=None, help="Override permission policy for SDK agents, e.g. approve_all or manual")
    parser.add_argument("--list-agent-profiles", action="store_true", help="List configured agent profiles")
    parser.add_argument("--check-agent", action="store_true", help="Check selected agent backend availability and exit")
    parser.add_argument("--check-all-agents", action="store_true", help="Check all configured agent backend profiles and exit")
    parser.add_argument("--require-agent-available", action="store_true", help="Fail before running evals if the selected agent backend is unavailable")
    parser.add_argument("--agent-command", default=None, help="AI agent command, default from selected profile")
    parser.add_argument("--agent-args", default=None, help="Agent args as a shell-like string, default from config or 'exec --json'")
    parser.add_argument("--agent-timeout", type=int, default=None, help="Agent timeout seconds")
    parser.add_argument("--isolate-agent-home", dest="isolate_agent_home", action="store_true", default=None, help="Run CLI agents with per-run HOME/CODEX_HOME isolation")
    parser.add_argument("--no-isolate-agent-home", dest="isolate_agent_home", action="store_false", help="Disable per-run HOME/CODEX_HOME isolation even if enabled in the agent profile")
    parser.add_argument("--judge", dest="judge", action="store_true", default=None, help="Run optional LLM judge after deterministic validation")
    parser.add_argument("--no-judge", dest="judge", action="store_false", help="Disable optional LLM judge even if enabled in config")
    parser.add_argument("--judge-backend", choices=["codex", "deterministic-fake"], default=None, help="Judge backend, default from config or codex")
    parser.add_argument("--judge-command", default=None, help="Judge command, default from config or codex")
    parser.add_argument("--judge-args", default=None, help="Judge args as a shell-like string, default from config or 'exec --json'")
    parser.add_argument("--judge-timeout", type=int, default=None, help="Judge timeout seconds")
    parser.add_argument("--judge-rubric", default=None, help="Override rubric id for all judged cases")
    parser.add_argument("--judge-fixture-file", default=None, help="Deterministic fake judge fixture path, relative to suite root when not absolute")
    parser.add_argument("--setup-only", action="store_true", help="Materialize selected modes and run pre-agent setup in disposable check worktrees, then stop before agent execution")
    parser.add_argument("--dry-run", action="store_true", help="Print selected plan without running setup or agent")
    parser.add_argument("--report-only", action="store_true", help="Regenerate reports from an existing summary.json in --out")
    parser.add_argument("--explain-run", help="Print a Markdown diagnostic for a run directory or run summary.json and exit")
    parser.add_argument("--compare-results", nargs="+", help="Compare two or more result folders at a high level and exit")
    parser.add_argument("--resume", action="store_true", help="Skip runs that already have summary.json and include them in regenerated reports")
    parser.add_argument("--cleanup", choices=["never", "on-success", "always"], help="Remove per-run worktrees never, after successful runs, or always after each run")
    parser.add_argument("--cleanup-prepared", action="store_true", help="Deprecated compatibility flag; v0.9-clean no longer creates prepared per-mode worktrees")
    parser.add_argument("--list-worktrees", action="store_true", help="List generated worktree folders under --worktree-root")
    parser.add_argument("--clean-worktrees", action="store_true", help="Remove all generated worktree folders under --worktree-root; requires --yes")
    parser.add_argument("--clean-results", action="store_true", help="Remove and recreate --out results folder; requires --yes")
    parser.add_argument("--yes", action="store_true", help="Confirm destructive lifecycle commands")
    parser.add_argument("--validate-config", action="store_true", help="Validate suite YAML files and exit before selecting repositories or running evals")
    parser.add_argument("--list-modes", action="store_true")
    parser.add_argument("--list-cases", action="store_true")
    parser.add_argument("--version", action="store_true", help="Print evaluator version and exit")
    parser.add_argument("--run-conformance", action="store_true", help="Run the built-in no-token conformance fixture and exit")
    parser.add_argument("--conformance-out", help="Optional output directory for --run-conformance")
    parser.add_argument("--export-lorq-shard", help="Migration-only: export existing Python v0 --out results into a v1-alpha LORQ run-shard package at this directory")
    parser.add_argument("--lorq-shard-id", default="shard-001", help="Shard id to write into --export-lorq-shard; default: shard-001")
    parser.add_argument("--lorq-package-id", default=None, help="Optional package id for --export-lorq-shard or --merge-lorq-shards; default: output directory name")
    parser.add_argument("--merge-lorq-shards", nargs="+", help="Migration-only: merge one or more v1-alpha LORQ run-shard packages")
    parser.add_argument("--lorq-merge-out", help="Output directory for --merge-lorq-shards")
    parser.add_argument("--lorq-benchmark", help="Optional deterministic benchmark.yaml used to compute expected cells for merge coverage")
    parser.add_argument("--lorq-allow-incompatible", action="store_true", help="Allow duplicate cells or fingerprint mismatches during migration-only LORQ merge and mark integrity instead of failing")
    return parser.parse_args(argv)



def _resolve_fixture_path(value: str | None, suite_root: Path) -> str | None:
    if not value:
        return value
    path = Path(value).expanduser()
    if not path.is_absolute():
        path = suite_root / path
    return str(path.resolve())


def _resolve_profile_paths(profile: dict[str, Any], suite_root: Path) -> dict[str, Any]:
    resolved = dict(profile)
    for key in ("fixture_file", "scenario_file"):
        if key in resolved and resolved[key] is not None:
            resolved[key] = _resolve_fixture_path(str(resolved[key]), suite_root)
    return resolved


def _resolve_judge_config_paths(config: dict[str, Any], suite_root: Path) -> dict[str, Any]:
    resolved = dict(config)
    if resolved.get("fixture_file") is not None:
        resolved["fixture_file"] = _resolve_fixture_path(str(resolved["fixture_file"]), suite_root)
    return resolved

def _agent_args_from_string(value: str | None, default: list[str]) -> list[str]:
    # Backwards-compatible wrapper; v0.6 uses shlex-aware split_args.
    return split_args(value, default)


def _apply_agent_overrides(profile: dict[str, Any], args: argparse.Namespace) -> dict[str, Any]:
    resolved = dict(profile)
    if args.agent_backend:
        resolved["backend"] = args.agent_backend
    if args.agent_command:
        resolved["command"] = args.agent_command
    if args.agent_args is not None:
        resolved["args"] = split_args(args.agent_args, [])
    if args.agent_timeout is not None:
        resolved["timeout_seconds"] = args.agent_timeout
    if getattr(args, "isolate_agent_home", None) is not None:
        resolved["isolate_home"] = bool(args.isolate_agent_home)
        resolved["isolate_codex_home"] = bool(args.isolate_agent_home)
    if getattr(args, "agent_model", None):
        resolved["model"] = args.agent_model
    if getattr(args, "agent_reasoning_effort", None):
        resolved["reasoning_effort"] = args.agent_reasoning_effort
    if getattr(args, "agent_permission_policy", None):
        resolved["permission_policy"] = args.agent_permission_policy
    return resolved


def _mode_setup_scope(mode: dict[str, Any], override: str | None) -> str:
    scope = override or ((mode.get("pre_agent") or {}).get("setup_scope") or "per-run")
    if scope == "per-case":
        return "per-run"
    return scope


def _cleanup_policy(config: dict[str, Any], override: str | None) -> str:
    if override:
        return override
    value = (config.get("worktrees") or {}).get("cleanup", False)
    if value is True:
        return "on-success"
    if value is False or value is None:
        return "never"
    if value in {"never", "on-success", "always"}:
        return str(value)
    raise ValueError(f"Invalid worktrees.cleanup value: {value!r}")


def _run_succeeded(agent_summary: dict[str, Any]) -> bool:
    return agent_summary.get("exit_code") == 0 and not agent_summary.get("timed_out")


def _setup_summary(run_setup: dict[str, Any]) -> dict[str, Any]:
    if not run_setup:
        return {"ok": True, "commands": [], "command_count": 0, "elapsed_ms": 0}
    return run_setup.get("setup") or {"ok": True, "commands": [], "command_count": 0, "elapsed_ms": 0}


def _setup_ok(run_setup: dict[str, Any]) -> bool:
    return bool(_setup_summary(run_setup).get("ok", True))


def _validation_ok(validation: dict[str, Any]) -> bool:
    return bool(validation.get("ok", False))


def _judge_ok_or_disabled(validation: dict[str, Any]) -> bool:
    judge = validation.get("judge") or {}
    if not judge.get("enabled"):
        return True
    return bool(judge.get("ok"))


def _final_status(run_setup: dict[str, Any], agent_summary: dict[str, Any] | None, validation: dict[str, Any] | None) -> str:
    if not _setup_ok(run_setup):
        return "setup_failed"
    if not agent_summary or not _run_succeeded(agent_summary):
        return "agent_failed"
    validation = validation or {}
    if not _validation_ok(validation):
        return "validation_failed"
    if not _judge_ok_or_disabled(validation):
        return "judge_failed"
    return "completed"


def _skipped_agent_summary(reason: str) -> dict[str, Any]:
    return {
        "agent": "skipped",
        "backend": "skipped",
        "output_format": "none",
        "exit_code": None,
        "timed_out": False,
        "elapsed_ms": 0,
        "usage": {},
        "counts": {},
        "skipped": True,
        "reason": reason,
    }


def _setup_failed_validation(reason: str) -> dict[str, Any]:
    return with_schema({
        "ok": False,
        "hard_passed": 0,
        "hard_total": 1,
        "soft_passed": 0,
        "soft_total": 0,
        "results": [
            {"id": "setup_ok", "ok": False, "hard": True, "message": reason}
        ],
    }, "agent-eval.validation-result.v1")


def _resolve_case_repository(repo_override: str | None, case: dict[str, Any] | None, repos: dict[str, dict[str, Any]]) -> tuple[str, dict[str, Any]]:
    # CLI --repo always wins. Otherwise case.repo selects a repository when present.
    if repo_override:
        return resolve_repository(repo_override, repos)
    case_repo = (case or {}).get("repo")
    if case_repo:
        try:
            return resolve_repository(str(case_repo), repos)
        except ValueError:
            # Keep the bundled example suite usable: if only one repo is configured,
            # allow it to stand in for the example case repo id, but record the
            # effective repo id in run metadata.
            if len(repos) == 1:
                key = next(iter(repos))
                return key, repos[key]
            raise
    return resolve_repository(None, repos)



def _bool_profile(profile: dict[str, Any], *keys: str, default: bool = False) -> bool:
    for key in keys:
        if key in profile:
            return bool(profile.get(key))
    return default


def _active_skill_names(worktree: Path) -> list[str]:
    skills_root = worktree / ".agents" / "skills"
    if not skills_root.exists():
        return []
    names: list[str] = []
    for skill_md in sorted(skills_root.glob("*/SKILL.md")):
        names.append(skill_md.parent.name)
    return names


def _profile_for_run(base_profile: dict[str, Any], output_root: Path, run_output_dir: Path, run_id: str) -> tuple[dict[str, Any], dict[str, Any]]:
    """Return a per-run agent profile plus isolation metadata.

    When enabled, HOME is pointed at a generated per-run runtime home so Codex
    cannot discover user-level skills from the real $HOME/.agents/skills. For
    Codex CLI, CODEX_HOME can also be isolated to avoid global instructions.
    """
    profile = dict(base_profile)
    backend = str(profile.get("backend") or profile.get("type") or "codex").lower()
    isolate_home = _bool_profile(profile, "isolate_home", "isolated_home", default=False)
    isolate_codex_home = _bool_profile(profile, "isolate_codex_home", "isolated_codex_home", default=isolate_home and backend in {"codex", "codex-cli"})
    metadata = {
        "enabled": bool(isolate_home or isolate_codex_home),
        "isolate_home": bool(isolate_home),
        "isolate_codex_home": bool(isolate_codex_home),
        "runtime_home": None,
        "codex_home": None,
    }
    if isolate_home or isolate_codex_home:
        runtime_home = output_root / "runtime-homes" / slug(run_id)
        ensure_dir(runtime_home)
        write_generated_marker(runtime_home, kind="runtime-home", metadata={"run_id": run_id})
        env = dict(profile.get("env") or {})
        if isolate_home:
            env["HOME"] = str(runtime_home)
            metadata["runtime_home"] = str(runtime_home)
        if isolate_codex_home:
            codex_home = runtime_home / ".codex"
            ensure_dir(codex_home)
            env["CODEX_HOME"] = str(codex_home)
            metadata["codex_home"] = str(codex_home)
        profile["env"] = env
    return profile, metadata

def _run_conformance(args: argparse.Namespace) -> int:
    """Run the bundled no-token fixture and verify stable contract invariants."""
    import tempfile
    import json as _json

    package_root = Path(__file__).resolve().parents[1]
    candidate_roots = [package_root, package_root.parent]
    fixture_root = next(
        (root / "examples" / "simple-local-repo" for root in candidate_roots if (root / "examples" / "simple-local-repo").exists()),
        package_root / "examples" / "simple-local-repo",
    )
    if not fixture_root.exists():
        print(f"Built-in conformance fixture not found: {fixture_root}", file=sys.stderr)
        return 2

    if args.conformance_out:
        out_root = Path(args.conformance_out).expanduser().resolve()
        worktree_root = out_root / "worktrees"
        results_root = out_root / "results"
        ensure_dir(out_root)
    else:
        temp_root = Path(tempfile.mkdtemp(prefix="agent-eval-conformance-"))
        worktree_root = temp_root / "worktrees"
        results_root = temp_root / "results"

    code = _main([
        "--suite-root", str(fixture_root),
        "--repo", str(fixture_root / "fake_project"),
        "--modes", "local-fake",
        "--cases", "explain-demo",
        "--agent-profile", "fake",
        "--agent-command", sys.executable,
        "--prompt-style", "neutral",
        "--worktree-root", str(worktree_root),
        "--out", str(results_root),
        "--dirty-policy", "allow",
        "--cleanup", "never",
    ])
    if code != 0:
        print(f"Conformance run failed with exit code {code}", file=sys.stderr)
        return code

    summary = read_json(results_root / "summary.json", default={}) or {}
    runs = summary.get("runs") or []
    failures: list[str] = []
    if summary.get("schema_version") != "agent-eval.summary.v1":
        failures.append("summary.json schema_version mismatch")
    if len(runs) != 1:
        failures.append(f"expected exactly one run, got {len(runs)}")
    record = runs[0] if runs else {}
    if record.get("run_status") != "completed":
        failures.append(f"expected completed run_status, got {record.get('run_status')!r}")
    validation = record.get("validation") or {}
    if validation.get("schema_version") != "agent-eval.validation-result.v1":
        failures.append("validation schema_version mismatch")
    if not validation.get("ok"):
        failures.append("validation did not pass")
    run_dir = Path(record.get("run_dir") or "")
    if not (run_dir / "events.normalized.jsonl").exists():
        failures.append("events.normalized.jsonl missing")
    manifest = read_json(run_dir / "run.manifest.json", default={}) or {}
    if manifest.get("schema_version") != "agent-eval.run-manifest.v1":
        failures.append("run.manifest.json schema_version mismatch")

    result = {
        "schema_version": "agent-eval.conformance-result.v1",
        "contract_version": "agent-eval.contract.v1",
        "ok": not failures,
        "failures": failures,
        "results_root": str(results_root),
        "run_dir": str(run_dir),
    }
    print(_json.dumps(result, indent=2, ensure_ascii=False))
    return 0 if not failures else 2


def _apply_dirty_policy(repo_id: str, repo_status: dict[str, Any], policy: str) -> None:
    if not repo_status.get("dirty") or policy == "allow":
        return
    msg = (
        f"Repository {repo_id!r} has uncommitted changes. "
        "git-worktree runs evaluate committed HEAD, not dirty files. "
        "Use --dirty-policy allow to proceed, or commit/stash changes."
    )
    if policy == "fail":
        raise RuntimeError(msg)
    print(f"Warning: {msg}")


def _main(argv: list[str] | None = None) -> int:
    args = parse_args(argv)
    if args.version:
        print(f"agent-eval {__version__}")
        return 0

    if args.run_conformance:
        return _run_conformance(args)

    if args.merge_lorq_shards:
        import json as _json
        if not args.lorq_merge_out:
            print("--merge-lorq-shards requires --lorq-merge-out", file=sys.stderr)
            return 2
        benchmark_file = Path(args.lorq_benchmark).expanduser().resolve() if args.lorq_benchmark else None
        try:
            result = merge_lorq_run_shards(
                [Path(value) for value in args.merge_lorq_shards],
                Path(args.lorq_merge_out),
                package_id=args.lorq_package_id,
                benchmark_file=benchmark_file,
                strict=not args.lorq_allow_incompatible,
            )
        except LorqPackageError as exc:
            print(f"LORQ merge failed: {exc}", file=sys.stderr)
            return 2
        print(_json.dumps(result, indent=2, ensure_ascii=False))
        return 0 if result.get("ok") else 2

    suite_paths, config = load_config(Path(args.suite_root), args.config)

    all_modes = load_modes(suite_paths.modes_dir)
    all_cases = load_cases(suite_paths.cases_dir)
    all_rubrics_for_validation = load_rubrics(suite_paths.rubrics_dir)
    all_prompt_styles_for_validation = validate_prompt_styles_dir(suite_paths.prompt_styles_dir)

    if args.validate_config:
        # Reaching this point means config, modes, cases, and prompt/rubric loaders passed schema validation.
        print(f"Configuration valid: {suite_paths.root}")
        print(f"Modes: {len(all_modes)}")
        print(f"Cases: {len(all_cases)}")
        print(f"Rubrics: {len(all_rubrics_for_validation)}")
        print(f"Prompt styles: {len(all_prompt_styles_for_validation)}")
        return 0

    if args.list_modes:
        for mode_id, mode in all_modes.items():
            print(f"{mode_id}\t{mode.get('description', '')}")
        return 0

    if args.list_cases:
        for case_id, case in all_cases.items():
            print(f"{case_id}\t{case.get('category', '')}\t{case.get('title', '')}")
        return 0

    if args.list_agent_profiles:
        profiles = config.get("agent_profiles") or config.get("agents") or {}
        if not profiles:
            print("No agent profiles configured. The legacy inline Codex config will be used.")
        for profile_id, profile in profiles.items():
            backend = (profile or {}).get("backend") or (profile or {}).get("type") or ""
            command = (profile or {}).get("command") or ""
            description = (profile or {}).get("description") or ""
            print(f"{profile_id}\t{backend}\t{command}\t{description}")
        return 0

    if args.check_agent or args.check_all_agents:
        import json as _json
        profiles = config.get("agent_profiles") or config.get("agents") or {}
        checks: list[dict[str, Any]] = []
        if args.check_all_agents:
            if profiles:
                for profile_id, profile in profiles.items():
                    merged = dict(profile or {})
                    merged.setdefault("id", profile_id)
                    merged = _resolve_profile_paths(merged, suite_paths.root)
                    checks.append(check_agent_availability(merged, cwd=suite_paths.root))
            else:
                profile_id, profile = resolve_agent_profile(config, profile_name=args.agent_profile)
                profile = _apply_agent_overrides(profile, args)
                profile.setdefault("id", profile_id)
                profile = _resolve_profile_paths(profile, suite_paths.root)
                checks.append(check_agent_availability(profile, cwd=suite_paths.root))
        else:
            profile_id, profile = resolve_agent_profile(config, profile_name=args.agent_profile)
            profile = _apply_agent_overrides(profile, args)
            profile.setdefault("id", profile_id)
            profile = _resolve_profile_paths(profile, suite_paths.root)
            checks.append(check_agent_availability(profile, cwd=suite_paths.root))

        for check in checks:
            status = "ok" if check.get("ok") else "missing"
            profile_label = check.get("profile_id") or check.get("backend") or "agent"
            detail = check.get("resolved_path") or check.get("origin") or check.get("message") or ""
            print(f"{profile_label}\t{status}\t{check.get('backend')}\t{detail}")
        payload = checks[0] if len(checks) == 1 else {"agent_checks": checks}
        print("\n" + _json.dumps(payload, indent=2, ensure_ascii=False))
        return 0 if all(check.get("ok") for check in checks) else 2

    output_root = Path(args.out or (config.get("output") or {}).get("root") or "./results").expanduser().resolve()
    worktree_root = Path(args.worktree_root or (config.get("worktrees") or {}).get("root") or "/tmp/agent-eval-worktrees").expanduser().resolve()
    pricing_config = None if args.no_pricing else resolve_pricing_config(config, model_override=args.pricing_model, pricing_file=args.pricing_file)
    worktree_strategy = args.worktree_strategy or (config.get("worktrees") or {}).get("strategy") or "git-worktree"

    if args.explain_run:
        try:
            record = load_run_record_from_path(Path(args.explain_run).expanduser().resolve())
        except Exception as exc:  # noqa: BLE001 - CLI diagnostic should be concise.
            print(f"Could not explain run: {exc}", file=sys.stderr)
            return 2
        print(explain_run_markdown(record))
        return 0

    if args.compare_results:
        result_sets = []
        for raw_path in args.compare_results:
            path = Path(raw_path).expanduser().resolve()
            records = load_run_records(path)
            result_sets.append((path.name or str(path), records))
        print(compare_result_sets(result_sets))
        return 0

    ensure_dir(output_root)
    ensure_dir(worktree_root)
    write_generated_marker(worktree_root, kind="worktree-root")

    if args.list_worktrees:
        entries = list_generated_worktrees(worktree_root)
        for entry in entries:
            marker = "git" if entry["is_git_worktree"] else "dir"
            generated = "marked" if entry.get("has_marker") else "unmarked"
            print(f"{marker}	{generated}	{entry['name']}	{entry['path']}")
        if not entries:
            print(f"No generated worktrees found under {worktree_root}")
        return 0

    if args.clean_worktrees:
        payload = clean_worktree_root(worktree_root, yes=args.yes)
        write_lifecycle_event(output_root, "clean-worktrees", payload)
        print(f"Cleaned generated worktrees under {worktree_root}")
        return 0

    if args.clean_results:
        payload = clean_results_root(output_root, yes=args.yes)
        write_lifecycle_event(output_root, "clean-results", payload)
        print(f"Cleaned results under {output_root}")
        return 0

    write_generated_marker(output_root, kind="results-root")

    selected_modes = select_modes(all_modes, args.modes)
    selected_cases = select_cases(all_cases, args.cases, args.categories)

    repos = load_repositories(suite_paths.root, config)
    dirty_policy = args.dirty_policy or (config.get("worktrees") or {}).get("dirty_policy") or "warn"
    repo_cache: dict[str, tuple[dict[str, Any], dict[str, Any]]] = {}

    def repo_for_case(case: dict[str, Any] | None) -> tuple[str, dict[str, Any], dict[str, Any]]:
        rid, resolved_repo = _resolve_case_repository(args.repo, case, repos)
        if rid not in repo_cache:
            status = inspect_repository(resolved_repo)
            _apply_dirty_policy(rid, status, dirty_policy)
            repo_cache[rid] = (resolved_repo, status)
        cached_repo, cached_status = repo_cache[rid]
        return rid, cached_repo, cached_status

    prompt_style = args.prompt_style or config.get("default_prompt_style") or "neutral"
    prompt_template = load_prompt_style(suite_paths.prompt_styles_dir, prompt_style)

    if args.report_only:
        run_records = load_run_records(output_root)
        write_reports(output_root, run_records, pricing=pricing_config)
        print(f"Regenerated reports for {len(run_records)} run(s): {output_root}")
        return 0

    if args.export_lorq_shard:
        import json as _json
        package_root = Path(args.export_lorq_shard).expanduser().resolve()
        result = export_lorq_run_shard(
            output_root,
            package_root,
            shard_id=args.lorq_shard_id,
            package_id=args.lorq_package_id,
        )
        print(_json.dumps(result, indent=2, ensure_ascii=False))
        return 0

    agent_profile_id, agent_profile = resolve_agent_profile(config, profile_name=args.agent_profile)
    agent_profile = _apply_agent_overrides(agent_profile, args)
    agent_profile.setdefault("id", agent_profile_id)
    agent_profile = _resolve_profile_paths(agent_profile, suite_paths.root)
    agent_check = check_agent_availability(agent_profile, cwd=suite_paths.root)
    if args.require_agent_available and not agent_check.get("ok"):
        import json as _json
        print("Selected agent backend is unavailable:", file=sys.stderr)
        print(_json.dumps(agent_check, indent=2, ensure_ascii=False), file=sys.stderr)
        return 2
    judge_config = _resolve_judge_config_paths(config.get("judge") or {}, suite_paths.root)
    judge_enabled = bool(judge_config.get("enabled", False)) if args.judge is None else bool(args.judge)
    judge_backend = args.judge_backend or judge_config.get("backend") or "codex"
    judge_command = args.judge_command or judge_config.get("command") or "codex"
    default_judge_args = judge_config.get("args") or ["exec", "--json"]
    judge_args = _agent_args_from_string(args.judge_args, default_judge_args)
    judge_timeout = args.judge_timeout or judge_config.get("timeout_seconds")
    judge_default_rubric = args.judge_rubric or judge_config.get("default_rubric")
    judge_fixture_file = _resolve_fixture_path(args.judge_fixture_file, suite_paths.root) or judge_config.get("fixture_file")
    if judge_enabled and judge_backend in {"fake", "deterministic-fake", "lorq-fake"}:
        if not judge_fixture_file:
            raise ValueError("deterministic fake judge requires judge.fixture_file or --judge-fixture-file")
        judge = DeterministicFakeJudge(str(judge_fixture_file))
    else:
        judge = CodexCliJudge(judge_command, judge_args, judge_timeout) if judge_enabled else None
    rubrics = load_rubrics(suite_paths.rubrics_dir) if judge_enabled else {}

    # Resolve selected repositories once for clear pre-run diagnostics and dirty-repo policy.
    selected_repo_ids: list[str] = []
    for selected_case in selected_cases:
        rid, _repo_obj, status = repo_for_case(selected_case)
        if rid not in selected_repo_ids:
            selected_repo_ids.append(rid)
            if status.get("commit"):
                print(f"Repository: {rid} @ {status.get('commit')}")
            else:
                print(f"Repository: {rid}")
    print(f"Modes: {', '.join(m['id'] for m in selected_modes)}")
    print(f"Cases: {', '.join(c['id'] for c in selected_cases)}")
    print(f"Prompt style: {prompt_style}")
    isolation_label = "isolated" if _bool_profile(agent_profile, "isolate_home", "isolated_home", default=False) else "normal-home"
    print(f"Agent profile: {agent_profile_id} ({agent_profile.get('backend', 'generic')}, {isolation_label})")
    if not agent_check.get("ok"):
        print(f"Warning: selected agent availability check failed: {agent_check.get('message') or agent_check.get('error_category')}")
    print(f"Judge: {'enabled' if judge_enabled else 'disabled'}")
    print(f"Output: {output_root}")
    cleanup_policy = _cleanup_policy(config, args.cleanup)
    print(f"Worktrees: {worktree_root}")
    print(f"Cleanup: {cleanup_policy}")
    if args.resume:
        print("Resume: enabled")

    if args.dry_run:
        return 0

    manager = WorktreeManager(suite_paths.root, worktree_root, worktree_strategy)
    run_records: list[dict[str, Any]] = load_run_records(output_root) if args.resume else []
    existing_run_keys = {
        (r.get("mode"), r.get("prompt_style"), r.get("case"), r.get("repetition"))
        for r in run_records
    }
    runs_root = output_root / "runs"

    # v0.9-clean intentionally avoids prepared-worktree artifact reuse.
    # Each real run gets a fresh worktree and runs setup there.
    selected_scopes = {_mode_setup_scope(mode, args.setup_scope) for mode in selected_modes}
    if "per-mode" in selected_scopes:
        print(
            "Error: setup_scope=per-mode is disabled in v0.9-clean. "
            "Use per-run (default) for fairness-first evals, or none for modes with no setup.",
            file=sys.stderr,
        )
        return 2

    if args.setup_only:
        for mode in selected_modes:
            mode_id = mode["id"]
            setup_scope = _mode_setup_scope(mode, args.setup_scope)
            check_id = f"setup-check__{mode_id}"
            check_worktree = worktree_root / slug(check_id)
            check_output_dir = output_root / "setup-checks" / mode_id
            ensure_dir(check_output_dir)
            write_generated_marker(check_output_dir, kind="setup-check-output", metadata={"mode": mode_id})
            setup_case = selected_cases[0] if selected_cases else None
            setup_repo_id, setup_repo, _setup_repo_status = repo_for_case(setup_case)
            worktree_record = manager.create_from_repo(setup_repo, check_worktree)
            materialize_records = materialize_mode(suite_paths.root, check_worktree, mode)
            if setup_scope == "none":
                setup_summary = {"commands": [], "ok": True, "command_count": 0, "elapsed_ms": 0}
            else:
                setup_summary = run_pre_agent_commands(check_worktree, mode, check_output_dir / "setup")
            write_json(check_output_dir / "setup-check.summary.json", {
                "mode": mode_id,
                "repo": setup_repo_id,
                "setup_scope": setup_scope,
                "worktree": worktree_record,
                "materialize": materialize_records,
                "setup": setup_summary,
            })
            _print_setup_check_result(mode_id, check_output_dir, setup_summary)
        write_reports(output_root, run_records, pricing=pricing_config)
        return 0

    for mode in selected_modes:
        mode_id = mode["id"]
        setup_scope = _mode_setup_scope(mode, args.setup_scope)
        for case in selected_cases:
            case_id = case["id"]
            for repetition in range(1, args.repetitions + 1):
                run_id = f"{mode_id}__{prompt_style}__{case_id}__r{repetition}"
                run_worktree = worktree_root / slug(run_id)
                run_output_dir = runs_root / mode_id / prompt_style / case_id / f"r{repetition}"
                run_key = (mode_id, prompt_style, case_id, repetition)
                if args.resume and (run_output_dir / "summary.json").exists():
                    if run_key not in existing_run_keys:
                        skipped_record = read_json(run_output_dir / "summary.json", default={}) or {}
                        if skipped_record:
                            run_records.append(skipped_record)
                            existing_run_keys.add(run_key)
                    print(f"Skipping existing run: {run_id}")
                    continue

                ensure_dir(run_output_dir)
                write_generated_marker(run_output_dir, kind="run-output", metadata={"run_id": run_id})

                run_repo_id, run_repo, run_repo_status = repo_for_case(case)
                run_setup: dict[str, Any] = {"setup_scope": setup_scope}
                worktree_for_snapshots: Path | None = run_worktree
                worktree_record = manager.create_from_repo(run_repo, run_worktree)
                materialize_records = materialize_mode(suite_paths.root, run_worktree, mode)
                if setup_scope == "none":
                    setup_summary = {"commands": [], "ok": True, "command_count": 0, "elapsed_ms": 0}
                else:
                    setup_summary = run_pre_agent_commands(run_worktree, mode, run_output_dir / "setup")
                run_setup.update({
                    "worktree": worktree_record,
                    "materialize": materialize_records,
                    "setup": setup_summary,
                })

                write_environment_files(run_output_dir, worktree_for_snapshots or suite_paths.root)
                write_run_snapshots(
                    run_output_dir,
                    mode=mode,
                    case=case,
                    repo_id=run_repo_id,
                    repo=run_repo,
                    repo_status=run_repo_status,
                    prompt_style=prompt_style,
                    prompt_template=prompt_template,
                    agent_profile_id=agent_profile_id,
                    agent_profile=agent_profile,
                    repetition=repetition,
                    worktree=worktree_for_snapshots,
                )

                if not _setup_ok(run_setup):
                    reason = f"Required setup failed: {_setup_summary(run_setup).get('failed_required_command') or 'unknown command'}"
                    prompt = render_prompt(prompt_template, case, mode, prompt_style)
                    write_json(run_output_dir / "agent.summary.json", _skipped_agent_summary(reason))
                    from .utils import write_text
                    write_text(run_output_dir / "prompt.txt", prompt)
                    write_text(run_output_dir / "answer.md", "")
                    write_text(run_output_dir / "stdout.raw.jsonl", "")
                    write_text(run_output_dir / "stdout.raw.txt", "")
                    write_text(run_output_dir / "stderr.txt", reason + "\n")
                    active_skills = _active_skill_names(run_worktree)
                    write_json(run_output_dir / "active-skills.json", {
                        "schema_version": "agent-eval.active-skills.v1",
                        "skills": active_skills,
                        "skill_count": len(active_skills),
                    })
                    agent_summary = _skipped_agent_summary(reason)
                    agent_summary["active_skills"] = active_skills
                    write_json(run_output_dir / "agent.summary.json", agent_summary)
                    validation = _setup_failed_validation(reason)
                    write_json(run_output_dir / "validation.json", validation)
                else:
                    prompt = render_prompt(prompt_template, case, mode, prompt_style)
                    run_agent_profile, agent_isolation = _profile_for_run(agent_profile, output_root, run_output_dir, run_id)
                    active_skills = _active_skill_names(run_worktree)
                    write_json(run_output_dir / "active-skills.json", {
                        "schema_version": "agent-eval.active-skills.v1",
                        "skills": active_skills,
                        "skill_count": len(active_skills),
                    })
                    agent = create_agent(run_agent_profile)
                    agent_summary = agent.run(run_worktree, prompt, run_output_dir)
                    if pricing_config:
                        agent_summary["pricing"] = estimate_cost(agent_summary.get("usage"), pricing_config)
                    agent_summary["home_isolation"] = agent_isolation
                    agent_summary["active_skills"] = active_skills
                    write_json(run_output_dir / "agent.summary.json", agent_summary)
                    validation = run_validators(run_output_dir, case, mode)
                    if judge is not None:
                        rubric = resolve_rubric(rubrics, case, judge_default_rubric)
                        validation["judge"] = judge.run(
                            worktree=run_worktree,
                            output_dir=run_output_dir,
                            case=case,
                            mode=mode,
                            rubric=rubric,
                            validation=validation,
                        )
                        write_json(run_output_dir / "validation.json", validation)

                run_status = _final_status(run_setup, agent_summary, validation)
                record = with_schema({
                    "run_status": run_status,
                    "repo": run_repo_id,
                    "repo_status": run_repo_status,
                    "mode": mode_id,
                    "prompt_style": prompt_style,
                    "case": case_id,
                    "category": case.get("category"),
                    "repetition": repetition,
                    "worktree": str(run_worktree),
                    "run_dir": str(run_output_dir),
                    "setup": run_setup,
                    "agent_check": agent_check,
                    "agent_summary": agent_summary,
                    "validation": validation,
                }, RESULT_SCHEMA_VERSION)

                cleanup_record = None
                if run_worktree.exists() and (cleanup_policy == "always" or (cleanup_policy == "on-success" and run_status == "completed")):
                    cleanup_record = remove_execution_path(run_worktree, allowed_root=worktree_root, require_marker=True)
                    record["cleanup"] = cleanup_record
                    record["git_prune"] = prune_git_worktrees(run_repo)

                manifest = read_json(run_output_dir / "run.manifest.json", default={}) or {}
                manifest["run_status"] = run_status
                write_json(run_output_dir / "run.manifest.json", manifest)
                write_json(run_output_dir / "summary.json", record)
                run_records.append(record)
                existing_run_keys.add(run_key)
                write_reports(output_root, run_records, pricing=pricing_config)

    if args.cleanup_prepared:
        write_lifecycle_event(output_root, "cleanup-prepared", {"deprecated": True, "records": []})

    write_reports(output_root, run_records, pricing=pricing_config)
    return 0



def _preview_text(path: Path, max_chars: int = 1200) -> str:
    try:
        text = path.read_text(encoding="utf-8", errors="replace")
    except FileNotFoundError:
        return ""
    if len(text) > max_chars:
        return text[:max_chars] + "\n...<truncated>"
    return text


def _print_setup_check_result(mode_id: str, check_output_dir: Path, setup_summary: dict[str, Any]) -> None:
    ok = bool(setup_summary.get("ok"))
    print(f"Setup check for {mode_id}: {'ok' if ok else 'failed'}")
    if ok:
        return
    failed_id = setup_summary.get("failed_required_command")
    if failed_id:
        print(f"  failed required command: {failed_id}")
    commands = setup_summary.get("commands") or []
    failing = None
    if failed_id:
        failing = next((c for c in commands if c.get("id") == failed_id), None)
    if failing is None:
        failing = next((c for c in commands if c.get("failed")), None)
    if not failing:
        print(f"  setup summary: {check_output_dir / 'setup' / 'setup.summary.json'}")
        print(f"  setup log:     {check_output_dir / 'setup' / 'setup.log'}")
        return
    print(f"  command: {failing.get('run')}")
    print(f"  cwd:     {failing.get('cwd')}")
    print(f"  exit:    {failing.get('exit_code')} timed_out={failing.get('timed_out')}")
    stdout_path = check_output_dir / 'setup' / str(failing.get('stdout_path', ''))
    stderr_path = check_output_dir / 'setup' / str(failing.get('stderr_path', ''))
    stdout_preview = _preview_text(stdout_path)
    stderr_preview = _preview_text(stderr_path)
    if stdout_preview.strip():
        print("  stdout preview:")
        for line in stdout_preview.rstrip().splitlines():
            print(f"    {line}")
    if stderr_preview.strip():
        print("  stderr preview:")
        for line in stderr_preview.rstrip().splitlines():
            print(f"    {line}")
    print(f"  setup summary: {check_output_dir / 'setup' / 'setup.summary.json'}")
    print(f"  setup log:     {check_output_dir / 'setup' / 'setup.log'}")

def main(argv: list[str] | None = None) -> int:
    try:
        return _main(argv)
    except SchemaError as exc:
        print("Configuration validation failed:", file=sys.stderr)
        print(str(exc), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
