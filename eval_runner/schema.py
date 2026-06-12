from __future__ import annotations

from pathlib import Path
from typing import Any


class SchemaError(ValueError):
    """Raised when an eval-suite YAML file does not match the expected schema."""

    def __init__(self, errors: list[str]) -> None:
        self.errors = errors
        super().__init__("\n".join(errors))

    def __str__(self) -> str:  # pragma: no cover - trivial formatting
        return "\n".join(f"- {error}" for error in self.errors)


class _Collector:
    def __init__(self, file_label: str | Path) -> None:
        self.file_label = str(file_label)
        self.errors: list[str] = []

    def fail(self, path: str, message: str) -> None:
        self.errors.append(f"{self.file_label}: {path}: {message}")

    def require_mapping(self, value: Any, path: str) -> dict[str, Any]:
        if not isinstance(value, dict):
            self.fail(path, f"expected mapping, got {type(value).__name__}")
            return {}
        return value

    def optional_mapping(self, value: Any, path: str) -> dict[str, Any]:
        if value is None:
            return {}
        return self.require_mapping(value, path)

    def require_string(self, data: dict[str, Any], key: str, path: str) -> str | None:
        if key not in data:
            self.fail(path, f"missing required field '{key}'")
            return None
        value = data.get(key)
        if not isinstance(value, str) or not value.strip():
            self.fail(f"{path}.{key}", "expected non-empty string")
            return None
        return value

    def optional_string(self, data: dict[str, Any], key: str, path: str, *, allow_null: bool = True) -> str | None:
        if key not in data:
            return None
        value = data.get(key)
        if value is None and allow_null:
            return None
        if not isinstance(value, str):
            self.fail(f"{path}.{key}", f"expected string, got {type(value).__name__}")
            return None
        return value

    def optional_bool(self, data: dict[str, Any], key: str, path: str) -> bool | None:
        if key not in data:
            return None
        value = data.get(key)
        if not isinstance(value, bool):
            self.fail(f"{path}.{key}", f"expected boolean, got {type(value).__name__}")
            return None
        return value

    def optional_int(self, data: dict[str, Any], key: str, path: str, *, min_value: int | None = None) -> int | None:
        if key not in data:
            return None
        value = data.get(key)
        if not isinstance(value, int) or isinstance(value, bool):
            self.fail(f"{path}.{key}", f"expected integer, got {type(value).__name__}")
            return None
        if min_value is not None and value < min_value:
            self.fail(f"{path}.{key}", f"expected integer >= {min_value}")
        return value

    def optional_number(self, data: dict[str, Any], key: str, path: str) -> float | int | None:
        if key not in data:
            return None
        value = data.get(key)
        if not isinstance(value, (int, float)) or isinstance(value, bool):
            self.fail(f"{path}.{key}", f"expected number, got {type(value).__name__}")
            return None
        return value

    def enum_value(self, data: dict[str, Any], key: str, path: str, allowed: set[str], *, required: bool = False) -> str | None:
        if key not in data:
            if required:
                self.fail(path, f"missing required field '{key}'")
            return None
        value = data.get(key)
        if not isinstance(value, str):
            self.fail(f"{path}.{key}", f"expected one of {sorted(allowed)}, got {type(value).__name__}")
            return None
        if value not in allowed:
            self.fail(f"{path}.{key}", f"expected one of {sorted(allowed)}, got {value!r}")
        return value

    def list_of_strings(self, value: Any, path: str, *, required: bool = False) -> list[str]:
        if value is None:
            if required:
                self.fail(path, "missing required list")
            return []
        if not isinstance(value, list):
            self.fail(path, f"expected list, got {type(value).__name__}")
            return []
        out: list[str] = []
        for i, item in enumerate(value):
            if not isinstance(item, str) or not item.strip():
                self.fail(f"{path}[{i}]", "expected non-empty string")
            else:
                out.append(item)
        return out

    def no_unknown_keys(self, data: dict[str, Any], path: str, allowed: set[str]) -> None:
        for key in sorted(data):
            if key not in allowed:
                self.fail(f"{path}.{key}", f"unknown field; allowed fields: {', '.join(sorted(allowed))}")


def _raise_if_errors(c: _Collector) -> None:
    if c.errors:
        raise SchemaError(c.errors)


