# Pricing and cached-token accounting

`agent-eval` records token usage from structured backend traces when available:

- `input_tokens`
- `cached_input_tokens`
- `uncached_input_tokens`
- `output_tokens`
- `reasoning_output_tokens`
- `total_tokens`
- `cache_hit_rate`

Cost estimation is optional because prices change. Enable it with a configured model:

```bash
python3 -m eval_runner.cli \
  --pricing-model gpt-5.5 \
  --no-judge \
  ...
```

Or provide an explicit pricing file:

```bash
python3 -m eval_runner.cli \
  --pricing-file pricing/openai-pricing.example.yaml \
  --pricing-model gpt-5.5 \
  ...
```

Prices are per 1M tokens. The evaluator assumes `input_tokens` includes cached input tokens, so estimated cost is:

```text
uncached_input = input_tokens - cached_input_tokens
cost = uncached_input * input_rate / 1_000_000
     + cached_input_tokens * cached_input_rate / 1_000_000
     + output_tokens * output_rate / 1_000_000
```

`reasoning_output_tokens` is reported separately for analysis. It is not added as a second output charge by default; the estimator uses `output_tokens` as the output-billed token count.

The bundled pricing example is a convenience snapshot. Check OpenAI's pricing page before formal cost reporting.
