# Eval Summary

## Aggregate by mode and case

| Mode | Prompt | Case | Runs | Completed | Setup OK | Val score | Judge | Hard | Soft | Behavior | Setup ms | Agent ms | Avg tokens | Avg graph q | Avg generic q |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| local-fake | neutral | explain-demo | 1 | 100.0% | 100.0% | 0.9 |  | 100.0% | 50.0% | 100.0% | 0 | 1260 |  | 0 | 0 |

## Mode-level aggregate

| Mode | Prompt | Runs | Completed | Setup OK | Val score | Judge | Validation OK | Behavior OK | Setup ms | Agent ms | Tokens |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| local-fake | neutral | 1 | 100.0% | 100.0% | 0.9 |  | 100.0% | 100.0% | 0 | 1260 |  |

## Per-run scorecard

| Mode | Agent | Prompt | Case | Rep | Status | Setup | Exit | Validation | Behavior | Judge | Graphify q | Generic q | Source reads | Elapsed ms | Tokens |
|---|---|---|---|---:|---|---|---:|---|---|---:|---:|---:|---:|---:|---:|
| local-fake | fake | neutral | explain-demo | 1 | completed | True | 0 | 8/8 hard | 0/0 hard |  | 0 | 0 | 0 | 1260 |  |

## Notes

`Val score` is a deterministic proxy: 80% hard-validator pass rate + 20% soft-validator pass rate when both exist. It is not an LLM quality score.
Behavior metrics are trace-derived heuristics. They are best used to compare runs, not as absolute correctness judgements.
For text-oriented CLIs, command extraction is best-effort from stdout transcripts and may be incomplete compared with Codex JSONL traces.
Judge scores are optional LLM assessments and are reported separately from deterministic validation.
Setup metrics are recorded separately from agent runtime so pre-agent setup costs remain visible.
Agent availability checks are lightweight preflight checks; a failed check warns that the backend may fail before spending tokens.
