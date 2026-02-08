# Narratoria System Architecture

**Version:** 1.0.0-draft  
**Date:** February 2026  
**Status:** Living Document  
**Authors:** Narratoria Project Contributors

---

## Executive Summary

Narratoria is an AI-driven interactive storytelling platform that delivers perplexingly on-point narrative experiences while respecting player agency, preserving privacy, and maintaining system reliability. Unlike cloud-based storytelling systems that expose user data and require constant connectivity, Narratoria runs entirely on-device using efficient small language models, enabling offline play with no data exfiltration.

### The Problem

Modern interactive fiction systems face a fundamental tension between three competing demands:

1. **Narrative Quality**: AI-driven storytelling requires sophisticated language models to generate coherent, engaging prose and contextually relevant player choices.

2. **System Reliability**: LLM-based systems are inherently unreliable—they hallucinate, generate invalid plans, invoke unavailable tools, and occasionally fail completely. Traditional software engineering assumes deterministic behavior; AI systems require new architectural patterns to handle graceful failure.

3. **Privacy & Autonomy**: Cloud-based AI storytelling (AI Dungeon, NovelAI) transmits intimate player narratives to remote servers, creating privacy concerns and dependency on third-party services. Players cannot own their stories.

Existing approaches compromise one dimension to satisfy the others: rule-based systems (Inform 7, Twine) are reliable but lack emergent narrative; cloud-based AI systems deliver quality but sacrifice privacy; local procedural generation maintains privacy but lacks coherence.

### Our Solution

Narratoria introduces a **protocol-first architecture with graceful degradation** that delivers reliable AI storytelling without cloud dependency. The system is built on four foundational principles:

#### 1. Protocol-Boundary Isolation

All AI capabilities are exposed as independent processes communicating through a minimal NDJSON protocol. Tools (skill scripts) emit structured events over stdin/stdout, enabling:
- **Language independence**: Skills can be written in any language (Dart, Python, Rust, Go)
- **Failure isolation**: One skill's crash does not cascade to others
- **Testing simplicity**: Protocol contracts are testable independently of implementation
- **Future extensibility**: New tools conform to the protocol without modifying core runtime

#### 2. Bounded Replan Loop with Skill Degradation

When plan generation or execution fails, the system retries intelligently rather than crashing:
- **Max 5 plan generation attempts** before fallback to template narration
- **Skill error states** (healthy → degraded → temporaryFailure → permanentFailure) track reliability
- **Disabled skills list** prevents repeated selection of failed tools in replanning
- **Exponential backoff** for transient failures (network timeouts, temporary resource exhaustion)
- **Graceful fallback**: Simple template-based narration when all else fails

This ensures the player *always* receives a response, maintaining narrative flow even when AI components fail.

#### 3. Hybrid On-Device AI Strategy

Narratoria combines two specialized models running entirely in-process:

- **Phi-3.5 Mini (3.8B parameters, 2.5GB GGUF quantized)**: Generates Plan JSON documents that orchestrate skill invocations and produces scene narration. Automatically downloads from HuggingFace Hub on first launch; cached locally for offline use.

- **sentence-transformers/all-MiniLM-L6-v2 (33MB, 384-dimensional embeddings)**: Generates semantic vectors for memory retrieval, lore search, and contextual matching. Enables "perplexingly on-point" choices that reference past events through vector similarity search.

Both models run on-device (compatible with iPhone 17+ and equivalent Android hardware), ensuring:
- **Privacy**: No player data leaves the device
- **Offline capability**: No network required after initial model download
- **Predictable cost**: No API fees, no usage limits
- **Low latency**: Local inference completes in <3 seconds per scene

#### 4. Semantic Memory for Narrative Continuity

Cross-session continuity is achieved through ObjectBox persistence with vector search:
- **Scene summaries** stored after each player choice with semantic embeddings
- **Lore chunks** from campaign content (paragraph-based, 512-token max) indexed for retrieval
- **NPC perception** and **faction reputation** tracked persistently
- **Character portraits** cached to avoid redundant generation

The Plan Generator (Phi-3.5 Mini) decides *contextually* what data to retrieve based on narrative needs—there are no fixed "memory tiers" or rigid context budgets. The LLM analyzes the scene and generates plans that invoke memory retrieval skills with semantic queries.

### Key Technical Components

| Component | Technology | Purpose |
|-----------|-----------|---------|
| **Plan Generator** | Phi-3.5 Mini (3.8B) | Converts player choices to executable Plan JSON |
| **Plan Executor** | Dart runtime | Executes skill scripts in dependency order with retry |
| **Semantic Memory** | ObjectBox + sentence-transformers | Cross-session narrative continuity via vector search |
| **Skills Framework** | Agent Skills Standard | Discover, configure, and execute modular capabilities |
| **Campaign Format** | Directory + manifest.json | Story packages with lore, NPCs, plot beats, assets |
| **UI Layer** | Flutter (Material Design 3) | Rich cross-platform storytelling interface |
| **Persistence** | ObjectBox (in-process) | ACID storage for memories, reputation, state |

### Performance Targets

| Metric | Target | Rationale |
|--------|--------|-----------|
| Scene transition latency | <3 seconds | Maintains narrative flow without perceived lag |
| Semantic memory search | <500ms | Fast context retrieval for 1000+ stored events |
| Plan execution timeout | 60 seconds | Prevents hung processes; triggers replan |
| Per-skill timeout | 30 seconds | Isolates slow tools without blocking plan |
| Memory-driven choices | 80%+ | Choices reference past events/knowledge |
| Cross-session data integrity | 100% | Zero data loss across restarts |
| Sentiment accuracy | 95% | NPC reactions match relationship history |

### Architecture Layers

The system is organized as an 8-layer stack:

```
┌─────────────────────────────────────────────────────┐
│ Layer 8: Narrative Engine (Scene Pipeline)         │  Player experience
│  - Choice → Plan → Execute → Results → Choices     │
├─────────────────────────────────────────────────────┤
│ Layer 7: Campaign Format (Story Packages)          │  Content authoring
│  - world/, characters/, plot/, lore/, art/, music/ │
├─────────────────────────────────────────────────────┤
│ Layer 6: Skill State Persistence (ObjectBox)       │  Data management
│  - Memory events, lore, reputation, portraits      │
├─────────────────────────────────────────────────────┤
│ Layer 5: Dart/Flutter Implementation (UI)          │  User interface
│  - Story View, Asset Gallery, Settings, Tools      │
├─────────────────────────────────────────────────────┤
│ Layer 4: Narratoria Skills (Individual Skills)     │  Capabilities
│  - Storyteller, Memory, Dice, Reputation, Choices  │
├─────────────────────────────────────────────────────┤
│ Layer 3: Skills Framework (Discovery & Config)     │  Skill lifecycle
│  - Manifest parsing, configuration, execution      │
├─────────────────────────────────────────────────────┤
│ Layer 2: Plan Execution (Orchestration)            │  Coordination
│  - Topological sort, retry, replan loop           │
├─────────────────────────────────────────────────────┤
│ Layer 1: Tool Protocol (NDJSON Events)             │  Communication
│  - log, state_patch, asset, ui_event, error, done │
└─────────────────────────────────────────────────────┘
```

Each layer builds on the contracts defined by layers below, enabling independent evolution of components while maintaining system coherence.

### Value Proposition

**For Players:**
- Engaging AI-driven stories that remember your choices across sessions
- Complete privacy—your stories never leave your device
- Offline play after initial setup (no internet required)
- Rich multimedia experiences (narration, character portraits, ambient music)
- Free from usage limits, API costs, and platform lock-in

**For Story Authors:**
- Low barrier to entry—minimal campaign structure generates playable stories through AI enrichment
- Full creative control—detailed campaigns are executed faithfully by the AI
- Ethical AI transparency—all AI-generated content marked with provenance metadata
- Extensible skill system—custom game mechanics without platform modification

**For Developers:**
- Protocol-first architecture enables polyglot skill development
- Comprehensive test contracts for all components
- Clear separation of concerns across 8 architectural layers
- Graceful degradation patterns applicable beyond storytelling

### Design Philosophy

Narratoria embodies five constitutional principles:

1. **Dart+Flutter First**: All client logic in idiomatic Dart/Flutter for cross-platform consistency
2. **Protocol-Boundary Isolation**: Skills run as independent OS processes; no shared memory
3. **Single-Responsibility Tools**: Each skill performs one well-defined task
4. **Graceful Degradation**: System continues functioning when features fail; bounded retries prevent infinite loops
5. **Testability and Composability**: Unit-testable modules, contract tests, integration tests at every layer

These principles ensure the system remains maintainable, extensible, and reliable even as complexity grows.

### Current Status

- **Specification Phase**: Complete architecture defined across 8 interconnected specifications
- **Implementation Status**: Early development; core protocol and execution engine defined
- **Target Platforms**: macOS, Windows, Linux (desktop); iOS 17+, Android (mobile)
- **License**: Open source (GNU GENERAL PUBLIC LICENSE v3.0)
- **Repository**: github.com/abandonedbranch/narratoria

This living document serves as the authoritative reference for system design, capturing architectural decisions, rationale, and open questions. It evolves alongside the implementation.

---

---

## 1. System Overview

### 1.1 Problem Statement

Interactive fiction has existed since the 1970s with text adventure engines like Zork, evolving through hypertext systems like Twine, parser-based environments like Inform 7, and most recently AI-powered storytelling platforms like AI Dungeon and NovelAI. Each generation added capability but inherited new limitations:

- **Parser systems** (Inform 7, TADS) deliver reliable, deterministic worlds but require authors to anticipate every player action and cannot generate emergent narrative.
- **Hypertext systems** (Twine, Ink) offer branching narrative with rich authoring but the combinatorial explosion of branches limits story depth.
- **Cloud AI systems** (AI Dungeon, NovelAI) generate coherent prose and respond to arbitrary player input but transmit intimate narrative data to remote servers, depend on API availability, and provide no architectural guarantee of graceful failure.
- **Local procedural generation** (roguelikes, Dwarf Fortress) maintains player privacy and offline capability but produces mechanically-driven narrative rather than coherent, character-rich storytelling.

Narratoria is designed to occupy the intersection of these approaches: **AI-generated narrative quality, system reliability through architectural patterns, and complete player privacy through on-device inference.**

### 1.2 Design Constraints

The architecture is governed by the following constraints:

1. **On-device inference only**: No network calls during gameplay. Models download once from HuggingFace Hub and cache locally. All AI processing occurs in-process.
2. **Mobile-capable**: Must run on devices with 8GB RAM (iPhone 17+, equivalent Android). Model sizes constrained to 2.5GB (narrator) + 33MB (embeddings).
3. **Offline after setup**: After initial model download, the application must function without any network connectivity.
4. **No infinite loops**: Every execution path must terminate within bounded time. Plan generation capped at 5 attempts; per-tool retries capped at 3; per-tool timeout at 30 seconds; per-plan timeout at 60 seconds.
5. **Language-agnostic extensibility**: Third-party developers must be able to write skills in any programming language without modifying the core runtime.
6. **Cross-platform**: Single codebase targeting macOS, Windows, Linux, iOS, and Android.

### 1.3 Data Flow

The fundamental data flow in Narratoria follows a cycle:

```
                    ┌──────────────────────┐
                    │   Player selects     │
                    │   a choice           │
                    └──────────┬───────────┘
                               │
                               ▼
                    ┌──────────────────────┐
                    │   Plan Generator     │  Phi-3.5 Mini analyzes:
                    │   (Narrator AI)      │  - Player choice
                    │                      │  - Session state
                    │                      │  - Available skills
                    │                      │  - Campaign constraints
                    └──────────┬───────────┘
                               │  Produces Plan JSON
                               ▼
                    ┌──────────────────────┐
                    │   Plan Executor      │  For each skill script:
                    │                      │  - Resolve dependencies
                    │                      │  - Launch OS process
                    │                      │  - Parse NDJSON events
                    │                      │  - Apply retries
                    └──────────┬───────────┘
                               │  Produces Execution Result
                               ▼
                    ┌──────────────────────┐
                    │   Event Aggregator   │  Merges results:
                    │                      │  - state_patch → session state
                    │                      │  - asset → asset gallery
                    │                      │  - ui_event → UI dispatch
                    │                      │  - log → developer console
                    └──────────┬───────────┘
                               │
                               ▼
                    ┌──────────────────────┐
                    │   Scene Renderer     │  Displays:
                    │                      │  - Narrative prose (2-3 paragraphs)
                    │                      │  - Character portraits + background art
                    │                      │  - Ambient music
                    │                      │  - 3-4 contextual choices
                    └──────────┬───────────┘
                               │
                               └────────────────► (cycle repeats)
```

When failures occur at any stage, the system enters the **replan loop**—a bounded state machine that disables failed skills, generates alternative plans, and eventually falls back to template-based narration if all attempts are exhausted.

### 1.4 Component Map

```
┌─────────────────────────────────────────────────────────────────┐
│                     NARRATORIA RUNTIME                          │
│                                                                 │
│  ┌─────────────────┐  ┌──────────────────┐                    │
│  │ Narrator AI      │  │ Plan Executor     │                    │
│  │ (Phi-3.5 Mini)   │──│ (Dart runtime)    │                    │
│  │                   │  │                   │                    │
│  │ • Generates plans │  │ • Topological sort│                    │
│  │ • Selects skills  │  │ • Parallel exec   │                    │
│  │ • Produces prose  │  │ • Retry + backoff │                    │
│  └─────────────────┘  └────────┬──────────┘                    │
│            │                    │                                │
│            │        ┌───────────┴────────────┐                  │
│            │        │ NDJSON Protocol         │                  │
│            │        │ (stdin/stdout pipes)    │                  │
│            │        └───────────┬────────────┘                  │
│            │                    │                                │
│            │     ┌──────────────┼──────────────┐                │
│            │     │              │              │                │
│            │  ┌──┴───┐   ┌─────┴────┐  ┌─────┴─────┐          │
│            │  │Story-│   │  Memory   │  │   Dice    │  ...     │
│            │  │teller│   │  Skill    │  │   Roller  │          │
│            │  └──────┘   └─────┬────┘  └───────────┘          │
│            │                    │                                │
│  ┌─────────┴────────────────────┴──────────────────────┐        │
│  │          Persistence Layer (ObjectBox)               │        │
│  │  ┌──────────┐ ┌──────────┐ ┌────────┐ ┌─────────┐  │        │
│  │  │ Memories │ │   Lore   │ │  NPC   │ │Portraits│  │        │
│  │  │ (vectors)│ │ (chunks) │ │  Data  │ │ (cache) │  │        │
│  │  └──────────┘ └──────────┘ └────────┘ └─────────┘  │        │
│  └─────────────────────────────────────────────────────┘        │
│                                                                 │
│  ┌──────────────────────────────────────────────────────┐       │
│  │          Flutter UI (Material Design 3)              │       │
│  │  ┌──────────┐ ┌──────────┐ ┌────────┐ ┌──────────┐  │       │
│  │  │  Story   │ │  Asset   │ │ State  │ │ Settings │  │       │
│  │  │  View    │ │  Gallery │ │ Panel  │ │  (Skills)│  │       │
│  │  └──────────┘ └──────────┘ └────────┘ └──────────┘  │       │
│  └──────────────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────────────┘
```

### 1.5 Terminology

The following terms are used consistently throughout this document:

| Term | Definition |
|------|-----------|
| **Skill** | A capability bundle that the Narrator AI can invoke. Contains behavioral prompts, optional scripts, configuration, and data storage. Follows the Agent Skills Standard. |
| **Skill Script** | An executable component within a skill that performs actions (e.g., `roll-dice.dart`, `narrate.dart`). Communicates via NDJSON over stdin/stdout. |
| **Plan JSON** | Structured document produced by the Narrator AI describing which skill scripts to invoke, their inputs, dependencies, and execution strategy. |
| **Skill Invocation** | An entry in the Plan JSON `tools` array that references a specific skill script to execute. The array is named `tools` for protocol compatibility. |
| **Session State** | Runtime data model containing narrative state accumulated from `state_patch` events (e.g., `{"inventory": {"torch": {"lit": true}}}`). |
| **Deep Merge** | State patch merge semantics where nested objects merge recursively, arrays replace entirely, and null values remove keys. |
| **Execution Trace** | Complete record of skill script execution including results, events, timing, and errors. |
| **Replan Loop** | Bounded retry system (max 5 plan generation attempts) that learns from failures and disables failed skills in subsequent plans. |
| **Story Session** | A single continuous play session within a narrative playthrough. |
| **Story Playthrough** | A complete or ongoing narrative arc spanning multiple sessions. |
| **Campaign** | A self-contained story package containing world-building, characters, plot structure, lore, and creative assets. |
| **Lore Chunk** | A paragraph-sized segment of campaign lore stored with a semantic embedding vector for retrieval. |
| **Memory Event** | A recorded narrative occurrence with timestamp and semantic embedding (e.g., "player befriends blacksmith"). |
| **Persona Profile** | Player character data including stats, traits, background, and preferences. |
| **Behavioral Prompt** | Markdown file (`prompt.md`) injected into Narrator AI system context; guides behavior for a specific skill. |
| **Error State** | Skill health status: `healthy`, `degraded`, `temporaryFailure`, or `permanentFailure`. |
| **Perception Score** | Numerical value (-100 to +100) representing an individual NPC's opinion of the player. |
| **Faction Reputation** | Player standing with named factions, influencing NPC attitudes and available choices. |

