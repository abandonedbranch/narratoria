# Research: Plan Generation and Skill Discovery

**Feature**: 002-plan-generation-skills  
**Date**: 2026-01-27  
**Status**: Consolidated

## Overview

This document consolidates research findings for implementing the plan generation and skill discovery system in Narratoria. Key decisions include: local LLM for plan generation (Flutter AI Toolkit + Ollama), Agent Skills Standard for skill organization, topological sorting for dependency resolution, and bounded retry loops for robustness.

---

## 1. Local LLM Integration for Plan Generation

### Decision: Flutter AI Toolkit + Ollama Backend

**Rationale**: Narratoria requires offline-capable plan generation that runs entirely in-process (Constitution II exception). The narrator AI converts player input into structured Plan JSON without network calls.

**Alternatives Considered**:

| Option | Pros | Cons | Verdict |
|--------|------|------|---------|
| **Flutter AI Toolkit** | Official pub.dev package, supports multiple backends (Ollama, llama.cpp, Gemini Nano), active maintenance, simple API | Requires Dart integration, limited model selection compared to Python ecosystem | ✅ **SELECTED** - Best fit for Flutter, clean API, extensible |
| llama.cpp via FFI | Direct native integration, maximum control, no external dependencies | Complex FFI bindings, manual memory management, platform-specific builds | ❌ Rejected - Too complex for MVP, violates Dart-first principle |
| Gemini Nano (on-device) | Google-backed, optimized for mobile/desktop, good performance | Limited availability (Pixel devices initially), API restrictions, unclear desktop support | ❌ Rejected - Not yet viable for cross-platform desktop |
| Hosted APIs (Claude, GPT-4) | Best quality reasoning, extensive context windows | Requires network, violates offline requirement, costs per-token | ❌ Rejected - Violates Constitution II for narrator AI (OK for skill scripts though) |

**Recommended Models**:

- **MVP**: `llama3.2:3b` (3 billion parameters, ~2GB RAM, fast inference, good instruction-following)
- **Production**: `mistral:7b` (7 billion parameters, ~4GB RAM, better reasoning, still fast)
- **Fallback**: Simple pattern-based planner (regex + templates) if LLM unavailable

**Implementation Notes**:

- Use `flutter_ai_toolkit` package from pub.dev
- Install Ollama locally, pull models via `ollama pull llama3.2:3b`
- Integrate via `LLMService` class in `services/narrator_ai.dart`
- Provide structured prompt with skill list, session state, and player input
- Parse JSON response into `PlanJson` model with validation

**References**:

- Flutter AI Toolkit: https://pub.dev/packages/flutter_ai_toolkit
- Ollama: https://ollama.ai/
- Llama 3.2: https://ai.meta.com/llama/

---

## 2. Agent Skills Standard Integration

### Decision: Adopt Agent Skills Standard for Skill Organization

**Rationale**: Narratoria needs a modular, extensible plugin architecture for storytelling capabilities. Agent Skills Standard provides a well-documented specification for skill discovery, configuration, and behavioral prompts.

**Alternatives Considered**:

| Option | Pros | Cons | Verdict |
|--------|------|------|---------|
| **Agent Skills Standard** | Formal specification, industry adoption, clear separation of concerns, supports prompt-only and script-based skills | Relatively new (2024), limited tooling | ✅ **SELECTED** - Perfect fit for campaign-authoring use case |
| Custom Plugin System | Full control, tailored to Narratoria | Reinventing wheel, maintenance burden, poor interoperability | ❌ Rejected - No compelling advantage over standard |
| VSCode Extension Model | Mature, well-understood | Web-focused, requires Node.js runtime, JavaScript dependencies | ❌ Rejected - Wrong domain, violates Dart-first |
| Python Plugin Ecosystem | Rich ecosystem, many examples | Language mismatch, requires Python runtime | ❌ Rejected - Not Flutter-compatible |

**Key Features**:

