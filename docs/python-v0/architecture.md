# Architecture contract

`agent-eval` evaluates an agent by changing the execution environment, not by injecting mode-specific workflow instructions into the prompt.

A run is identified by:

```text
mode × prompt_style × case × repetition
```

Each real run follows this order:

```text
1. create a fresh execution worktree
2. materialize mode files from execution/
3. run pre-agent commands in that worktree
4. render the prompt
5. launch the configured agent backend
6. normalize backend trace events
7. run deterministic validators
8. optionally run the judge
9. write reports
```

The primary public contract is the filesystem layout, YAML schemas, normalized event schema, validation result schema, and scorecard columns. Python implementation details are not part of the contract.

## Stable concepts

- modes are declarative environments
- cases are tasks plus hidden validation
- prompt style is an eval axis, not a mode property
- setup cost is recorded separately from agent cost
- generated artifacts are not copied by default; setup commands generate them per run
- backend traces are normalized before behavior validation

## Non-goals

- exact Markdown wording is not stable
- raw backend traces are not stable
- optional judge prompt wording is not stable