def _list_of_symbol_specs(c: _Collector, value: Any, path: str) -> None:
    if value is None:
        return
    if not isinstance(value, list):
        c.fail(path, f"expected list, got {type(value).__name__}")
        return
    for i, item in enumerate(value):
        item_path = f"{path}[{i}]"
        if isinstance(item, str) and item.strip():
            continue
        if isinstance(item, dict):
            symbol = item.get("symbol") or item.get("name") or item.get("value")
            if not isinstance(symbol, str) or not symbol.strip():
                c.fail(item_path, "expected non-empty 'symbol' string")
            c.optional_bool(item, "must_be_near_file_reference", item_path)
            continue
        c.fail(item_path, "expected non-empty string or mapping with 'symbol'")


def validate_config(config: dict[str, Any], path: str | Path = "eval.config.yaml") -> None:
    c = _Collector(path)
    c.require_mapping(config, "$")
    c.no_unknown_keys(config, "$", {"default_prompt_style", "repositories", "worktrees", "output", "agent", "agent_profiles", "agents", "judge", "pricing"})
    c.optional_string(config, "default_prompt_style", "$")

    repositories = c.optional_mapping(config.get("repositories"), "$.repositories")
    for repo_id, repo in repositories.items():
        if not isinstance(repo_id, str) or not repo_id.strip():
            c.fail("$.repositories", "repository ids must be non-empty strings")
            continue
        validate_repository(repo_id, repo, f"$.repositories.{repo_id}", c)

    worktrees = c.optional_mapping(config.get("worktrees"), "$.worktrees")
    if worktrees:
        c.no_unknown_keys(worktrees, "$.worktrees", {"root", "strategy", "cleanup", "dirty_policy"})
        c.optional_string(worktrees, "root", "$.worktrees")
        c.enum_value(worktrees, "strategy", "$.worktrees", {"git-worktree", "copy", "clone"})
        if "cleanup" in worktrees and worktrees.get("cleanup") not in {True, False, None, "never", "on-success", "always"}:
            c.fail("$.worktrees.cleanup", "expected boolean or one of ['never', 'on-success', 'always']")
        c.enum_value(worktrees, "dirty_policy", "$.worktrees", {"warn", "fail", "allow"})

    output = c.optional_mapping(config.get("output"), "$.output")
    if output:
        c.no_unknown_keys(output, "$.output", {"root"})
        c.optional_string(output, "root", "$.output")

    agent = c.optional_mapping(config.get("agent"), "$.agent")
    if agent:
        c.no_unknown_keys(agent, "$.agent", {"profile"})
        c.optional_string(agent, "profile", "$.agent")

    profiles = c.optional_mapping(config.get("agent_profiles") or config.get("agents"), "$.agent_profiles")
    for profile_id, profile in profiles.items():
        if not isinstance(profile_id, str) or not profile_id.strip():
            c.fail("$.agent_profiles", "profile ids must be non-empty strings")
            continue
        validate_agent_profile(profile_id, profile, f"$.agent_profiles.{profile_id}", c)
    if agent.get("profile") and profiles and agent.get("profile") not in profiles:
        c.fail("$.agent.profile", f"unknown agent profile {agent.get('profile')!r}; available: {', '.join(sorted(profiles))}")

    pricing = c.optional_mapping(config.get("pricing"), "$.pricing")
    if pricing:
        c.no_unknown_keys(pricing, "$.pricing", {"enabled", "currency", "model", "source", "rates"})
        c.optional_bool(pricing, "enabled", "$.pricing")
        c.optional_string(pricing, "model", "$.pricing")
        c.optional_string(pricing, "currency", "$.pricing")
        c.optional_string(pricing, "source", "$.pricing")
        rates = c.optional_mapping(pricing.get("rates"), "$.pricing.rates")
        for model_id, rate in rates.items():
            rate_map = c.require_mapping(rate, f"$.pricing.rates.{model_id}")
            c.no_unknown_keys(rate_map, f"$.pricing.rates.{model_id}", {"input_per_1m", "cached_input_per_1m", "output_per_1m"})
            c.optional_number(rate_map, "input_per_1m", f"$.pricing.rates.{model_id}")
            c.optional_number(rate_map, "cached_input_per_1m", f"$.pricing.rates.{model_id}")
            c.optional_number(rate_map, "output_per_1m", f"$.pricing.rates.{model_id}")

    judge = c.optional_mapping(config.get("judge"), "$.judge")
    if judge:
        c.no_unknown_keys(judge, "$.judge", {"enabled", "command", "args", "timeout_seconds", "default_rubric"})
        c.optional_bool(judge, "enabled", "$.judge")
        c.optional_string(judge, "command", "$.judge")
        c.list_of_strings(judge.get("args"), "$.judge.args")
        c.optional_int(judge, "timeout_seconds", "$.judge", min_value=1)
        c.optional_string(judge, "default_rubric", "$.judge")

    _raise_if_errors(c)