---

## 2. Core Architecture Patterns

### 2.1 Tool Protocol

#### 2.1.1 Transport Model

Narratoria launches skill scripts as external processes using platform-native mechanisms (fork/exec on Unix, CreateProcess on Windows). The runtime supplies arguments and/or JSON input via stdin; scripts write output to stdout using **NDJSON** (Newline-Delimited JSON)—one complete JSON object per line, UTF-8 encoded with Unix-style newlines (`\n`). Scripts may write human-readable diagnostics to stderr without restriction.

#### 2.1.2 Event Envelope

Every JSON object emitted by a script must include:

```json
{
  "version": "0",
  "type": "<event-type>",
  ...
}
```

- `version`: String. Must equal `"0"` for the current protocol version. Incremented only on breaking protocol changes.
- `type`: String. One of: `log`, `state_patch`, `asset`, `ui_event`, `error`, `done`.

Optional envelope fields include `requestId` (opaque identifier provided by Narratoria) and `timestamp` (ISO-8601 datetime). Additional fields may be included and must be ignored by the runtime unless otherwise specified.

#### 2.1.3 Event Types

**`log`** — Communicate progress or diagnostic information.

```json
{
  "version": "0",
  "type": "log",
  "level": "debug" | "info" | "warn" | "error",
  "message": "<string>",
  "fields": { "...optional key/value data..." }
}
```

The runtime must not treat log events as errors. Logs are displayed in a developer or debug console.

**`state_patch`** — Update the session state.

```json
{
  "version": "0",
  "type": "state_patch",
  "patch": { "...arbitrary JSON object..." }
}
```

The `patch` is a JSON object merged into session state using **deep merge** semantics:
- Nested objects merge recursively (keys added/updated, parent objects not replaced)
- Arrays replace entirely (not merged element-by-element)
- Null values remove keys from the state tree

Example: existing state `{"a": {"b": 1, "c": 2}}` + patch `{"a": {"c": 3, "d": 4}}` → `{"a": {"b": 1, "c": 3, "d": 4}}`

**`asset`** — Notify that a new asset has been created.

```json
{
  "version": "0",
  "type": "asset",
  "assetId": "<string>",
  "kind": "<string>",
  "mediaType": "<MIME-type>",
  "path": "<filesystem-path>",
  "metadata": { "...optional..." }
}
```

- `assetId` must be unique within the invocation.
- `kind` describes a broad category (image, audio, video, model).
- `mediaType` must be a valid MIME type.
- `path` must be an absolute path or resolvable from the runtime's working directory.
- Scripts create and write asset files before emitting the event. Unsupported media types degrade to placeholder UI.

**`ui_event`** — Request a UI action.

```json
{
  "version": "0",
  "type": "ui_event",
  "event": "<string>",
  "payload": { "...event-specific parameters..." }
}
```

The `narrative_choice` event must be supported: payload format `{"choices": ["string", "string", ...]}`, displayed as clickable buttons. Unsupported events degrade gracefully via placeholder messages showing the event name and payload details.

**`error`** — Represent a structured error.

```json
{
  "version": "0",
  "type": "error",
  "errorCode": "<string>",
  "errorMessage": "<string>",
  "details": { "...optional structured data..." }
}
```

Error events do not terminate the invocation by themselves. The script should emit a subsequent `done` event with `ok: false`.

**`done`** — Signal completion.

```json
{
  "version": "0",
  "type": "done",
  "ok": true | false,
  "summary": "<string, optional>"
}
```

Each invocation must end with exactly one `done` event or a premature process termination. `ok: true` indicates successful logical completion; `ok: false` indicates controlled logical failure. The runtime stops accepting events after processing `done`. A missing `done` plus a non-zero exit code is treated as a protocol-level failure.

#### 2.1.4 Tool Input

The runtime may send structured input to a script via stdin in JSON format:

```json
{
  "requestId": "<string>",
  "tool": "<toolName>",
  "operation": "<string>",
  "input": { "...arbitrary data..." }
}
```

Scripts may ignore any fields they do not recognize.

#### 2.1.5 Protocol Semantics

- **Event Ordering**: Scripts may emit events in any order. Typical flow: logs → assets → ui events → state patches → done.
- **Unknown Event Types**: If `type` is unknown, the runtime treats it as a protocol error and terminates processing for the invocation.
- **Process Exit**: Exit code 0 means protocol intact; `done.ok` determines logical success. Exit code ≠ 0 means protocol-level failure regardless of events.
- **Streaming**: Scripts may produce incremental events during long-running operations. The runtime processes each JSON line as received.

#### 2.1.6 Forward Compatibility

Scripts must include a stable `version: "0"` field, use only defined `type` strings, and tolerate additional fields from future runtime versions. The runtime must ignore unknown fields within known event types, not require any event types other than `done` for correctness, and degrade gracefully on unknown MIME types or `ui_event` names.

#### 2.1.7 Minimum Viable Tool

A compliant minimal tool emits NDJSON events to stdout:

```
{"version":"0","type":"log","level":"info","message":"Starting"}
{"version":"0","type":"state_patch","patch":{"flags":{"torchLit":true}}}
{"version":"0","type":"done","ok":true,"summary":"Torch lit."}
```

### 2.2 Plan Execution

#### 2.2.1 Plan JSON Structure

The Narrator AI produces Plan JSON documents with this structure:

```json
{
  "requestId": "<uuid>",
  "narrative": "<optional narrator response>",
  "tools": [
    {
      "toolId": "<string>",
      "toolPath": "<path-to-executable>",
      "input": { "...arbitrary JSON..." },
      "dependencies": ["<toolId>", "..."],
      "required": true,
      "async": false,
      "retryPolicy": {
        "maxRetries": 3,
        "backoffMs": 100
      }
    }
  ],
  "parallel": false,
  "disabledSkills": ["<skillName>", "..."],
  "metadata": {
    "generationAttempt": 1,
    "parentPlanId": null
  }
}
```

**Fields:**

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `requestId` | UUID | required | Unique identifier for this plan execution |
| `narrative` | string | null | Narrative text to display before/during script execution |
| `tools` | array | required | Skill script invocation descriptors |
| `parallel` | boolean | false | If true and dependencies allow, scripts run concurrently |
| `disabledSkills` | string[] | [] | Skills that failed previously; planner must not select |
| `metadata.generationAttempt` | integer | 1 | Current attempt number (1-5) in replan loop |
| `metadata.parentPlanId` | UUID | null | Previous plan's UUID when replanning |

**Skill Invocation Fields:**

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `toolId` | string | required | Unique ID within the plan (for dependency tracking) |
| `toolPath` | string | required | Path to skill script executable |
| `input` | object | {} | JSON object passed to script via stdin |
| `dependencies` | string[] | [] | toolId values that must complete first |
| `required` | boolean | true | If true, failure aborts dependent scripts |
| `async` | boolean | false | If true, may run in parallel with other async scripts |
| `retryPolicy.maxRetries` | integer | 3 | Max retry attempts after initial failure |
| `retryPolicy.backoffMs` | integer | 100 | Base delay for exponential backoff |

**Retry Backoff Formula:** $\text{delay} = \text{backoffMs} \times 2^{(\text{attempt} - 1)}$

Example with backoffMs = 100: Attempt 1 → 100ms, Attempt 2 → 200ms, Attempt 3 → 400ms.

#### 2.2.2 Dependency Graph

Skill invocations form a directed acyclic graph (DAG) based on `dependencies` arrays:

```
tools: [
  {toolId: "A", dependencies: []},
  {toolId: "B", dependencies: ["A"]},
  {toolId: "C", dependencies: ["A"]},
  {toolId: "D", dependencies: ["B", "C"]}
]

Graph:
    A
   / \
  B   C
   \ /
    D
```

#### 2.2.3 Execution Rules

1. **Circular Dependency Detection**: Before execution, the runtime detects circular dependencies (direct or transitive). If detected, the plan is rejected and a new plan is requested from the Narrator AI.