- **Skill Manifest** (`skill.json`): Name, version, description, author, license
- **Behavioral Prompts** (`prompt.md`): Markdown instructions for narrator AI
- **Configuration Schema** (`config-schema.json`): JSON Schema for user settings
- **User Configuration** (`config.json`): User-provided values (API keys, preferences)
- **Scripts** (`scripts/` directory): Executable tools following Spec 001 NDJSON protocol
- **Data Storage** (`data/` directory): Skill-owned persistent storage

**Implementation Notes**:

- Scan `skills/` directory at startup
- Parse `skill.json` manifests, validate required fields
- Load `prompt.md` and inject into plan generator system context
- Discover scripts in `scripts/` subdirectories
- Skills without scripts are pure behavioral modifications (prompt-only)

**References**:

- Agent Skills Standard: https://agentskills.io/specification

---

## 3. Plan Execution Engine Architecture

### Decision: DAG-Based Executor with Topological Sort

**Rationale**: Plan JSON defines tool dependencies and execution order. Circular dependencies must be detected and rejected. Topological sorting (Kahn's algorithm) provides deterministic execution order and cycle detection.

**Alternatives Considered**:

| Option | Pros | Cons | Verdict |
|--------|------|------|---------|
| **Topological Sort (Kahn's)** | Standard CS algorithm, O(V+E) complexity, detects cycles, well-tested | Requires in-degree calculation | ✅ **SELECTED** - Industry standard, efficient, reliable |
| DFS-Based Cycle Detection | Simple recursive implementation | No execution order (separate pass needed), harder to debug | ❌ Rejected - Less efficient, two-pass approach |
| Linear Execution (No DAG) | Simplest implementation | Cannot handle dependencies, breaks parallelism | ❌ Rejected - Insufficient for Plan JSON schema |
| Dataflow Framework (like Airflow) | Rich ecosystem, battle-tested | Overkill for in-memory execution, external dependencies | ❌ Rejected - Too heavy for Narratoria |

**Execution Semantics**:

- **`dependencies` array**: Tool B depends on Tool A → A executes before B
- **`required` flag**: If true and tool fails → abort dependents; if false → dependents proceed with null/empty
- **`async` flag**: If true → may run parallel with unrelated tasks; if false → sequential
- **`retryPolicy`**: Per-tool max retries and exponential backoff (e.g., `{maxRetries: 3, backoffMs: 500}`)

**Algorithm Pseudocode**:

```text
1. Build dependency graph from Plan JSON tools array
2. Calculate in-degree for each node (count of incoming edges)
3. Enqueue all nodes with in-degree = 0
4. While queue not empty:
   a. Dequeue node N
   b. Execute tool N (with retries per retryPolicy)
   c. For each neighbor M of N:
      - Decrement M's in-degree
      - If M's in-degree = 0, enqueue M
5. If any nodes remain unvisited → circular dependency detected
```

**Implementation Notes**:

- Implement in `services/plan_executor.dart`
- Use `Queue<ToolInvocation>` for BFS traversal
- Track `Map<String, int>` for in-degree counts
- Emit `ExecutionResult` with full trace, failed tools, retry counts
- Support async execution via `Future.wait()` for parallel tasks

**References**:

- Kahn's Algorithm: https://en.wikipedia.org/wiki/Topological_sorting#Kahn's_algorithm

---

## 4. Retry Logic and Failure Handling

### Decision: Bounded Retry Loops (3-3-5 Pattern)

**Rationale**: Storytelling experiences must degrade gracefully without infinite loops. Constitution IV.A specifies bounded retry limits at three levels.

**Retry Levels**:

1. **Per-Tool**: MAX 3 retries per skill script invocation (configurable via `retryPolicy` in Plan JSON)
   - Example: `narrate.dart` fails due to network timeout → retry 3 times with exponential backoff (500ms, 1000ms, 2000ms)
   - After 3 failures → mark tool as FAILED, check `required` flag
   
2. **Per-Plan-Execution**: MAX 3 attempts to execute a single plan
   - Example: Plan with 5 tools fails because Tool 3 (required) failed after retries → executor returns failure + execution trace
   - Plan generator receives feedback, disables failed skill, generates new plan (attempt 2)
   - After 3 plan execution attempts → escalate to plan generation level
   
3. **Per-Plan-Generation**: MAX 5 attempts to generate viable plans
   - Example: Attempt 1: Plan uses Skill A → A fails. Attempt 2: Plan uses Skill B → B fails. ... Attempt 5: All options exhausted
   - After 5 plan generation attempts → fall back to template-based narration ("The narrator pauses as the story unfolds...")

**Skill Error States**:

- `healthy`: Available for planning
- `degraded`: Available but slow/unreliable (retry anyway)
- `temporaryFailure`: Network timeout or transient issue (retry with backoff, then disable if persistent)
- `permanentFailure`: Cannot recover in this session (disable immediately, do not retry)

**Failure Propagation**:

- **required=true**: Dependent tools abort, plan fails overall
- **required=false**: Dependent tools proceed with null/empty input, plan may succeed with degraded output

**Implementation Notes**:

- Track retry counts in `ToolResult.retryCount`
- Implement exponential backoff: `backoffMs * 2^attemptNumber`
- Plan executor returns `ExecutionResult.canReplan=true` if failures recoverable
- Plan generator maintains `Set<String> disabledSkills` across attempts
- After max attempts: log detailed error context (plan IDs, failed tools, retry counts, timestamps)

**References**:

- Constitution IV.A: `.specify/memory/constitution.md`
- Exponential Backoff: https://en.wikipedia.org/wiki/Exponential_backoff

---

## 5. Skills Settings UI Design

### Decision: Dynamic Form Generation from JSON Schema

**Rationale**: Each skill has unique configuration requirements (API keys, model selection, preferences). JSON Schema provides a standard way to define configuration fields with types, validation, and metadata.

**Alternatives Considered**:

| Option | Pros | Cons | Verdict |
|--------|------|------|---------|
| **JSON Schema Forms** | Standard, declarative, extensible, supports validation | Requires schema parsing and widget generation | ✅ **SELECTED** - Flexible, maintainable, industry standard |
| Hard-Coded Forms | Simple, type-safe, IDE support | Breaks when skills added/changed, unmaintainable | ❌ Rejected - Violates extensibility principle |
| YAML Configuration | Human-readable, simple syntax | No standard for UI hints, requires custom parser | ❌ Rejected - JSON Schema more feature-complete |
| Graphical Form Builder | User-friendly design tool | Adds complexity, requires separate tool | ❌ Rejected - Overkill for MVP |

**JSON Schema Example** (from `skills/storyteller/config-schema.json`):

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "provider": {
      "type": "string",
      "enum": ["ollama", "claude", "openai"],
      "default": "ollama",
      "title": "LLM Provider",
      "description": "Backend LLM service for narration"
    },
    "model": {
      "type": "string",
      "title": "Model Name",
      "description": "Model identifier (e.g., 'llama3.2:3b', 'claude-3-sonnet')"
    },
    "apiKey": {
      "type": "string",
      "title": "API Key",
      "description": "Provider API key (leave empty for Ollama)",
      "format": "password"
    },
    "style": {
      "type": "string",
      "enum": ["terse", "vivid", "poetic"],
      "default": "vivid",
      "title": "Narrative Style"
    }
  },
  "required": ["provider", "model"]
}
```

**UI Components**:

- `SkillsSettingsScreen`: List view of all discovered skills
- `SkillConfigForm`: Dynamic form widget generated from schema
- Field types: `string` → `TextField`, `enum` → `DropdownButton`, `boolean` → `Switch`, `number` → `TextField` with numeric keyboard
- `format: "password"` → obscured text input
- Inline validation with error messages below fields
- Save button → write to `skills/<skill-name>/config.json`

**Implementation Notes**:

- Use `json_schema` package for validation
- Build Flutter form dynamically using `Column` of widgets
- Support environment variable substitution: `${OPENAI_API_KEY}` → read from `Platform.environment`
- Validate on save, display errors inline
- Persist to JSON file, gitignore user configs

**References**:

- JSON Schema: https://json-schema.org/
- json_schema package: https://pub.dev/packages/json_schema

---

## 6. Core Skills Implementation Strategy

### Decision: MVP Skills (storyteller, dice-roller, memory, reputation)

**Rationale**: These four skills provide a complete interactive storytelling experience: narration enhancement, randomness, continuity, and consequences.

### 6.1 Storyteller Skill

**Purpose**: Rich prose generation using LLM (local or hosted)

**Scripts**:
- `narrate.dart`: Takes structured input (player action, session context, style preferences) → calls LLM → emits rich narrative text

**Configuration**:
- `provider`: ollama / claude / openai
- `model`: Model identifier
- `apiKey`: API key for hosted providers (optional for Ollama)
- `style`: terse / vivid / poetic
- `temperature`: 0.0-2.0 (creativity)
- `maxTokens`: 200-1000 (response length)

**Fallback Strategy**:
1. Try configured provider (e.g., Claude)
2. If network fails → try local Ollama
3. If Ollama unavailable → simple template ("You [action]. [random flavor text].")

### 6.2 Dice Roller Skill

**Purpose**: Randomness for game mechanics (combat, skill checks, random events)

**Scripts**:
- `roll-dice.dart`: Parses dice formula (e.g., "1d20+5", "3d6") → rolls → emits `ui_event` with results

**Configuration**:
- `showIndividualRolls`: boolean (display each die or just total)
- `randomSource`: crypto / pseudo (use secure random or faster PRNG)

**Dice Formula Grammar**:
```text
<formula> ::= <count> "d" <sides> ("+" | "-") <modifier>?
<count>   ::= integer (1-100)
<sides>   ::= integer (2-100)
<modifier> ::= integer (0-100)

