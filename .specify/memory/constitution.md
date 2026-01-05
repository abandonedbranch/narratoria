<!--
Sync Impact Report

- Version change: 0.1.0 → 0.1.1
- Modified principles: N/A (initial ratification from template)
- Added sections: None (filled existing template sections)
- Removed sections: None
- Templates requiring updates:
	- ✅ .specify/templates/plan-template.md (remove missing command reference; add explicit Constitution Check gates)
	- ✅ .specify/templates/tasks-template.md (clarify test requirements vs UI Playwright mandate)
	- ✅ .specify/templates/spec-template.md (clarify test obligations for UI work)
	- ⚠ pending: .specify/templates/commands/*.md (directory not present in this repo)

-->

# Narratoria Constitution

## Core Principles

### Spec-First, Drift Visible
Specs in `specs/` are the authoritative system definition. Implementations MUST follow specs; if code and spec conflict, update the spec (or explicitly record a drift/gap in `TODO`) rather than silently “fixing” code. Work that cannot be completed without placeholders MUST be omitted and tracked as a gap in `TODO`.

### Small Interfaces, Composition Over Inheritance
Public interfaces MUST be tiny (1–3 members) and focused on a single concern. Implementations SHOULD compose behavior via collaborators (fields/delegates) and prefer concrete types over generics unless generics improve clarity. Inheritance is forbidden unless required by the framework.

### Immutability and a Pure Core
Data SHOULD be immutable by default (`record`, `readonly struct`, `init`, `required`), using `with` expressions for copies. Logic SHOULD be pure and deterministic; IO/time/random MUST be isolated behind small interfaces. Shared mutable state is forbidden.

### Async Discipline and Cancellation
All async flows MUST accept and honor `CancellationToken`. Concurrency MUST be structured (owned tasks, `async/await`, `Channel<T>` when appropriate); fire-and-forget is forbidden unless explicitly self-managed. Streaming/cancellation semantics MUST be testable and observable.

### Deterministic Testing and Coverage by Risk
Tests MUST be deterministic: inject time/random/IO, avoid external dependencies, and use table-driven patterns where appropriate. UI component behavior MUST have end-to-end coverage using Playwright for .NET in addition to applicable unit tests. Avoid adding tests not required by the relevant spec, except when needed to preserve determinism or prevent regressions introduced by the change.

## Quality Gates

- Changes MUST be minimal and focused; do not refactor unrelated code.
- Constructors/factories MUST enforce invariants; prefer returning interfaces when callers only need behavior.
- Expected outcomes SHOULD use Result-style patterns; exceptions are for exceptional failures, and MUST be wrapped with context before rethrowing.
- Namespaces MUST align with folders; keep one concept per file; minimize public surface area.
- If work is unfinished or blocked, record it in `TODO` (drift/gaps/debt) before considering the work “done.”

## Development Workflow

- All feature work SHOULD be driven by specs in `specs/` (or a new spec added per `SPEC`).
- PRs MUST be reviewed for constitution compliance; violations are release-blocking.
- When a spec is ambiguous, update the spec first; do not guess.
- When implementing UI changes, add/maintain Playwright E2E coverage against stable routes.

## Governance
This constitution supersedes all other practices in the repo.

- Amendment process: propose changes via PR that includes (a) rationale, (b) any required template updates, and (c) any necessary migration notes.
- Versioning policy: constitution versions follow SemVer.
	- MAJOR: removes/redefines a principle or materially changes governance.
	- MINOR: adds a principle/section or materially expands mandatory guidance.
	- PATCH: clarifies wording without changing meaning.
- Compliance expectation: feature plans MUST include a “Constitution Check” gate; reviews MUST block merges on MUST-level violations.

**Version**: 0.1.1 | **Ratified**: 2026-01-04 | **Last Amended**: 2026-01-04