def validate_repositories_file(data: dict[str, Any], path: str | Path) -> None:
    c = _Collector(path)
    root = c.require_mapping(data, "$")
    if "repositories" in root:
        c.no_unknown_keys(root, "$", {"repositories"})
    repos_value = root.get("repositories", root)
    if repos_value is None:
        repos_value = {}
    repos = c.require_mapping(repos_value, "$.repositories" if "repositories" in root else "$")
    for repo_id, repo in repos.items():
        if not isinstance(repo_id, str) or not repo_id.strip():
            c.fail("$.repositories", "repository ids must be non-empty strings")
            continue
        validate_repository(repo_id, repo, f"$.repositories.{repo_id}", c)
    _raise_if_errors(c)


def validate_repositories_map(repos: dict[str, Any], path: str | Path = "repositories") -> None:
    c = _Collector(path)
    root = c.require_mapping(repos, "$")
    for repo_id, repo in root.items():
        if not isinstance(repo_id, str) or not repo_id.strip():
            c.fail("$", "repository ids must be non-empty strings")
            continue
        validate_repository(repo_id, repo, f"$.{repo_id}", c)
    _raise_if_errors(c)


def validate_repository(repo_id: str, repo: Any, path: str, c: _Collector) -> None:
    repo_map = c.require_mapping(repo, path)
    repo_type = repo_map.get("type", "local")
    allowed_repo_keys = {"type", "path", "ref"} if repo_type == "local" else {"type", "url", "ref"}
    c.no_unknown_keys(repo_map, path, allowed_repo_keys)
    if repo_type not in {"local", "git"}:
        c.fail(f"{path}.type", "expected 'local' or 'git'")
        return
    if repo_type == "local":
        c.require_string(repo_map, "path", path)
        c.optional_string(repo_map, "ref", path)
    if repo_type == "git":
        c.require_string(repo_map, "url", path)
        c.optional_string(repo_map, "ref", path)


def validate_agent_profile(profile_id: str, profile: Any, path: str, c: _Collector | None = None) -> None:
    own = c is None
    c = c or _Collector(path)
    prof = c.require_mapping(profile, path)
    c.no_unknown_keys(prof, path, {"id", "description", "backend", "type", "command", "args", "availability_args", "availability_timeout_seconds", "input_mode", "output_format", "timeout_seconds", "model", "reasoning_effort", "permission_policy", "github_token_env", "use_logged_in_user", "base_directory", "log_level", "session_idle_timeout_seconds", "env", "prompt_arg", "shell", "isolate_home", "isolated_home", "isolate_codex_home", "isolated_codex_home"})
    backend = prof.get("backend") or prof.get("type") or "generic"
    if not isinstance(backend, str) or backend not in {"codex", "copilot", "copilot-sdk", "generic"}:
        c.fail(f"{path}.backend", "expected one of ['codex', 'copilot', 'copilot-sdk', 'generic']")
    c.optional_string(prof, "description", path)
    if backend in {"codex", "copilot", "generic"}:
        c.require_string(prof, "command", path)
    else:
        c.optional_string(prof, "command", path)
    c.list_of_strings(prof.get("args"), f"{path}.args")
    c.list_of_strings(prof.get("availability_args"), f"{path}.availability_args")
    c.optional_int(prof, "availability_timeout_seconds", path, min_value=1)
    c.enum_value(prof, "input_mode", path, {"stdin", "argument"})
    c.optional_string(prof, "output_format", path)
    c.optional_int(prof, "timeout_seconds", path, min_value=1)
    c.optional_string(prof, "model", path)
    c.optional_string(prof, "reasoning_effort", path)
    c.optional_string(prof, "permission_policy", path)
    c.optional_string(prof, "github_token_env", path)
    c.optional_bool(prof, "use_logged_in_user", path)
    c.optional_string(prof, "base_directory", path, allow_null=True)
    c.optional_string(prof, "log_level", path)
    c.optional_int(prof, "session_idle_timeout_seconds", path, min_value=1)
    c.optional_string(prof, "prompt_arg", path)
    c.optional_bool(prof, "shell", path)
    c.optional_bool(prof, "isolate_home", path)
    c.optional_bool(prof, "isolated_home", path)
    c.optional_bool(prof, "isolate_codex_home", path)
    c.optional_bool(prof, "isolated_codex_home", path)
    env = c.optional_mapping(prof.get("env"), f"{path}.env")
    for key, value in env.items():
        if not isinstance(key, str) or not key:
            c.fail(f"{path}.env", "environment variable names must be non-empty strings")
        if not isinstance(value, (str, int, float, bool)) and value is not None:
            c.fail(f"{path}.env.{key}", "environment values must be scalar")
    if own:
        _raise_if_errors(c)


