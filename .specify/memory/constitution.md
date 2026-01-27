<!--
Sync Impact Report
==================
Version change: 1.0.0 → 1.1.0
Bump rationale: Added Agent Skills Standard integration guidance

Modified principles: None (clarifications only)
Added sections:
  - Skills vs Scripts (Agent Skills Standard) section
  - Narrator AI clarification in Principle II
  - Skill-owned data storage guidance
Removed sections: None

Templates requiring updates:
  ⚠ plan-template.md - Should reference skills vs tools distinction
  ⚠ spec-template.md - Should accommodate skill specifications
  ✅ tasks-template.md - No changes needed

Follow-up TODOs:
  - Create Spec 002 for plan generation and skill discovery
  - Implement core skills (storyteller, memory, reputation, dice-roller)
  - Design skill configuration UI
-->

# Narratoria Constitution

Narratoria is a cross-platform, idiomatic Dart+Flutter application that serves as the core runtime for interactive, agent-driven storytelling. The architecture is explicitly testable, composable, and language-agnostic. All application logic that belongs to the Narratoria client—UI, state management, networking, agent orchestration—is authored in Dart and follows clear separation of concerns and unit-testing best practices.

## Core Principles

### I. Dart+Flutter First

All Narratoria client logic—UI, state management, networking, agent orchestration—MUST be authored in idiomatic Dart using Flutter. Clear separation of concerns and unit-testing best practices are mandatory. No application logic may bypass the Dart runtime except through the defined tool protocol boundary.

**Rationale**: A single, well-understood runtime simplifies testing, debugging, and maintenance. Flutter's cross-platform capabilities ensure consistent behavior across macOS, Windows, and Linux without platform-specific forks.

### II. Protocol-Boundary Isolation

All external tool interactions MUST occur through the defined Tool Protocol (Spec 001). Tools run as independent OS processes, communicate via structured NDJSON on stdout, and receive input via stdin or command-line arguments. The runtime remains stable even as tools evolve independently.

**Rationale**: Tool Protocol provides predictability, modularity, and long-term evolvability. Protocol boundaries enable tools to be authored in any language (Rust, Go, Python, etc.) without compromising the Dart runtime's integrity.

### III. Single-Responsibility Tools

Each external tool MUST perform one well-defined task (e.g., generate an image, synthesize audio, compute a state update). Tools MUST NOT bundle unrelated capabilities. Tool authors MUST adhere to Spec 001 semantics: emit `log`, `state_patch`, `asset`, `ui_event`, `error`, and exactly one `done` event per invocation.

**Rationale**: Single-responsibility design keeps tools small, testable, and replaceable. It enables independent versioning and reduces blast radius when a tool fails or is updated.

### IV. Graceful Degradation

Unsupported media types, UI events, or tool capabilities MUST degrade gracefully without breaking the user experience. Narratoria MUST display placeholder or degraded UI for unknown asset kinds and MUST log unsupported events without crashing. Users MUST always maintain narrative continuity even when optional capabilities are unavailable.

**Rationale**: Interactive storytelling experiences should never hard-fail due to missing optional content. Graceful degradation preserves immersion and allows the ecosystem to grow without strict version lockstep.

### V. Testability and Composability

The architecture MUST be explicitly testable and composable. All Dart modules MUST support unit testing in isolation. Integration tests MUST verify tool protocol interactions via mock processes. Acceptance tests MUST validate end-to-end user journeys without requiring live external services.

**Rationale**: Predictable behavior requires verifiable code. Composability ensures features can be developed, tested, and deployed independently, enabling iterative delivery and reducing regression risk.

## Skills vs Scripts (Agent Skills Standard)

