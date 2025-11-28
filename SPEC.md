## Rules

RULE: All implementations must comply with the rules in CONTRIB.md.

## Spec Directory

- All specs live in `specs/`.
- Template: `specs/_template.spec.md`.
- Current specs:
  - `specs/openai-api-service.spec.md`
  - `specs/narrator-pipeline-service.spec.md`
- Add any new spec to `specs/` and link it here.

## Format

Mode
Behavior (WHAT / INPUT / OUTPUT / FAILS / NEVER)
Policies
Invariants
Observability
Never
Non-goals
Output

## Clarifications

- Mode: `isolated` (pure function, no shared state), `compositional` (cooperates with collaborators), `stateful` (reads/writes owned state).
- Behavior: spell out caller obligations (auth/session/state loading), allowed side effects, and typed inputs/outputs referencing existing DTOs/interfaces when possible.
- Policies: include retry/timeout/idempotency/concurrency/cancellation requirements; async methods accept `CancellationToken`.
- Invariants: note determinism, thread-safety, and constraints that must be true for all executions.
- Observability: define required log/event fields (e.g., `trace_id`, `request_id`, `stage`, `elapsed_ms`, `status`, `error_class`) and when they emit.
- Never/Non-goals: list forbidden behaviors and explicitly excluded scope to prevent drift.
- Output: minimal implementation only—no commentary, no TODOs.

## Defaults

Mode: Isolated behavior
Output: Minimal implementation only. No commentary.

## Checklist

- Mode selected and justified.
- Inputs/outputs typed and reference known types.
- Failure modes are structured with required side effects.
- Policies cover retry/timeout/idempotency/concurrency/cancellation.
- Invariants and observability are testable and log fields are defined.
- Never/Non-goals prevent unwanted side effects.
- Spec is added to `specs/` and linked above.
