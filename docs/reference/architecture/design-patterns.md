# Design Pattern Guidance

Use patterns only when they clarify a local design problem.

## Default rule

Prefer simple, explicit code. Add a pattern when it improves testability, isolates infrastructure, makes variation explicit, or expresses a domain concept more clearly.

## Patterns used in LORQ

### Command

Use command objects, command handlers, and typed options for CLI behavior.

### Strategy or provider

Use strategy/provider objects for interchangeable behavior such as validation checks, rendering, adapter profiles, or runtime selection.

### Adapter

Use adapters for process execution, file-system interactions, optional provider runtimes, and any external system.

### Factory

Use factories for policy-driven construction, package readers/writers, adapter profile application, and report model assembly when direct construction becomes noisy.

### Builder

Use builders for complex package, report, or test-fixture construction when they improve readability.

### Decorator

Use decorators for logging, diagnostics, timing, retries, or future metrics when cross-cutting behavior should stay outside the core use case.

## Pattern review checklist

Before adding a pattern, ask:

- What concrete variation or boundary does it express?
- Does it make the behavior easier to test?
- Does it reduce duplication without hiding the flow?
- Is the name tied to a LORQ domain concept?
- Could a simpler function or class be clearer?