Examples: "1d20", "3d6+2", "2d10-1"
```

**Implementation Notes**:
- Use `dart:math` `Random.secure()` for crypto-quality randomness
- Emit `ui_event` type = `dice_roll` with payload: `{formula, rolls: [int], total: int}`

### 6.3 Memory Skill

**Purpose**: Semantic memory for long-form storytelling, vector search for relevant context

**Scripts**:
- `store-memory.dart`: Takes event summary → generates embedding → stores in SQLite with timestamp
- `recall-memory.dart`: Takes query → performs vector similarity search → returns top K relevant events

**Configuration**:
- `storageBackend`: sqlite / json-files
- `embeddingModel`: text-embedding-3-small / local-sentence-transformers
- `maxContextEvents`: 5-20 (how many events to return)
- `similarityThreshold`: 0.0-1.0 (minimum cosine similarity)

**Data Schema** (`data/memories.db`):
```sql
CREATE TABLE memories (
  id INTEGER PRIMARY KEY,
  timestamp TEXT NOT NULL,
  summary TEXT NOT NULL,
  embedding BLOB NOT NULL,  -- serialized float vector
  metadata TEXT             -- JSON: {importance, characters, locations}
);
```

**Implementation Notes**:
- Use lightweight embedding model (96-384 dimensions)
- Cosine similarity for vector search: `dot(a, b) / (||a|| * ||b||)`
- Store embeddings as BLOB (binary serialization)
- Build index for fast search (SQLite FTS5 or manual kd-tree)

### 6.4 Reputation Skill

**Purpose**: Track player standing with factions, enable context-aware narration

**Scripts**:
- `update-reputation.dart`: Takes faction ID + delta → updates reputation score
- `query-reputation.dart`: Takes faction ID → returns current reputation value

**Configuration**:
- `factionList`: array of faction names (user-configurable for different campaigns)
- `reputationScale`: -100 to +100 (or custom min/max)
- `decayRate`: 0.0-1.0 per in-game day (optional: reputation fades over time)
- `storageBackend`: sqlite / json

**Data Schema** (`data/reputation.db`):
```sql
CREATE TABLE reputation (
  faction_id TEXT PRIMARY KEY,
  score INTEGER NOT NULL DEFAULT 0,
  last_modified TEXT NOT NULL
);

