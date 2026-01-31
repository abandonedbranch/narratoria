<!--
Sync Impact Report
==================
Version change: 1.1.0 → 1.2.0
Bump rationale: Added plan generation robustness sub-principle to Principle IV

Modified principles: Principle IV (Graceful Degradation) - expanded with robustness requirements
Added sections:
  - IV.A Plan Generation Robustness (Sub-Principle)
    - Skill failure isolation
    - Bounded retry loops (3 per-tool, 3 per-plan-execution, 5 per-plan-generation)
    - Skill error states
    - Plan failure semantics
    - Replan strategy with tracing
    - Graceful fallback to template narration
    - Timeout and resource bounds
Removed sections: None

Templates requiring updates:
  ⚠ plan-template.md - Should reference plan execution robustness
  ✅ spec-template.md - No changes needed
  ✅ tasks-template.md - No changes needed

Follow-up TODOs:
  - Update Spec 001 with extended Plan JSON schema (DONE)
  - Update Spec 002 with execution engine requirements (DONE)
  - Create data-model.md for Spec 002
  - Implement replan loop in plan executor
  - Track skill error states across plan attempts
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

### IV.A Plan Generation Robustness (Sub-Principle)

The narrator AI's plan generation system MUST be resilient to skill failures and environmental constraints:

1. **Skill Failure Isolation**: When a skill script fails, only that skill's availability is affected. The planner MUST NOT modify its fundamental logic or decision-making process.

2. **Bounded Retry Loops**:
   - **Per-Tool**: MAX 3 retries per skill script invocation before marking skill as ERROR_STATE (configurable per skill via `retryPolicy`)
   - **Per-Plan-Execution**: MAX 3 attempts to execute a single plan before requesting new plan from generator
   - **Per-Plan-Generation**: MAX 5 attempts to generate viable plans before escalating to user with graceful fallback
   
   Exceeding these limits indicates a systemic problem that demands user intervention, not infinite looping.

3. **Skill Error States**:
   - `healthy`: Available for planning
   - `degraded`: Available but may be slow/unreliable (retry anyway)
   - `temporaryFailure`: Network timeout or transient issue (retry with backoff)
   - `permanentFailure`: Cannot recover in this session (disable and replan without)
   
   Plan generator MUST consult error states before selecting skills.

4. **Plan Failure Semantics**:
   - When a required skill fails, dependent tools abort execution; plan fails overall
   - When an optional skill fails, dependent tasks proceed with degraded input; plan may succeed
   - Partial success is preferable to hard failure; users should see degraded narration rather than crashes

5. **Replan Strategy**:
   - Plan executor returns full execution trace with failed tool list and specific error reasons
   - Plan generator disabled failed skills and attempts new plan with remaining capability
   - Each replan increments `metadata.generationAttempt` and sets `parentPlanId` to previous plan UUID
   - After 5 planner attempts: cease retrying, display clear error to user, offer session recovery options

6. **Graceful Fallback**:
   - If planner exhausts 5 attempts: provide simple template-based narration (e.g., "The narrator pauses as the story unfolds...")
   - Log detailed error context with plan IDs, failed tools, retry counts, and timestamps
   - Allow user to continue story or restart session
   - NEVER crash; NEVER show "Internal Error" to player
   - Exception: protocol violations (invalid JSON, circular dependencies) are logged as bugs for developer review

7. **Timeout and Resource Bounds**:
   - Per-skill timeout: 30 seconds (configurable)
   - Per-plan execution timeout: 60 seconds (configurable)
   - Per-plan generation timeout: 5 seconds (strict, no LLM hangs)
   - If timeout exceeded: treat as `temporaryFailure`, retry or disable skill as appropriate

**Rationale**: Players' immersion depends on continuous storytelling. Cascading failures (one skill breaks planner, planner crash breaks UI) must not occur. Bounded loops prevent resource exhaustion. Explicit error states let the system make intelligent tradeoffs between perfect execution and acceptable degradation.

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
| Narrator AI | Local LLM (Gemma 2B, Llama 3.2 3B, Qwen 2.5 3B) | In-process plan generation |
| Skills Framework | Agent Skills Standard | See agentskills.io/specification |
| Tool Protocol | NDJSON over stdin/stdout | See Spec 001 (scripts use this) |
| Skill Scripts | Any (Rust, Go, Python, etc.) | Must comply with Spec 001 |
| State Management | Provider | ChangeNotifier pattern for state management |
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

**Version**: 1.2.0 | **Ratified**: 2026-01-24 | **Last Amended**: 2026-01-27
