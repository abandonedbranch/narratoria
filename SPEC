## Rules

RULE: All implementations must comply with the rules in [CONTRIB.md](CONTRIB.md).

RULE: Audience and intent
- All specs and project documentation (EXCEPT `README.md`) are written for GPT-5.x models only.
- Optimize for deterministic regeneration of the project in a blank workspace.
- Treat specs as the authoritative implementation; code is a derived byproduct.
- Prefer precise, machine-checkable constraints (MUST/SHOULD/MAY), explicit failure modes, and explicit observability fields.
- Do not spend effort on human-friendly narrative, pedagogy, or stylistic prose.

## Spec Directory

- All specs live in `specs/`.
- Template: `specs/_template.spec.md`.
- Completed specs:
  - `specs/indexeddb-storage-service.spec.md`
  - `specs/storage-quota-awareness-service.spec.md`
  - `specs/openai-api-service.spec.md`
  - `specs/narrator-pipeline-service.spec.md`
  - `specs/narration-persistence-middleware.spec.md`
  - `specs/narration-provider-dispatch-middleware.spec.md`
  - `specs/narration-content-guardian-middleware.spec.md`
  - `specs/narration-system-prompt-middleware.spec.md`
  - `specs/narration-attachment-ingestion-service.spec.md`
- If any spec is not yet complete, add it under a "Not completed" list with owner and expected completion date.
- Implementors must explicitly state what is complete and what remains when updating this file.
- IMPORTANT: After any work on a spec/implementation pair (including drift reduction), update this file with an explicit status note. Listing a spec as “Completed” is not sufficient unless “what remains” is explicitly “none”.

## Status Reporting (Required)

When you touch a spec or its implementation, you must also update this file with a brief status note.

Minimum required update (pick one):
- Add/refresh a “What remains” list (even if the answer is “none”).
- Or, if the work is partial, move the spec under a “Not completed” list with an owner.

The goal is to keep SPEC.md truthful about drift and remaining work, not just about whether a spec document exists.

## What remains

- Provider dispatch: ensure metrics match spec and review any remaining drift around stream composition vs terminal-stage semantics.
- Attachment ingestion: drop or properly implement PDF support; ensure purge semantics remain best-effort but reliable.
- IndexedDB storage: fill out remaining CRUD surface (e.g., Get/Delete) and align observability fields where practical.

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
