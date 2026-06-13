from eval_runner.pricing import normalize_usage, estimate_cost


def test_normalize_usage_keeps_cached_tokens():
    usage = normalize_usage({"input_tokens": 1000, "cached_input_tokens": 800, "output_tokens": 50, "reasoning_output_tokens": 10})
    assert usage["uncached_input_tokens"] == 200
    assert usage["cached_input_tokens"] == 800
    assert usage["total_tokens"] == 1050
    assert usage["cache_hit_rate"] == 0.8


def test_estimate_cost_uses_cached_input_rate():
    payload = estimate_cost({"input_tokens": 1000, "cached_input_tokens": 800, "output_tokens": 50}, {
        "enabled": True,
        "model": "x",
        "currency": "USD",
        "rates": {"x": {"input_per_1m": 10, "cached_input_per_1m": 1, "output_per_1m": 20}},
    })
    assert payload["ok"] is True
    assert round(payload["estimated_cost"], 6) == round((200*10 + 800*1 + 50*20)/1_000_000, 6)