CREATE TABLE reputation_log (
  id INTEGER PRIMARY KEY,
  faction_id TEXT NOT NULL,
  delta INTEGER NOT NULL,
  reason TEXT,
  timestamp TEXT NOT NULL
);
```

**Implementation Notes**:
- Support multiple factions simultaneously
- Log all changes for audit trail
- Apply decay on query (calculate time since last modification)

---

## 7. Data Model Deep Dive

### Decision: See [data-model.md](data-model.md) for Full Specification

**Summary**: The data model document (created 2026-01-27) provides comprehensive definitions for:

1. **Skill Entity**: With error state enum, skill scripts, config fields
2. **Plan JSON**: Extended schema with dependencies, retryPolicy, required/async flags, disabledSkills, metadata
3. **Plan Execution Context**: Tool execution state, dependency graph operations
4. **Tool Result & Execution Result**: Full trace structure with retry counts, execution times, error details
5. **Protocol Events**: Sealed class hierarchy (LogEvent, StatePatchEvent, AssetEvent, UiEvent, ErrorEvent, DoneEvent)
6. **Replan Request/Response**: Feedback loop for bounded retry system
7. **Session State**: Deep merge algorithm with detailed examples
8. **Execution Trace**: Chronological log of tool invocations
9. **Config Persistence**: Environment variable substitution
10. **Error Hierarchy**: CircularDependencyException, ToolExecutionFailedException, etc.
11. **Deep Merge Algorithm**: Recursive object merge, array replacement, null removal

**References**:

- Full specification: [data-model.md](data-model.md)

---

## 8. Testing Strategy

### Unit Tests

**Target**: Individual Dart classes and functions

- `plan_json_test.dart`: JSON parsing, validation, schema compliance
- `topological_sort_test.dart`: Kahn's algorithm, cycle detection, edge cases (empty, linear, complex DAGs)
- `deep_merge_test.dart`: SessionState merge logic, null handling, nested objects
- `skill_discovery_test.dart`: Manifest parsing, invalid JSON handling, missing fields

**Tools**: `flutter_test` package

### Integration Tests

**Target**: Tool protocol interactions with mock processes

- `tool_protocol_test.dart`: Spawn mock skill scripts, verify NDJSON parsing, event emission
- `replan_loop_test.dart`: Simulate tool failures, verify bounded retry limits, track disabled skills
- `skill_config_test.dart`: Write config files, verify skill scripts read values correctly

**Tools**: `flutter_test` with `Process.start()` for mock scripts

### Contract Tests

**Target**: Validate external tool outputs against Spec 001

- `ndjson_protocol_test.dart`: Parse real skill script output, verify event schema compliance

**Tools**: `flutter_test` with JSON schema validation

### Acceptance Tests

**Target**: End-to-end user journeys without live LLM

- P1: "Player types action → narrator responds with narration" (use mock plan generator)
- P2: "User configures storyteller skill → config persists → skill uses new settings"
- P3: "Memory skill stores event → recall script retrieves relevant context"

**Tools**: Manual testing + screen recording for user story validation

---

## Summary

All technical unknowns resolved. Implementation ready to proceed with:

- **Local LLM**: Flutter AI Toolkit + Ollama (llama3.2:3b for MVP)
- **Skill Architecture**: Agent Skills Standard
- **Execution Engine**: Topological sort (Kahn's algorithm) with bounded retries (3-3-5)
- **Skills Settings UI**: Dynamic forms from JSON Schema
- **Core Skills**: storyteller, dice-roller, memory, reputation (4 MVP skills)
- **Data Model**: Comprehensive specification in [data-model.md](data-model.md)
- **Testing**: Unit + integration + contract + acceptance layers

**No NEEDS CLARIFICATION items remain**. Proceed to Phase 1 (contracts, quickstart).
