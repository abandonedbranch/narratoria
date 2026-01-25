<!--
Sync Impact Report
==================
Version change: 0.0.0 → 1.0.0
Bump rationale: Initial ratification of project constitution

Modified principles: N/A (first version)
Added sections:
  - Core Principles (5 principles)
  - Technology Stack section
  - Development Workflow section
  - Governance rules
Removed sections: N/A

Templates requiring updates:
  ✅ plan-template.md - Constitution Check section now has defined gates
  ✅ spec-template.md - No changes needed (already generic)
  ✅ tasks-template.md - No changes needed (already generic)

Follow-up TODOs: None
-->

# Narratoria Constitution

Narratoria is a cross-platform, idiomatic Dart+Flutter application that serves as the core runtime for interactive, agent-driven storytelling. The architecture is explicitly testable, composable, and language-agnostic. All application logic that belongs to the Narratoria client—UI, state management, networking, agent orchestration—is authored in Dart and follows clear separation of concerns and unit-testing best practices.

## Core Principles

### I. Dart+Flutter First

All Narratoria client logic—UI, state management, networking, agent orchestration—MUST be authored in idiomatic Dart using Flutter. Clear separation of concerns and unit-testing best practices are mandatory. No application logic may bypass the Dart runtime except through the defined tool protocol boundary.

**Rationale**: A single, well-understood runtime simplifies testing, debugging, and maintenance. Flutter's cross-platform capabilities ensure consistent behavior across macOS, Windows, and Linux without platform-specific forks.

### II. Protocol-Boundary Isolation

Narratoria MUST NOT load untrusted code into its own process. All external tool interactions MUST occur through the defined Tool Protocol (Spec 001). Tools run as independent OS processes, communicate via structured NDJSON on stdout, and receive input via stdin or command-line arguments. The runtime remains stable even as tools evolve independently.

**Rationale**: Process isolation provides safety, modularity, and long-term evolvability. Protocol boundaries enable tools to be authored in any language (Rust, Go, Python, etc.) without compromising the Dart runtime's integrity.

### III. Single-Responsibility Tools

Each external tool MUST perform one well-defined task (e.g., generate an image, synthesize audio, compute a state update). Tools MUST NOT bundle unrelated capabilities. Tool authors MUST adhere to Spec 001 semantics: emit `log`, `state_patch`, `asset`, `ui_event`, `error`, and exactly one `done` event per invocation.

**Rationale**: Single-responsibility design keeps tools small, testable, and replaceable. It enables independent versioning and reduces blast radius when a tool fails or is updated.

### IV. Graceful Degradation

Unsupported media types, UI events, or tool capabilities MUST degrade gracefully without breaking the user experience. Narratoria MUST display placeholder or degraded UI for unknown asset kinds and MUST log unsupported events without crashing. Users MUST always maintain narrative continuity even when optional capabilities are unavailable.

**Rationale**: Interactive storytelling experiences should never hard-fail due to missing optional content. Graceful degradation preserves immersion and allows the ecosystem to grow without strict version lockstep.

### V. Testability and Composability

The architecture MUST be explicitly testable and composable. All Dart modules MUST support unit testing in isolation. Integration tests MUST verify tool protocol interactions via mock processes. Acceptance tests MUST validate end-to-end user journeys without requiring live external services.

**Rationale**: Predictable behavior requires verifiable code. Composability ensures features can be developed, tested, and deployed independently, enabling iterative delivery and reducing regression risk.

## Technology Stack

| Layer | Technology | Notes |
|-------|------------|-------|
| Client Runtime | Dart 3.x + Flutter | Cross-platform (macOS, Windows, Linux) |
| Tool Protocol | NDJSON over stdin/stdout | See Spec 001 |
| Tool Languages | Any (Rust, Go, Python, etc.) | Must comply with Spec 001 |
| State Management | TBD (Provider, Riverpod, Bloc) | Must support unit testing |
| Testing | `flutter_test`, integration_test | Contract + integration + unit layers |

## Development Workflow

1. **Feature branches**: Named `###-feature-name` (e.g., `001-tool-protocol-spec`).
2. **Spec-first**: Every feature begins with a specification in `specs/###-feature-name/spec.md`.
3. **Plan-then-implement**: Implementation plans document technical context, constitution compliance, and structure before coding.
4. **Constitution check**: All plans and PRs MUST verify compliance with these principles. Violations require explicit justification in the Complexity Tracking section.
5. **Test coverage**: Unit tests for Dart modules; contract tests for protocol compliance; integration tests for tool interactions.

## Governance

This constitution supersedes all other development practices. Amendments require:
1. A documented rationale explaining the change.
2. Version increment following semantic versioning (MAJOR for principle removal/redefinition, MINOR for additions, PATCH for clarifications).
3. Update propagation to dependent templates and agent context files.

All PRs and code reviews MUST verify compliance with these principles. Complexity that violates a principle MUST be justified and tracked.

**Version**: 1.0.0 | **Ratified**: 2026-01-24 | **Last Amended**: 2026-01-24