2. **Topological Execution Order (Kahn's Algorithm)**: Tools execute in dependency-respecting order. A tool must not begin until all tools listed in its `dependencies` array have completed.

   ```
   function topologicalSort(tools):
       Build adjacency list and in-degree map
       Initialize queue with zero in-degree nodes
       While queue is not empty:
           Dequeue current, add to result
           For each neighbor, decrement in-degree
           If in-degree reaches 0, enqueue
       If result length != tools length:
           throw CyclicDependencyError
       return result
   ```

3. **Parallel Execution**: If `parallel: true` in the plan AND `async: true` for a tool, tools with satisfied dependencies may run concurrently. Concurrent execution must not exceed the number of available CPU cores.

4. **Sequential Fallback**: If `parallel: false`, tools run in topological order, each waiting for the previous to complete.

5. **Retry Logic**: If a tool fails (`done.ok: false` or non-zero exit), the runtime retries up to `maxRetries` times with exponential backoff. After exhausting retries, the tool is marked as failed.

6. **Failure Handling by `required` flag**:
   - `required: true` and tool fails → dependent tools must not execute; plan execution fails
   - `required: false` and tool fails → dependent tools may execute with null/empty input; plan continues
   - Independent tools (no dependency on failed tool) continue automatically

7. **Event Aggregation**: The runtime collects all events from all tools and merges:
   - `log` events → Tool Execution Panel
   - `state_patch` events → session state (deep merge)
   - `asset` events → Asset Gallery
   - `ui_event` events → UI handlers
   - `error` events → displayed with context

#### 2.2.4 Execution Result

After executing a plan, the runtime returns a trace:

| Field | Type | Description |
|-------|------|-------------|
| `planId` | UUID | ID of executed plan |
| `success` | boolean | True if all required scripts succeeded |
| `canReplan` | boolean | True if Narrator AI should attempt a new plan |
| `failedTools` | string[] | List of failed script toolIds |
| `disabledSkills` | string[] | Skills to disable in next plan attempt |
| `toolResults` | array | Per-tool results (state, output, events, timing, retries, errors) |
| `aggregatedState` | object | Merged session state from all `state_patch` events |
| `aggregatedAssets` | array | All assets generated during execution |
| `executionTimeMs` | integer | Total execution time |
| `attemptNumber` | integer | Plan generation attempt number |

Each tool result records: `toolId`, `state` (success/failed/skipped/timeout), `output`, `events`, `executionTimeMs`, `retryCount`, and `error` details (code, message, category).

Error categories: `tool_failure`, `circular_dependency`, `timeout`, `invalid_json`, `process_error`.

### 2.3 Replan Loop

#### 2.3.1 State Machine

```
                    ┌─────────────┐
                    │   START     │
                    └──────┬──────┘
                           │
                           ▼
              ┌────────────────────────┐
              │  GENERATING (attempt N) │◄────────┐
              └───────────┬────────────┘          │
                          │                       │
                          ▼                       │
                   ┌─────────────┐                │
                   │  EXECUTING  │                │
                   └──────┬──────┘                │
                          │                       │
              ┌───────────┴───────────┐           │
              │                       │           │
              ▼                       ▼           │
       ┌──────────┐           ┌────────────┐     │
       │ SUCCESS  │           │  REPLANNING │─────┘
       └──────────┘           └──────┬─────┘  (N < 5)
                                     │
                                     │ (N >= 5)
                                     ▼
                              ┌────────────┐
                              │  FALLBACK  │
                              └────────────┘
```

| State | Description |
|-------|-------------|
| GENERATING | Narrator AI generating Plan JSON (must complete within 5 seconds) |
| EXECUTING | Runtime executing plan tools (60-second plan timeout) |
| SUCCESS | All required tools completed successfully |
| REPLANNING | Plan failed; generating new plan with disabled skills |
| FALLBACK | Max attempts (5) exceeded; using template narration |

#### 2.3.2 Replan Algorithm

```
function replanLoop(playerInput, maxAttempts = 5):
    disabledSkills = []
    parentPlanId = null

    for attempt in 1..maxAttempts:
        plan = narratorAI.generate(
            input: playerInput,
            disabledSkills: disabledSkills,
            metadata: {generationAttempt: attempt, parentPlanId: parentPlanId}
        )

        result = executor.execute(plan)

        if result.success:
            return result

        parentPlanId = plan.requestId
        disabledSkills = disabledSkills ∪ result.disabledSkills
        log("Plan attempt {attempt} failed, disabling: {result.failedTools}")

    // Exhausted all attempts
    return fallbackNarration(playerInput)
```

#### 2.3.3 Skill Error States

Skills track runtime health using four states:

| State | Description | Planner Behavior |
|-------|-------------|------------------|
| `healthy` | Available for planning | Select normally |
| `degraded` | Slow or unreliable | Select with caution; may increase timeout |
| `temporaryFailure` | Transient issue | Retry with backoff; may recover |
| `permanentFailure` | Unrecoverable in session | Add to `disabledSkills`; do not select |

**State Transitions:**

```
healthy ──[timeout/network error]──▶ temporaryFailure
healthy ──[3 consecutive failures]──▶ degraded
degraded ──[success]──▶ healthy
degraded ──[3 more failures]──▶ permanentFailure
temporaryFailure ──[retry success]──▶ healthy
temporaryFailure ──[max retries exceeded]──▶ permanentFailure
permanentFailure ──[session restart]──▶ healthy
```

#### 2.3.4 Fallback Narration

When replanning exhausts all 5 attempts, simple templates provide continuity:

```
"The narrator pauses, considering your words: '{input}'"
"Your action '{input}' echoes in the stillness..."
"The story continues, though the path is unclear..."
```

The player always receives a response. The system must never loop infinitely, crash, or leave the player staring at a blank screen.

### 2.4 Deep Merge Algorithm

Session state is updated using deep merge semantics:

```
function deepMerge(target, patch):
    for key, value in patch:
        if value is null:
            delete target[key]              // Null removes key
        else if value is object and target[key] is object:
            deepMerge(target[key], value)   // Recurse into objects
        else if value is array:
            target[key] = value             // Arrays replace entirely
        else:
            target[key] = value             // Primitives replace
    return target
```

**Examples:**

```
// Nested object merge
target: {"a": {"b": 1, "c": 2}}  +  patch: {"a": {"c": 3, "d": 4}}
result: {"a": {"b": 1, "c": 3, "d": 4}}

// Array replacement
target: {"items": [1, 2, 3]}  +  patch: {"items": [4, 5]}
result: {"items": [4, 5]}

// Key deletion
target: {"a": 1, "b": 2}  +  patch: {"b": null}
result: {"a": 1}
```

### 2.5 Timeout and Resource Bounds

| Resource | Default | Configurable | Notes |
|----------|---------|--------------|-------|
| Per-skill timeout | 30 seconds | Yes | Via skill manifest or plan |
| Per-plan execution | 60 seconds | Yes | Total time for all tools |
| Per-plan generation | 5 seconds | No | Strict; for typical inputs under 100 words |
| Max concurrent tools | CPU cores | Yes | Implementation-specific |
| Max replan attempts | 5 | No | Hard limit per Constitution |
| Max retries per tool | 3 (default) | Yes | Via retryPolicy in plan |

### 2.6 Session State and UI Binding

#### 2.6.1 State Structure

Session state is a single JSON object that accumulates throughout a play session via `state_patch` events from skills. The state structure is **not rigidly enforced**, allowing skills flexibility, but conventional paths enable deterministic UI rendering.

**State Lifecycle:**

```
Session Start → Initial State (empty or loaded from save)
       ↓
Skill Execution → state_patch events
       ↓
Deep Merge → Updated Session State
       ↓
UI Subscription → Reactive Rendering
       ↓
Player Action → New Plan → Skill Execution (loop)
```

#### 2.6.2 Conventional State Paths

The following top-level keys establish conventions for common game data:

| Path | Type | Purpose | UI Binding Examples |
|------|------|---------|---------------------|
| `player.*` | object | Player character data | Character sheet, status icons |
| `player.stats.*` | object | Campaign-defined stats (from `stats/*.stat.txt`) | Stat gauges grouped by category |
| `player.stats.{id}` | number | Individual stat value (e.g., health, mana, chemistry) | Bar, hearts, pips, ring, number |
| `player.inventory` | object | Item data keyed by item ID | Inventory grid, equipment slots |
| `player.status` | array | Active status effects | Status effect icons with tooltips |
| `world.*` | object | World state | Location header, environmental indicators |
| `world.location` | string | Current location name | Location display |
| `world.time` | string | Time of day | Time indicator, day/night styling |
| `npcs.*` | object | NPC data keyed by NPC ID | Relationship meters, dialogue indicators |
| `npcs.<id>.stats.*` | object | NPC-scoped stats (from `stats/npc.{id}.*.stat.txt`) | NPC relationship gauges |
| `npcs.<id>.perception` | number | NPC opinion (-100 to +100) | Relationship meter, dialogue tone |
| `quest.*` | object | Quest tracking | Quest log, objective markers |
| `flags.*` | object | Binary story flags | Conditional content unlocking |
| `combat.*` | object | Combat state (when active) | Combat UI, turn order, action buttons |

**Example State Evolution:**

```json
// Initial state (session start)
{}

// After character creation skill (stats initialized from *.stat.txt defaults)
{
  "player": {
    "name": "Aria",
    "class": "rogue",
    "stats": {
      "health": 100,
      "mana": 50,
      "stamina": 80
    }
  }
}

// After combat encounter (damage taken, item gained)
{
  "player": {
    "name": "Aria",
    "class": "rogue",
    "stats": {
      "health": 72,   // updated via state_patch
      "mana": 50,
      "stamina": 65   // updated
    },
    "inventory": {  // added
      "rusty_dagger": {"name": "Rusty Dagger", "type": "weapon", "damage": "1d4", "equipped": true}
    }
  },
  "world": {  // added
    "location": "Darkwood Forest Clearing"
  }
}
```

#### 2.6.3 UI Reactive Rendering

The Flutter UI layer uses the **Provider** pattern with `ChangeNotifier` to subscribe to session state changes:

```dart
// Conceptual (not strict implementation)
class SessionStateNotifier extends ChangeNotifier {
  Map<String, dynamic> _state = {};
  
  void applyPatch(Map<String, dynamic> patch) {
    _state = deepMerge(_state, patch);
    notifyListeners();  // Triggers UI rebuild
  }
  
  Map<String, dynamic> get state => _state;
}
```

Widgets rebuild automatically when state changes:

```dart
// Health bar widget
Consumer<SessionStateNotifier>(builder: (context, state, child) {
  final health = state.state['player']?['stats']?['health'] ?? 0;
  final healthDef = campaignStats['health'];  // From stat definition
  final maxHealth = healthDef?.rangeMax ?? 100;
  return HealthBar(current: health, max: maxHealth);
})
```

**Key Properties:**

1. **Deterministic**: Given the same sequence of `state_patch` events, the UI always renders identically.
2. **Real-time**: State updates occur during plan execution, not just at the end.
3. **Granular**: Skills can update specific nested paths without replacing entire subtrees.
4. **Extensible**: Custom skills can introduce new state paths; the UI gracefully ignores unknown paths.

#### 2.6.4 Skill Output Examples

**Example 1: Dice Roller Skill**

```json
// Input: Roll 2d6 for damage
// Output events:
{"version":"0","type":"log","level":"info","message":"Rolling 2d6"}
{"version":"0","type":"state_patch","patch":{"lastRoll":{"dice":"2d6","result":9,"rolls":[4,5]}}}
{"version":"0","type":"done","ok":true,"summary":"Rolled 9"}
```

**Example 2: Combat Skill**

```json
// After player attacks goblin
{"version":"0","type":"state_patch","patch":{
  "combat": {
    "active": true,
    "enemies": {
      "goblin_1": {"hp": 3, "maxHp": 12, "status": ["wounded"]}
    }
  }
}}
{"version":"0","type":"ui_event","event":"combat_log","payload":{
  "message":"Your dagger strikes the goblin for 9 damage!"
}}
{"version":"0","type":"done","ok":true}
```

**Example 3: Reputation Skill**

```json
// After helping NPC
{"version":"0","type":"state_patch","patch":{
  "npcs": {
    "blacksmith_gareth": {
      "perception": 45,  // increased from 25
      "relationshipTier": "friendly"
    }
  }
}}
{"version":"0","type":"done","ok":true,"summary":"Gareth's opinion improved"}
```

#### 2.6.5 Handling Missing or Invalid State Data

The UI must handle missing, null, or invalid state values gracefully:

- **Missing `player.hp`**: Display "—" or hide health bar
- **Invalid number (string "abc" for hp)**: Log warning, display placeholder
- **Empty arrays**: Render empty state UI ("No active quests")
- **Unknown state paths**: Ignore silently; do not crash

Skills should emit well-formed state patches, but the UI must be defensive against malformed data to maintain system reliability.

### 2.7 Analytics Logging

The system must log all plan generation and execution attempts with timestamps, plan IDs, skill selections, and outcomes for debugging and analytics.

---

## 3. Skills Framework

### 3.1 Agent Skills Standard

Narratoria implements the [Agent Skills Standard](https://agentskills.io/specification). A skill is a capability bundle containing:

- **`skill.json`** — Manifest file defining identity, version, author, scripts, and capabilities
- **`prompt.md`** — Behavioral prompt injected into Narrator AI system context
- **`config-schema.json`** — JSON Schema defining user-configurable settings
- **`config.json`** — User-saved configuration values
- **`scripts/`** — Executable programs that perform the skill's work
- **`data/`** — Private persistent storage for the skill

#### Skill Manifest Schema (`skill.json`)

```json
{
  "name": "storyteller",
  "displayName": "Storyteller",
  "description": "Rich narrative enhancement using LLM",
  "version": "1.0.0",
  "author": "Narratoria",
  "license": "MIT",
  "prompt": "prompt.md",
  "configSchema": "config-schema.json",
  "scripts": [
    {
      "name": "narrate",
      "path": "narrate.dart",
      "description": "Generate narrative prose",
      "timeout": 30000,
      "required": true
    }
  ],
  "capabilities": ["narration", "prose"],
  "priority": 80,
  "retryPolicy": { "maxRetries": 3, "backoffMs": 100 }
}
```

Required fields: `name` (lowercase, alphanumeric, hyphens), `version` (semver), `description`. The `name` must match the pattern `^[a-z0-9-]+$`.

### 3.2 Skill Discovery

At startup, the runtime scans the `skills/` directory and discovers all valid skills:

1. Parse `skill.json` manifests; validate required fields (name, version, description)
2. Load optional `prompt.md` files; make behavioral prompts available to the Plan Generator
3. Identify all executable scripts in `skills/*/scripts/` directories
4. Skip skills with invalid manifests; log warnings without crashing
5. Hot-reloading should be supported: when skill changes are detected, handle gracefully (auto-reload or notify user)

### 3.3 Skill Configuration

The runtime provides a Skills Settings UI accessible from application settings:

- Display all discovered skills with name, description, and enabled/disabled toggle
- Dynamically generate configuration forms from `config-schema.json` files
- Supported input types: string (text, freeform), number, boolean (toggle), enum (dropdown)
- Sensitive fields (API keys, passwords) use password-style masking; the `x-sensitive` flag triggers this
- Environment variable substitution supported via `${VAR_NAME}` syntax in config values
- Validation against schema constraints (required fields, type checking, min/max) before saving
- Validation errors displayed inline in configuration forms with actionable error messages
- Configuration saved to skill-specific `config.json` files

**Configuration Schema Meta-Schema:**

Each skill's `config-schema.json` is itself a JSON Schema with Narratoria extensions:

| Extension | Type | Description |
|-----------|------|-------------|
| `x-sensitive` | boolean | If true, mask value in UI (API keys, passwords) |
| `x-env-var` | string | Environment variable for `${VAR_NAME}` substitution |
| `x-category` | string | UI grouping category for related fields |
| `format: "password"` | string | UI rendering hint for password fields |

### 3.4 Skill Script Execution

The Plan Executor invokes skill scripts as independent OS processes:

1. Scripts communicate via NDJSON protocol over stdin/stdout
2. Script input is passed as a single JSON object via stdin
3. The executor parses all NDJSON events: `log`, `state_patch`, `asset`, `ui_event`, `error`, `done`
4. Script dependencies declared in Plan JSON are respected; scripts execute in topological order
5. Both parallel and sequential execution are supported per Plan JSON flags
6. Per-script timeout (default 30 seconds) is enforced; unresponsive scripts are terminated
7. Script failures are handled per the `required` flag in Plan JSON
8. All events are collected for the full execution trace

### 3.5 Skill Data Management

Each skill maintains private data storage in `skills/<skill-name>/data/`:

- Data persists across application restarts
- Skill data is private; other skills must not directly access another skill's data directory
- Skills may use SQLite, JSON files, or other local storage formats
- Data directories are created on first use if they don't exist

For persistent narrative data shared across skills (memory events, reputation, NPC perception, character portraits), the shared persistence layer provides a common query interface. Skills communicate through this shared layer, not by accessing each other's private directories. This preserves the "no direct skill-to-skill calls" rule.

### 3.6 Graceful Degradation

The system continues functioning when skills are unavailable:

- Optional skills that are not installed or disabled do not prevent normal operation
- Misconfigured skills produce user-friendly warnings, not crashes
- Skills using hosted APIs fall back to local models when the network is unavailable
- If plan generation fails completely, the Narrator AI provides template-based narration
- The Plan Executor continues executing remaining plan steps when one script fails (if independent)
- All skills log failures and continue functioning for remaining capabilities

---

## 4. Narratoria Skills

### 4.1 Core Skills

#### 4.1.1 Storyteller

Rich narrative enhancement using the local LLM (Phi-3.5 Mini) or a configured hosted provider.

**Components:**
- Behavioral prompt for evocative narration
- `narrate.dart` script that calls LLM (local or hosted) for detailed prose; must produce 2-3 paragraphs of scene-setting narrative

**Configuration:**

| Field | Type | Description |
|-------|------|-------------|
| `provider` | enum | `ollama`, `claude`, `openai` |
| `model` | string | Model identifier |
| `apiKey` | string (sensitive) | API key for hosted providers |
| `style` | enum | `terse`, `vivid`, `poetic` |
| `fallbackProvider` | string | Provider to use when primary fails |

When the configured hosted API fails (network error, invalid key), the script gracefully falls back to the local model and logs the fallback. Fallback must complete within 10 seconds.

#### 4.1.2 Dice Roller

Randomness and game mechanics for outcome resolution.

**Components:**
- `roll-dice.dart` script: parses dice formulas (e.g., `1d20+5`, `3d6`, `2d6+modifier`)
- Emits `ui_event` with roll results for player display

**Configuration:**

| Field | Type | Description |
|-------|------|-------------|
| `showIndividualRolls` | boolean | Display each die result |
| `randomSource` | enum | `crypto` or `pseudo` random source |

#### 4.1.3 Memory

Semantic memory and continuity across sessions. This skill enables the "perplexingly on-point" narrative experience—the system's ability to reference past events, relationships, and player knowledge in contextually relevant ways.

**Components:**
- `store-memory.dart` — Receives event summaries, generates sentence-transformers embeddings, stores via the persistence layer
- `recall-memory.dart` — Receives semantic queries, performs vector similarity search, returns ranked results

**Store Input:**
```json
{
  "summary": "Player helped blacksmith repair anvil",
  "characters": ["player", "blacksmith_aldric"],
  "location": "blacksmith_shop",
  "significance": "high"
}
```

**Recall Input:**
```json
{
  "query": "interactions with blacksmith",
  "limit": 3,
  "filters": {"location": "blacksmith_shop"}
}
```

**Recall Output:**
```json
{
  "memories": [
    {"summary": "...", "timestamp": "...", "relevance": 0.92},
    {"summary": "...", "timestamp": "...", "relevance": 0.85}
  ]
}
```

**Configuration:**

| Field | Type | Description |
|-------|------|-------------|
| `embeddingModel` | string | Must be `sentence-transformers/all-MiniLM-L6-v2` |
| `similarityThreshold` | float | Minimum similarity score (default: 0.7) |

**Embedding Model Details:**
- **Model**: sentence-transformers/all-MiniLM-L6-v2
- **Size**: 33MB (~60MB on disk with dependencies)
- **Dimensions**: 384-dimensional vectors
- **Latency**: ~10-50ms per sentence on typical mobile hardware
- **Coverage**: All stored memories, lore chunks, and semantic queries

#### 4.1.4 Reputation

Faction standing tracking with persistence. Player actions have lasting consequences that affect NPC interactions and available choices.

**Components:**
- `update-reputation.dart` — Records reputation changes by faction
- `query-reputation.dart` — Returns current reputation values

**Configuration:**

| Field | Type | Description |
|-------|------|-------------|
| `factionList` | string[] | Faction names |
| `reputationScale` | object | Min/max values |
| `decayRate` | float | Reputation decay per in-game time unit |
| `storageBackend` | enum | `objectbox` or `files` |

Reputation decay is time-based: faction reputation loses approximately 10% per configured time unit for neutral relationships, with slower decay for strong opinions (±50 points).

### 4.2 Advanced Skills

#### 4.2.1 Player Choices

Generates contextual multiple-choice options that reflect the player's character, history, and relationships.

**Components:**
- `generate-choices.dart` — Analyzes context and produces 3-4 choices
- `evaluate-choice.dart` — Determines outcome modifiers for selected choice

**Configuration:**

| Field | Type | Description |
|-------|------|-------------|
| `minOptions` | integer | Minimum choices (default: 3) |
| `maxOptions` | integer | Maximum choices (default: 4) |
| `showDifficultyThreshold` | float | Show difficulty below this success probability |
| `consequenceHintVerbosity` | enum | `none`, `brief`, `detailed` |

**Contextual Factors:**

The choice skill considers multiple data sources when generating options:

1. **Persona Profile**: Player stats, traits, background. High Stealth (>15) unlocks lockpicking options. Low Charisma (<8) marks persuasion options as "unlikely to succeed."
2. **Narrative History**: Past choices and events. Previous betrayal of the Thieves Guild removes "Ask for guild assistance" as an option.
3. **Faction Reputation**: Queries the reputation skill for faction-based option filtering.
4. **NPC Perception**: Queries the NPC perception skill when applicable. Positive perception (>30) enables cooperation options; negative perception (<-30) hides friendly options.
5. **Difficulty Indicators**: Options are marked when player stats suggest low success probability.
6. **Consequence Hints**: Brief hints for each option (without spoiling outcomes).

Choice generation must complete within 3 seconds. Options are emitted as a `ui_event` with event type `narrative_choice`. Players select from presented choices only; free-text input is not supported. If the choice skill fails, the narrator falls back to a simplified set of generic choices (e.g., "Continue", "Look around", "Wait").

#### 4.2.2 Character Portraits

Visual character generation and persistent caching.

**Components:**
- `generate-portrait.dart` — Creates character images from narrative descriptions
- `lookup-portrait.dart` — Retrieves cached portraits by character identifier
- `update-portrait.dart` — Regenerates portrait for existing character

**Configuration:**

| Field | Type | Description |
|-------|------|-------------|
| `imageProvider` | enum | `local` or `hosted` |
| `stylePreset` | enum | `realistic`, `stylized`, `anime`, `pixel` |
| `resolution` | enum | 256, 512, 1024 |
| `timeout` | integer | Generation timeout in seconds |
| `storageLocation` | string | Path for portrait cache |

**Behavior:**
- Generates images based on narrative character descriptions
- Stores in persistent cache with character identifier
- Retrieves cached portraits when the same character reappears (semantic matching)
- Supports player character portraits from persona profile
- Emits `asset` events containing generated portrait data
- Degrades to placeholder silhouette when generation fails
- Generation must complete within 15 seconds (configurable)
- Supports regeneration when character description significantly changes

Portraits are stored in `skills/character-portraits/data/` with a character-to-portrait mapping in a local database.

#### 4.2.3 NPC Perception

Individual NPC relationship tracking, distinct from faction reputation. While reputation tracks standing with *groups*, perception tracks standing with *individuals*.

**Components:**
- `update-perception.dart` — Records perception changes
- `query-perception.dart` — Returns current perception value and modifiers
- `initialize-perception.dart` — Seeds perception for new NPCs

**Configuration:**

| Field | Type | Description |
|-------|------|-------------|
| `decayRate` | float | Perception decay per in-game time unit |
| `factionInfluenceWeight` | float | How much faction reputation affects initial perception (0-1) |
| `modifierScale` | float | Dice roll modifier per perception tier |
| `storageBackend` | enum | `objectbox` or `files` |

**Perception Mechanics:**
- Perception scores range from -100 to +100 per NPC identifier
- New NPC perception is initialized from: faction reputation (50%), visible player traits/equipment (30%), random variance (20%)
- Dice roll modifiers based on perception level:
  - Positive perception (>30): +1 to +3 bonus
  - Neutral perception (-30 to +30): no modifier
  - Negative perception (<-30): -1 to -3 penalty
- Perception decay is configurable (default: 10% per in-game week); strong impressions (±50) decay slower
- Perception queries must complete within 100ms
- Data persists across sessions

### 4.3 Skill Integration

Skills do not call each other directly. All inter-skill communication occurs through Plan JSON orchestration:

- The Player Choices skill queries NPC Perception when generating options for NPC interactions
- The Player Choices skill queries Reputation for faction-based option filtering
- NPC Perception consults Reputation when initializing perception for new NPCs
- The Portrait skill associates portraits with NPC identifiers used by the Perception skill
- The Narrator AI orchestrates these interactions through multi-step plans

---

## 5. AI Strategy

### 5.1 Narrator AI

The Narrator AI is Phi-3.5 Mini (3.8B parameters, 2.5GB GGUF quantized), running entirely in-process. It converts player choices into structured Plan JSON documents by analyzing context and selecting appropriate skills.

**Responsibilities:**
- Convert player text input into Plan JSON following the schema
- Select relevant skills based on player intent; determine which scripts to invoke
- Inject active skills' behavioral prompts into system context
- Fall back to simple pattern-based planning if LLM fails
- Complete plan generation within 5 seconds for typical inputs
- Never make network calls or access external APIs
- Consult `disabledSkills` in execution results to avoid selecting failed skills
- Track generation attempt count in metadata
- Avoid creating circular dependencies in the tools array

**Model Loading:**
- Downloads automatically from HuggingFace Hub (`microsoft/Phi-3.5-mini-instruct` GGUF variant) on first app launch
- Cached locally in the app's documents directory for offline use
- No network calls during gameplay

### 5.2 Semantic Embeddings

sentence-transformers/all-MiniLM-L6-v2 provides the semantic backbone for memory retrieval. Every stored memory, lore chunk, and query is converted to a 384-dimensional vector, enabling similarity-based retrieval that understands narrative intent rather than requiring exact keyword matches.

**Example**: A query for "interactions with craftspeople" semantically matches stored memories about "blacksmith", "weaponsmith", and "armorsmith"—even though no exact keywords overlap.

**Model Details:**
| Property | Value |
|----------|-------|
| Model | sentence-transformers/all-MiniLM-L6-v2 |
| Size | 33MB (~60MB on disk) |
| Dimensions | 384-dimensional vectors |
| Latency | ~10-50ms per sentence |
| Threshold | Default similarity threshold: 0.7 |

### 5.3 Contextual Retrieval

The Plan Generator decides *contextually* what data to retrieve. There are no fixed "memory tier budgets" or rigid context window allocations. Instead, Phi-3.5 Mini analyzes the current scene and generates plans that invoke memory retrieval skills with semantic queries:

```json
{
  "toolId": "recall",
  "toolPath": "skills/memory/recall.dart",
  "input": {"query": "past betrayals", "limit": 3}
}
```

The LLM may query lore, recent events, NPC relationships, faction reputation, or episodic memories based on scene needs. Retrieval is adaptive, driven by narrative context rather than predetermined budgets.

### 5.4 Testing Stub

For development and testing, a pattern-based plan generator provides deterministic plan generation without loading the full model:

```dart
abstract class NarratorAI {
  Future<PlanJson> generatePlan({
    required String playerInput,
    required Set<String> disabledSkills,
    required List<Skill> availableSkills,
    String? parentPlanId,
    int generationAttempt = 1,
  });
}
```

The testing stub uses hard-coded RegExp pattern → plan mappings (e.g., `roll.*dice?` → dice roll plan, `recall|remember` → memory plan) and returns a fallback plan with narrative-only response for unrecognized patterns. It supports at least 5 patterns for integration testing and respects `disabledSkills` and replan metadata.

---

## 6. Data Architecture

### 6.1 Persistence Layer

The persistence layer is shared infrastructure providing a unified ObjectBox-based storage and retrieval interface for narrative data. It stores data and answers queries, but does not decide when or why data is retrieved—that's the responsibility of the Plan Generator and individual skills.

**Primary Responsibilities:**
1. Store narrative data: memory events, lore chunks, faction reputation, NPC perception, character portraits
2. Semantic search: vector similarity search for context-relevant retrieval
3. Query interface: fast, filtered access to stored data (<200ms latency)
4. Persistence: data survives application restarts and session boundaries

**Architectural Note:** Like the Narrator AI, the persistence layer runs within the Dart runtime as shared infrastructure. This differs from skill-private `data/` directories. The shared layer enables cross-skill data access through a query interface without violating the "no direct skill-to-skill calls" rule.

#### 6.1.1 Storage Schema

**Memory Event:**
| Attribute | Type | Description |
|-----------|------|-------------|
| ID | auto | Primary key |
| summary | string | Event narrative text |
| embedding | float[384] | Semantic vector via sentence-transformers |
| timestamp | datetime | When event occurred |
| sessionId | string | Story session identifier |
| playthroughId | string | Story playthrough identifier |
| characterIds | string[] | Characters involved |
| actionType | string | Category of action |
| semanticTags | string[] | Searchable tags |

**Lore Chunk:**
| Attribute | Type | Description |
|-----------|------|-------------|
| ID | auto | Primary key |
| filePath | string | Original source file |
| chunkIndex | integer | Position within file |
| paragraphId | integer | Paragraph identifier |
| content | string | Chunk text |
| embedding | float[384] | Semantic vector |
| tokenCount | integer | Token count for context budgeting |

**Faction Reputation:**
| Attribute | Type | Description |
|-----------|------|-------------|
| factionId | string | Faction identifier |
| playthroughId | string | Story playthrough |
| currentScore | float | Current reputation value |
| lastUpdated | datetime | Last modification |
| decayRate | float | Decay per time unit |

**NPC Perception:**
| Attribute | Type | Description |
|-----------|------|-------------|
| npcId | string | NPC identifier |
| playthroughId | string | Story playthrough |
| perceptionScore | float | Score (-100 to +100) |
| lastInteraction | datetime | Last interaction timestamp |
| eventHistory | string | Interaction history summary |

**Character Portrait:**
| Attribute | Type | Description |
|-----------|------|-------------|
| characterId | string | Character identifier |
| imagePath | string | Path to cached image |
| descriptionHash | string | Hash of source description |
| generatedAt | datetime | Generation timestamp |
| playthroughId | string | Story playthrough |

**Playthrough Session:**
| Attribute | Type | Description |
|-----------|------|-------------|
| sessionId | string | Session identifier |
| playthroughId | string | Playthrough identifier |
| startTime | datetime | Session start |
| endTime | datetime | Session end |
| currentLocation | string | Current narrative location |

#### 6.1.2 Query Interface

The persistence layer exposes five core methods:

**`semanticSearch(query, dataType, limit, filters)`** — Returns data ranked by embedding similarity above the configured threshold (default: 0.7). Supports filtering by timestamp range, session, playthrough, character, location, and custom tags.

**`exactMatch(filters)`** — Returns data matching exact filter criteria: timestamp range, story session, playthrough ID, character identifier, NPC identifier, faction ID, source file path.

**`store(dataType, record)`** — Persists a new record atomically.

**`update(dataType, identifier, changes)`** — Modifies an existing record atomically.

**`delete(dataType, identifier)`** — Removes a record (used for data retention policies).

#### 6.1.3 Performance Requirements

- Typical narrative sessions generate 50-200 memory events; the storage layer is designed for thousands of events per playthrough, not millions
- All query methods must complete within 200ms for databases containing up to 10,000 records
- Semantic search must return results in under 500ms for 1000+ stored events
- Concurrent read access from multiple skills must not block or corrupt data (read-optimized with write locks)
- Query performance monitoring: log latency, result count, search scope for debugging

#### 6.1.4 Data Retention and Decay

- Configurable retention policies (time-based, count-based, storage-based) executed during idle periods
- Time-sensitive numeric data (reputation, perception) decays based on configured rates when queried
- Memory events do not decay; they remain accessible indefinitely
- Each playthrough has its own data scope; cross-playthrough queries are not supported

### 6.2 Campaign Format

A campaign is a self-contained directory containing world-building, characters, plot structure, lore, and creative assets. The directory structure serves as the ingestion interface for the persistence layer.

**Core Principle**: *The more a campaign provides, the less the AI invents.* This is a spectrum, not a toggle.

#### 6.2.1 Campaign Format Creeds

Every campaign includes a `README.md` encoding these principles:

1. **Respect Human Artistry**: Generated content is a tool to accelerate creative work, never to replace human authorship.
2. **Radical Transparency**: All AI-generated assets are explicitly marked with `generated: true` in their metadata.
3. **Human Override**: Authors can refine, replace, or delete any AI-generated content at any time.
4. **Attribution and Credit**: When campaigns are shared, generated content sources are disclosed.
5. **Preserve Intent**: When bootstrapping from sparse data, the LLM prioritizes staying true to the author's explicit input.

#### 6.2.2 Directory Structure

```
campaign_name/
├── manifest.json                    # Required: campaign metadata
├── README.md                        # Campaign format creeds
├── world/
│   ├── setting.md                   # World, era, tone, environment
│   ├── rules.md                     # Custom game mechanics (optional)
│   └── constraints.md               # Absolute AI boundaries (optional)
├── characters/
│   ├── npcs/{name}/
│   │   ├── profile.json             # Name, role, personality, motivations
│   │   ├── secrets.md               # Hidden information (optional)
│   │   └── portrait.png             # Character artwork (optional)
│   └── player/
│       └── template.json            # Character creation constraints
├── plot/
│   ├── premise.md                   # Starting situation and hook
│   ├── beats.json                   # Key story moments with conditions
│   └── endings/                     # Multiple ending definitions
│       ├── redemption.md
│       └── betrayal.md
├── lore/                            # Indexed for semantic search (RAG)
│   ├── history/
│   ├── magic/
│   └── locations/
├── stats/                           # Stat definitions (*.stat.txt)
│   ├── health.stat.txt              # Player stat: health gauge
│   ├── mana.stat.txt                # Player stat: magic resource
│   └── hidden/                      # Hidden stats (not shown to player)
│       └── suspicion.stat.txt
├── items/                           # Item definitions (*.item.txt)
│   ├── weapon.short_sword.item.txt
│   └── potion.healing.item.txt
├── art/                             # Images (PNG, JPEG, WebP)
│   ├── characters/
│   ├── locations/
│   └── items/
└── music/                           # Audio (MP3, OGG, WAV, FLAC)
    ├── ambient/
    └── combat/
```

All directories and files except `manifest.json` are optional. Markdown files are used for prose; JSON for structured data.

#### 6.2.3 Manifest Schema

The manifest (`manifest.json`) requires only `title` and `version` (semver):

```json
{
  "title": "Chronicles of Merlin",
  "version": "2.1.0",
  "author": "Jane Storyteller",
  "description": "An epic fantasy retelling of Arthurian legend",
  "genre": "High Fantasy",
  "tone": "Epic, Introspective, Morally Complex",
  "content_rating": "Teen",
  "rules_hint": "narrative",
  "hydration_guidance": "Execute faithfully - all content is intentionally crafted",
  "content_warnings": ["violence", "moral complexity"],
  "estimated_playtime_hours": 10,
  "tags": ["magic", "medieval", "quest", "moral-choices"]
}
```

Optional fields include `author`, `description`, `genre`, `tone`, `content_rating` (Everyone/Everyone 10+/Teen/Mature/Adults Only), `rules_hint` (rules-light/narrative/crunchy/tactical), `hydration_guidance`, `content_warnings`, `estimated_playtime_hours`, `tags`, `license`, and `homepage`.

#### 6.2.4 NPC Profile Schema

NPC profiles are structured JSON files requiring `name`, `role`, and `personality`:

```json
{
  "name": "Merlin",
  "role": "Protagonist - Court Wizard",
  "age": "Unknown (appears elderly)",
  "personality": {
    "traits": ["wise", "melancholic", "secretive", "compassionate"],
    "flaws": ["burdened by foresight", "reluctant to share truth"],
    "virtues": ["loyal", "patient", "protective"]
  },
  "motivations": [
    "Guide Arthur to unite the kingdoms",
    "Prevent the fall of Camelot (which he has foreseen)"
  ],
  "speech_patterns": {
    "style": "Formal, archaic, uses metaphors from nature",
    "examples": [
      "The river of time flows only one direction, young king.",
      "Even the mightiest oak was once a fragile acorn."
    ]
  },
  "relationships": {
    "arthur": "Mentor and protector, loves like a son",
    "morgana": "Former student, now enemy - feels guilt and sorrow"
  },
  "secrets": "See secrets.md",
  "portrait": "art/characters/merlin.png"
}
```

Additional optional fields: `appearance`, `background`, `goals` (with priority and status), `stats`, `inventory`, and `metadata` (importance tier, first appearance, tags).

#### 6.2.5 Plot Beat Schema

Plot beats define key story moments the AI works toward:

```json
{
  "beats": [
    {
      "id": "beat_001",
      "title": "Vision of the Future",
      "description": "Merlin has a prophetic vision showing Camelot in flames.",
      "conditions": {
        "scene_count": { "min": 2, "max": 5 },
        "player_state": "established in Camelot",
        "requires_beat": "beat_000"
      },
      "priority": "critical",
      "consequences": ["Merlin becomes more secretive", "Player gains 'Burden of Knowledge'"],
      "skippable": false,
      "mood": "Foreboding",
      "music": "music/beats/vision_theme.mp3"
    }
  ]
}
```

Condition types: `scene_count` (min/max window), `requires_beat` / `requires_any_beat` (prerequisites), `player_choice` / `player_choices`, `player_state`, `npc_state`, `world_state`, and `custom` expressions.

Priority levels: `critical` (story-essential), `high`, `medium`, `low`, `optional`. Beats can reference `outcomes` with branching probabilities, include `dialogue` lines, and define `timeout` behavior with fallback beats.

#### 6.2.6 Player Character Template

Character creation can be `freeform`, `guided`, or `preset` mode:

```json
{
  "character_creation": {
    "mode": "guided",
    "guidance": "You are a traveler arriving in Camelot."
  },
  "allowed_races": [
    {"name": "Human", "description": "Versatile and adaptable", "traits": ["diplomatic"]}
  ],
  "allowed_classes": [
    {"name": "Knight", "abilities": ["swordsmanship", "leadership"]}
  ],
  "starting_location": "Gates of Camelot",
  "starting_items": ["traveler's cloak", "worn dagger"],
  "constraints": {
    "must_be_human": true,
    "era_locked": "Medieval"
  }
}
```

#### 6.2.7 Stats System

Stats are **named numeric gauges that constrain what the narrative can do**. Every RPG system — tabletop, CRPG, visual novel, dating sim — uses stats for three purposes:

1. **Gate**: Determine what options are available ("You need 14 Strength to force the door")
2. **Modify**: Shift probability of outcomes ("Roll + Dexterity modifier")
3. **Resource**: Deplete and replenish to create tension ("You have 3 HP left")

Whether a stat is called "HP," "Hull Integrity," "Composure," "Chemistry," or "Favor with the Empress," it's always a named number with a range that gates, modifies, or depletes. Narratoria treats stats generically — the system doesn't need to know what "health" means in advance. It knows there are N stats, each has a range, and each gets a UI gauge.

**Core Principle**: *Convention over configuration.* Authors define stats by creating files in a `stats/` directory. The filename, extension, and a simple header block carry all the semantics the runtime needs.

##### Stat File Convention

**Location**: `campaign_name/stats/`
**Filename pattern**: `{stat_id}.stat.txt`
**Hidden stats**: `campaign_name/stats/hidden/` (or `hidden: yes` in header)

**File format**: A key-value header block (lines before the first blank line), followed by freeform behavioral prose.

**Example: Fantasy RPG — `stats/health.stat.txt`**

```
range: 0-100
default: 100
display: bar
label: Health
category: vital

Health represents physical well-being. When health reaches 0,
the character is incapacitated. Combat damage, poison, and
exhaustion reduce health. Rest, potions, and healing magic
restore it.

The narrator should describe declining health through
increasingly vivid physical symptoms — heavy breathing at 70,
visible wounds at 40, barely standing at 15.
```

**Example: Dating Sim — `stats/chemistry.stat.txt`**

```
range: 0-10
default: 0
display: hearts
label: Chemistry
category: relationship

Chemistry measures romantic tension between the player and
a love interest. It rises through flirting, shared vulnerability,
and meaningful gifts. It drops through insensitivity, betrayal,
or prolonged absence.

At 8+, the love interest initiates romantic dialogue unprompted.
At 3 or below, they become distant and formal.
```

**Example: Sci-Fi — `stats/hull_integrity.stat.txt`**

```
range: 0-1000
default: 1000
display: bar
label: Hull Integrity
category: ship

Hull integrity represents the structural health of the player's
starship. Asteroid impacts, weapons fire, and hard landings
reduce it. Repair drones, spacedock maintenance, and emergency
patches restore it.

Below 200, the narrator should describe sparking conduits,
flickering lights, and hull breach warnings.
```

**Example: Hidden Stat — `stats/hidden/suspicion.stat.txt`**

```
range: 0-100
default: 0
display: bar
label: Suspicion
category: social
hidden: yes

Suspicion tracks how much the town guard suspects the player
of criminal activity. Witnessing theft, finding contraband,
or receiving tips from informants raises suspicion. Bribes,
good deeds, and time passing lower it.

The narrator should reveal suspicion indirectly — guards
watching more closely at 30, being followed at 60, an
arrest warrant at 90.
```

##### Header Fields

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `range` | string | Yes | — | Numeric bounds as `{min}-{max}` (e.g., `0-100`, `1-20`, `-50-50`) |
| `default` | number | No | `min` value | Starting value for new playthroughs |
| `display` | enum | No | `bar` | UI rendering hint: `bar`, `number`, `hearts`, `pips`, `ring`, `hidden` |
| `label` | string | No | Filename stem, title-cased | Human-readable display name |
| `category` | string | No | `general` | Grouping key for UI layout (e.g., `vital`, `resource`, `relationship`, `ship`, `social`) |
| `hidden` | boolean | No | `false` | If `yes`, the system tracks but does not display to the player |

**Parsing Rules:**

1. Header lines are `key: value` pairs (colon-space separated)
2. First blank line ends the header
3. Everything after the blank line is **behavioral prose** — injected into the Narrator AI's context alongside skill `prompt.md` files
4. Lines starting with `#` in the header are comments (ignored)
5. Unknown header keys are stored as metadata but not interpreted by the runtime

##### Ingestion and State Binding

On campaign load, the ingestion pipeline processes `stats/`:

```
For each *.stat.txt in stats/ (including stats/hidden/):
  1. Parse filename stem → stat ID ("health", "chemistry")
  2. Parse header → range, default, display, label, category, hidden
  3. Parse prose body → behavioral prompt text
  4. Store stat definition in ObjectBox:
     - Stat ID, range bounds, default, display mode, category
     - Behavioral prose stored as embedding + raw text
     - Hidden flag
  5. Register state path: player.stats.{stat_id}
  6. Initialize session state: player.stats.{stat_id} = default
  7. Check for UI asset: ui/state/player.stats.{stat_id}.{ext}
  8. Inject behavioral prose into Narrator AI system context
```

**Resulting Session State:**

```json
{
  "player": {
    "stats": {
      "health": 100,
      "mana": 50,
      "chemistry": 0
    }
  }
}
```

Skills update stats via standard `state_patch` events:

```json
{"version":"0","type":"state_patch","patch":{"player":{"stats":{"health":72}}}}
```

The runtime enforces range bounds — a patch setting `health` to `-5` is clamped to `0`; setting it to `150` is clamped to `100`.

##### NPC Stats

NPC definitions can include their own stats. An author has two options:

**Option 1: Inline in NPC profile**

NPC stats defined within `characters/npcs/{name}/profile.json`:

```json
{
  "name": "Owen",
  "role": "Love Interest",
  "stats": {
    "chemistry": {"value": 0, "range": "0-10"},
    "trust": {"value": 3, "range": "0-10"}
  }
}
```

**Option 2: Dedicated stat files**

Authors can create stat files scoped to specific NPCs for richer behavioral prose:

```
stats/npc.owen.chemistry.stat.txt
stats/npc.owen.trust.stat.txt
```

**`stats/npc.owen.chemistry.stat.txt`:**

```
range: 0-10
default: 0
display: hearts
label: Chemistry with Owen
category: relationship

Owen is guarded after a past betrayal. Chemistry builds slowly
through consistent kindness and shared creative pursuits.
Grand gestures make him uncomfortable — he values quiet moments.

At 7+, Owen begins sharing personal stories unprompted.
At 2 or below, he avoids being alone with the player.
```

**NPC stat state path**: `npcs.{npc_id}.stats.{stat_id}`

```json
{
  "npcs": {
    "owen": {
      "stats": {
        "chemistry": 4,
        "trust": 6
      }
    }
  }
}
```

When both an inline stat and a dedicated file exist for the same NPC stat, the dedicated file takes precedence (richer behavioral guidance).

##### Items with Stat-Relevant Data

Items are defined in `items/` using `.item.txt` files with the same header-plus-prose convention:

**`items/weapon.short_sword.item.txt`:**

```
type: weapon
damage: 1d6
weight: 3
label: Short Sword
category: melee

A reliable sidearm favored by scouts and rogues. Its short
blade excels in tight quarters — corridors, ship decks,
and tavern brawls.

The narrator should describe its use as quick, precise strikes
rather than heavy cleaving blows.
```

**`items/potion.healing.item.txt`:**

```
type: consumable
effect: health +25
uses: 1
weight: 0.5
label: Healing Potion
category: consumable

A small glass vial containing a warm crimson liquid. When
consumed, it rapidly mends wounds and restores vitality.

The narrator should describe a spreading warmth, the taste
of honey and copper, and the visible closure of minor wounds.
```

On ingestion, item headers are tokenized and stored in ObjectBox with semantic embeddings. When the Narrator AI generates a plan involving a dice roll or stat check, it can look up the relevant item's properties:

```json
{
  "toolId": "dice_roll",
  "toolPath": "skills/dice-roller/roll.dart",
  "input": {
    "formula": "1d20 + player.stats.dexterity",
    "item_context": "weapon.short_sword"
  }
}
```

The dice roller skill queries ObjectBox for `weapon.short_sword`, retrieves `damage: 1d6`, and factors it into the result.

##### UI Rendering

The UI renders stats deterministically based on definition metadata:

1. **Discovery**: Query all stat definitions from ObjectBox where `hidden = false`
2. **Grouping**: Group by `category` field
3. **Ordering**: Within each category, alphabetical by label (or author-specified order if present)
4. **Rendering**: Apply `display` mode:

| Display Mode | Rendering | Best For |
|-------------|-----------|----------|
| `bar` | Horizontal fill bar with current/max | HP, mana, hull integrity |
| `number` | Plain numeric display | Strength, intelligence |
| `hearts` | Row of filled/empty heart icons | Relationship stats |
| `pips` | Discrete filled circles | Skill levels (1-5 scale) |
| `ring` | Circular progress indicator | Single prominent stat |
| `hidden` | Not rendered (equivalent to `hidden: yes`) | Behind-the-scenes trackers |

5. **Asset binding**: Check `ui/state/player.stats.{stat_id}.{ext}` for a custom icon
6. **Fallback**: No custom icon → use generic icon based on `category` (heart for vital, star for resource, etc.)

**Category-based UI layout:**

```
┌─────────────────────────────────────┐
│ VITAL                               │
│ ❤️ Health  ████████░░░  72/100      │
│ 💧 Mana    ██████░░░░░  30/50      │
├─────────────────────────────────────┤
│ RELATIONSHIP                        │
│ 💕 Chemistry with Owen  ♥♥♥♥♡♡♡♡♡♡ │
│ 🤝 Trust with Owen      ♥♥♥♥♥♥♡♡♡♡ │
├─────────────────────────────────────┤
│ RESOURCE                            │
│ 💰 Gold    247                      │
└─────────────────────────────────────┘
```

##### Narrator AI Integration

Stat definitions are injected into the Narrator AI's system context during plan generation:

```
CAMPAIGN STATS:
- health (vital): 0-100, currently 72. "Health represents physical 
  well-being. The narrator should describe declining health through 
  increasingly vivid physical symptoms..."
- chemistry (relationship, NPC: Owen): 0-10, currently 4. "Owen is 
  guarded after a past betrayal. Chemistry builds slowly through 
  consistent kindness..."
```

This enables the AI to:
- Reference stats when generating narrative prose
- Select appropriate skills for stat-modifying actions
- Generate choices that reflect current stat values
- Respect hidden stats without revealing them to the player

##### Stat Change Narration

When a stat changes, the behavioral prose guides how the narrator describes it. The system does not require the narrator to announce stat changes numerically — instead, the prose teaches the narrator to weave stat effects into the story naturally.

**Example flow:**

1. Player drinks healing potion
2. Dice roller resolves: health +25 (72 → 97)
3. State patch applied: `{"player": {"stats": {"health": 97}}}`
4. Storyteller skill receives updated state + behavioral prose for health
5. Narrator generates: *"Warmth spreads through your chest as the crimson liquid does its work. The throbbing in your shoulder fades to a dull ache, and you can finally draw a full breath."*

The player sees health bar update from 72 to 97. The narrative describes the *experience* of healing, not the number.

#### 6.2.8 Lore System

All files in `lore/` are indexed for semantic search (RAG retrieval):

- Files are chunked by paragraph (split on `\n\n`)
- Maximum 512 tokens per chunk
- If a single paragraph exceeds 512 tokens, it is split on sentence boundaries (`.`, `!`, `?`)
- Each chunk is stored with metadata: original file path, chunk index, paragraph ID, token count, chunk method ("paragraph")
- Token counts must be computed using the `tiktoken` library with the `cl100k_base` tokenizer (compatible with the sentence-transformers embedding model)
- Nested directories within `lore/` are supported for organization

#### 6.2.9 Creative Assets

**Image assets** (`art/`): Supported formats are PNG, JPEG, and WebP. Nested subdirectories are supported (e.g., `art/characters/`, `art/locations/`, `art/items/`).

**Audio assets** (`music/`): Supported formats are MP3, OGG, WAV, and FLAC. Nested subdirectories are supported (e.g., `music/ambient/`, `music/combat/`).

File naming conventions enable semantic linking: `art/characters/npc_wizard.png` is indexed alongside `characters/npcs/wizard/profile.json`.

##### Asset-Driven Narration Constraints

The system enforces a **grounding principle**: *Provided campaign content takes absolute precedence over AI generation.*

When the Narrator AI generates narrative prose or makes planning decisions, it **must prioritize** campaign-provided data:

1. **Character Descriptions**: If an NPC has a `profile.json`, the AI uses those exact personality traits, motivations, and speech patterns. It does not invent new traits.

2. **World Details**: If `world/setting.md` specifies "magic is forbidden in the Northern Kingdom," the AI never generates scenes where Northern guards use spellcasting.

3. **Visual Consistency**: If `art/characters/npc_wizard.png` shows a young woman in blue robes, the AI describes her as young and wearing blue—not elderly with a staff.

4. **Lore Authority**: If `lore/history/founding.md` states "The kingdom was founded 200 years ago," the AI uses that timeline. It does not hallucinate "ancient origins dating back millennia."

5. **Item Properties**: If `world/rules.md` defines healing potions as single-use with exact HP restoration, the AI honors those mechanics rather than inventing gradual regeneration.

**Implementation Mechanism:**

- During plan generation, the Narrator AI receives campaign constraints in its system prompt:
  ```
  CAMPAIGN CONSTRAINTS:
  - Setting: [contents of world/setting.md]
  - Tone: [from manifest.json]
  - Custom Rules: [world/rules.md if present]
  - Active NPCs: [list of NPC names with profile summaries]
  - Available Items: [items defined in lore/ or world/rules.md]
  ```

- Memory and lore retrieval skills inject grounding facts into the context:
  ```json
  {
    "toolId": "recall",
    "toolPath": "skills/memory/recall.dart",
    "input": {
      "query": "wizard appearance",
      "sources": ["characters/npcs/wizard/profile.json", "art/characters/npc_wizard.png.keywords.txt"]
    }
  }
  ```

- The Storyteller skill's behavioral prompt (`prompt.md`) includes explicit instructions:
  ```markdown
  When describing characters, locations, or items:
  1. Check if campaign data exists for this entity
  2. If YES: Use provided details verbatim; do not embellish or contradict
  3. If NO: Generate details consistent with campaign tone and constraints
  ```

**Grounding Hierarchy:**

| Priority | Source | Behavior |
|----------|--------|----------|
| 1 | Structured data (JSON profiles, manifest) | Use exactly as written; no modifications |
| 2 | Prose data (markdown files in lore/, world/) | Paraphrase naturally but preserve all facts |
| 3 | Asset metadata (keywords, alt text) | Use as hints for visual consistency |
| 4 | AI generation with campaign tone constraint | Generate only when no data exists; match tone |
| 5 | Fallback generic narration | Last resort when plan+skills fail entirely |

**Example Scenario:**

Campaign provides:
- `characters/npcs/blacksmith/profile.json`: Name "Gareth", gruff, secretly kind
- `art/characters/blacksmith.png.keywords.txt`: muscular, scarred, leather apron
- `lore/locations/forge.md`: "The forge has been in Gareth's family for three generations"

Player action: "I approach the blacksmith"

Narrator AI generates plan invoking memory skill to retrieve Gareth's profile and forge lore. Storyteller skill produces:

> "You approach the forge, its heat radiating even from the threshold. Gareth, a muscular man with old scars crossing his forearms, looks up from his work. His leather apron is stained dark with soot. 'What do you want?' he grumbles, though there's no real malice in his tone. The forge has been in his family for three generations—the pride evident in how meticulously he maintains every tool."

**What the AI did NOT do:**
- ❌ Invent that Gareth has a friendly demeanor (profile says "gruff")
- ❌ Describe him as elderly (keywords say "muscular")
- ❌ Add details like "he wears a gold ring" (not in provided data)
- ❌ Make up family history beyond what lore provides

**Debugging Content Grounding:**

Skills can emit verification logs showing grounding evidence:

```json
{"version":"0","type":"log","level":"debug","message":"Grounded 'blacksmith' from characters/npcs/blacksmith/profile.json"}
{"version":"0","type":"log","level":"debug","message":"Grounded 'forge history' from lore/locations/forge.md"}
{"version":"0","type":"log","level":"warning","message":"No data for 'number of anvils'—generated detail"}
```

The system logs show what was grounded vs. generated, enabling authors to identify gaps and add missing content in subsequent campaign versions.

#### 6.2.10 Asset Metadata Structure

All ingested assets follow a consistent metadata schema in the persistence layer:

**Core Metadata (All Assets):**
```json
{
  "path": "art/characters/npc_wizard.png",
  "type": "image",
  "keywords": ["wizard", "archmage", "merlin"],
  "generated": false,
  "checksum": "sha256:abc123...",
  "created_at": "2026-02-03T10:30:00Z",
  "data": "<content>"
}
```

**Provenance Metadata (when `generated: true`):**
```json
{
  "provenance": {
    "source_model": "ollama/gemma:2b",
    "generated_at": "2026-02-03T11:00:00Z",
    "seed_data": "I am bread",
    "version": "1.0.0"
  }
}
```

**Type-Specific Metadata:**
- `image`: format, width, height, alt_text
- `audio`: format, duration_seconds, bitrate, loop flag
- `prose`: word_count, language, chunks, chunk_method, per-chunk metadata
- `structured`: schema_type, schema_version, entity_id

**Relationship Metadata:**
```json
{
  "relationships": {
    "references": ["characters/npcs/wizard/profile.json"],
    "referenced_by": ["plot/beats.json"],
    "entity_links": ["wizard", "npc_merlin"]
  }
}
```

#### 6.2.11 Keyword Sidecar Files

Authors can override auto-extracted keywords by creating `.keywords.txt` sidecar files alongside assets:

```
# art/characters/npc_wizard.png.keywords.txt
wizard
archmage
merlin
elderly
staff
magic_user
```

Format: plain text, one keyword per line, comments with `#` prefix. When a sidecar exists, the system uses its keywords instead of auto-extracting from filename or content.

#### 6.2.12 State-Bound UI Assets

Campaign authors can provide custom artwork and icons that bind to specific session state paths, enabling rich visual customization of UI elements. This feature allows authors to replace generic UI elements with thematic artwork that matches their campaign's aesthetic.

**Core Principle**: *The more UI assets a campaign provides, the less the system uses generic placeholders.*

##### State Asset Directory Structure

```
campaign_name/
├── ui/
│   ├── state/
│   │   ├── player.hp.webp                    # Icon for health stat
│   │   ├── player.maxHp.webp                 # Optional: max health icon
│   │   ├── player.attributes.strength.webp   # Strength attribute icon
│   │   ├── player.attributes.dexterity.webp  # Dexterity attribute icon
│   │   ├── player.inventory.webp             # Inventory container icon
│   │   ├── world.location.webp               # Location marker icon
│   │   ├── world.time.webp                   # Time indicator icon (e.g., clock)
│   │   ├── quest.active.webp                 # Active quest icon
│   │   └── combat.active.webp                # Combat mode indicator
│   └── items/                                 # Item-specific icons
│       ├── rusty_dagger.webp
│       ├── torch.webp
│       └── healing_potion.webp
```

**Naming Convention**: State-bound asset filenames follow the pattern `{state.path}.{extension}` where:
- `state.path` is the dot-notation path from session state (e.g., `player.hp`, `world.time`)
- Nested paths use dots as separators (e.g., `player.attributes.strength`)
- Supported formats: WebP (preferred), PNG, SVG

**Recommended Dimensions**:
- **State icons** (hp, attributes, etc.): 48×48px to 128×128px
- **Item icons**: 64×64px to 256×256px
- **Location/world icons**: 64×64px to 128×128px

Assets should be optimized for file size. WebP is preferred for raster images; SVG for vector graphics.

##### State Asset Metadata Schema

State-bound assets include additional metadata fields:

```json
{
  "path": "ui/state/player.hp.webp",
  "type": "state_icon",
  "state_binding": {
    "path": "player.hp",
    "display_mode": "icon_with_value",
    "value_format": "{current} / {max}",
    "fallback": "heart_generic"
  },
  "dimensions": {"width": 64, "height": 64},
  "generated": false,
  "keywords": ["health", "hit points", "player stat"]
}
```

**`state_binding` Fields**:

| Field | Type | Description |
|-------|------|-------------|
| `path` | string | Session state path this asset represents (e.g., `player.hp`) |
| `display_mode` | enum | How UI renders: `icon_only`, `icon_with_value`, `icon_with_label`, `icon_with_bar` |
| `value_format` | string | Template for displaying numeric values (e.g., `"{current} / {max}"`, `"{value}%"`) |
| `fallback` | string | Generic icon ID to use if this asset fails to load |
| `interactive` | boolean | Whether tapping/clicking shows detailed view (default: `true`) |
| `tooltip` | string | Optional tooltip text override |

##### UI Rendering Behavior

When the UI needs to display a state value, the rendering logic follows this sequence:

1. **Check for state-bound asset**: Look for `ui/state/{state.path}.{ext}` in campaign
2. **Apply display mode**:
   - `icon_only`: Show icon; value appears on tap/hover
   - `icon_with_value`: Show icon + formatted value inline
   - `icon_with_label`: Show icon + custom label
   - `icon_with_bar`: Show icon + progress bar (for numeric ranges)
3. **Handle interaction**:
   - Tapping icon opens detailed view with full state data
   - For `player.inventory`, opens inventory grid
   - For `player.hp`, shows health details (current, max, status effects)
   - For `world.location`, shows location description and available actions
4. **Fallback on missing asset**: Use generic themed icon (heart for hp, clock for time, etc.)

##### Item-Specific Icons

Campaign authors can provide icons for individual inventory items:

```
ui/items/rusty_dagger.webp
ui/items/torch.webp
ui/items/healing_potion.webp
```

Items are matched by:
1. **Item ID match**: `state.player.inventory.{item_id}` → `ui/items/{item_id}.webp`
2. **Item type match**: `state.player.inventory.{id}.type` → `ui/items/{type}.webp`
3. **Keyword match**: Item keywords → asset keywords via semantic similarity
4. **Generic fallback**: Use placeholder icon with item name label

##### Skill Integration

Skills can reference state-bound assets when emitting state patches:

```json
{
  "version": "0",
  "type": "state_patch",
  "patch": {
    "player": {
      "inventory": {
        "rusty_dagger": {
          "name": "Rusty Dagger",
          "type": "weapon",
          "damage": "1d4",
          "icon": "ui/items/rusty_dagger.webp",  // Optional: explicit reference
          "equipped": true
        }
      }
    }
  }
}
```

If `icon` field is provided, the UI uses that path directly. If omitted, the UI uses the item ID matching logic described above.

##### Campaign Manifest Configuration

Authors can configure state asset behavior in `manifest.json`:

```json
{
  "title": "Chronicles of Merlin",
  "version": "2.1.0",
  "ui_customization": {
    "state_icons": {
      "enabled": true,
      "theme": "medieval_fantasy",
      "fallback_style": "themed",  // "themed" or "generic"
      "icon_size": "medium"        // "small", "medium", "large"
    },
    "item_display": {
      "mode": "grid",               // "grid" or "list"
      "columns": 4,
      "show_quantity": true,
      "show_weight": false
    }
  }
}
```

##### Runtime Asset Resolution

The system uses a deterministic lookup algorithm to resolve state-bound assets at runtime. Asset resolution happens in two phases: **campaign load** (indexing) and **runtime lookup** (retrieval).

**Phase 1: Campaign Load Indexing**

When a campaign loads, the ingestion pipeline scans `ui/state/` and `ui/items/` directories and builds an in-memory index:

```dart
// Conceptual data structure
class StateAssetIndex {
  // Exact path matches
  Map<String, AssetMetadata> exactMatches;
  
  // Pattern matches (wildcards)
  List<PatternMatch> patternMatches;
  
  // Type-based fallbacks
  Map<String, AssetMetadata> typeFallbacks;
  
  // Category-level fallbacks
  Map<String, AssetMetadata> categoryFallbacks;
}
```

**Indexing Rules:**

1. **Exact paths**: `player.hp.webp` → indexed as `"player.hp"`
2. **Wildcard paths**: `world.locations.*.marker.webp` → pattern `world.locations.{id}.marker`
3. **Type fallbacks**: `ui/items/weapon.webp` → fallback for all items with `type: "weapon"`
4. **Category fallbacks**: `ui/state/player.attributes.*.webp` → pattern for any attribute

The index is built once per campaign load and cached in memory. Index construction completes in <100ms for campaigns with up to 1000 assets.

**Phase 2: Runtime Lookup Algorithm**

When the UI needs to render a state value, it queries the asset index using this resolution chain:

```
function resolveStateAsset(statePath: String) -> AssetMetadata? {
  // Step 1: Exact match
  if (exactMatches.containsKey(statePath)) {
    return exactMatches[statePath];
  }
  
  // Step 2: Pattern match (wildcards)
  for (pattern in patternMatches) {
    if (pattern.matches(statePath)) {
      return pattern.asset;
    }
  }
  
  // Step 3: Type-based fallback
  stateValue = sessionState.get(statePath);
  if (stateValue.hasType()) {
    typePath = stateValue.type;
    if (typeFallbacks.containsKey(typePath)) {
      return typeFallbacks[typePath];
    }
  }
  
  // Step 4: Category-based fallback
  category = extractCategory(statePath);  // e.g., "player", "world", "combat"
  if (categoryFallbacks.containsKey(category)) {
    return categoryFallbacks[category];
  }
  
  // Step 5: System default
  return getSystemDefault(statePath);
}
```

**Lookup Performance:**

- **Exact match**: O(1) hash lookup, <1ms
- **Pattern match**: O(n) where n = number of wildcard patterns, typically <5ms
- **Fallback chain**: O(1) per fallback level, <2ms total
- **Cache hit rate**: >95% for typical campaigns with good asset coverage

**Dynamic State Paths with IDs**

For state paths containing dynamic identifiers (e.g., location IDs, NPC IDs, item IDs), campaign authors use wildcard notation:

**Example: World Map Location Markers**

```
# Campaign structure
ui/state/
├── world.locations.*.marker.webp           # Wildcard: matches any location ID
├── world.locations.capital_city.marker.webp # Specific override for capital
└── world.locations.dungeon.marker.webp      # Specific override for dungeon
```

**Session State:**
```json
{
  "world": {
    "locations": {
      "capital_city": {"name": "Camelot", "discovered": true},
      "forest_grove": {"name": "Darkwood", "discovered": true},
      "dungeon": {"name": "Crypt of Sorrows", "discovered": false}
    }
  }
}
```

**Resolution Examples:**

| State Path | Lookup Result | Explanation |
|------------|---------------|-------------|
| `world.locations.capital_city.marker` | `ui/state/world.locations.capital_city.marker.webp` | Exact match wins |
| `world.locations.forest_grove.marker` | `ui/state/world.locations.*.marker.webp` | Wildcard match |
| `world.locations.dungeon.marker` | `ui/state/world.locations.dungeon.marker.webp` | Exact match (even if undiscovered) |

**Wildcard Syntax:**

- `*` matches any single path segment (alphanumeric + underscore)
- `**` matches any number of path segments (for deep hierarchies)
- Exact matches always take precedence over wildcards
- Multiple wildcards are matched in order of specificity (more specific patterns first)

**World Map with Fog of War Example:**

```dart
// UI rendering logic for world map
class WorldMapWidget extends StatelessWidget {
  Widget build(BuildContext context) {
    final locations = sessionState['world']['locations'];
    
    return Stack(
      children: [
        // Base map image
        Image.asset('ui/maps/world_map.webp'),
        
        // Location markers (only discovered locations)
        ...locations.entries
          .where((entry) => entry.value['discovered'] == true)
          .map((entry) {
            final locationId = entry.key;
            final statePath = 'world.locations.$locationId.marker';
            
            // Deterministic asset lookup
            final markerAsset = assetIndex.resolve(statePath);
            
            // Position from state or campaign data
            final position = entry.value['map_position'] ?? 
                             campaignData.getLocationPosition(locationId);
            
            return Positioned(
              left: position.x,
              top: position.y,
              child: GestureDetector(
                onTap: () => showLocationDetails(locationId),
                child: Image(image: markerAsset.image),
              ),
            );
          }).toList(),
      ],
    );
  }
}
```

**Key Behaviors:**

1. **Undiscovered locations**: No marker rendered (filtered by `discovered: true`)
2. **Discovered locations**: Marker asset resolved via `assetIndex.resolve()`
3. **Asset not found**: Falls back to `categoryFallbacks['world.locations']` or system default
4. **Position data**: Derived from state (`map_position`) or campaign data
5. **Interaction**: Tapping marker opens location details view

**Asset Metadata Extensions for Maps:**

```json
{
  "path": "ui/state/world.locations.capital_city.marker.webp",
  "type": "state_icon",
  "state_binding": {
    "path": "world.locations.capital_city.marker",
    "display_mode": "icon_only",
    "anchor_point": "center_bottom",  // Where to position relative to coordinates
    "scale_with_zoom": true,
    "tooltip": "Camelot - Capital City"
  },
  "dimensions": {"width": 64, "height": 64}
}
```

**Performance Optimization:**

- **Asset preloading**: All discovered location markers pre-loaded on map view open
- **Lazy loading**: Undiscovered location markers not loaded until revealed
- **Texture atlas**: Small icons packed into sprite sheets for GPU efficiency
- **Cache warm-up**: Common state paths (hp, inventory, time) pre-resolved on app start

##### Index Persistence and Memory Strategy

**Memory Footprint Analysis:**

The in-memory asset index is deliberately chosen for performance despite minimum hardware constraints (8GB RAM on target devices). Footprint calculations:

| Campaign Size | Asset Count | Index Size | Percentage of Available RAM |
|---------------|-------------|------------|---------------------------|
| Small | 50 assets | ~12.5 KB | 0.00023% of 5.5GB headroom |
| Medium | 500 assets | ~125 KB | 0.0023% |
| Large | 2,000 assets | ~500 KB | 0.009% |
| Very Large | 10,000 assets | ~2.5 MB | 0.045% |

**Per-asset overhead**: ~250 bytes (state path string ~50 bytes, metadata reference ~100 bytes, hash map overhead ~100 bytes)

**Total memory budget context:**

```
Target Device: 8GB RAM
├── Phi-3.5 Mini model: 2.5GB (31%)
├── sentence-transformers: 60MB (0.75%)
├── ObjectBox + Flutter + OS: ~500MB (6%)
├── Available headroom: ~5GB (62%)
└── Asset index (10K assets): 2.5MB (0.03% of total)
```

The asset index is **negligible** compared to model sizes and UI framework overhead. Even very large campaigns stay well under 5MB for index data.

**Persistence Strategy:**

The system uses a **three-tier caching strategy** balancing speed, memory, and disk usage:

**Tier 1: In-Memory (Active Session)**
- Primary lookup location during gameplay
- Built on campaign load; retained for session lifetime
- Zero disk I/O during lookups
- Invalidated on campaign reload or app restart

**Tier 2: ObjectBox Cache (Cross-Session Persistence)**
- Index serialized to ObjectBox during campaign ingestion
- Includes campaign version checksum for invalidation
- Rebuilt only when campaign files change (detected via SHA-256 hash)
- Load time: <50ms for large campaigns

**Tier 3: Campaign Directory (No Cache)**
- Fallback if ObjectBox cache is missing or invalidated
- Full directory scan + metadata parsing
- Build time: <100ms for 1000 assets, <500ms for 10,000 assets

**Indexing Lifecycle:**

```
Campaign Load
    ↓
Check ObjectBox for cached index
    ↓
    ├─ Cache Hit (version matches)
    │    ↓
    │  Deserialize to in-memory (50ms)
    │    ↓
    │  Ready for lookups
    │
    └─ Cache Miss or Stale
         ↓
       Scan ui/state/ and ui/items/ directories
         ↓
       Build index (100-500ms depending on size)
         ↓
       Serialize to ObjectBox with version hash
         ↓
       Ready for lookups
```

**ObjectBox Schema for Cached Index:**

```dart
@Entity()
class CampaignAssetIndexCache {
  @Id()
  int id = 0;
  
  String campaignId;           // Unique campaign identifier
  String campaignVersion;      // Semver from manifest.json
  String contentHash;          // SHA-256 of ui/ directory tree
  DateTime indexedAt;          // When index was built
  
  // Serialized index data (JSON or MessagePack)
  @Property(type: PropertyType.byteVector)
  List<int> indexData;
  
  int assetCount;              // For metadata/debugging
  int sizeBytes;               // Serialized size
}
```

**Cache Invalidation Rules:**

1. **Campaign version changes**: `manifest.json` version field updated → rebuild
2. **Asset files modified**: SHA-256 hash of `ui/` directory differs → rebuild
3. **Schema version upgrade**: App updates index schema version → rebuild all
4. **Manual clear**: User clears app cache → rebuild on next launch
5. **Corruption detected**: Deserialization fails → rebuild + log error

**Very Large Campaign Handling (10,000+ assets):**

For campaigns exceeding 5,000 assets, additional optimizations activate:

1. **Incremental Indexing**: Index built in chunks; UI remains responsive
2. **Demand-based Loading**: Wildcard patterns indexed lazily on first match
3. **Bloom Filter Pre-check**: Fast negative lookups before hash map queries
4. **Compressed Storage**: Index data compressed with zstd in ObjectBox (2-3x reduction)

**Memory vs. Disk Trade-off Decision:**

| Strategy | Lookup Speed | Memory Usage | Disk I/O | Cache Warmup |
|----------|--------------|--------------|----------|--------------|
| In-memory only | <1ms | 2.5MB (10K) | 0 during play | 100-500ms |
| ObjectBox only | 5-10ms | ~500KB | Every lookup | 0ms |
| Hybrid (chosen) | <1ms | 2.5MB | 0 during play | 50ms (cached) |

**Why Hybrid Wins:**

- **Speed**: In-memory lookups are 10-20x faster than disk queries
- **Negligible RAM**: Even 10K assets use <0.05% of available memory
- **Fast Startup**: ObjectBox cache reduces cold-start from 500ms → 50ms
- **Offline Friendly**: No external dependencies; fully local

**Alternative Rejected:**

Writing a `cache/` directory to the campaign folder was considered but rejected:

**Cons:**
1. **Campaign directory pollution**: Campaigns ship as clean content; cache files clutter authoring
2. **Permissions**: Read-only campaign installs (iOS app bundles) can't write cache
3. **Versioning conflicts**: Git tracking of campaigns includes generated cache files
4. **Multi-user**: Shared campaigns need per-user cache; filesystem location unclear

**Pros:**
- Simple implementation
- Fast cache loading (direct file I/O)

**Decision**: ObjectBox-based caching provides the speed benefits without filesystem complexity or campaign directory pollution.

**Monitoring and Telemetry:**

The system logs index performance metrics:

```dart
// Logged on campaign load
{
  "event": "asset_index_built",
  "campaign_id": "chronicles_of_merlin",
  "asset_count": 1247,
  "index_build_time_ms": 142,
  "cache_hit": true,
  "index_size_bytes": 311750,
  "memory_allocated_mb": 0.297
}
```

These metrics enable authors to optimize asset organization and detect performance regressions.

**Error Handling:**

- **Asset not found**: Log warning, use fallback, continue rendering
- **Asset load failure**: Display placeholder, retry once, fall back to text label
- **Invalid state path**: Log error, return null asset, render without icon
- **Malformed metadata**: Use file directly, ignore metadata, log warning

##### Example: Complete State UI Customization

**Minimal Campaign** (no custom icons):
- UI uses generic themed icons (heart for hp, clock for time, bag for inventory)
- Inventory items show as text labels
- Functional but not visually distinctive

**Fully Customized Campaign**:
- Custom ominous grandfather clock icon for `world.time`
- Blood-red heart with crack pattern for `player.hp` (horror theme)
- Ornate leather satchel for `player.inventory`
- Individual icons for 50+ items (weapons, potions, quest items)
- Distinctive visual identity reinforcing campaign tone

**Impact**: Authors control the visual language of their story without modifying code. The same UI framework adapts to whimsical pixel art, photorealistic renders, or minimalist line drawings based purely on campaign assets.

#### 6.2.13 Provenance Validation

Provenance rules are enforced mechanically at the campaign ingestion layer / ObjectBox persistence adapter. The adapter must reject assets that violate provenance requirements and must not write them to ObjectBox:

- `generated: true` assets **require** a `provenance` object with `source_model`, `generated_at`, and `seed_data`. If any field is missing, the adapter rejects the store and returns error: `"Generated asset missing provenance (generated=true requires provenance.source_model, provenance.generated_at, provenance.seed_data)"`.
- `generated: false` assets **must not** have a `provenance` object. If present, the adapter rejects the store and returns error: `"Human-created asset must not contain provenance (generated=false conflicts with provenance object)"`.
- `generated_at` must be a valid ISO 8601 datetime. Non-conforming timestamps are rejected.
- On campaign load, a warning is displayed: "Campaign contains [N] AI-generated asset(s). Review generated flags and provenance metadata to verify correctness."

#### 6.2.14 Ingestion Enrichment Pipeline

When a campaign contains sparse data (fewer than 3 content files), the system invokes an on-device LLM to enrich it:

1. System detects sparse data and triggers enrichment
2. An on-device LLM (candidate models: Ollama Gemma 2B, Llama 3.2 3B, Qwen 2.5 3B) generates world setting, NPCs, plot beats, and lore entries
3. Generated files have `_generated` suffix and `generated: true` metadata
4. Provenance metadata records source model, timestamp, and seed data
5. Human-authored assets are *never* overwritten or regenerated
6. Enrichment completes within 15 seconds on target hardware

**Minimal Campaign Example:**

A campaign with only 3 files (`manifest.json`, `world/setting.md`, `plot/premise.md`) triggers enrichment that generates NPCs, plot beats, lore entries, and world rules—all marked with provenance metadata. Time to play: ~10 seconds.

**Complete Campaign Example:**

A campaign with 20+ content files (defined NPCs with profiles, plot beats with conditions, lore, artwork with keyword sidecars, music) requires no enrichment. The AI executes faithfully. Time to play: ~5 seconds.

#### 6.2.15 Campaign Validation

- Campaign structure is validated on load; errors are reported clearly with file paths and line numbers
- JSON files are validated against their schemas
- Orphaned asset references (files referenced but missing) generate warnings
- Validation completes within 2 seconds

### 6.3 Session State Management

Session state is maintained as an in-memory JSON structure updated by `state_patch` events through deep merge semantics. State supports dot-notation path access (e.g., `"inventory.torch.lit"`) for convenient querying. A copy mechanism enables snapshot-based undo or branching.

The state model has three tiers:

1. **Session State (transient)**: In-memory JSON updated by `state_patch` events. Lost when the application exits.
2. **Skill Private Data**: Per-skill `data/` directories for caches and working files. Persists across restarts.
3. **Shared Persistence**: ObjectBox-based storage for cross-skill data (memories, reputation, perception, portraits). ACID-compliant, persists across restarts.

---

## 7. Runtime Execution

### 7.1 Scene Pipeline

The narrative engine executes campaigns by managing the scene loop and orchestrating skills. The pipeline runs continuously during gameplay:

**Choice → Plan Generation → Plan Execution → Aggregate Results → Display Prose and New Choices**

1. **Player selects a choice** from the 3-4 displayed options
2. **Plan Generator analyzes context**: player choice, session state, available skills, campaign constraints (world constraints, NPC profiles, plot beats), and retrieved memories
3. **Plan JSON is generated**: Phi-3.5 Mini decides which skills to invoke, what parameters to pass, and what data to retrieve
4. **Plan Executor runs**: Skill scripts execute in dependency order; events are collected
5. **Results aggregate**: State patches merge into session state; assets register; UI events dispatch
6. **Scene renders**: Narrative prose (2-3 paragraphs) displays with character portraits, ambient music, and 3-4 new contextual choices
7. **Memory stores**: After each choice, the Memory skill is invoked to store a scene summary with: choice made, outcome, characters involved, location, and significance

### 7.2 Scene Types

The Plan Generator determines scene type based on narrative context and generates appropriate skill invocations:

- **Travel scenes**: Environmental description, random encounters, lore retrieval
- **Dialogue scenes**: NPC interaction, perception queries, relationship-aware responses
- **Danger scenes**: Dice rolls, skill checks, consequence determination
- **Resolution scenes**: Plot beat triggers, story progression, ending approaches

### 7.3 Choice Generation

Player interaction is exclusively choice-based—players select from AI-generated options only. Free-text input is not supported. This ensures all player actions are contextually valid and narratively coherent.

Choice requirements:
- **Contextually grounded**: Reference relevant memories and past events
- **Character-appropriate**: Match the player's established personality and abilities
- **Narratively interesting**: Nudge toward compelling outcomes
- **Mechanically valid**: Respect game rules and world constraints
- Generated within 3 seconds for typical scenarios

Campaign-defined stat schemas influence choice generation: dating sims use relationships/reputation, combat-heavy campaigns use armor/strength, etc.

### 7.4 Rules System

**Default Rules**: 2d6 + modifiers.
- 2-6 = failure
- 7-9 = partial success
- 10-12 = success

**Modifiers:**
- +1 for relevant skill/trait
- ±1 for NPC sentiment
- +1 for episodic memory callback (triumph/failure reference)

Custom rules defined in `world/rules.md` override the default system.

### 7.5 Plot Beat Integration

The Narrative Engine checks plot beat conditions during each scene transition:

- Beat conditions include: scene count windows, prerequisite beats, player choices, state conditions, NPC state, and world state
- Satisfied beats trigger within 2 scenes of conditions being met
- Unreachable beats are gracefully skipped
- When player choices trend toward a defined ending, the AI subtly steers toward conclusion
- Multiple triggerable beats prioritize by priority level (critical > high > medium > low > optional)

### 7.6 Sentiment and Memory

**NPC Sentiment**: Numeric scores (-1.0 to +1.0) track NPC attitudes toward the player. Actions adjust sentiment (±0.1 to ±0.5 based on significance). Positive NPCs respond warmly and offer help; negative NPCs refuse cooperation or demand compensation.

**Episodic Memory**: Major story events (triumphs and failures) are permanently stored with full context. These are always retrieved when relevant and grant +1 mechanical modifiers in related situations.

**Context Compression**: For very long sessions (1000+ choices), older scene summaries are compressed to preserve key information within manageable context sizes.

---

## 8. Implementation

### 8.1 Technology Stack

| Component | Technology | Purpose |
|-----------|-----------|---------|
| Language | Dart 3.x | Cross-platform client logic |
| Framework | Flutter SDK (latest stable) | UI and platform integration |
| Design System | Material Design 3 | Consistent, accessible UI |
| State Management | Provider (ChangeNotifier) | Reactive UI updates |
| Database | ObjectBox (in-process) | Persistent storage with vector search |
| Narrator AI | Phi-3.5 Mini (GGUF) | Plan generation and narration |
| Embeddings | sentence-transformers/all-MiniLM-L6-v2 | Semantic search |
| Testing | flutter_test, integration_test | Unit and integration testing |

### 8.2 Project Structure

```
src/
├── lib/
│   ├── models/                     # Dart class implementations
│   │   ├── plan_json.dart          # PlanJson, ToolInvocation, RetryPolicy, PlanMetadata
│   │   ├── protocol_events.dart    # Sealed class hierarchy for protocol events
│   │   ├── session_state.dart      # SessionState with DeepMerge
│   │   └── tool_execution_status.dart  # ToolResult, ExecutionResult, ToolError
│   ├── services/                   # Business logic
│   │   ├── tool_invoker.dart       # Process launch and NDJSON parsing
│   │   ├── plan_executor.dart      # Topological sort, retry, parallel execution
│   │   ├── narrator_ai.dart        # NarratorAI interface and stub
│   │   ├── skill_discovery.dart    # Manifest parsing and validation
│   │   └── skill_config.dart       # Configuration loading and UI generation
│   └── ui/                         # Flutter widgets
│       ├── screens/
│       │   ├── main_screen.dart
│       │   ├── storytelling_screen.dart
│       │   └── skills_settings_screen.dart
│       └── widgets/
│           ├── tool_execution_panel.dart
│           ├── asset_gallery.dart
│           ├── narrative_state_panel.dart
│           ├── player_choice_interface.dart
│           └── story_view.dart
└── skills/                         # Skill implementations
    ├── storyteller/
    │   ├── skill.json
    │   ├── config-schema.json
    │   ├── prompt.md
    │   └── narrate.dart
    ├── dice-roller/
    │   ├── skill.json
    │   └── roll-dice.dart
    ├── memory/
    │   ├── skill.json
    │   ├── store-memory.dart
    │   └── recall-memory.dart
    └── reputation/
        ├── skill.json
        ├── update-reputation.dart
        └── query-reputation.dart
```

### 8.3 Flutter UI

#### 8.3.1 Theming

Dark-themed Material Design 3 for immersive storytelling:

```dart
ThemeData(
  useMaterial3: true,
  colorScheme: ColorScheme.fromSeed(
    seedColor: Colors.deepPurple,
    brightness: Brightness.dark,
  ),
  typography: Typography.material2021(),
)
```

#### 8.3.2 Layout

```
┌─────────────────────────────────────────────┐
│  NavigationRail   │   Main Content Area     │
│                   │                         │
│  • Narrative      │  ┌──────────────────┐  │
│  • Tools          │  │  Story View      │  │
│  • Assets         │  │  (narrative text │  │
│  • State          │  │   + assets)      │  │
│                   │  └──────────────────┘  │
│                   │                         │
│                   │  ┌──────────────────┐  │
│                   │  │  Player Choices  │  │
│                   │  │  [choice 1]      │  │
│                   │  │  [choice 2]      │  │
│                   │  │  [choice 3]      │  │
│                   │  └──────────────────┘  │
└─────────────────────────────────────────────┘
```

#### 8.3.3 UI Components

**Story View**: Narrative text with rendered assets (character portraits, scene backgrounds). Prose displays as rich formatted text; assets display inline.

**Tool Execution Panel**: Real-time progress for active tool invocations—tool name, status, streaming log output, progress indicators, error display, and completion status.

**Asset Gallery**: Images with preview and metadata, audio player controls, video player controls, and placeholder cards for unsupported media types.

**Narrative State Panel**: Expandable tree view of session state, highlighted state changes from `state_patch` events, and a JSON inspector for debugging.

**Player Choice Interface**: 3-4 choice buttons arranged vertically, descriptive text per choice, active/hover selection feedback, disabled state during scene generation. Players select from presented choices only.

#### 8.3.4 Error Recovery UI

| Error Source | Display Location | User Action |
|--------------|------------------|-------------|
| Tool failure (`done.ok=false`) | Tool Panel + inline Story View notice | Retry or continue |
| Plan generation failure | Story View with fallback narrative | Select different choice |
| Network/API error | Toast notification + Tool Panel detail | Check connection, retry |
| Protocol violation | Developer console only | None (internal) |
| Max replan exceeded | Modal dialog with options | Restart session or continue with fallback |

**Inline Error Notice:**
```
┌─────────────────────────────────────┐
│ ⚠ The torch-lighter skill failed.  │
│ The story continues without it...   │
│ [Show Details] [Retry] [Dismiss]    │
└─────────────────────────────────────┘
```

**Max Replan Modal:**
```
┌─────────────────────────────────────┐
│  ⚠ Story Generation Issue           │
│  The narrator couldn't complete     │
│  your request after 5 attempts.     │
│  [Continue with Fallback]           │
│  [Restart Session]                  │
│  [View Error Details]               │
└─────────────────────────────────────┘
```

Error recovery requirements: user-friendly messages (no raw stack traces), actionable recovery options, preserved narrative continuity via fallback narration, detailed logging for debugging, animated transitions between error and recovery states.

### 8.4 Dart Class Model

The following core Dart classes implement the architecture:

**Skill Model:**
- `Skill` — Capability bundle: name, version, description, scripts, error state, enabled flag. Factory `Skill.fromManifest()` for loading from `skill.json`. Check `isAvailable` (enabled AND not permanently failed).
- `SkillScript` — Executable within a skill: name, path, timeout (default 30s), required flag.
- `SkillErrorState` — Enum: `healthy`, `degraded`, `temporaryFailure`, `permanentFailure`. Extension method `canSelect` returns false only for `permanentFailure`.
- `ConfigField` — Configuration field with type, title, description, default, enum values, min/max, sensitive flag, env var substitution, UI category.

**Plan Model:**
- `PlanJson` — Plan document: requestId, narrative, tools list, parallel flag, disabled skills set, metadata. Factory `PlanJson.fromJson()` and `toJson()`.
- `ToolInvocation` — Invocation descriptor: toolId, toolPath, input, dependencies, required, async, retryPolicy. Factory from JSON.
- `RetryPolicy` — Retry configuration with `delayForAttempt(n)` computing $\text{backoffMs} \times 2^{(n-1)}$.
- `PlanMetadata` — Generation tracking: attempt number, parent plan ID.

**Execution Model:**
- `PlanExecutionContext` — Dependency resolution: builds adjacency graph, performs cycle detection via DFS, computes topological order via Kahn's algorithm, provides `getReadyTools(completed)` and `getDependents(toolId)`.
- `ToolResult` — Per-tool result: toolId, state (pending/running/success/failed/skipped/timeout), output, events, execution time, retry count, error.
- `ExecutionResult` — Plan-level result: planId, success, canReplan, failed tools, disabled skills, tool results, aggregated state, aggregated assets, execution time, attempt number.
- `ToolError` — Error details: code, message, category (toolFailure/circularDependency/timeout/invalidJson/processError), details.

**Protocol Events (Sealed Class Hierarchy):**
- `ProtocolEvent` — Base sealed class. Factory `ProtocolEvent.fromJson()` dispatches on `type` field.
  - `LogEvent` — level (debug/info/warn/error), message, fields
  - `StatePatchEvent` — patch (JSON object)
  - `AssetEvent` — assetId, kind, mediaType, path, metadata
  - `UiEventEvent` — event name, payload
  - `ErrorEvent` — errorCode, errorMessage, details
  - `DoneEvent` — ok (bool), summary

**Session State:**
- `SessionState` — Container with deep merge. `applyPatch()` updates state. `get<T>(path)` supports dot-notation access (e.g., `"inventory.torch.lit"`). Copy support for snapshots.

**Asset Reference:**
- `AssetReference` — Generated asset: assetId, kind, mediaType, path, toolId, metadata. Factory `fromEvent()` converts `AssetEvent`.

**Exception Hierarchy:**
- `NarratoriaException` — Base exception
  - `CyclicDependencyException` — Circular dependency in plan
  - `PlanExecutionException` — Plan execution failure with failed tool list
  - `ToolTimeoutException` — Tool exceeded timeout
  - `ProtocolViolationException` — Invalid JSON, unknown event type
  - `UnknownEventTypeException` — Unrecognized event type string
  - `SkillScriptNotFoundError` — Script missing from skill
  - `MaxReplanAttemptsException` — 5 replan attempts exhausted

---

## 9. Design Rationale

### 9.1 Why Protocol-First?

**Decision**: All tool communication via NDJSON over stdin/stdout pipes.

**Alternatives Considered**: Shared memory, gRPC, REST APIs, FFI bindings.

**Rationale**: NDJSON over pipes is the simplest possible IPC mechanism. It requires no dependencies, works on every platform, is trivially testable (pipe a file to stdin, validate stdout), and enables language independence. A skill written in Python, Rust, or Go conforms to the same protocol as one written in Dart.

The trade-off is performance—process launch overhead is higher than in-process calls. But skill scripts typically run for hundreds of milliseconds to seconds (LLM inference, image generation, database queries), so process launch latency (<50ms) is negligible relative to execution time.

### 9.2 Why Bounded Retry?

**Decision**: Maximum 5 plan attempts, 3 retries per tool, exponential backoff.

**Alternatives Considered**: Unbounded retry, no retry (fail fast), manual retry.

**Rationale**: LLM-based systems fail in ways that traditional retry logic doesn't handle: the model might consistently generate invalid JSON, repeatedly select broken skills, or enter degenerate plan loops. Unbounded retry risks infinite loops that freeze the application. Fail-fast frustrates players who would benefit from a single retry. The bounded approach combines automatic recovery (most failures are transient) with guaranteed termination (the system always eventually responds).

The key innovation is **skill disabling during replanning**: when a skill fails, it's removed from the available skill set for subsequent plans, forcing the Narrator AI to find alternative approaches.

### 9.3 Why On-Device AI?

**Decision**: Phi-3.5 Mini (3.8B) + sentence-transformers, both running locally.

**Alternatives Considered**: Cloud APIs (OpenAI, Claude), hybrid cloud/local, larger local models.

**Rationale**: Privacy is the primary driver—interactive fiction sessions contain intimate creative expression that players may not want transmitted to third parties. Secondary drivers include offline capability (games should work on airplanes), predictable cost (no per-token API fees), and latency (local inference eliminates network round-trips).

The 3.8B parameter model is the sweet spot for mobile devices with 8GB RAM: large enough for coherent multi-paragraph prose and structured JSON generation, small enough to run in-process with acceptable latency (<3 seconds per scene).

### 9.4 Why No Free-Text Input?

**Decision**: Players select from AI-generated choices only.

**Alternatives Considered**: Free-text input (like AI Dungeon), hybrid (choices + free text).

**Rationale**: Free-text input creates the "parser doesn't understand" problem—players type actions the AI can't meaningfully handle, breaking immersion. Choice-based input ensures every player action is contextually valid and narratively coherent. The AI generates options that respect character abilities, world constraints, and narrative momentum.

This also enables measurable quality: we can verify that 80% of choices reference past events (SC-002), which is impossible to measure with arbitrary free-text input.

### 9.5 Why ObjectBox?

**Decision**: ObjectBox as the persistence backend.

**Alternatives Considered**: SQLite (sqflite), raw JSON files, Hive, Isar.

**Rationale**: ObjectBox provides native vector search capability alongside traditional CRUD operations, which is essential for the semantic memory system. SQLite with extension modules could achieve similar results but with more complexity. ObjectBox's in-process architecture aligns with the "no network" constraint, and its Dart SDK provides ergonomic integration.

### 9.6 Why Campaign Format Creeds?

**Decision**: Mandatory provenance tracking and transparency metadata.

**Alternatives Considered**: No tracking (treat all content equally), opt-in tracking.

**Rationale**: As AI content generation becomes ubiquitous, distinguishing human-authored from AI-generated content becomes critical for creative integrity, attribution, and trust. By making provenance tracking mandatory and enforced at the data layer (store operation fails without valid provenance for generated content), the system prevents accidental distribution of unlabeled AI content.

This is especially important for shared campaigns: when one author shares a campaign with another, the recipient knows exactly which elements are human-crafted and which are AI-generated.

---

## 10. Open Design Decisions

### 10.1 Multiplayer Coordination

How should Narratoria handle multiple players in a shared narrative? Options include:
- Shared session state with turn-based choice selection
- Independent parallel narratives that occasionally intersect
- GM-player model where one player acts as narrator

Currently out of scope (single-player focus), but the protocol-boundary architecture is extensible to multiplayer: skill scripts don't know how many players exist.

### 10.2 Voice Narration

Should the system support text-to-speech for narration? The on-device constraint limits model options, but small TTS models (e.g., Piper, Coqui) are becoming viable. This would enhance accessibility and immersion.

### 10.3 Campaign Marketplace

How should campaigns be distributed? A community repository with rating, search, and verification could accelerate the content ecosystem. Provenance tracking already supports this (campaigns declare AI content transparently).

### 10.4 Model Upgrade Migration

When embedding models improve, how should old embeddings be handled? Current plan: old embeddings remain; new memories use the new model; query vectors use the current model. This may cause degraded relevance for old memories. A background re-embedding process could resolve this but adds complexity.

### 10.5 Cancellation and Interruption

The current architecture has no cancellation mechanism for in-flight skill scripts. If a player wants to undo a choice or the system needs to interrupt a long-running tool, there's no protocol-level support. Future options: SIGINT handling, stdin-based cancel messages, or timeout-only termination.

### 10.6 Tool Versioning and Capability Negotiation

Currently, tools declare a static `version: "0"` and there's no mechanism for tools to advertise their capabilities to the runtime. As skills evolve, version negotiation may become necessary to maintain backward compatibility.

### 10.7 Network Protocols for Remote Tools

All tools currently run as local OS processes. For computationally expensive operations (large model inference, high-resolution image generation), remote tool execution over network protocols could extend capability—at the cost of the privacy and offline guarantees.

### 10.8 Authentication and Sandboxing

Tools currently have full filesystem access. For third-party skills downloaded from untrusted sources, sandboxing (filesystem restrictions, network limitations, resource caps) would improve security. This requires platform-specific implementation.

---

## 11. Success Criteria & Metrics

### 11.1 Core System Metrics

| ID | Metric | Target | Test Method |
|----|--------|--------|-------------|
| SC-001 | Plan generation speed | Valid Plan JSON for 95% of inputs within 5 seconds | Automated: time plan generation for 100+ diverse inputs |
| SC-002 | Skill selection accuracy | Correctly selects relevant skills for 90% of actions | Acceptance tests with known-correct skill mappings |
| SC-003 | Skills settings usability | Configure any core skill in under 2 minutes | Timed user task completion |
| SC-004 | Skill discovery reliability | All valid skills loaded from `skills/` without errors | Startup validation with diverse skill sets |
| SC-005 | Script execution success | 99% of invocations complete within timeout | Automated: run 1000+ script invocations, measure success rate |
| SC-006 | Graceful degradation | App continues without crash on skill failure; helpful error message | Fault injection tests |

### 11.2 Skill Performance Metrics

| ID | Metric | Target | Test Method |
|----|--------|--------|-------------|
| SC-007 | Memory search speed | <500ms for databases with 1000+ events | Benchmark with synthetic data |
| SC-008 | Reputation query speed | <100ms per query | Benchmark with multiple factions |
| SC-009 | Storyteller fallback | Local LLM fallback within 10 seconds | Network failure simulation |
| SC-010 | Skill installation ease | Install by directory copy + restart, no code changes | Manual test with new skill |
| SC-011 | Config persistence | Config survives restarts; correctly loaded on next invocation | Automated: save, restart, validate |
| SC-012 | Plan failure handling | Logs error, marks step failed, continues remaining steps | Fault injection in plan execution |

### 11.3 Advanced Skill Metrics

| ID | Metric | Target | Test Method |
|----|--------|--------|-------------|
| SC-013 | Choice generation speed | 3-4 options within 3 seconds for 95% of decision points | Automated timing |
| SC-014 | Choice stat relevance | ≥70% of choices mention stat-relevant keywords | (1) Verify Phi-3.5 prompt template includes `{player_stats}` injection, (2) Automated keyword grep: 20+ choices with stat-variant inputs, verify ≥70% mention stat-relevant keywords, (3) Code review: confirm prompt structures stats for LLM. Pass if all three checks succeed. |
| SC-015 | Portrait generation speed | <15 seconds for 90% of requests (local generation) | Automated timing |
| SC-016 | Portrait cache accuracy | Cached portrait retrieved in 95% of character reappearances | Semantic matching validation |
| SC-017 | NPC perception init speed | <100ms, informed by faction reputation | Benchmark with new NPCs |
| SC-018 | Dice modifier accuracy | Correct perception-based modifiers in 100% of applicable rolls | Deterministic test cases |
| SC-019 | Cross-restart persistence | 100% data integrity across restarts | Save, kill, restart, validate |
| SC-020 | Session stability | 30-minute play session with all skills, no crashes | End-to-end integration test |
| SC-021 | Cross-factor choices | Choices respect both reputation AND perception when applicable | Scenario tests with both factors |
| SC-022 | Portrait cross-session | Images associated correctly across sessions | Multi-session portrait retrieval test |

### 11.4 Persistence Metrics

| ID | Metric | Target | Test Method |
|----|--------|--------|-------------|
| SC-023 | Memory accuracy | Store/retrieve with 99% accuracy for 1000+ events | Automated round-trip validation |
| SC-024 | Semantic search quality | Relevant results (human-verified) in <500ms | Benchmark + manual review |
| SC-025 | Context augmentation speed | <200ms for up to 5 data points | Benchmark |
| SC-026 | Zero data loss | 100% recovery rate across restarts | Kill-restart-validate cycle |
| SC-027 | Cross-session continuity | Session 1 action referenced in Session 2 by narrator | Acceptance test |
| SC-028 | NPC perception accuracy | Helped NPC scores >30 points higher than betrayed NPC | Scenario comparison |
| SC-029 | Reputation decay | 10% loss per configured time unit, slower for strong opinions | Time-stepped validation |
| SC-030 | Portrait reuse rate | 90% same-character reuse via semantic match | Multi-appearance portrait test |
| SC-031 | Concurrent query safety | 5+ simultaneous queries without blocking or deadlock | Stress test |
| SC-032 | Scope filtering | No out-of-scope results in filtered queries | Cross-playthrough isolation test |

### 11.5 Narrative Engine Metrics

| ID | Metric | Target | Test Method |
|----|--------|--------|-------------|
| SC-033 | Scene transition speed | <3 seconds on 8GB RAM device | Automated timing on target hardware |
| SC-034 | Memory-driven choices | 80%+ reference past events | Automated entity extraction from choice text, cross-referenced against stored memory events in ObjectBox via embedding similarity match. Pass if ≥80% of sampled choices (50+ choices across 3 campaigns) retrieve ≥1 matching memory event with similarity score ≥0.7. |
| SC-035 | Plot beat timing | Trigger within 2 scenes of conditions met, 95% of cases | Automated condition monitoring |
| SC-036 | NPC sentiment accuracy | 95% of interactions reflect correct sentiment | Dialogue sentiment analysis |
| SC-037 | Long-session coherence | Coherent narrative across 100+ consecutive choices | End-to-end session test |
| SC-038 | Episodic memory surfacing | Relevant episodic memories appear 100% of applicable situations | Targeted scenario tests |

> **Note on SC-003 (Removed)**: Previously stated as "Players report feeling the AI remembers their choices in 90% of post-session surveys." Recognized as an emergent property rather than a formal requirement—when SC-034 (memory-driven choices) is achieved, players naturally feel the system remembers because it demonstrably references past events. No player survey required.

### 11.6 Campaign Format Metrics

| ID | Metric | Target | Test Method |
|----|--------|--------|-------------|
| SC-039 | Minimal campaign load | Load and play within 5 seconds | Automated timing |
| SC-040 | Minimal campaign authoring | Playable with fewer than 5 files | Manual test |
| SC-041 | Validation speed | Errors identified within 2 seconds | Automated timing |
| SC-042 | Lore retrieval relevance | 90% of narrative queries return relevant context | Author-defined test queries |
| SC-043 | NPC dialogue consistency | 95% recognizably consistent with personality | Author review |
| SC-044 | Plot beat responsiveness | Trigger within 2 scenes of conditions met | Automated monitoring |
| SC-045 | Large campaign stability | 100MB campaign loads on 8GB RAM without issues | Resource monitoring |
| SC-046 | Enrichment speed | <15 seconds for 1-3 seed files | Automated timing |
| SC-047 | Generated asset marking | 100% marked with `generated: true` | Metadata audit |
| SC-048 | Human override preservation | 100% of human assets preserved post-generation | Before/after comparison |

---

## 12. Appendices

### A. Glossary

See Section 1.5 (Terminology) for the complete glossary of terms used throughout this document.

### B. Contracts Index

All machine-readable contracts are maintained as JSON Schema files:

| Contract | Location | Validates |
|----------|----------|-----------|
| Plan JSON Schema | `contracts/plan-json.schema.json` | Plan documents from Narrator AI |
| Execution Result Schema | `contracts/execution-result.schema.json` | Plan execution traces |
| Skill Manifest Schema | `contracts/skill-manifest.schema.json` | `skill.json` manifest files |
| Config Schema Meta-Schema | `contracts/config-schema-meta.schema.json` | Skill `config-schema.json` files |
| Campaign Manifest Schema | `contracts/manifest.schema.json` | Campaign `manifest.json` files |
| Asset Metadata Schema | `contracts/asset-metadata.schema.json` | Ingested asset metadata |
| NPC Profile Schema | `contracts/npc-profile.schema.json` | NPC `profile.json` files |
| Plot Beats Schema | `contracts/plot-beats.schema.json` | Plot `beats.json` files |
| Player Template Schema | `contracts/player-template.schema.json` | Player `template.json` files |

### C. Related Work

| System | Type | Strengths | Limitations vs. Narratoria |
|--------|------|-----------|---------------------------|
| AI Dungeon | Cloud AI | Arbitrary free-text input, GPT-powered | Privacy concerns, API dependency, no graceful degradation |
| NovelAI | Cloud AI | Fine-tuned narrative models | API dependency, limited extensibility |
| Twine | Hypertext | Rich authoring, no AI dependency | Combinatorial explosion, no emergence |
| Inform 7 | Parser IF | Deterministic, deep world models | No AI generation, limited natural language |
| Ink (Inkle) | Scripted | Excellent branching tools | Static branches, no AI, no memory |
| LangChain | AI Framework | Flexible agent orchestration | No narrative focus, no graceful degradation |
| AutoGen | Multi-agent | Multi-agent coordination | No narrative focus, cloud-dependent |
| Dwarf Fortress | Procedural | Deep emergent systems | No narrative prose, not character-driven |

### D. Architecture Dependency Graph

```
Layer 1: Tool Protocol
    ↑
Layer 2: Plan Execution ←──────── Layer 3: Skills Framework
    ↑                                  ↑
    ├──────────────────────────────────┤
    ↑                                  ↑
Layer 4: Narratoria Skills ──────► Layer 6: Persistence
    ↑                                  ↑
Layer 5: Dart Implementation ─────────┤
    ↑                                  ↑
Layer 7: Campaign Format ─────────────┤
    ↑                                  ↑
Layer 8: Narrative Engine ────────────┘
```

**Reading Order for Understanding:**
1. Start with Layer 1 (Tool Protocol) — foundational communication
2. Read Layers 2 and 3 together — plan execution and skills are co-dependent
3. Read Layers 4 and 6 together — skill interfaces and storage implementation are co-dependent
4. Read Layer 5 — reference implementation ties everything together
5. Read Layers 7 and 8 — campaign content and runtime execution

### E. Future Roadmap

**Phase 1 (MVP)**: Core protocol, plan execution, 4 core skills (storyteller, dice-roller, memory, reputation), basic Flutter UI, ObjectBox persistence, minimal campaign support.

**Phase 2 (Advanced Skills)**: Player Choices skill, Character Portraits skill, NPC Perception skill, skill integration (cross-skill plans), campaign enrichment pipeline.

**Phase 3 (Polish)**: Error recovery UI, performance optimization, large campaign support, comprehensive testing against success criteria.

**Phase 4 (Ecosystem)**: Campaign authoring tools, skill development SDK, community campaign sharing, documentation site.

**Beyond**: Multiplayer support, voice narration, remote tool execution, model upgrade migration, advanced sandboxing.

