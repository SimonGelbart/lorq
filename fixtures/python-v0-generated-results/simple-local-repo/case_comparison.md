# Per-case Comparative Report

## explain-demo — neutral

Heuristic winner: **local-fake**

| Mode | Runs | Completed | Setup OK | Val score | Judge | Hard | Soft | Behavior OK | Setup ms | Agent ms | Tokens | Avg graph q | Avg generic q | Avg source reads |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| local-fake | 1 | 100.0% | 100.0% | 0.9 |  | 100.0% | 50.0% | 100.0% | 0 | 1260 |  | 0 | 0 | 0 |

Interpretation checklist:

- Prefer higher hard pass rate before considering cost.
- Check whether behavior improvements come with unacceptable token/time regressions.
- Treat the heuristic winner as a triage signal, not as a final judgement.