def validate_mode(mode: dict[str, Any], path: str | Path) -> None:
    c = _Collector(path)
    data = c.require_mapping(mode, "$")
    c.no_unknown_keys(data, "$", {"id", "description", "materialize", "pre_agent", "expectations"})
    c.require_string(data, "id", "$")
    c.optional_string(data, "description", "$")

    materialize = c.optional_mapping(data.get("materialize"), "$.materialize")
    if materialize:
        c.no_unknown_keys(materialize, "$.materialize", {"copy"})
    copies = materialize.get("copy", []) if materialize else []
    if copies is None:
        copies = []
    if not isinstance(copies, list):
        c.fail("$.materialize.copy", f"expected list, got {type(copies).__name__}")
    else:
        for i, entry in enumerate(copies):
            entry_map = c.require_mapping(entry, f"$.materialize.copy[{i}]")
            c.no_unknown_keys(entry_map, f"$.materialize.copy[{i}]", {"from", "to", "overwrite", "allow_absolute_from"})
            c.require_string(entry_map, "from", f"$.materialize.copy[{i}]")
            c.require_string(entry_map, "to", f"$.materialize.copy[{i}]")
            c.optional_bool(entry_map, "overwrite", f"$.materialize.copy[{i}]")
            c.optional_bool(entry_map, "allow_absolute_from", f"$.materialize.copy[{i}]")

    pre_agent = c.optional_mapping(data.get("pre_agent"), "$.pre_agent")
    if pre_agent:
        c.no_unknown_keys(pre_agent, "$.pre_agent", {"setup_scope", "commands"})
    c.enum_value(pre_agent, "setup_scope", "$.pre_agent", {"none", "per-run", "per-case", "per-mode"})
    commands = pre_agent.get("commands", []) if pre_agent else []
    if commands is None:
        commands = []
    if not isinstance(commands, list):
        c.fail("$.pre_agent.commands", f"expected list, got {type(commands).__name__}")
    else:
        for i, command in enumerate(commands):
            validate_pre_agent_command(command, f"$.pre_agent.commands[{i}]", c)

    expectations = c.optional_mapping(data.get("expectations"), "$.expectations")
    if expectations:
        c.no_unknown_keys(expectations, "$.expectations", {"should_verify_with_source", "should_avoid_generic_graph_queries", "max_graph_queries_before_source_fallback"})
        c.optional_bool(expectations, "should_verify_with_source", "$.expectations")
        c.optional_bool(expectations, "should_avoid_generic_graph_queries", "$.expectations")
        c.optional_int(expectations, "max_graph_queries_before_source_fallback", "$.expectations", min_value=0)

    _raise_if_errors(c)


def validate_pre_agent_command(command: Any, path: str, c: _Collector) -> None:
    command_map = c.require_mapping(command, path)
    c.no_unknown_keys(command_map, path, {"id", "name", "argv", "run", "cwd", "timeout_seconds", "required", "shell", "env", "continue_on_failure"})
    c.require_string(command_map, "id", path)
    has_argv = "argv" in command_map
    has_run = "run" in command_map
    if has_argv and has_run:
        c.fail(path, "use either 'argv' or 'run', not both")
    if not has_argv and not has_run:
        c.fail(path, "missing required command field: 'argv' or 'run'")
    if has_argv:
        c.list_of_strings(command_map.get("argv"), f"{path}.argv", required=True)
    if has_run:
        c.require_string(command_map, "run", path)
    c.optional_string(command_map, "cwd", path)
    c.optional_int(command_map, "timeout_seconds", path, min_value=1)
    c.optional_bool(command_map, "required", path)
    c.optional_bool(command_map, "shell", path)
    c.optional_bool(command_map, "continue_on_failure", path)
    env = c.optional_mapping(command_map.get("env"), f"{path}.env")
    for key, value in env.items():
        if not isinstance(key, str) or not key:
            c.fail(f"{path}.env", "environment variable names must be non-empty strings")
        if not isinstance(value, (str, int, float, bool)) and value is not None:
            c.fail(f"{path}.env.{key}", "environment values must be scalar")