Narratoria follows the [Agent Skills Standard](https://agentskills.io/specification) for organizing narrator capabilities. This approach enables modular, composable storytelling features while maintaining constitutional compliance.

### What is a Skill?

A **skill** is a capability bundle that MAY include:

- **Behavioral prompts** (`prompt.md`): Instructions that guide the narrator AI's behavior (e.g., narrative style, tone, genre conventions)
- **Scripts** (`scripts/` directory): Executable tools that perform computations, generate content, or manage data
- **Configuration schema** (`config-schema.json`): Defines user-configurable settings for the skill
- **User configuration** (`config.json`): User-provided values (API keys, preferences, paths)
- **Data storage** (`data/` directory): Skill-owned databases, caches, or persistent state

### Constitutional Status of Skills

**✅ IN-PROCESS (Dart runtime)**:
- Skill metadata (`skill.json`)
- Behavioral prompts (`prompt.md`)
- Configuration schemas and values
- Skill discovery and loading logic

**✅ OUT-OF-PROCESS (Principle II compliance)**:
- Scripts in `scripts/` directory MUST follow Spec 001 NDJSON protocol
- Scripts run as independent OS processes
- Scripts communicate via stdin/stdout only
- Scripts MAY be written in any language

**✅ PROMPT-ONLY SKILLS (no scripts)**:
- Skills without scripts are pure behavioral modifications
- These inject prompt instructions into the narrator AI system context
- No protocol boundary crossing occurs (fully in-process)

### Narrator AI vs External Tools

**Narrator AI** (in-process, Principle II exception):
- Small local language model for plan generation (e.g., Gemma 2B, Llama 3.2 3B)
- Converts player input → Plan JSON
- Selects relevant skills and scripts for execution
- Lives entirely within Dart process (no network calls, no hosted APIs)

**Skill Scripts** (out-of-process, Principle II compliant):
- Rich storytelling generators (MAY use hosted APIs like Claude, GPT-4)
- Memory systems, reputation tracking, dice rolling
- Asset generation (images, audio, music)
- Rules engines for specific game systems (D&D 5e, Pathfinder, etc.)

**Rationale**: The narrator AI performs structured reasoning (plan generation) locally and cheaply. Complex, creative, or network-dependent work happens in skill scripts, which can fail gracefully without crashing the application.

### Skill-Owned Data

Each skill MAY maintain its own data storage in `skills/<skill-name>/data/`. This storage is:
- **Skill-private**: Other skills MUST NOT directly access another skill's data directory
- **Persistent**: Survives application restarts
- **Portable**: Can use SQLite, JSON files, or other local formats
- **Testable**: Can be mocked or seeded with test data

Examples:
- `skills/memory/data/memories.db` - Semantic memory embeddings and summaries
- `skills/reputation/data/reputation.db` - Faction standings and relationship graphs
- `skills/world-state/data/campaign.db` - NPCs, locations, items, timeline
- `skills/character-sheet/data/characters/` - Player and NPC character files

### Graceful Degradation with Skills

Per Principle IV, skills MUST degrade gracefully:
- **Missing skill**: Narrator continues without that capability
- **Script failure**: Script emits `done.ok=false`, plan executor logs error, narrative continues with fallback
- **Configuration incomplete**: Skill displays setup prompt in UI but does not crash
- **Network failures**: Skill scripts that use hosted APIs MUST fall back to local models or simpler behavior

**Example**: The `storyteller` skill configured for Claude API will fall back to local Ollama if network unavailable, and ultimately to simple template-based narration if no LLM is accessible.

## Technology Stack

| Layer | Technology | Notes |
|-------|------------|-------|
| Client Runtime | Dart 3.x + Flutter | Cross-platform (macOS, Windows, Linux) |
| Narrator AI | Local LLM (Gemma, Llama 3.2) | In-process plan generation |
| Skills Framework | Agent Skills Standard | See agentskills.io/specification |
| Tool Protocol | NDJSON over stdin/stdout | See Spec 001 (scripts use this) |
| Skill Scripts | Any (Rust, Go, Python, etc.) | Must comply with Spec 001 |
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

**Version**: 1.1.0 | **Ratified**: 2026-01-24 | **Last Amended**: 2026-01-26
