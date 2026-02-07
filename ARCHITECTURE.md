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
- **License**: Open source (license TBD)
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
                    │                      │  - Character portraits
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

### 2.6 Analytics Logging

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

#### 6.2.7 Lore System

All files in `lore/` are indexed for semantic search (RAG retrieval):

- Files are chunked by paragraph (split on `\n\n`)
- Maximum 512 tokens per chunk
- If a single paragraph exceeds 512 tokens, it is split on sentence boundaries (`.`, `!`, `?`)
- Each chunk is stored with metadata: original file path, chunk index, paragraph ID, token count, chunk method ("paragraph")
- Token counts must be computed using the `tiktoken` library with the `cl100k_base` tokenizer (compatible with the sentence-transformers embedding model)
- Nested directories within `lore/` are supported for organization

#### 6.2.8 Creative Assets

**Image assets** (`art/`): Supported formats are PNG, JPEG, and WebP. Nested subdirectories are supported (e.g., `art/characters/`, `art/locations/`, `art/items/`).

**Audio assets** (`music/`): Supported formats are MP3, OGG, WAV, and FLAC. Nested subdirectories are supported (e.g., `music/ambient/`, `music/combat/`).

File naming conventions enable semantic linking: `art/characters/npc_wizard.png` is indexed alongside `characters/npcs/wizard/profile.json`.

#### 6.2.9 Asset Metadata Structure

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

#### 6.2.10 Keyword Sidecar Files

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

#### 6.2.11 Provenance Validation

Provenance rules are enforced mechanically at the campaign ingestion layer / ObjectBox persistence adapter. The adapter must reject assets that violate provenance requirements and must not write them to ObjectBox:

- `generated: true` assets **require** a `provenance` object with `source_model`, `generated_at`, and `seed_data`. If any field is missing, the adapter rejects the store and returns error: `"Generated asset missing provenance (generated=true requires provenance.source_model, provenance.generated_at, provenance.seed_data)"`.
- `generated: false` assets **must not** have a `provenance` object. If present, the adapter rejects the store and returns error: `"Human-created asset must not contain provenance (generated=false conflicts with provenance object)"`.
- `generated_at` must be a valid ISO 8601 datetime. Non-conforming timestamps are rejected.
- On campaign load, a warning is displayed: "Campaign contains [N] AI-generated asset(s). Review generated flags and provenance metadata to verify correctness."

#### 6.2.12 Ingestion Enrichment Pipeline

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

#### 6.2.13 Campaign Validation

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