def validate_case(case: dict[str, Any], path: str | Path) -> None:
    c = _Collector(path)
    data = c.require_mapping(case, "$")
    c.no_unknown_keys(data, "$", {"id", "title", "category", "difficulty", "repo", "task", "validation", "rubric"})
    c.require_string(data, "id", "$")
    c.require_string(data, "title", "$")
    c.optional_string(data, "category", "$")
    c.optional_string(data, "difficulty", "$")
    c.optional_string(data, "repo", "$")
    c.require_string(data, "task", "$")
    c.optional_string(data, "rubric", "$")

    validation = c.optional_mapping(data.get("validation"), "$.validation")
    if validation:
        c.no_unknown_keys(validation, "$.validation", {"required_symbols", "required_files", "expected_concepts", "forbidden_claims", "forbidden_patterns", "required_evidence", "behavior"})
        _list_of_symbol_specs(c, validation.get("required_symbols"), "$.validation.required_symbols")
        c.list_of_strings(validation.get("required_files"), "$.validation.required_files")
        c.list_of_strings(validation.get("expected_concepts"), "$.validation.expected_concepts")
        c.list_of_strings(validation.get("forbidden_claims"), "$.validation.forbidden_claims")
        c.list_of_strings(validation.get("forbidden_patterns"), "$.validation.forbidden_patterns")
        required_evidence = c.optional_mapping(validation.get("required_evidence"), "$.validation.required_evidence")
        if required_evidence:
            c.no_unknown_keys(required_evidence, "$.validation.required_evidence", {"min_existing_file_references", "min_source_files", "max_missing_cited_files", "require_existing_required_files", "require_symbol_near_file_reference"})
            c.optional_int(required_evidence, "min_existing_file_references", "$.validation.required_evidence", min_value=0)
            c.optional_int(required_evidence, "min_source_files", "$.validation.required_evidence", min_value=0)
            c.optional_int(required_evidence, "max_missing_cited_files", "$.validation.required_evidence", min_value=0)
            c.optional_bool(required_evidence, "require_existing_required_files", "$.validation.required_evidence")
            c.optional_bool(required_evidence, "require_symbol_near_file_reference", "$.validation.required_evidence")
        behavior = c.optional_mapping(validation.get("behavior"), "$.validation.behavior")
        if behavior:
            c.no_unknown_keys(behavior, "$.validation.behavior", {"require_graphify_query", "should_verify_with_source", "avoid_generic_graph_queries", "max_generic_graph_queries"})
            c.optional_bool(behavior, "require_graphify_query", "$.validation.behavior")
            c.optional_bool(behavior, "should_verify_with_source", "$.validation.behavior")
            c.optional_bool(behavior, "avoid_generic_graph_queries", "$.validation.behavior")
            c.optional_int(behavior, "max_generic_graph_queries", "$.validation.behavior", min_value=0)

    _raise_if_errors(c)


def validate_rubric(rubric: dict[str, Any], path: str | Path) -> None:
    c = _Collector(path)
    data = c.require_mapping(rubric, "$")
    c.no_unknown_keys(data, "$", {"id", "extends", "notes", "dimensions"})
    c.require_string(data, "id", "$")
    c.optional_string(data, "extends", "$")
    c.optional_string(data, "notes", "$")
    dimensions = c.optional_mapping(data.get("dimensions"), "$.dimensions")
    for dimension_id, dimension in dimensions.items():
        dim_path = f"$.dimensions.{dimension_id}"
        if not isinstance(dimension_id, str) or not dimension_id.strip():
            c.fail("$.dimensions", "dimension ids must be non-empty strings")
            continue
        dim = c.require_mapping(dimension, dim_path)
        c.no_unknown_keys(dim, dim_path, {"weight", "scale", "description"})
        c.optional_number(dim, "weight", dim_path)
        if "scale" in dim and not isinstance(dim.get("scale"), (str, int)):
            c.fail(f"{dim_path}.scale", "expected string or integer")
    _raise_if_errors(c)


def validate_prompt_style_text(text: str, path: str | Path) -> None:
    c = _Collector(path)
    if "{task}" not in text:
        c.fail("$", "prompt style must contain the {task} placeholder")
    _raise_if_errors(c)
