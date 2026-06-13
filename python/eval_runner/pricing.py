from __future__ import annotations

import json
from pathlib import Path
from typing import Any

try:
    import yaml  # type: ignore
except Exception:  # pragma: no cover
    yaml = None

PRICING_SCHEMA_VERSION = "agent-eval.pricing.v1"


def _num(value: Any) -> float | None:
    if isinstance(value, bool) or value is None:
        return None
    if isinstance(value, (int, float)):
        return float(value)
    if isinstance(value, str):
        try:
            return float(value)
        except ValueError:
            return None
    return None


def _int(value: Any) -> int | None:
    if isinstance(value, bool) or value is None:
        return None
    if isinstance(value, int):
        return value
    if isinstance(value, float) and value.is_integer():
        return int(value)
    if isinstance(value, str) and value.isdigit():
        return int(value)
    return None


def normalize_usage(usage: dict[str, Any] | None) -> dict[str, Any]:
    """Normalize token usage fields across Codex/OpenAI/Copilot-style payloads.

    Assumption for pricing: input_tokens includes cached input tokens. Cached
    input tokens are separated so billable uncached input can be priced at the
    normal input rate and cached input at the cached-input rate.
    """
    raw = dict(usage or {})
    aliases = {
        "prompt_tokens": "input_tokens",
        "completion_tokens": "output_tokens",
        "inputTokens": "input_tokens",
        "outputTokens": "output_tokens",
        "cached_tokens": "cached_input_tokens",
        "cachedInputTokens": "cached_input_tokens",
        "cacheReadTokens": "cached_input_tokens",
        "cache_read_tokens": "cached_input_tokens",
        "reasoningOutputTokens": "reasoning_output_tokens",
    }
    out: dict[str, Any] = {}
    for key, value in raw.items():
        dest = aliases.get(key, key)
        if dest.endswith("tokens") or dest.endswith("_tokens"):
            ivalue = _int(value)
            if ivalue is not None:
                out[dest] = ivalue
        elif key in {"model", "currency"}:
            out[key] = value
        elif key == "cost":
            n = _num(value)
            if n is not None:
                out[key] = n

    input_tokens = _int(out.get("input_tokens")) or 0
    output_tokens = _int(out.get("output_tokens")) or 0
    cached_input_tokens = _int(out.get("cached_input_tokens")) or 0
    if cached_input_tokens > input_tokens and input_tokens:
        # Defensive: do not allow negative uncached input.
        cached_input_tokens = input_tokens
        out["cached_input_tokens"] = cached_input_tokens
    out.setdefault("cached_input_tokens", cached_input_tokens)
    out.setdefault("uncached_input_tokens", max(input_tokens - cached_input_tokens, 0))
    if input_tokens or output_tokens:
        out.setdefault("total_tokens", input_tokens + output_tokens)
    if input_tokens:
        out["cache_hit_rate"] = cached_input_tokens / input_tokens
    else:
        out.setdefault("cache_hit_rate", None)
    return out


def load_pricing_file(path: str | Path | None) -> dict[str, Any]:
    if not path:
        return {}
    p = Path(path)
    if not p.exists():
        raise FileNotFoundError(f"pricing file not found: {p}")
    text = p.read_text(encoding="utf-8")
    if p.suffix.lower() == ".json":
        return json.loads(text)
    if yaml is None:
        raise RuntimeError("PyYAML is required to load YAML pricing files")
    data = yaml.safe_load(text) or {}
    if not isinstance(data, dict):
        raise ValueError(f"pricing file must contain a mapping: {p}")
    return data


def resolve_pricing_config(config: dict[str, Any] | None, *, model_override: str | None = None, pricing_file: str | None = None) -> dict[str, Any] | None:
    base = dict((config or {}).get("pricing") or {})
    if pricing_file:
        loaded = load_pricing_file(pricing_file)
        # Allow either full {pricing: ...} or direct pricing mapping.
        loaded_pricing = loaded.get("pricing") if isinstance(loaded.get("pricing"), dict) else loaded
        merged = dict(base)
        for k, v in loaded_pricing.items():
            if k == "rates" and isinstance(v, dict):
                rates = dict(merged.get("rates") or {})
                rates.update(v)
                merged["rates"] = rates
            else:
                merged[k] = v
        base = merged
    if model_override:
        base["model"] = model_override
        base["enabled"] = True
    if not base or not base.get("enabled"):
        return None
    return base


def estimate_cost(usage: dict[str, Any] | None, pricing: dict[str, Any] | None) -> dict[str, Any] | None:
    usage = normalize_usage(usage)
    if not pricing:
        return None
    model = pricing.get("model") or usage.get("model")
    rates_by_model = pricing.get("rates") or {}
    rates = rates_by_model.get(model) if model else None
    if not isinstance(rates, dict):
        return {
            "schema_version": PRICING_SCHEMA_VERSION,
            "ok": False,
            "model": model,
            "error": f"pricing model not configured: {model!r}",
        }
    input_rate = _num(rates.get("input_per_1m"))
    cached_rate = _num(rates.get("cached_input_per_1m"))
    output_rate = _num(rates.get("output_per_1m"))
    if input_rate is None or cached_rate is None or output_rate is None:
        return {
            "schema_version": PRICING_SCHEMA_VERSION,
            "ok": False,
            "model": model,
            "error": "pricing rates must include input_per_1m, cached_input_per_1m, output_per_1m",
        }
    uncached = _int(usage.get("uncached_input_tokens")) or 0
    cached = _int(usage.get("cached_input_tokens")) or 0
    output = _int(usage.get("output_tokens")) or 0
    input_cost = uncached * input_rate / 1_000_000
    cached_cost = cached * cached_rate / 1_000_000
    output_cost = output * output_rate / 1_000_000
    return {
        "schema_version": PRICING_SCHEMA_VERSION,
        "ok": True,
        "model": model,
        "currency": pricing.get("currency", "USD"),
        "source": pricing.get("source", "user-configured"),
        "rates_per_1m": {
            "input": input_rate,
            "cached_input": cached_rate,
            "output": output_rate,
        },
        "usage": usage,
        "input_cost": input_cost,
        "cached_input_cost": cached_cost,
        "output_cost": output_cost,
        "estimated_cost": input_cost + cached_cost + output_cost,
    }
