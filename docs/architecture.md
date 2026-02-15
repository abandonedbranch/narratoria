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

- **Phi-4 or Phi-4-mini (3.8B-14B parameters, 2.5GB-8GB GGUF quantized)**: Generates Plan JSON documents that orchestrate skill invocations and produces scene narration. Phi-4 provides significant quality improvements over Phi-3.5, with Phi-4-mini offering a smaller footprint for resource-constrained devices. Automatically downloads from HuggingFace Hub on first launch; cached locally for offline use.

- **sentence-transformers/all-MiniLM-L6-v2 (33MB, 384-dimensional embeddings)**: Generates semantic vectors for memory retrieval, lore search, and contextual matching. Enables "perplexingly on-point" choices that reference past events through vector similarity search.

Both models run on-device (compatible with iPhone 17+ and equivalent Android hardware), ensuring:
- **Privacy**: No player data leaves the device
- **Offline capability**: No network required after initial model download
- **Predictable cost**: No API fees, no usage limits
- **Low latency**: Local inference completes in <3 seconds per scene

#### 4. Semantic Memory for Narrative Continuity

Cross-session continuity is achieved through persistent embedded storage with vector search:
- **Scene summaries** stored after each player choice with semantic embeddings
- **Lore chunks** from campaign content (paragraph-based, 512-token max) indexed for retrieval
- **NPC perception** and **faction reputation** tracked persistently
- **Character portraits** cached to avoid redundant generation

The Plan Generator (Phi-4 or Phi-4-mini) decides *contextually* what data to retrieve based on narrative needs—there are no fixed "memory tiers" or rigid context budgets. The LLM analyzes the scene and generates plans that invoke memory retrieval skills with semantic queries.

### Key Technical Components

| Component | Technology | Purpose |
|-----------|-----------|---------|
| **Plan Generator** | Phi-4 or Phi-4-mini (3.8B-14B) | Converts player choices to executable Plan JSON |
| **Plan Executor** | Runtime engine | Executes skill scripts in dependency order with retry |
| **Semantic Memory** | Vector database + sentence-transformers | Cross-session narrative continuity via vector search |
| **Skills Framework** | Agent Skills Standard | Discover, configure, and execute modular capabilities |
| **Campaign Format** | Directory + campaign.yml | Story packages with semantic files, YAML metadata, assets |
| **UI Layer** | Cross-platform UI framework | Rich cross-platform storytelling interface |
| **Persistence** | Embedded database (in-process) | ACID storage for memories, reputation, state |

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
│  - campaign.yml, semantic .txt files, assets/       │
├─────────────────────────────────────────────────────┤
│ Layer 6: Skill State Persistence                   │  Data management
│  - Memory events, lore, reputation, portraits      │
├─────────────────────────────────────────────────────┤
│ Layer 5: UI Implementation                         │  User interface
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

1. **Cross-Platform First**: All client logic targets a single cross-platform codebase for consistency
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
                    │   Plan Generator     │  Phi-4/Phi-4-mini analyzes:
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
│  │ (Phi-4/Phi-4-mini)│──│ (Runtime engine)  │                    │
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
│  │          Persistence Layer (Embedded Database)       │        │
│  │  ┌──────────┐ ┌──────────┐ ┌────────┐ ┌─────────┐  │        │
│  │  │ Memories │ │   Lore   │ │  NPC   │ │Portraits│  │        │
│  │  │ (vectors)│ │ (chunks) │ │  Data  │ │ (cache) │  │        │
│  │  └──────────┘ └──────────┘ └────────┘ └─────────┘  │        │
│  └─────────────────────────────────────────────────────┘        │
│                                                                 │
│  ┌──────────────────────────────────────────────────────┐       │
│  │          UI Layer                                    │       │
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
| **Skill** | A capability bundle that the Narrator AI can invoke. Defined by a `SKILL.md` file (YAML frontmatter + markdown body) following the [Agent Skills Standard](https://agentskills.io/specification), with Narratoria extensions for scripts, configuration, and data storage. |
| **Skill Script** | An executable component within a skill that performs actions (e.g., `roll-dice`, `narrate`). Communicates via NDJSON over stdin/stdout. |
| **Plan JSON** | Structured document produced by the Narrator AI describing which skill scripts to invoke, their inputs, dependencies, and execution strategy. |
| **Skill Invocation** | An entry in the Plan JSON `tools` array that references a specific skill script to execute. The array is named `tools` for protocol compatibility. |
| **Session State** | Runtime data model containing narrative state accumulated from `state_patch` events (e.g., `{"inventory": {"torch": {"lit": true}}}`). |
| **Deep Merge** | State patch merge semantics where nested objects merge recursively, arrays replace entirely, and null values remove keys. |
| **Execution Trace** | Complete record of skill script execution including results, events, timing, and errors. |
| **Replan Loop** | Bounded retry system (max 5 plan generation attempts) that learns from failures and disables failed skills in subsequent plans. |
| **Story Session** | A single continuous play session within a narrative playthrough. |
| **Story Playthrough** | A complete or ongoing narrative arc spanning multiple sessions. |
| **Campaign** | A self-contained story package containing world-building, characters, plot structure, lore, and creative assets. |
| **Lore Chunk** | A paragraph-sized segment of campaign content (from semantic `.txt` files) stored with a semantic embedding vector for retrieval. |
| **Memory Event** | A recorded narrative occurrence with timestamp and semantic embedding (e.g., "player befriends blacksmith"). |
| **Persona Profile** | A realized player character generated by the LLM at campaign start from a fresh character description. Contains name, archetype, personality, background, goals, and speech patterns tailored to the campaign world. Ephemeral—chunked and embedded into ObjectBox, then discarded. |
| **Behavioral Prompt** | The markdown body of a skill's `SKILL.md` file, injected into the Narrator AI system context when the skill is activated; guides AI behavior for that skill. |
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

The UI layer uses a reactive state management pattern to subscribe to session state changes. The state container:

- Holds the current session state as a JSON object
- Applies incoming `state_patch` events via deep merge
- Notifies all subscribed UI components when state changes
- Triggers selective re-renders of affected widgets

UI components bind to specific state paths (e.g., `player.stats.health`) and rebuild automatically when those paths change.

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

Narratoria implements the [Agent Skills Standard](https://agentskills.io/specification) with Narratoria-specific extensions for AI orchestration. A skill is a directory containing at minimum a `SKILL.md` file:

- **`SKILL.md`** — Required. YAML frontmatter (metadata) + markdown body (behavioral guidance and documentation)
- **`scripts/`** — Optional. Executable programs that perform the skill's work
- **`references/`** — Optional. Additional documentation agents can read on demand (Narratoria stores configuration schemas here)
- **`assets/`** — Optional. Templates, images, and other static resources
- **`config.json`** — Optional, Narratoria extension. User-saved configuration values (written by the runtime)
- **`data/`** — Optional, Narratoria extension. Private persistent storage for the skill

#### SKILL.md Format

The `SKILL.md` file uses YAML frontmatter for metadata followed by a markdown body containing behavioral guidance and documentation. Standard fields go in the frontmatter root; Narratoria-specific extensions go under the `x-narratoria` key in `metadata`.

```markdown
---
name: storyteller
description: Rich narrative enhancement using local or hosted LLMs. Use when
  the narrator needs to generate evocative prose for scene descriptions,
  dialogue, or transitions.
license: MIT
metadata:
  author: Narratoria
  version: "1.0.0"
  x-narratoria:
    displayName: Storyteller
    capabilities:
      - narration
      - prose
      - scene-setting
    priority: 80
    retryPolicy:
      maxRetries: 3
      backoffMs: 100
---

# Storyteller Skill

This skill enhances narrative scenes with rich, evocative prose using a
configurable LLM provider (local Phi-4 or Phi-4-mini by default, or hosted APIs).

## Scripts

- **`scripts/narrate`** — Generate narrative prose from scene context. Required
  for the skill to function. Default timeout: 30 seconds.

## Configuration

See [references/config-schema.json](references/config-schema.json) for the
configuration schema. Settings include LLM provider selection, API keys, and
narrative style preferences.

## Behavioral Guidance

When generating plans that involve storytelling:
- Use vivid sensory details appropriate to the campaign tone
- Match narrative style to campaign setting (epic fantasy, noir, sci-fi, etc.)
- Reference recent memories when contextually relevant
- Produce 2-3 paragraphs of scene-setting narrative per invocation
- Avoid purple prose; maintain readability
```

**Standard Fields** (per [Agent Skills Standard](https://agentskills.io/specification)):

| Field | Required | Description |
|-------|----------|-------------|
| `name` | Yes | 1-64 chars, lowercase alphanumeric + hyphens, must match directory name |
| `description` | Yes | Max 1024 chars. What the skill does and when to use it |
| `license` | No | SPDX identifier or reference to bundled license file |
| `compatibility` | No | Environment requirements (max 500 chars) |
| `metadata` | No | Arbitrary key-value map for additional metadata |

**Narratoria Extensions** (under `metadata.x-narratoria`):

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `displayName` | string | — | Human-readable name for UI display |
| `capabilities` | string[] | [] | Semantic tags for plan generator skill selection |
| `priority` | integer | 50 | Selection weight when multiple skills match (0-100) |
| `retryPolicy` | object | — | Default retry behavior for all scripts in `scripts/` |

Scripts are not declared in the `SKILL.md` frontmatter. They are executable files in the `scripts/` directory, discovered by the runtime at startup. The `SKILL.md` markdown body should document available scripts, their purpose, and any required inputs. Script-specific metadata (timeouts, required flags) can be documented in `references/` as a Narratoria extension (e.g., `references/script-manifest.json`).

The markdown body after frontmatter serves dual purposes:
1. **Human documentation** — skill users and authors can read it directly
2. **Behavioral guidance** — injected into the Plan Generator's system context when the skill is activated

This follows the Agent Skills Standard's progressive disclosure model: the runtime loads only `name` and `description` at startup, then reads the full `SKILL.md` body when the skill is activated for a plan.

### 3.2 Skill Discovery

At startup, the runtime scans the `skills/` directory and discovers all valid skills:

1. Locate all `SKILL.md` files in `skills/*/SKILL.md`; parse YAML frontmatter
2. Validate required fields (`name`, `description`); verify `name` matches directory name
3. Scan `scripts/` directories within each skill to discover available script executables
4. Store `name` + `description` for lightweight skill selection during plan generation
5. Skip skills with invalid or missing `SKILL.md`; log warnings without crashing
6. Hot-reloading should be supported: when `SKILL.md` changes are detected, handle gracefully (auto-reload or notify user)

The full `SKILL.md` markdown body is read into the Plan Generator's context only when the skill is activated for a plan — not at startup. This keeps initial context usage low per the Agent Skills Standard's progressive disclosure model.

### 3.3 Skill Configuration

> **Note:** Configuration management (`references/config-schema.json`, `config.json`) is a Narratoria extension, not part of the Agent Skills Standard. The standard defines `SKILL.md` and optional `scripts/`, `references/`, and `assets/` directories; Narratoria uses the `references/` directory for configuration schemas and adds `config.json` and `data/` as extensions for skills that need user-editable settings (API keys, model selection, tuning parameters) and private storage.

The runtime provides a Skills Settings UI accessible from application settings:

- Display all discovered skills with name, description, and enabled/disabled toggle
- Dynamically generate configuration forms from `references/config-schema.json` files within each skill directory
- Supported input types: string (text, freeform), number, boolean (toggle), enum (dropdown)
- Sensitive fields (API keys, passwords) use password-style masking; the `x-sensitive` flag triggers this
- Environment variable substitution supported via `${VAR_NAME}` syntax in config values
- Validation against schema constraints (required fields, type checking, min/max) before saving
- Validation errors displayed inline in configuration forms with actionable error messages
- Configuration saved to skill-specific `config.json` files within the skill directory

**Configuration Schema Meta-Schema:**

Each skill's `config-schema.json` is a standard JSON Schema with Narratoria-specific extensions:

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

Rich narrative enhancement using the local LLM (Phi-4 or Phi-4-mini) or a configured hosted provider.

**Components:**
- Behavioral prompt for evocative narration
- `narrate` script that calls LLM (local or hosted) for detailed prose; must produce 2-3 paragraphs of scene-setting narrative

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
- `roll-dice` script: parses dice formulas (e.g., `1d20+5`, `3d6`, `2d6+modifier`)
- Emits `ui_event` with roll results for player display

**Configuration:**

| Field | Type | Description |
|-------|------|-------------|
| `showIndividualRolls` | boolean | Display each die result |
| `randomSource` | enum | `crypto` or `pseudo` random source |

#### 4.1.3 Memory

Semantic memory and continuity across sessions. This skill enables the "perplexingly on-point" narrative experience—the system's ability to reference past events, relationships, and player knowledge in contextually relevant ways.

**Components:**
- `store-memory` — Receives event summaries, generates sentence-transformers embeddings, stores via the persistence layer
- `recall-memory` — Receives semantic queries, performs vector similarity search, returns ranked results

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
- `update-reputation` — Records reputation changes by faction
- `query-reputation` — Returns current reputation values

**Configuration:**

| Field | Type | Description |
|-------|------|-------------|
| `factionList` | string[] | Faction names |
| `reputationScale` | object | Min/max values |
| `decayRate` | float | Reputation decay per in-game time unit |
| `storageBackend` | enum | `database` or `files` |

Reputation decay is time-based: faction reputation loses approximately 10% per configured time unit for neutral relationships, with slower decay for strong opinions (±50 points).

### 4.2 Advanced Skills

#### 4.2.1 Player Choices

Generates contextual multiple-choice options that reflect the player's character, history, and relationships.

**Components:**
- `generate-choices` — Analyzes context and produces 3-4 choices
- `evaluate-choice` — Determines outcome modifiers for selected choice

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
- `generate-portrait` — Creates character images from narrative descriptions
- `lookup-portrait` — Retrieves cached portraits by character identifier
- `update-portrait` — Regenerates portrait for existing character

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
- `update-perception` — Records perception changes
- `query-perception` — Returns current perception value and modifiers
- `initialize-perception` — Seeds perception for new NPCs

**Configuration:**

| Field | Type | Description |
|-------|------|-------------|
| `decayRate` | float | Perception decay per in-game time unit |
| `factionInfluenceWeight` | float | How much faction reputation affects initial perception (0-1) |
| `modifierScale` | float | Dice roll modifier per perception tier |
| `storageBackend` | enum | `database` or `files` |

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

The Narrator AI uses Phi-4 or Phi-4-mini (3.8B-14B parameters, 2.5GB-8GB GGUF quantized), running entirely in-process. Phi-4 provides significant improvements in reasoning and instruction-following over Phi-3.5, with Phi-4-mini offering a smaller footprint suitable for mobile devices. It converts player choices into structured Plan JSON documents by analyzing context and selecting appropriate skills.

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
- Never generate dialogue or decisions for the player character without player-initiated action. The Narrator AI narrates *to* the player, not *as* the player — it describes the world, presents choices, and narrates consequences, but the player character's words and decisions belong exclusively to the player. Player character chunks in context (marked `isPlayer: true`) are reference material for the AI, not license to act on the player's behalf.

**Model Loading:**
- Downloads automatically from HuggingFace Hub (`microsoft/Phi-4` or `microsoft/Phi-4-mini` GGUF variant) on first app launch
- Phi-4-mini recommended for mobile devices (iPhone, Android); full Phi-4 for desktop
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

The Plan Generator decides *contextually* what data to retrieve. There are no fixed "memory tier budgets" or rigid context window allocations. Instead, Phi-4 or Phi-4-mini analyzes the current scene and generates plans that invoke memory retrieval skills with semantic queries:

```json
{
  "toolId": "recall",
  "toolPath": "skills/memory/scripts/recall",
  "input": {"query": "past betrayals", "limit": 3}
}
```

The LLM may query lore, recent events, NPC relationships, faction reputation, or episodic memories based on scene needs. Retrieval is adaptive, driven by narrative context rather than predetermined budgets.

#### 5.3.1 Context Precedence Hierarchy

While retrieval is contextual rather than budget-driven, the data assembled into Phi-4's context window follows a strict precedence hierarchy. This hierarchy determines how conflicts between data sources are resolved and how the system prompt is ordered. Higher-priority data appears earlier in context and takes precedence when assertions conflict.

**Precedence Tiers:**

| Tier | Source | Weight | Description |
|------|--------|--------|-------------|
| 1 | Campaign frontmatter assertions | `absolute` | Hard constraints declared in YAML frontmatter. Immutable within the context of their source file. |
| 2 | Active session state | `immediate` | Current location, inventory, active effects, realized player character profile. Reflects the live narrative situation. |
| 3 | Campaign prose (same-file) | `authoritative` | Author-written narrative body text from the same file as Tier 1 frontmatter. Enriches constraints with flavor and detail. |
| 4 | Campaign prose (cross-file) | `contextual` | Author-written prose from other campaign files retrieved via semantic search. Provides background, lore, and world-building. |
| 5 | Persistent runtime data | `accumulated` | NPC perception scores, faction reputation, session-local memory events from the current playthrough. |
| 6 | Cross-session memory | `historical` | Episodic and semantic memory from prior sessions, retrieved by similarity search. Lowest priority but enables continuity. |

**Context Assembly Algorithm:**

1. **Query ObjectBox** with scene-relevant semantic searches (driven by Plan Generator)
2. **Tag each result** with its `sourceType` (`frontmatter` or `prose`) and `weightTier` (1-6)
3. **Order context fragments** by tier (lowest number first), then by semantic relevance within each tier
4. **Inject into Phi-4's system prompt** with tier boundaries clearly delineated
5. **Enforce the No-Shadow Rule**: frontmatter assertions from one file cannot be overridden or contradicted by prose from a different file (see below)

**The No-Shadow Rule:**

Frontmatter assertions are the highest-priority data *within the scope of their source file*. Critically, prose in other files cannot override or contradict frontmatter assertions from a different file. This prevents campaign authors from accidentally (or intentionally) creating conflicting authorities across files.

**Example — Correct Behavior:**

```yaml
# character_merlin.txt (frontmatter)
---
entity_type: character
entity_id: merlin
alignment: lawful_good
core_traits: [never_accepts_evil, protective_of_innocents]
---

Merlin is a figure of profound wisdom who would NEVER align with evil...
```

```yaml
# npc_merlin_morgana_relationship.txt (prose body)
---
entity_type: relationship
---

Morgana often tempts Merlin, suggesting his rigid morality blinds him
to deeper truths. Despite her provocations, Merlin secretly harbors
doubts about the nature of good and evil...
```

In this case, the relationship prose (Tier 4, `contextual`) enriches narration — Phi-4 may use it to generate scenes where Morgana challenges Merlin. But Merlin's `core_traits: [never_accepts_evil]` (Tier 1, `absolute`) remains inviolable. The AI must never generate a plan or narration where Merlin actually accepts or aligns with evil.

**Implications for Campaign Authors:**

- Use **frontmatter** for hard constraints: alignment, immutable character traits, forbidden actions, mechanical facts, world laws
- Use **prose body** for narrative flavor: personality details, relationship dynamics, atmospheric descriptions, situational context
- The system automatically ranks frontmatter higher — stylistic emphasis in prose (e.g., "NEVER EVER") does not amplify precedence weight beyond Tier 3/4
- To make a constraint truly binding, declare it in frontmatter rather than relying on prose emphasis
- Frontmatter keys are freeform (no enforced schema); authors choose their own constraint vocabulary

**Storage Metadata:**

Every chunk stored in ObjectBox carries precedence metadata (see Section 6.1.1 for schema details):

| Field | Type | Description |
|-------|------|-------------|
| `sourceType` | string | `frontmatter` or `prose` — origin within the source file |
| `weightTier` | integer (1-6) | Precedence tier per the hierarchy above |
| `fileOrigin` | string | Relative path to the source campaign file |

These fields enable the context assembly algorithm to correctly order and deduplicate data before injection into Phi-4's prompt.

**Realized Player Characters:**

When a fresh character is realized at campaign start (see Section 6.2.7), the LLM generates a structured character profile — name, archetype, personality, background, goals, speech patterns — tailored to the campaign world. This ephemeral JSON is chunked, embedded, and stored in ObjectBox as regular character chunks with `isPlayer: true`. The intermediate JSON is then discarded.

Realized player characters are stored identically to campaign NPCs — same chunking, same embedding, same semantic search retrieval — differentiated only by the `isPlayer` flag. This means:

- **Tier 2 (`immediate`)**: Player character chunks receive `weightTier: 2` because they represent the active player identity in the current campaign session
- **Same campaign, same lifetime**: Player character chunks persist with the playthrough data, not across campaigns. The same fresh character realizes differently in each campaign (wizard in fantasy, detective in noir)
- **Player agency constraint**: When `isPlayer: true` chunks appear in Phi-4's context, the Narrator AI must never generate dialogue or make decisions for that character without player-initiated action (see Section 5.1). The AI narrates *to* the player, not *as* the player

### 5.4 Testing Stub

For development and testing, a pattern-based plan generator provides deterministic plan generation without loading the full model. The Narrator AI interface requires:

- **Inputs**: player input text, set of disabled skills, list of available skills, optional parent plan ID, generation attempt number
- **Output**: a Plan JSON document

The testing stub uses hard-coded RegExp pattern → plan mappings (e.g., `roll.*dice?` → dice roll plan, `recall|remember` → memory plan) and returns a fallback plan with narrative-only response for unrecognized patterns. It supports at least 5 patterns for integration testing and respects `disabledSkills` and replan metadata.

---

## 6. Data Architecture

### 6.1 Persistence Layer

The persistence layer is shared infrastructure providing a unified storage and retrieval interface for narrative data. It stores data and answers queries, but does not decide when or why data is retrieved—that's the responsibility of the Plan Generator and individual skills.

**Primary Responsibilities:**
1. Store narrative data: memory events, lore chunks, faction reputation, NPC perception, character portraits
2. Semantic search: vector similarity search for context-relevant retrieval
3. Query interface: fast, filtered access to stored data (<200ms latency)
4. Persistence: data survives application restarts and session boundaries

**Architectural Note:** Like the Narrator AI, the persistence layer runs within the runtime as shared infrastructure. This differs from skill-private `data/` directories. The shared layer enables cross-skill data access through a query interface without violating the "no direct skill-to-skill calls" rule.

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
| sourceType | string | `frontmatter` or `prose` — origin within the source file |
| weightTier | integer (1-6) | Precedence tier per Section 5.3.1 hierarchy |
| fileOrigin | string | Relative path to source campaign file |
| isPlayer | boolean | `true` for realized player character chunks; enables player-agency constraint during context assembly |

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

A campaign is a self-contained collection of semantic text files with YAML frontmatter, describing characters, lore, plot, and world-building. Authors package campaigns as `.campaign.gz` files (compressed directories) that the runtime extracts and ingests at campaign load time.

**Core Principle**: *The more a campaign provides, the less the AI invents.* Authors organize content however they choose; the ingestion pipeline infers intent from frontmatter and content.

#### 6.2.1 Campaign Format Creeds

Every campaign embodies these principles (documented in `campaign.yml`):

1. **Respect Human Artistry**: Authored prose is canonical; AI fills gaps only when invited.
2. **Radical Transparency**: Any AI-generated content marked with provenance metadata.
3. **Human Override**: Authors can refine, delete, or replace any content.
4. **Attribution**: When sharing campaigns, all sources disclosed.
5. **Flexible Intent Expression**: Authors write frontmatter minimally or richly; ingestion infers missing details.

#### 6.2.2 Directory Structure & File Format

Campaigns use simple, author-friendly file organization:

```
chronicles_of_merlin/
├── campaign.yml                     # Campaign metadata
├── character_merlin.txt             # Character definition (frontmatter + prose)
├── character_arthur.txt
├── character_morgana.txt
├── character_guinevere.txt
├── npc_merlin_morgana_relationship.txt  # Relationship (optional)
├── lore_camelot_history.txt         # Lore chunk
├── lore_magic_system.txt
├── lore_fae_heritage.txt
├── world_setting.txt                # World context
├── world_rules.txt                  # Custom mechanics (optional)
├── plot_beat_vision.txt             # Plot beat
├── plot_beat_betrayal.txt
├── stat_health.yml                  # Structured config (YAML)
├── stat_mana.yml
├── item_excalibur.yml
├── art/
│   ├── characters/
│   │   ├── merlin_profile.webm
│   │   ├── arthur_young.jpg
│   │   └── morgana_portrait.png
│   ├── locations/
│   │   └── camelot_throne_room.png
│   └── items/
│       └── excalibur_scabbard.png
├── music/
│   ├── ambient_camelot.mp3
│   ├── combat_theme.ogg
│   └── ending_bittersweet.flac
└── .gitignore
```

**File Naming Convention**: `{entity_type}_{entity_id}_{optional_aspect}.txt`

Examples:
- `character_merlin.txt` — Character definition (entity_type=character, entity_id=merlin)
- `lore_magic_system.txt` — Lore chunk (entity_type=lore, entity_id=magic_system)
- `npc_merlin_morgana_relationship.txt` — Relationship prose (entity_type=npc, linking merlin and morgana)
- `plot_beat_vision.txt` — Plot moment (entity_type=plot_beat, entity_id=vision)
- `world_setting.txt` — World context (entity_type=world)

Authors are **free to name files however they choose** as long as frontmatter declares intent. Filenames are hints; frontmatter is canonical.

#### 6.2.3 Campaign Metadata File

Campaign configuration goes in `campaign.yml` (YAML, human-readable):

```yaml
title: Chronicles of Merlin
version: 2.1.0
author: Jane Storyteller
description: An epic fantasy retelling of Arthurian legend

# Campaign style
genre: High Fantasy
tone: Epic, Introspective, Morally Complex
rules_hint: narrative  # (rules-light | narrative | crunchy | tactical)

# Content guidance
content_rating: Teen
content_warnings:
  - violence
  - moral complexity
estimated_playtime_hours: 10

# Metadata for discovery
tags: [magic, medieval, quest, moral-choices]
license: CC-BY-NC-SA-4.0
homepage: https://example.com/chronicles-of-merlin

# Narrative guidance for AI
hydration_guidance: "Execute faithfully - all content is intentionally crafted"
```

All fields except `title` and `version` are optional.

#### 6.2.4 Semantic File Format with Optional Frontmatter

Every content file (`.txt`, `.md`) may include YAML frontmatter declaring its intent. The ingestion pipeline treats frontmatter and prose body as structurally distinct data sources with different precedence weights (see Section 5.3.1):

- **Frontmatter** (YAML block between `---` markers): Declares hard constraints and factual assertions. Stored with `sourceType: frontmatter` and `weightTier: 1` (absolute priority). Frontmatter keys are freeform — authors choose their own vocabulary. No schema is enforced beyond valid YAML.
- **Prose body** (everything after the closing `---`): Provides narrative flavor, personality details, and contextual richness. Stored with `sourceType: prose` and `weightTier: 3` (same-file) or `weightTier: 4` (when retrieved cross-file via semantic search).

This separation ensures that an author who writes `core_traits: [never_accepts_evil]` in frontmatter establishes an inviolable constraint, while prose emphasis like "Merlin would NEVER EVER EVER align with evil" serves as narrative flavor within that constraint — not as precedence amplification.

```yaml
---
entity_type: character
entity_id: merlin
content_type: personality
priority: 1
portrait_asset: art/characters/merlin_profile.webm
tags: [wise, mentor, wizard, melancholic]
---

Merlin is a figure of profound wisdom and tragic foresight. He appears as an elderly man
with penetrating gray eyes that seem to see beyond the veil of time itself. His manner is
deliberate; he speaks in metaphor and riddle, testing wisdom before revealing truth.

Despite his vast power, Merlin carries an immense burden: he has foreseen Camelot's fall
and knows he cannot prevent it, only delay. This knowledge makes him protective of Arthur
yet distant, holding counsel close.
```

**Frontmatter Fields (All Optional):**

| Field | Type | Description | Inference Rule |
|-------|------|-------------|-----------------|
| `entity_type` | string | `character`, `npc`, `lore`, `world`, `plot_beat`, `stat`, `item` | Infer from content keywords ("This is...", "Merlin is...") |
| `entity_id` | string | Unique identifier for entity | Derive from filename prefix: `character_merlin.txt` → `merlin` |
| `content_type` | string | `personality`, `background`, `goals`, `speech`, `relationship`, `history`, `mechanics`, `setting` | Infer from prose: "How magic works" → `mechanics` |
| `priority` | integer (1-5) | Narrative importance | Default: 1 (can be inferred from keywords: "critical", "important") |
| `portrait_asset` | string | Relative path to image/video | Search `art/characters/` for matching name if omitted |
| `tags` | array | Semantic labels | Extract via keyword matching: "wise", "melancholic", "mentor" |
| `mood` | string | Narrative tone for this content | Infer from prose: somber/hopeful/tense/romantic |
| `plot_requirements` | array | Prerequisites (["beat_id_1", "beat_id_2"]) | Parse from relationships in prose |

**Minimal Frontmatter Example:**

```yaml
---
entity_type: character
---

Merlin is a wizard of legendary power...
```

Ingestion infers: `entity_id=merlin` (from filename), `content_type=personality` (from prose), `tags=[wizard, wise]` (keyword extraction).

**Rich Frontmatter Example:**

```yaml
---
entity_type: lore
entity_id: magic_system
content_type: world_knowledge
priority: 2
mood: ancient
tags: [magic, system, rules, world]
---

The magic of Camelot flows from three sources...
```

#### 6.2.5 Structured Configuration Files

Structured data (stats, items, rules) remains in YAML/JSON for validation:

**Example: `stat_health.yml`**

```yaml
entity_type: stat
entity_id: health
display_name: Health
icon: heart
range:
  min: 0
  max: 100
starting_value: 100
visible: true
tooltip: "Physical well-being. Reduced by combat, restored by healing."
```

**Example: `item_excalibur.yml`**

```yaml
entity_type: item
entity_id: excalibur
display_name: Excalibur
description: The legendary sword of Britain, symbol of kingship
properties:
  damage_bonus: 5
  rarity: legendary
  equip_slot: right_hand
flavor_text: "The blade catches light like liquid silver, humming with ancient power."
```

**Example: `plot_beat_vision.yml`**

```yaml
entity_type: plot_beat
entity_id: vision
title: Vision of the Future
description: Merlin receives a prophetic vision of Camelot in flames
conditions:
  scene_count_min: 2
  scene_count_max: 5
  requires_beat: [opening_encounter]
  player_state: established_in_camelot
priority: critical
consequences:
  - state_patch: {player.gained_knowledge: true}
  - npc_mood_change: {merlin: more_secretive}
skippable: false
mood: foreboding
music: music/theme_vision.mp3
```

#### 6.2.6 Player Character Constraints

Optional file: `player_character_constraints.yml`

```yaml
entity_type: player_character
allowed_races: [human, elf, dwarf]
allowed_classes: [knight, wizard, rogue]
starting_location: gates_of_camelot
starting_items:
  - travelers_cloak
  - worn_dagger
hard_constraints:
  must_be_humanoid: true
  forbidden_attributes: [immortal]
soft_constraints:
  recommended_archetype: warrior
  narrative_note: "You arrive as a stranger to Camelot"
```

If not provided, campaign defaults to permissive mode (any fresh character allowed).

#### 6.2.7 Player Character Profiles

**Core Principle**: *Characters begin as lightweight templates (freeform description + portrait) and "realize" into structured personas during campaign start, interpreted uniquely for each campaign's world.*

Player Character Profiles follow a **fresh → realized lifecycle** that optimizes creation speed while enabling campaign-specific interpretation. Unlike systems where characters are fully defined upfront, Narratoria stores minimal character data until campaign start, when the LLM generates a complete character.json tailored to the campaign's world, constraints, and narrative tone.

**Key Design Tenets:**

1. **Lightweight Creation**: Characters store only freeform description + portrait, enabling instant creation without LLM wait times.
2. **Campaign-Specific Realization**: The same character template can become a wizard in fantasy, a detective in noir, or a pilot in sci-fi—interpreted contextually at campaign start.
3. **Semantic Integration**: Realized characters undergo chunking, embedding, and ObjectBox ingestion alongside campaign NPCs, becoming part of the narrative knowledge graph.
4. **Reusable Templates**: A single fresh character can be realized multiple times across different campaigns, each realization independent and campaign-owned.

##### Character Lifecycle States

**Fresh Character** (pre-campaign):
- Stored in user data directory
- Contains: freeform description text + portrait image
- Status badge: "Fresh" (never used in campaign)
- Reusable across any campaign

**Realized Character** (post-campaign-start):
- Generated during campaign ingestion (2-5 seconds)
- Contains: full structured JSON (name, archetype, personality, background, goals, speech_patterns, stats)
- Stored within campaign data (campaign-owned)
- Lives in campaign's ObjectBox database as semantic chunks
- Evolves during gameplay (relationships, reputation, stat changes)

##### Storage Model

**Fresh Character Storage (Pre-Campaign)**

Fresh characters are stored in the application's user data directory:

```
# Platform-specific locations
iOS: <App Container>/Library/Application Support/narratoria/characters/
Android: <App Data>/narratoria/characters/
Desktop: ~/.narratoria/characters/

# File structure
characters/
├── char_a1b2c3d4.json          # Fresh character metadata
├── char_a1b2c3d4_portrait.webp # User-uploaded portrait
├── char_e5f6g7h8.json
└── char_e5f6g7h8_portrait.png
```

**Fresh Character JSON Schema:**

```json
{
  "id": "char_a1b2c3d4",
  "status": "fresh",
  "created_at": "2026-02-13T14:30:00Z",
  "description": "A gruff, battle-scarred knight who secretly loves poetry. Failed to save his king and now seeks redemption through unwavering service.",
  "portrait_path": "characters/char_a1b2c3d4_portrait.webp",
  "realizations": [
    {
      "campaign_id": "fantasy_quest_uuid",
      "campaign_title": "Chronicles of Merlin",
      "realized_at": "2026-02-14T09:15:00Z",
      "realized_name": "Sir Eredin",
      "last_played": "2026-02-15T18:45:00Z",
      "playtime_hours": 3.2,
      "completed": false
    }
  ]
}
```

**Required Fields:**
- `id` (UUID)
- `status` ("fresh" | "used")
- `created_at` (ISO 8601 timestamp)
- `description` (freeform text, 10-5000 characters)
- `portrait_path` (relative or absolute path to image)

**Optional Fields:**
- `realizations` (array of campaign usage records, populated after first use)

**Realized Character Storage (Campaign-Specific)**

Realized characters are stored within campaign data, not in global character storage:

```
campaigns/
└── fantasy_quest_uuid/
    └── sessions/
        └── session_a1b2c3d4/
            ├── state.json                    # Session state (includes character_id reference)
            └── objectbox/
                └── character_chunks/         # Embedded character data
                    ├── personality.chunk
                    ├── background.chunk
                    ├── goals.chunk
                    └── speech_patterns.chunk
```

Realized character JSON is **not stored as a single file**. Instead, it exists as:
1. **Semantic chunks** in ObjectBox (for narrative retrieval)
2. **Session state fields** (name, race, class, current stats)
3. **Reference to original fresh character** (via `character_id` in session state)

##### Character Creation Flow

**Options → Characters → New Character**

1. **Player Enters Description**
   - UI: Single multiline text area
   - Prompt: "Describe your character..."
   - Example: "A gruff, battle-scarred knight who secretly loves poetry..."
   - No structured fields, no LLM generation

2. **Player Uploads Portrait**
   - User-provided image (PNG/JPEG/WebP, max 5MB)
   - Resized to 512×512px
   - **Required** (in-process models cannot generate images)

3. **Fresh Character Saved**
   - JSON written to `<user_data>/characters/{uuid}.json`
   - Portrait saved to `<user_data>/characters/{uuid}_portrait.{ext}`
   - Status: "fresh"
   - **No LLM generation** at this stage
   - **Duration: <500ms**

##### Character Gallery Display

**Fresh Characters:**
```
┌─────────────────┐
│   [Portrait]    │
│                 │
│ "A gruff, ba... │
│ 🆕 Fresh        │
└─────────────────┘
```

**Used Characters:**
```
┌─────────────────┐
│   [Portrait]    │
│                 │
│ "A gruff, ba... │
│ Used in:        │
│ • Fantasy Quest │
│ • Noir Mystery  │
└─────────────────┘
```

Gallery shows:
- Portrait thumbnail
- First 50 characters of description (with ellipsis)
- Status badge ("Fresh" if never used)
- List of campaigns where character has been realized (if `realizations` array exists)

##### Character Realization (Campaign Start)

When a player selects a fresh character for a campaign:

**Phase 1: Character Realization via LLM**

```
Campaign Start Sequence:
1. User selects fresh character from gallery
2. Campaign loads manifest and constraints
3. LLM generates character.json from description + campaign context
4. Character.json chunked and ingested into campaign's ObjectBox
5. Fresh character's realizations array updated
6. Campaign starts with realized character
```

**LLM Realization Prompt:**

```
You are generating a character for the campaign "{campaign_title}".

Campaign world: {campaign.setting}
Campaign tone: {campaign.tone}
Allowed races: {campaign.allowed_races}
Allowed classes: {campaign.allowed_classes}

Player's character description:
"{fresh_character.description}"

Generate a complete character JSON with:
- name (creative, fitting the description and campaign world)
- archetype (race, class, subclass - must match campaign constraints)
- personality (traits, flaws, virtues arrays extracted from description)
- background (expanded narrative from description, tailored to campaign world)
- goals (2-4 character goals derived from description, campaign-relevant)
- speech_patterns (style + 2-3 example dialogue lines matching personality)
- starting_stats (appropriate values for archetype, matching campaign stat system)

Ensure the character feels like a natural inhabitant of this campaign's world.
```

**Realization Performance:**
- Generation time: 2-5 seconds (Phi-4-mini)
- Occurs during campaign load (user sees progress indicator)
- Generated character not saved as standalone JSON (exists only as ObjectBox chunks + session state)

**Phase 2: Character Chunking and Ingestion**

Realized character undergoes the same ingestion pipeline as NPCs (Section 6.2.15):

1. **Metadata Extraction**
   - Category: `character`
   - EntityType: `player`
   - EntityId: `{fresh_character.id}`
   - SemanticRole: `player_character_definition`

2. **Chunking**
   - Personality chunk: `"Personality traits: {traits}. Flaws: {flaws}. Virtues: {virtues}."`
   - Background chunk: Expanded narrative (paragraph-based splitting if >512 tokens)
   - Goals chunk: `"Character goals: {goals with priorities}"`
   - Speech patterns chunk: `"Speech style: {style}. Example dialogue: {examples}"`

3. **Semantic Embedding**
   - Each chunk embedded with sentence-transformers (384-dim vectors)
   - Stored with references to parent entity

4. **ObjectBox Storage**
   ```
   CampaignChunk {
     campaignId: "{campaign_id}",
     entityId: "{fresh_character.id}",
     entityType: "player",
     contentType: "personality",
     content: "Personality traits: honorable, protective...",
     embedding: [0.23, -0.41, 0.57, ...],
     metadata: {
       is_player_character: true,
       source_description: "{original freeform text}"
     }
   }
   ```

5. **Fresh Character Update**
   ```json
   // Fresh character JSON updated with realization record
   {
     "id": "char_a1b2c3d4",
     "status": "used",
     "realizations": [
       {
         "campaign_id": "fantasy_quest_uuid",
         "campaign_title": "Chronicles of Merlin",
         "realized_at": "2026-02-14T09:15:00Z",
         "realized_name": "Sir Eredin",
         "last_played": "2026-02-14T09:15:00Z",
         "playtime_hours": 0,
         "completed": false
       }
     ]
   }
   ```

**Phase 3: Session State Initialization**

```json
{
  "player": {
    "name": "Sir Eredin",           // Generated by LLM
    "race": "Human",                 // From generated archetype
    "class": "Knight",               // From generated archetype
    "stats": {                       // From generated starting_stats
      "strength": 16,
      /* Lines 1862-1865 omitted */
      "charisma": 13
    },
    "character_id": "char_a1b2c3d4"  // Reference to original fresh character
  }
}
```

##### Runtime Character Access

During gameplay, the Narrator AI retrieves character details through semantic search:

```
Example Query: "What are the player's personality traits?"

Semantic Search:
  query: "player character personality traits"
  filters: {entityType: "player", is_player_character: true}
  topK: 5

Returns chunks:
  1. "Personality traits: honorable, protective, stoic..." (similarity: 0.92)
  2. "Character goals: Protect the innocent (high priority)..." (similarity: 0.78)
  3. "Speech style: Formal, measured tones. Example: 'By my oath...'" (similarity: 0.71)
```

The Narrator AI synthesizes retrieved chunks into narrative prose without keeping the entire character profile in context memory.

##### Campaign-Specific Character Evolution

As gameplay progresses, the realized character evolves **within** the campaign data:

**Tracked Changes:**
- **Stat changes**: Stored in session state, updated via `state_patch` events
- **Relationship changes**: NPC perception scores stored in campaign database
- **Reputation changes**: Faction standing tracked in campaign database
- **Narrative history**: Scene summaries reference player actions and choices
- **Playtime tracking**: Fresh character's `realizations` array updated periodically

**Original fresh character remains unchanged** (description + portrait persist as reusable template).

##### Character Reusability

The same fresh character can be realized differently across campaigns:

```
Fresh Character:
  Description: "A cunning rogue with a heart of gold"
  
Campaign A (Fantasy): Sir Rowan, human thief, seeks redemption
Campaign B (Cyberpunk): Zero, netrunner, protects street urchins
Campaign C (Space Opera): Captain Vex, smuggler, defends outer colonies
```

Each realization:
- Interprets the description in campaign-specific context
- Generates unique name, archetype, and narrative details
- Exists independently in each campaign's data
- Shares only the original description and portrait

##### Portrait Management

**User-Provided Portraits Only:**

Narratoria's in-process models cannot generate images. Players must upload portraits during fresh character creation.

**Portrait Handling:**
- Uploaded images resized to 512×512px (preserves aspect ratio with letterboxing)
- Supported formats: PNG, JPEG, WebP
- Max file size: 5MB
- **Required** before fresh character can be saved

**Portrait Reuse:**
- Same portrait used across all realizations of a fresh character
- Portrait never regenerated or modified by system
- Players can update portrait by editing fresh character

##### Character Management UI

The system provides a character gallery interface (see User Journey: Options → Characters):

**Features:**
- **Create Character**: Enter description + Upload portrait → Save (instant, no LLM)
- **Edit Character**: Update description or replace portrait (no LLM, instant save)
- **Delete Character**: Remove fresh character JSON and portrait file
- **View Fresh Character**: Shows description, portrait, status badge, campaign usage list
- **View Realized Character**: Accessible through campaign save file viewer (shows generated identity)

**Gallery Actions:**
- Select character for campaign → triggers realization at campaign start
- View campaign history → shows which campaigns used this template
- Export fresh character → shares description + portrait as reusable template
- Import fresh character → adds description + portrait to gallery

**Validation:**
- Description: 10-5000 characters
- Portrait: PNG/JPEG/WebP, <5MB, resized to 512×512px
- No uniqueness checks (multiple fresh characters can have similar descriptions)

##### ObjectBox Integration and Character Persistence

**Design Principle**: *Realization is process, not persistence. Generated JSON is ephemeral; only chunks are durable.*

Realized characters are stored in a **game-wide ObjectBox singleton database** (not per-campaign), indexed by composite key `(source_character_id, campaign_id)`. This architecture enables:

1. **Efficient reuse**: Same fresh character + same campaign = retrieve existing chunks (character behavior unchanged across playthroughs)
2. **Campaign-specific variance**: Same fresh character + different campaign = new chunks from different realization (captured in new rows)
3. **No redundant storage**: Realized character JSON is not persisted; only semantic chunks with minimal metadata live in ObjectBox
4. **Global narrative coherence**: All characters (player + NPCs) share the same embedding space and semantic indexing

**ObjectBox Architecture:**

```
Game-Wide ObjectBox Singleton:
  └── CampaignChunks table
      ├── Composite Index: (campaignId, sourceCharacterId)
      ├── Columns:
      │   ├── id (UUID, primary key)
      │   ├── campaignId (UUID, indexed)
      │   ├── sourceCharacterId (UUID, indexed)
      │   ├── entityType ("player" | "npc" | "lore" | "location")
      │   ├── contentType ("personality" | "background" | "goals" | "speech_patterns" | ...)
      │   ├── content (string, markdown-formatted chunk text)
      │   ├── embedding (384-dim vector, from sentence-transformers)
      │   ├── metadata (JSON object)
      │   └── createdAt (timestamp)
      └── Indices: campaignId, sourceCharacterId, entityType, contentType
```

**Metadata Schema for Character Chunks:**

```json
{
  "metadata": {
    "is_player_character": true,
    "source_description": "A cunning rogue with a heart of gold",
    "realized_name": "Sir Rowan",
    "realized_archetype": "Rogue/Thief",
    "source_campaign_allowed_races": ["human", "halfling", "dwarf"],
    "created_at_realization": "2026-02-14T09:15:00Z",
    "chunk_index": 0,
    "chunk_total": 4,
    "relevance_tags": ["personality", "archetype", "canonical"],
    "violation_state": "none"
  }
}
```

**Violation State Values:**
- `none`: Character fully complies with campaign constraints
- `warning`: Character has minor violations (e.g., suggested race not in allowed list, but campaign permits override)
- `incompatible`: Character violates hard constraints (e.g., campaign is "No mages" and character is mage; requires player acknowledge before proceeding)

**Query Patterns (High-Level):**

1. **Retrieve Player Character Personality** (during narrative generation):
   ```
   Query: {
     campaignId: <current_campaign>,
     sourceCharacterId: <player_character_id>,
     contentType: "personality",
     entityType: "player"
   }
   Semantic Search: vectorSim(query_embedding, chunk.embedding) > 0.85
   Limit: 3 chunks
   Result: [personality_chunk, goals_chunk, speech_patterns_chunk]
   ```

2. **Retrieve All Player Character Data** (for session init or stat display):
   ```
   Query: {
     campaignId: <current_campaign>,
     sourceCharacterId: <player_character_id>,
     entityType: "player"
   }
   Result: [personality, background, goals, speech_patterns, stats_reference] (all 4-5 chunks)
   ```

3. **Check Character Constraint Violations** (at character selection):
   ```
   Query: {
     campaignId: <target_campaign>,
     sourceCharacterId: <selected_character_id>,
     entityType: "player"
   }
   Check: metadata.violation_state != "incompatible"
   Display: If "incompatible", show warning dialog with violation details
   ```

4. **Retrieve NPC Perception of Player** (for scene generation):
   ```
   Query: {
     campaignId: <current_campaign>,
     sourceCharacterId: <target_npc_id>,
     contentType: "perception",
     metadata.perceived_entity_id: <player_character_id>
   }
   Semantic Search: vectorSim(memory_query, chunk.embedding) > 0.70
   Result: [recent_memory_chunk, relationship_status_chunk]
   ```

5. **Retrieve Player Character by Campaign** (for load game preview):
   ```
   Query: {
     campaignId: <playthrough_campaign_id>,
     entityType: "player"
   }
   Result: All chunks for player character in that campaign
   ```

**Singleton Lifecycle:**

```
Game Start:
  1. ObjectBox initialized as game-wide singleton (never per-campaign)
  2. Existing ObjectBox file loaded (if exists in app data)
  3. Available across all campaigns, all playthroughs
  
Campaign Start:
  1. Pass existing ObjectBox instance to campaign loader
  2. Campaign realization phase queries (source_character_id, campaign_id) indices
  3. New chunks inserted with (source_character_id, campaign_id, "player") composite key
  4. All subsequent narrative queries join across (campaignId, sourceCharacterId)
  
Campaign End/Unload:
  1. ObjectBox remains in memory (not cleared; persists across playthroughs)
  2. Queries scoped by (current_campaignId, current_sourceCharacterId)
  3. No per-campaign ObjectBox cleanup
  
App Shutdown:
  1. ObjectBox serialized and persisted to device storage
  2. On next launch, deserialized and available immediately
```

**Idempotency Guarantee:**

- Same fresh character + same campaign → same ObjectBox query result (character behavior immutable across playthroughs)
- Different fresh character + same campaign → different ObjectBox rows (new source_character_id)
- Same fresh character + different campaign → different ObjectBox rows (new campaign_id)
- Query scoping by composite key (campaignId, sourceCharacterId) ensures no cross-contamination

#### 6.2.8 Stats System

Stats are **named numeric gauges that constrain what the narrative can do**. Every RPG system — tabletop, CRPG, visual novel, dating sim — uses stats for three purposes:

1. **Gate**: Determine what options are available ("You need 14 Strength to force the door")
2. **Modify**: Shift probability of outcomes ("Roll + Dexterity modifier")
3. **Resource**: Deplete and replenish to create tension ("You have 3 HP left")

Whether a stat is called "HP," "Hull Integrity," "Composure," "Chemistry," or "Favor with the Empress," it's always a named number with a range that gates, modifies, or depletes. Narratoria treats stats generically — the system doesn't need to know what "health" means in advance. It knows there are N stats, each has a range, and each gets a UI gauge.

**Core Principle**: *Convention over configuration.* Authors define stats by creating JSON files in a `stats/` directory.

##### Stat File Convention

**Location**: `campaign_name/stats/`
**Filename pattern**: `{stat_id}.json`
**Hidden stats**: `campaign_name/stats/hidden/` (or `"hidden": true` in JSON)

**Example: Fantasy RPG — `stats/health.json`**

```json
{
  "id": "health",
  "range": {"min": 0, "max": 100},
  "default": 100,
  "display": "bar",
  "label": "Health",
  "category": "vital",
  "hidden": false,
  "behavioral_prompt": "Health represents physical well-being. When health reaches 0, the character is incapacitated. Combat damage, poison, and exhaustion reduce health. Rest, potions, and healing magic restore it. The narrator should describe declining health through increasingly vivid physical symptoms — heavy breathing at 70, visible wounds at 40, barely standing at 15."
}
```

**Example: Dating Sim — `stats/chemistry.json`**

```json
{
  "id": "chemistry",
  "range": {"min": 0, "max": 10},
  "default": 0,
  "display": "hearts",
  "label": "Chemistry",
  "category": "relationship",
  "behavioral_prompt": "Chemistry measures romantic tension between the player and a love interest. It rises through flirting, shared vulnerability, and meaningful gifts. It drops through insensitivity, betrayal, or prolonged absence. At 8+, the love interest initiates romantic dialogue unprompted. At 3 or below, they become distant and formal."
}
```

**Example: Sci-Fi — `stats/hull_integrity.json`**

```json
{
  "id": "hull_integrity",
  "range": {"min": 0, "max": 1000},
  "default": 1000,
  "display": "bar",
  "label": "Hull Integrity",
  "category": "ship",
  "behavioral_prompt": "Hull integrity represents the structural health of the player's starship. Asteroid impacts, weapons fire, and hard landings reduce it. Repair drones, spacedock maintenance, and emergency patches restore it. Below 200, the narrator should describe sparking conduits, flickering lights, and hull breach warnings."
}
```

**Example: Hidden Stat — `stats/hidden/suspicion.json`**

```json
{
  "id": "suspicion",
  "range": {"min": 0, "max": 100},
  "default": 0,
  "display": "bar",
  "label": "Suspicion",
  "category": "social",
  "hidden": true,
  "behavioral_prompt": "Suspicion tracks how much the town guard suspects the player of criminal activity. Witnessing theft, finding contraband, or receiving tips from informants raises suspicion. Bribes, good deeds, and time passing lower it. The narrator should reveal suspicion indirectly — guards watching more closely at 30, being followed at 60, an arrest warrant at 90."
}
```

##### JSON Field Definitions

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `id` | string | Yes | — | Unique stat identifier (matches filename stem) |
| `range` | object | Yes | — | Numeric bounds with `min` and `max` (e.g., `{"min": 0, "max": 100}`) |
| `default` | number | No | `min` value | Starting value for new playthroughs |
| `display` | enum | No | `bar` | UI rendering hint: `bar`, `number`, `hearts`, `pips`, `ring`, `hidden` |
| `label` | string | No | Filename stem, title-cased | Human-readable display name |
| `category` | string | No | `general` | Grouping key for UI layout (e.g., `vital`, `resource`, `relationship`, `ship`, `social`) |
| `hidden` | boolean | No | `false` | If `true`, the system tracks but does not display to the player |
| `behavioral_prompt` | string | No | — | Prose injected into Narrator AI context to guide stat narration |

##### Ingestion and State Binding

On campaign load, the ingestion pipeline processes `stats/`:

```
For each *.json in stats/ (including stats/hidden/):
  1. Parse filename stem → stat ID ("health", "chemistry")
  2. Load and validate JSON → range, default, display, label, category, hidden, behavioral_prompt
  3. Store stat definition in the persistence layer:
     - Stat ID, range bounds, default, display mode, category
     - Behavioral prompt stored as embedding + raw text
     - Hidden flag
  4. Register state path: player.stats.{stat_id}
  5. Initialize session state: player.stats.{stat_id} = default
  6. Check for UI asset: ui/state/player.stats.{stat_id}.{ext}
  7. Inject behavioral_prompt into Narrator AI system context
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
stats/npc.owen.chemistry.json
stats/npc.owen.trust.json
```

**`stats/npc.owen.chemistry.json`:**

```json
{
  "id": "npc.owen.chemistry",
  "range": {"min": 0, "max": 10},
  "default": 0,
  "display": "hearts",
  "label": "Chemistry with Owen",
  "category": "relationship",
  "behavioral_prompt": "Owen is guarded after a past betrayal. Chemistry builds slowly through consistent kindness and shared creative pursuits. Grand gestures make him uncomfortable — he values quiet moments. At 7+, Owen begins sharing personal stories unprompted. At 2 or below, he avoids being alone with the player."
}
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

Items are defined in `items/` using JSON files with structured data and behavioral prompts:

**`items/weapon.short_sword.json`:**

```json
{
  "id": "short_sword",
  "type": "weapon",
  "damage": "1d6",
  "weight": 3,
  "label": "Short Sword",
  "category": "melee",
  "behavioral_prompt": "A reliable sidearm favored by scouts and rogues. Its short blade excels in tight quarters — corridors, ship decks, and tavern brawls. The narrator should describe its use as quick, precise strikes rather than heavy cleaving blows."
}
```

**`items/potion.healing.json`:**

```json
{
  "id": "healing_potion",
  "type": "consumable",
  "effect": "health +25",
  "uses": 1,
  "weight": 0.5,
  "label": "Healing Potion",
  "category": "consumable",
  "behavioral_prompt": "A small glass vial containing a warm crimson liquid. When consumed, it rapidly mends wounds and restores vitality. The narrator should describe a spreading warmth, the taste of honey and copper, and the visible closure of minor wounds."
}
```

On ingestion, item data is tokenized and stored in the persistence layer with semantic embeddings. When the Narrator AI generates a plan involving a dice roll or stat check, it can look up the relevant item's properties:

```json
{
  "toolId": "dice_roll",
  "toolPath": "skills/dice-roller/scripts/roll",
  "input": {
    "formula": "1d20 + player.stats.dexterity",
    "item_context": "weapon.short_sword"
  }
}
```

The dice roller skill queries the persistence layer for `weapon.short_sword`, retrieves `damage: 1d6`, and factors it into the result.

##### UI Rendering

The UI renders stats deterministically based on definition metadata:

1. **Discovery**: Query all stat definitions from the persistence layer where `hidden = false`
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

#### 6.2.9 Lore System

All files in `lore/` are indexed for semantic search (RAG retrieval):

- Files are chunked by paragraph (split on `\n\n`)
- Maximum 512 tokens per chunk
- If a single paragraph exceeds 512 tokens, it is split on sentence boundaries (`.`, `!`, `?`)
- Each chunk is stored with metadata: original file path, chunk index, paragraph ID, token count, chunk method ("paragraph")
- Token counts must be computed using the `tiktoken` library with the `cl100k_base` tokenizer (compatible with the sentence-transformers embedding model)
- Nested directories within `lore/` are supported for organization

#### 6.2.10 Creative Assets

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
    "toolPath": "skills/memory/scripts/recall",
    "input": {
      "query": "wizard appearance",
      "sources": ["characters/npcs/wizard/profile.json", "art/characters/npc_wizard.png.keywords.txt"]
    }
  }
  ```

- The Storyteller skill's `SKILL.md` behavioral guidance includes explicit instructions:
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

#### 6.2.11 Asset Metadata Structure

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

#### 6.2.12 Keyword Sidecar Files

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

#### 6.2.13 State-Bound UI Assets

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

```
// Conceptual data structure
StateAssetIndex {
  exactMatches:     Map<String, AssetMetadata>  // Exact path matches
  patternMatches:   List<PatternMatch>          // Wildcard patterns
  typeFallbacks:    Map<String, AssetMetadata>  // Type-based fallbacks
  categoryFallbacks: Map<String, AssetMetadata> // Category-level fallbacks
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

The world map UI renders discovered location markers by:

1. Reading `world.locations` from session state
2. Filtering to only `discovered: true` locations
3. For each discovered location, resolving the marker asset via `assetIndex.resolve(statePath)`
4. Positioning markers using coordinates from state (`map_position`) or campaign data
5. Enabling tap interaction to open location details

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
├── Phi-4-mini model: 2.5GB (31%) or Phi-4: 8GB (100% limit)
├── sentence-transformers: 60MB (0.75%)
├── Database + UI Framework + OS: ~500MB (6%)
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

**Tier 2: Embedded Database Cache (Cross-Session Persistence)**
- Index serialized to the persistence layer during campaign ingestion
- Includes campaign version checksum for invalidation
- Rebuilt only when campaign files change (detected via SHA-256 hash)
- Load time: <50ms for large campaigns

**Tier 3: Campaign Directory (No Cache)**
- Fallback if persistence cache is missing or invalidated
- Full directory scan + metadata parsing
- Build time: <100ms for 1000 assets, <500ms for 10,000 assets

**Indexing Lifecycle:**

```
Campaign Load
    ↓
Check persistence layer for cached index
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
       Serialize to persistence layer with version hash
         ↓
       Ready for lookups
```

**Persistence Schema for Cached Index:**

The cached index record stores:

| Field | Type | Description |
|-------|------|-------------|
| id | auto | Primary key |
| campaignId | string | Unique campaign identifier |
| campaignVersion | string | Semver from manifest.json |
| contentHash | string | SHA-256 of ui/ directory tree |
| indexedAt | datetime | When index was built |
| indexData | byte[] | Serialized index data (JSON or MessagePack) |
| assetCount | integer | For metadata/debugging |
| sizeBytes | integer | Serialized size |

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
4. **Compressed Storage**: Index data compressed with zstd in the persistence layer (2-3x reduction)

**Memory vs. Disk Trade-off Decision:**

| Strategy | Lookup Speed | Memory Usage | Disk I/O | Cache Warmup |
|----------|--------------|--------------|----------|--------------|
| In-memory only | <1ms | 2.5MB (10K) | 0 during play | 100-500ms |
| Database only | 5-10ms | ~500KB | Every lookup | 0ms |
| Hybrid (chosen) | <1ms | 2.5MB | 0 during play | 50ms (cached) |

**Why Hybrid Wins:**

- **Speed**: In-memory lookups are 10-20x faster than disk queries
- **Negligible RAM**: Even 10K assets use <0.05% of available memory
- **Fast Startup**: Database cache reduces cold-start from 500ms → 50ms
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

**Decision**: Database-backed caching provides the speed benefits without filesystem complexity or campaign directory pollution.

**Monitoring and Telemetry:**

The system logs index performance metrics:

```json
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

#### 6.2.14 Provenance Validation

Provenance rules are enforced mechanically at the campaign ingestion layer / persistence adapter. The adapter must reject assets that violate provenance requirements and must not write them to the database:

- `generated: true` assets **require** a `provenance` object with `source_model`, `generated_at`, and `seed_data`. If any field is missing, the adapter rejects the store and returns error: `"Generated asset missing provenance (generated=true requires provenance.source_model, provenance.generated_at, provenance.seed_data)"`.
- `generated: false` assets **must not** have a `provenance` object. If present, the adapter rejects the store and returns error: `"Human-created asset must not contain provenance (generated=false conflicts with provenance object)"`.
- `generated_at` must be a valid ISO 8601 datetime. Non-conforming timestamps are rejected.
- On campaign load, a warning is displayed: "Campaign contains [N] AI-generated asset(s). Review generated flags and provenance metadata to verify correctness."

#### 6.2.15 Campaign Ingestion Pipeline

**Core Principle**: *Every piece of campaign content becomes semantically searchable through universal tokenization and embedding. Authors write semantic units; ingestion transforms them into vectors.*

When a campaign loads, the ingestion pipeline transforms text files (with optional YAML frontmatter) into a fully-indexed semantic knowledge graph stored in ObjectBox. This enables the Narrator AI and all skills to query campaign content through natural language similarity search rather than rigid path-based lookups.

**File Format**:

Campaign content is stored in plain-text files (`.txt`, `.md`, `.yml`, `.json`) with optional YAML frontmatter declaring intent:

```yaml
---
entity_type: character
entity_id: merlin
content_type: personality
priority: 1
tags: [wise, mentor]
portrait_asset: art/characters/merlin_profile.webm
---

Merlin is a figure of profound wisdom...
```

**Why plain text?**
- Human-readable and version-controlable (Git diffs are meaningful)
- Author-friendly (no special editing tools required)
- Flexible (authors decide granularity: minimal or rich frontmatter)
- Universal (works with any text editor)

**Frontmatter is optional**: Ingestion intelligently infers missing metadata from filenames and content.

##### Phase 1: Directory Walking & Discovery

The ingestion pipeline recursively scans the campaign directory:

```
function discoverCampaignAssets(campaignPath):
    assets = []
    
    for file in recursiveDirectoryWalk(campaignPath):
        if file.extension in ['.txt', '.json', '.md']:
            assets.add({
                type: 'text',
                path: file.path,
                format: file.extension,
                relativePath: file.relativeTo(campaignPath),
                sizeBytes: file.size,
                modifiedAt: file.lastModified
            })
        else if file.extension in ['.png', '.jpg', '.webp', '.mp3', '.ogg', '.wav', '.flac']:
            assets.add({
                type: 'binary',
                path: file.path,
                format: file.extension,
                relativePath: file.relativePath(campaignPath),
                sizeBytes: file.size
            })
    
    return assets
```

**Discovery Performance**: <100ms for campaigns with up to 1000 files.

##### Phase 2: Metadata Extraction from Frontmatter & Filenames

The ingestion system extracts metadata from file frontmatter (YAML blocks) and infers missing fields from filenames using intelligent pattern-matching:

**Step 2.1: Parse YAML Frontmatter**

If a file begins with `---` (YAML block), extract frontmatter fields:

```
File: character_merlin.txt

---
entity_type: character
entity_id: merlin
content_type: personality
priority: 1
portrait_asset: art/characters/merlin_profile.webm
tags: [wise, mentor, wizard]
---

Merlin is a figure of profound wisdom...
```

Extracted metadata:
```json
{
  "entity_type": "character",
  "entity_id": "merlin",
  "content_type": "personality",
  "priority": 1,
  "portrait_asset": "art/characters/merlin_profile.webm",
  "tags": ["wise", "mentor", "wizard"]
}
```

**Step 2.2: Infer Missing Fields from Filename & Content**

If frontmatter lacks fields, infer from filename and prose:

| Missing Field | Inference Method | Example |
|---------------|------------------|---------|
| `entity_type` | Filename prefix or content keywords | `character_merlin.txt` → entity_type=character; "Merlin has foreseen..." → entity_type=character |
| `entity_id` | Filename after first underscore | `character_merlin.txt` → entity_id=merlin; `lore_magic_system.txt` → entity_id=magic_system |
| `content_type` | Prose analysis or filename hints | "How magic works" → content_type=mechanics; `*_backstory.txt` → content_type=background |
| `priority` | Keywords in prose/filename | "critical", "essential" → priority=5; default → priority=1 |
| `portrait_asset` | Search art/characters/ for matching entity_id | entity_id=merlin → search art/characters/ for `merlin*` (merlin_profile.webm, merlin.png, etc.) |
| `tags` | Keyword extraction from prose and filename | "wise, melancholic, mentor" → tags=[wise, melancholic, mentor] |
| `mood` | NLP sentiment analysis on prose | Somber tone → mood=foreboding; joyful → mood=hopeful |

**Examples of Inference:**

**Example 1: Minimal Frontmatter**
```yaml
---
entity_type: character
---

Merlin is a wizard of legendary power...
```
Inference result:
```json
{
  "entity_type": "character",
  "entity_id": "merlin",
  "content_type": "personality",
  "priority": 1,
  "tags": ["wizard", "legendary"],
  "mood": "mysterious"
}
```

**Example 2: No Frontmatter**
```
# File: magicsystemlore.txt

The magic of Camelot flows from three sources: the old blood...
```
Inference result:
```json
{
  "entity_type": "lore",
  "entity_id": "magicsystem",
  "content_type": "worldknowledge",
  "priority": 1,
  "tags": ["magic", "system", "rules"],
  "mood": "informative"
}
```

**Example 3: Rich Frontmatter**
```yaml
---
entity_type: plot_beat
entity_id: vision
title: Vision of the Future
priority: 5
tags: [critical, prophecy, emotional]
mood: foreboding
plot_requirements: [opening_encounter]
---

Merlin receives a prophetic vision showing Camelot in flames...
```
Result: All fields explicit; no inference needed.

**Step 2.3: Asset Reference Resolution**

Frontmatter fields like `portrait_asset` are resolved to actual filesystem paths:

```
File: character_merlin.txt
Frontmatter: portrait_asset: art/characters/merlin_profile.webm

Resolution:
  → Search campaign_path/art/characters/ for merlin_profile.webm
  → If found: Add AssetReference { path, type: "image", verified: true }
  → If not found: Add warning to ingestion log, continue without asset
```

**Step 2.4: Entity ID Uniqueness Check**

Validation ensures each entity_id appears only once (or check if intentional multi-chunk reference):

```
Entities Discovered:
  - character_merlin.txt → entity_id: merlin ✓
  - character_merlin_backstory.txt → entity_id: merlin ✓ (intentional; same character, different aspect)
  - lore_merlin_origins.txt → entity_id: merlin_origins ✓ (different entity)

Warning if found:
  - relationship_merlin_morgana.txt → entity_id: merlin ⚠️ (already defined in character_merlin.txt)
  → Log: "Entity 'merlin' defined in multiple files; this is expected if intentional relationship prose"
```

**Step 2.5: Metadata Assembly**

Final metadata object assembled for chunking phase:

```json
{
  "filePath": "character_merlin.txt",
  "format": "txt",
  "category": "character",
  "entityType": "character",
  "entityId": "merlin",
  "contentType": "personality",
  "semanticRole": "character_definition",
  "priority": 1,
  "tags": ["wise", "mentor", "wizard"],
  "mood": "mysterious",
  "portraitAsset": {
    "path": "art/characters/merlin_profile.webm",
    "verified": true,
    "type": "video"
  },
  "inferredFields": ["entity_id", "content_type", "tags"],
  "explicitFields": ["entity_type", "priority"],
  "frontmatterKeys": ["entity_type", "entity_id", "content_type", "priority", "portrait_asset", "tags"],
  "metadata": {}
}
```

The `inferredFields` list tracks which fields were auto-derived (useful for validation and debugging). The `frontmatterKeys` list captures all keys present in the YAML frontmatter block, enabling the chunking phase to produce separate `frontmatter`-typed chunks for frontmatter assertions and `prose`-typed chunks for body text (see Section 5.3.1 for precedence rules).

##### Phase 3: Text Chunking & Tokenization

All text content (`.txt`, `.md`) undergoes intelligent chunking based on entity type and content structure:

**Prose-Based Files** (`.txt`, `.md` with narrative content):

0. **Frontmatter chunking**: If the file has YAML frontmatter, serialize its key-value assertions into a separate chunk with `sourceType: frontmatter` and `weightTier: 1`. This chunk represents the author's hard constraints for this entity and takes absolute precedence during context assembly (see Section 5.3.1).
1. **Paragraph-level chunking**: Split prose body (after frontmatter) on double-newline (`\n\n`). Each prose chunk is tagged `sourceType: prose` and `weightTier: 3` (same-file context).
2. **Token checking**: If paragraph exceeds 512 tokens, split on sentence boundaries (`. `, `! `, `? `)
3. **Chunk metadata**: Retain chunk index, paragraph ID, token count, chunking method, `sourceType`, `weightTier`, `fileOrigin`
4. **Semantic tagging**: Tag each chunk with content_type from frontmatter (e.g., "personality", "background", "goals")

**Example:**
```
File: character_merlin.txt (frontmatter declares content_type: personality)

Merlin is a figure of profound wisdom and tragic foresight. He appears as an elderly man
with penetrating gray eyes that seem to see beyond the veil of time itself. His manner is
deliberate and measured; he speaks in metaphor and riddle.

Despite his vast power, Merlin carries an immense burden: he has foreseen Camelot's fall
and knows he cannot prevent it, only delay.
```

Output chunks:
```
Chunk[0]: "Merlin is a figure of profound wisdom... His manner is deliberate and measured..."
  - chunkMethod: "paragraph"
  - tokenCount: 42
  - semanticRole: "character_personality"

Chunk[1]: "Despite his vast power, Merlin carries an immense burden..."
  - chunkMethod: "paragraph"
  - tokenCount: 31
  - semanticRole: "character_personality"
```

**Structured Files** (`.yml`, `.json` with configuration or complex definitions):

1. **Field-level chunking**: Each major field or section becomes a searchable unit
2. **Nested handling**: JSON paths preserved in metadata (e.g., `personality.traits`)
3. **Semantic role assignment**: Fields tagged with content type from frontmatter or metadata

**Example:**
```yaml
# File: plot_beat_vision.yml

entity_type: plot_beat
entity_id: vision
title: Vision of the Future
description: Merlin receives a prophetic vision of Camelot in flames
conditions:
  scene_count_min: 2
  scene_count_max: 5
priority: critical
mood: foreboding
```

Output chunks:
```
Chunk[0]: "Vision of the Future. Merlin receives a prophetic vision of Camelot in flames."
  - jsonField: "title + description"
  - semanticRole: "plot_beat_definition"
  - tokenCount: 18

Chunk[1]: "Requires scenes 2-5. Player must be established in Camelot. Mood: foreboding."
  - jsonField: "conditions + priority + mood"
  - semanticRole: "plot_beat_constraints"
  - tokenCount: 22
```

**Player Character Realization and Ingestion**:

When a player selects a fresh character for a campaign at campaign start (see [Section 6.2.7](../../../architecture.md#6.2.7-player-character-profiles)):

1. **Character Realization** (2-5 seconds): Phi-4 or Phi-4-mini generates structured character.json from the fresh character's freeform description, constrained by campaign's `allowed_races`, `allowed_classes`, and hard constraints. Generated profile includes name, archetype, personality, background, goals, speech_patterns, and starting stats.

2. **Realized Ingestion**: Generated character.json is immediately chunked and embedded using the same field-level strategy as NPC profiles. Chunks become part of campaign ObjectBox, distinguishable from static NPCs via `is_player_character: true` metadata flag. The original fresh character remains unchanged (portrait + description reusable across campaigns).

3. **Fresh Character Update**: Fresh character JSON's `realizations` array incremented with new campaign record:
   ```json
   {
     "campaign_id": "fantasy_quest_uuid",
     "realized_name": "Sir Eredin",
     "realized_at": "2026-02-14T09:15:00Z"
   }
   ```

**Token Counting**: Uses `tiktoken` tokenizer with `cl100k_base` encoding (compatible with sentence-transformers).

**Chunking Philosophy**: Authors decide granularity (minimal frontmatter = whole file = one chunk; detailed frontmatter = semantic unit per field = multiple chunks). Ingestion respects author intent while inferring sensible defaults.

##### Phase 4: Semantic Embedding Generation

Every chunk is processed through sentence-transformers to generate a 384-dimensional embedding vector:

```
for chunk in textChunks:
    embedding = sentenceTransformers.encode(chunk.text)  # → float[384]
    
    chunk.embedding = embedding
    chunk.embeddingModel = "sentence-transformers/all-MiniLM-L6-v2"
    chunk.embeddingVersion = "v1"
```

**Embedding Performance**: ~10-50ms per chunk. Batch processing of 100 chunks: ~500ms-1000ms.

**Total Embedding Time Estimates:**

| Campaign Size | Text Chunks | Embedding Time |
|---------------|-------------|----------------|
| Minimal (5 files) | ~20 chunks | <1 second |
| Small (20 files) | ~100 chunks | 1-2 seconds |
| Medium (100 files) | ~500 chunks | 5-10 seconds |
| Large (500 files) | ~2500 chunks | 25-50 seconds |

##### Phase 5: Binary Asset Metadata Extraction

Binary files (images, audio) remain on the filesystem but get metadata entries in ObjectBox:

**Images** (`.png`, `.jpg`, `.webp`):
- Extract dimensions, format, file size
- Parse filename for keywords (`npc_wizard.png` → keywords: `["npc", "wizard"]`)
- Check for `.keywords.txt` sidecar file
- *Optional*: Generate visual embeddings using CLIP or similar (not in MVP; future enhancement)

**Audio** (`.mp3`, `.ogg`, `.wav`, `.flac`):
- Extract duration, bitrate, format
- Parse filename for keywords (`ambient_forest.mp3` → keywords: `["ambient", "forest"]`)
- Check for `.keywords.txt` sidecar

**Metadata Storage:**

```json
{
  "assetId": "art_characters_wizard_001",
  "path": "art/characters/npc_wizard.png",
  "type": "image",
  "format": "png",
  "dimensions": {"width": 512, "height": 512},
  "sizeBytes": 145823,
  "keywords": ["npc", "wizard", "archmage", "merlin"],
  "generated": false,
  "entityLinks": ["characters/npcs/leeory"],
  "semanticRole": "character_portrait"
}
```

##### Phase 6: ObjectBox Storage & Indexing

All chunks and metadata are persisted to ObjectBox with optimized indexing:

**Chunk Entity Schema:**

```dart
@Entity()
class CampaignChunk {
  @Id()
  int id;
  
  String campaignId;
  String filePath;           // Original source file
  int chunkIndex;            // Position within file
  String chunkMethod;        // "paragraph", "field", "sentence"
  
  String content;            // The actual text
  int tokenCount;
  
  @HnswIndex(dimensions: 384, distanceType: HnswDistance.cosine)
  List<double> embedding;    // 384-dim vector
  
  // Metadata for filtering
  String category;           // "character", "plot", "lore", "mechanics"
  String? entityType;        // "npc", "player", "beat", "stat"
  String? entityId;          // "leeory", "health", "beat_001"
  String? contentType;       // "profile", "premise", "backstory"
  String semanticRole;       // "character_definition", "worldbuilding", etc.
  
  // Precedence metadata (Section 5.3.1)
  String sourceType;         // "frontmatter" or "prose"
  int weightTier;            // 1-6 per Context Precedence Hierarchy
  String fileOrigin;         // Relative path to source campaign file
  bool isPlayer;             // true for realized player character chunks
  
  // JSON structure (if applicable)
  String? jsonField;         // Field path within JSON: "personality.traits"
  String? jsonParent;        // Parent object path
  
  DateTime indexedAt;
}
```

**Binary Asset Entity Schema:**

```dart
@Entity()
class CampaignAsset {
  @Id()
  int id;
  
  String campaignId;
  String path;               // Absolute or relative filesystem path
  String type;               // "image", "audio", "video"
  String format;             // "png", "mp3", etc.
  
  int sizeBytes;
  Map<String, dynamic>? dimensions;
  
  List<String> keywords;
  List<String> entityLinks;  // References to related entities
  
  bool generated;
  Map<String, dynamic>? provenance;
  
  String semanticRole;
  DateTime indexedAt;
}
```

**Index Strategy:**

- **Vector index**: HNSW (Hierarchical Navigable Small World) for fast similarity search on embeddings
- **Text index**: B-tree on `category`, `entityType`, `semanticRole` for filtered queries
- **Entity index**: B-tree on `entityId` for direct entity lookups
- **Composite index**: `(campaignId, category, entityType)` for complex queries

##### Phase 7: Relationship Graph Construction

After all content is indexed, the system builds a relationship graph connecting related entities:

```
Relationships:
  "leeory" (NPC)
    ├─ profile.json → character_definition
    ├─ backstory.txt → character_lore
    ├─ art/characters/leeory.png → character_portrait
    └─ Referenced by: plot/beats.json (beat_002)

  "betrayal" (plot beat)
    ├─ Defined in: plot/beats.json
    ├─ References: ["leeory", "player"]
    └─ Related lore: lore/history/wizard_trials.txt
```

**Graph Construction**:
1. Parse explicit references (JSON `references` fields, cross-file links)
2. Compute semantic similarity between chunks (cosine similarity > 0.75 = related)
3. Extract entity mentions using keyword matching
4. Store as bidirectional edges in the graph

**Graph Storage**: ObjectBox relations or embedded relationship arrays within entities.

##### Phase 8: Query Interface Initialization

The ingestion pipeline creates queryable indices that skills can access at runtime:

**Semantic Search API:**

```dart
// Natural language query with filters
List<CampaignChunk> query = await persistence.semanticSearch(
  query: "mentors who test their students",
  filters: {
    'category': 'character',
    'entityType': 'npc'
  },
  topK: 5,
  minSimilarity: 0.7
);
```

**Exact Lookup API:**

```dart
// Direct entity retrieval
List<CampaignChunk> profile = await persistence.getEntity(
  entityId: 'leeory',
  contentType: 'profile'
);
```

**Cross-Reference API:**

```dart
// Find related content
List<CampaignChunk> related = await persistence.getRelated(
  entityId: 'leeory',
  relationTypes: ['lore', 'plot_beats']
);
```

##### Phase 9: Manifest & Constraint Loading

Special handling for campaign-level configuration:

- `manifest.json` parsed and cached in memory (not chunked/embedded)
- `world/setting.txt` loaded into LLM system context (also embedded for search)
- `world/constraints.md` ingested through the standard chunking pipeline — frontmatter assertions receive `weightTier: 1` (absolute), prose body receives `weightTier: 3` (authoritative), per Section 5.3.1

These provide **global context** that modifies all Narrator AI generation.

> **Design Note — `world/constraints.md`:** This file's *intent* is to declare binding world laws (e.g., "magic cannot resurrect the dead," "no faster-than-light travel"). However, it follows the same precedence rules as every other campaign file. If the entire file were elevated to Tier 1 regardless of structure, Phi-4 would have no way to distinguish hard constraints from explanatory prose — everything would claim absolute authority, rendering the precedence system meaningless. Campaign authors should place world laws in the YAML frontmatter block and use the prose body for context, examples, and rationale. See Section 5.3.1 for the full precedence hierarchy and the No-Shadow Rule.

##### Phase 10: Validation & Completion

Final validation ensures data integrity:

1. Check all entity references resolve (no orphaned links)
2. Verify provenance metadata for generated content
3. Validate JSON schemas match expectations
4. Confirm all binary assets exist on filesystem
5. Log statistics: chunk count, embedding time, index size

**Completion Signal**: Ingestion emits a completion event with metadata:

```json
{
  "campaignId": "wizard_runner_v1",
  "totalFiles": 7,
  "textChunks": 45,
  "binaryAssets": 3,
  "embeddingTimeMs": 1847,
  "totalIngestionMs": 2341,
  "indexSizeMB": 0.8,
  "status": "complete"
}
```

##### Runtime Query Behavior

During gameplay, when the Narrator AI or a skill needs campaign data:

**Query Example 1: Generate narrative about a wizard**

```
Narrator AI generates plan:
{
  "toolId": "recall",
  "input": {"query": "wizard mentor characteristics", "limit": 3}
}

Persistence layer executes semantic search:
  embeds query → [0.23, -0.41, 0.57, ...]
  computes cosine similarity against all campaign chunks
  filters by relevance > 0.7
  returns top 3 matches:
    1. Leeory's personality (similarity: 0.89)
    2. Leeory's motivations (similarity: 0.84)
    3. Lore snippet about wizard trials (similarity: 0.76)

Chunks injected into Narrator AI context → narration generated
```

**Query Example 2: Check available plot beats**

```
Plot progression skill queries:
  filters: {category: 'plot', contentType: 'beats'}
  
Returns all plot beat definitions with conditions, priorities, outcomes

Skill checks conditions against current session state:
  beat "betrayal" conditions:
    - scene_count between 10-20 ✓
    - requires_beat "first_trial" ✓
  → ELIGIBLE for triggering
```

**Query Example 3: Lookup NPC portrait**

```
UI needs to display Leeory's portrait:
  query: {entityId: 'leeory', type: 'image', semanticRole: 'portrait'}
  
Returns: art/characters/leeory.png metadata with filesystem path

UI loads image from disk, renders with character nameplate
```

##### Authoring Flexibility & Discovery

This universal ingestion approach enables maximum authoring flexibility:

**Authors can:**
- Add files anywhere in the hierarchy (auto-discovered)
- Use arbitrary subdirectories for organization (`lore/magic/schools/elemental/`)
- Mix formats (`.txt` for prose, `.json` for structured data)
- Omit sections entirely (sparse campaigns work, dense campaigns work)
- Override with explicit metadata (`keywords.txt` sidecars)

**The system ensures:**
- Everything becomes searchable through semantic similarity
- Metadata from paths provides filtering precision
- Relationships discovered automatically through content analysis
- Binary assets linked to entities via convention and keywords

##### Performance Targets

| Metric | Target | Notes |
|--------|--------|-------|
| Directory scan | <100ms | For campaigns with <1000 files |
| Chunking | <500ms | Typical campaign (~50 files) |
| Embedding generation | <10 seconds | Typical campaign (~100 chunks) |
| ObjectBox storage | <1 second | Including index creation |
| Total ingestion | <15 seconds | Cold start; <3 seconds if cached |
| Memory usage during ingestion | <200MB | Peak, excluding models |
| Indexed database size | ~1-2MB per 100 chunks | Compressed with embeddings |

##### Cache & Incremental Updates

- **First load**: Full ingestion pipeline (5-15 seconds)
- **Subsequent loads**: Check file modification timestamps
  - If no changes: Load cached index from ObjectBox (<1 second)
  - If changes detected: Re-ingest only modified files (2-5 seconds)
- **Incremental ingestion**: Authors can edit a single file, system re-indexes only that file

##### Sparse Campaign Enrichment

When a campaign contains fewer than 5 content files, the system offers optional AI enrichment:

1. Detects sparse data during ingestion
2. Prompts author: "Generate missing content with AI?"
3. If accepted, invokes on-device LLM (Ollama Gemma 2B, Llama 3.2 3B)
4. Generates: NPCs, plot beats, lore, world rules (marked `generated: true`)
5. Enriched content goes through full ingestion pipeline
6. Time to completion: ~10-30 seconds

**Enrichment is optional** — authors can start with minimal campaigns and manually add content iteratively.

#### 6.2.16 Campaign Validation

- Campaign structure is validated on load; errors are reported clearly with file paths and line numbers
- JSON files are validated against their schemas
- Orphaned asset references (files referenced but missing) generate warnings
- Validation completes within 2 seconds

### 6.3 Campaign Storage and Distribution

Campaigns are self-contained, portable directory structures that must persist across sessions and be shareable between devices. Narratoria implements platform-specific storage strategies while maintaining a unified campaign format that works everywhere.

#### 6.3.1 Storage Strategy Overview

**Core Principle**: *Campaigns are files, not cloud-locked data. Players own their stories.*

The application must:
1. Discover campaigns from multiple sources (built-in, downloaded, cloud-synced)
2. Load campaigns quickly on all platforms (desktop and mobile)
3. Enable sharing and backup without vendor lock-in
4. Respect platform-specific file system conventions
5. Support offline access after initial download

#### 6.3.2 Platform-Specific Storage Locations

**macOS / Windows / Linux (Desktop)**

Campaigns reside in a platform-standard location:

```
macOS:
  ~/Library/Application Support/Narratoria/campaigns/

Windows:
  %APPDATA%\Narratoria\campaigns\

Linux:
  ~/.local/share/narratoria/campaigns/
```

Structure within the campaigns directory:

```
campaigns/
├── builtin/                          # Shipped with app
│   ├── wizard_runner/
│   └── ancient_ruins/
├── downloaded/                       # User-downloaded campaigns
│   ├── campaign_by_author.zip        # Auto-extracted on first launch
│   └── another_campaign/
└── saves/                            # Session playthroughs
    ├── wizard_runner__playthrough_1/
    │   ├── manifest.json             # Links to source campaign
    │   ├── saves/
    │   │   ├── session_001.json
    │   │   └── session_002.json
    │   └── persistent_data/          # Memory, reputation, portraits
    │       ├── memories.db
    │       └── npc_state.json
    └── ancient_ruins__playthrough_1/
```

**iOS 17+**

iOS restricts file system access to app-specific directories. Narratoria uses:

```
iOS:
  App-Specific Directory:
    /var/mobile/Containers/Data/VolatileGroupContainer/
    {app_bundle_id}/Library/Caches/campaigns/  (temporary)
    
  iCloud Drive (opt-in):
    /var/mobile/Library/Mobile Documents/
    {cloud_container_id}/Documents/Narratoria/campaigns/
    (synced automatically when enabled)
    
  File Sharing (if enabled in Info.plist):
    Files app → [Your App Name] → Uploaded files
    (accessible via UIDocumentPickerViewController)
```

The app opens campaigns via the Files app:
1. User downloads `.narratoria` file or `.zip` archive
2. User opens with Narratoria (typically via Files app or email)
3. On launch, the app prompts: "Import this campaign?" with options:
   - **Import to App** (copies to app-specific directory; always available)
   - **Link to iCloud** (symbolic link pointing to iCloud Drive; synced automatically)
   - **Use via File Sharing** (reference to Files app location; requires file picker each time)

**Android**

Android 10+ enforces scoped storage. Narratoria uses:

```
Android:
  App-Specific Directory:
    /sdcard/Android/data/com.narratoria.app/files/campaigns/
    (Auto-cleaned on uninstall; no permissions required)
    
  Shared Downloads:
    /sdcard/Download/Narratoria/          (Common for user downloads)
    (Requires READ/WRITE_EXTERNAL_STORAGE permission on < Android 10)
    
  Storage Access Framework (Users choose location):
    getExternalFilesDir() API bypasses scoped storage for app-specific dirs
    Use Intent.ACTION_OPEN_DOCUMENT_TREE for user-chosen directories
```

The app discovers campaigns via:
1. **Automatic scan**: App-specific directory (no permissions)
2. **User import**: Storage Access Framework file picker
3. **Download handler**: Downloads app stores `.narratoria` files in app directory

#### 6.3.3 Campaign File Format and Portability

Campaigns ship as `.narratoria` packages (ZIP archives with a specific structure):

```
campaign_name.narratoria
├── campaign/                        # Extracted to campaigns/ on import
│   ├── manifest.json
│   ├── README.md
│   ├── world/
│   ├── characters/
│   ├── plot/
│   ├── lore/
│   ├── stats/
│   ├── items/
│   ├── art/
│   └── music/
└── version.txt                      # metadata: format_version, app_version_min
```

**Portability:** Native campaign directories (uncompressed) are also supported. A user can:
- Download a `.zip` file from anywhere
- Rename it to `.narratoria`
- Open it with Narratoria
- The app automatically extracts and validates

#### 6.3.4 iCloud Synchronization (iOS Only)

**When enabled by user** (via settings), Narratoria automatically syncs campaigns and save data to iCloud Drive:

1. **Campaign Sync**:
   - Campaigns stored in iCloud Drive are automatically downloaded to device when needed
   - Modifications are uploaded asynchronously
   - Conflict resolution: Last-write-wins with user notification

2. **Save Data Sync**:
   - Session saves (playthroughs) in iCloud are synced across all user devices
   - Player can resume on iPhone where they left off on iPad
   - Uses CloudKit or File Provider APIs (depending on iOS version)

3. **Data Structure**:
   ```
   iCloud Drive/Narratoria/
   ├── campaigns/                    # Synced across devices
   │   ├── campaign_1/
   │   └── campaign_2/
   └── saves/                        # Synced across devices
       ├── playthrough_1/
       └── playthrough_2/
   ```

**Implementation Details:**
- Use `FileManager.url(forUbiquityContainerIdentifier:)` to get iCloud container
- Use `NSFileCoordinator` for atomic file operations
- Monitor `NSUbiquityIdentityDidChangeNotification` to handle account changes
- Graceful fallback: If iCloud unavailable, continue with local storage

#### 6.3.5 Google Play Games Backup (Android Only)

For Android, optional cloud backup integrates with Google Play Games Services:

1. **Automated Backup** (if user enables):
   - Session saves uploaded to Google Play Games after each scene save (async)
   - Playthrough metadata cached locally for quick access
   - Automatic restoration on app reinstall

2. **Data Structure**:
   - `GameData`: JSON serialization of session saves (max 1MB per snapshot)
   - `SaveGame`: Binary or textual snapshot (up to 3MB per entry)

3. **Conflict Resolution**:
   - Server timestamp wins if conflicts occur
   - Local save takes precedence if offline and later synced

#### 6.3.6 Campaign Discovery and Loading

##### Campaign Sources

The application discovers playable campaigns from:

1. **Built-in Campaigns** (App Bundle)
   - Shipped with application binary
   - Always available, never deleted
   - Appear first in campaign list with "Official" badge

2. **Downloaded Campaigns** (App-specific directory)
   - User downloaded or imported via file picker
   - Extraction happens transparently on first import
   - Appear as "User Imported" in campaign list
   - Can be deleted (uninstall-safe); persistent across app updates

3. **iCloud Synced Campaigns** (iOS only)
   - User opted-in to iCloud sync
   - Only appear on device if file-provider has downloaded them (check via `isUbiquitousItem`)
   - Download on-demand with progress indicator
   - Appear as "Cloud" in campaign list with sync status badge

4. **Playthrough Saves** (Campaign instances)
   - Previous playthroughs grouped by source campaign
   - Display as "Resume" entries in campaign list
   - Include thumbnail preview and play time

##### Discovery Algorithm

```
function discoverCampaigns():
    campaigns = []
    
    # Tier 1: Built-in campaigns (app bundle)
    for campaign_dir in bundled_campaigns/:
        if validate(manifest):
            campaigns.append({
                source: "builtin",
                path: campaign_dir,
                badge: "Official"
            })
    
    # Tier 2: Downloaded/imported campaigns (app-specific directory)
    for campaign_dir in app_documents/campaigns/:
        if validate(manifest):
            campaigns.append({
                source: "downloaded",
                path: campaign_dir,
                badge: "User Imported"
            })
    
    # Tier 3: iCloud campaigns (iOS only)
    if icloud_enabled():
        for campaign_dir in icloud_drive/campaigns/:
            if isUbiquitousItem(campaign_dir):
                campaigns.append({
                    source: "icloud",
                    path: campaign_dir,
                    status: "downloaded" | "downloading" | "not_downloaded",
                    badge: "☁️ Cloud"
                })
    
    # Tier 4: Recent playthroughs (grouped by campaign)
    for playthrough in saves/:
        source_campaign = load_manifest(playthrough/manifest.json)
        campaigns.append({
            source: "playthrough",
            parent_campaign: source_campaign.id,
            path: playthrough,
            type: "resume",
            last_played: playthrough.timestamp
        })
    
    return sort_by_last_played(campaigns)
```

##### Campaign Loading Performance

| Operation | Target | Platform | Notes |
|-----------|--------|----------|-------|
| Discover campaigns | <500ms | All | Scan directories + validate manifests |
| Load campaign metadata | <100ms | All | Parse manifest.json only |
| Full campaign ingestion | <15s | All | All files chunked + embedded |
| iCloud download | <5s/MB | iOS | Depends on connection; shows progress |
| Resume playthrough | <2s | All | Load cached index from session save |

#### 6.3.7 Campaign Sharing and Export

Users can export playthroughs and campaigns to share with others:

```
Campaign Export Options:
├─ Minimal (Campaign only)
│  ├── manifest.json + content files
│  ├── Size: 1-500MB (depending on art/music)
│  └── Format: .narratorio (single ZIP)
│
├─ Full (Campaign + Playthrough)
│  ├── Campaign + session saves + progress
│  ├── Size: 1-1000MB
│  └── Format: .narratorio-full
│
└─ Shareable Link (Cloud-hosted)
   ├── Campaign uploaded to server
   ├── Short URL generated (one-time or permanent)
   └── Recipients click link → download + import
```

This is implemented via:
- **macOS/Windows/Linux**: Share via Files app or drag-and-drop
- **iOS**: Share via Files app, AirDrop, or iCloud Drive link
- **Android**: Share via Android Share sheet, Drive, or email attachment

#### 6.3.8 Offline Capability

After initial download, campaigns operate entirely offline:

✓ Campaign loading — Local storage only, no network required
✓ Save/resume — Local database, no cloud sync required
✓ New scenes — Phi-4 inference on-device, no API calls
✓ Memory retrieval — Embedded database queries, no server needed
✓ Asset display — Cached images, no re-downloads

**iCloud sync is fully optional** — disable it in settings to guarantee offline-only operation.

#### 6.3.9 Migration Strategy (Desktop to Mobile)

Users can move campaigns and playthroughs from a desktop to mobile device:

**Desktop to iOS:**
1. Export playthrough as `.narratorio-full` from macOS/Windows/Linux
2. AirDrop to iPad, or upload to iCloud Drive, or email
3. Open in Narratoria app → "Import Campaign" confirmation
4. Session resumes on iPad with all progress intact

**Desktop to Android:**
1. Export playthrough as `.narratorio-full`
2. Upload to Google Drive or download link
3. Open on Android device → Narratoria app handles import
4. Or use Android File Transfer to drag directly into app directory

**Reverse migration:** iOS/Android playthroughs can export and sync to desktop for backup or continuation.

---

### 6.4 Session State Management

Session state is maintained as an in-memory JSON structure updated by `state_patch` events through deep merge semantics. State supports dot-notation path access (e.g., `"inventory.torch.lit"`) for convenient querying. A copy mechanism enables snapshot-based undo or branching.

The state model has three tiers:

1. **Session State (transient)**: In-memory JSON updated by `state_patch` events. Lost when the application exits.
2. **Skill Private Data**: Per-skill `data/` directories for caches and working files. Persists across restarts.
3. **Shared Persistence**: Embedded database storage for cross-skill data (memories, reputation, perception, portraits). ACID-compliant, persists across restarts.

---

## 7. Runtime Execution

### 7.1 Scene Pipeline

The narrative engine executes campaigns by managing the scene loop and orchestrating skills. The pipeline runs continuously during gameplay:

**Choice → Plan Generation → Plan Execution → Aggregate Results → Display Prose and New Choices**

1. **Player selects a choice** from the 3-4 displayed options
2. **Plan Generator analyzes context**: player choice, session state, available skills, campaign constraints (world constraints, NPC profiles, plot beats), and retrieved memories
3. **Plan JSON is generated**: Phi-4 or Phi-4-mini decides which skills to invoke, what parameters to pass, and what data to retrieve
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

## 8. Design Rationale

### 8.1 Why Protocol-First?

**Decision**: All tool communication via NDJSON over stdin/stdout pipes.

**Alternatives Considered**: Shared memory, gRPC, REST APIs, FFI bindings.

**Rationale**: NDJSON over pipes is the simplest possible IPC mechanism. It requires no dependencies, works on every platform, is trivially testable (pipe a file to stdin, validate stdout), and enables language independence. A skill written in Python, Rust, or Go conforms to the same protocol as one written in any other language.

The trade-off is performance—process launch overhead is higher than in-process calls. But skill scripts typically run for hundreds of milliseconds to seconds (LLM inference, image generation, database queries), so process launch latency (<50ms) is negligible relative to execution time.

### 8.2 Why Bounded Retry?

**Decision**: Maximum 5 plan attempts, 3 retries per tool, exponential backoff.

**Alternatives Considered**: Unbounded retry, no retry (fail fast), manual retry.

**Rationale**: LLM-based systems fail in ways that traditional retry logic doesn't handle: the model might consistently generate invalid JSON, repeatedly select broken skills, or enter degenerate plan loops. Unbounded retry risks infinite loops that freeze the application. Fail-fast frustrates players who would benefit from a single retry. The bounded approach combines automatic recovery (most failures are transient) with guaranteed termination (the system always eventually responds).

The key innovation is **skill disabling during replanning**: when a skill fails, it's removed from the available skill set for subsequent plans, forcing the Narrator AI to find alternative approaches.

### 8.3 Why On-Device AI?

**Decision**: Phi-4 or Phi-4-mini (3.8B-14B) + sentence-transformers, both running locally. Phi-4 offers significant improvements in reasoning, instruction-following, and plan generation quality compared to Phi-3.5.

**Alternatives Considered**: Cloud APIs (OpenAI, Claude), hybrid cloud/local, larger local models.

**Rationale**: Privacy is the primary driver—interactive fiction sessions contain intimate creative expression that players may not want transmitted to third parties. Secondary drivers include offline capability (games should work on airplanes), predictable cost (no per-token API fees), and latency (local inference eliminates network round-trips).

The 3.8B parameter model is the sweet spot for mobile devices with 8GB RAM: large enough for coherent multi-paragraph prose and structured JSON generation, small enough to run in-process with acceptable latency (<3 seconds per scene).

### 8.4 Why No Free-Text Input?

**Decision**: Players select from AI-generated choices only.

**Alternatives Considered**: Free-text input (like AI Dungeon), hybrid (choices + free text).

**Rationale**: Free-text input creates the "parser doesn't understand" problem—players type actions the AI can't meaningfully handle, breaking immersion. Choice-based input ensures every player action is contextually valid and narratively coherent. The AI generates options that respect character abilities, world constraints, and narrative momentum.

This also enables measurable quality: we can verify that 80% of choices reference past events (SC-002), which is impossible to measure with arbitrary free-text input.

### 8.5 Why ObjectBox?

**Decision**: ObjectBox as the current persistence backend.

**Alternatives Considered**: SQLite, raw JSON files, Hive, Isar.

**Rationale**: ObjectBox provides native vector search capability alongside traditional CRUD operations, which is essential for the semantic memory system. SQLite with extension modules could achieve similar results but with more complexity. ObjectBox's in-process architecture aligns with the "no network" constraint, and its client SDK provides ergonomic integration. This is a current technology choice that may evolve as alternatives mature.

### 8.6 Why Campaign Format Creeds?

**Decision**: Mandatory provenance tracking and transparency metadata.

**Alternatives Considered**: No tracking (treat all content equally), opt-in tracking.

**Rationale**: As AI content generation becomes ubiquitous, distinguishing human-authored from AI-generated content becomes critical for creative integrity, attribution, and trust. By making provenance tracking mandatory and enforced at the data layer (store operation fails without valid provenance for generated content), the system prevents accidental distribution of unlabeled AI content.

This is especially important for shared campaigns: when one author shares a campaign with another, the recipient knows exactly which elements are human-crafted and which are AI-generated.

---

## 9. Open Design Decisions

### 9.1 Multiplayer Coordination

How should Narratoria handle multiple players in a shared narrative? Options include:
- Shared session state with turn-based choice selection
- Independent parallel narratives that occasionally intersect
- GM-player model where one player acts as narrator

Currently out of scope (single-player focus), but the protocol-boundary architecture is extensible to multiplayer: skill scripts don't know how many players exist.

### 9.2 Voice Narration

Should the system support text-to-speech for narration? The on-device constraint limits model options, but small TTS models (e.g., Piper, Coqui) are becoming viable. This would enhance accessibility and immersion.

### 9.3 Campaign Marketplace

How should campaigns be distributed? A community repository with rating, search, and verification could accelerate the content ecosystem. Provenance tracking already supports this (campaigns declare AI content transparently).

### 9.4 Model Upgrade Migration

When embedding models improve, how should old embeddings be handled? Current plan: old embeddings remain; new memories use the new model; query vectors use the current model. This may cause degraded relevance for old memories. A background re-embedding process could resolve this but adds complexity.

### 9.5 Cancellation and Interruption

The current architecture has no cancellation mechanism for in-flight skill scripts. If a player wants to undo a choice or the system needs to interrupt a long-running tool, there's no protocol-level support. Future options: SIGINT handling, stdin-based cancel messages, or timeout-only termination.

### 9.6 Tool Versioning and Capability Negotiation

Currently, tools declare a static `version: "0"` and there's no mechanism for tools to advertise their capabilities to the runtime. As skills evolve, version negotiation may become necessary to maintain backward compatibility.

### 9.7 Network Protocols for Remote Tools

All tools currently run as local OS processes. For computationally expensive operations (large model inference, high-resolution image generation), remote tool execution over network protocols could extend capability—at the cost of the privacy and offline guarantees.

### 9.8 Authentication and Sandboxing

Tools currently have full filesystem access. For third-party skills downloaded from untrusted sources, sandboxing (filesystem restrictions, network limitations, resource caps) would improve security. This requires platform-specific implementation.

---

## 10. Success Criteria & Metrics

### 10.1 Core System Metrics

| ID | Metric | Target | Test Method |
|----|--------|--------|-------------|
| SC-001 | Plan generation speed | Valid Plan JSON for 95% of inputs within 5 seconds | Automated: time plan generation for 100+ diverse inputs |
| SC-002 | Skill selection accuracy | Correctly selects relevant skills for 90% of actions | Acceptance tests with known-correct skill mappings |
| SC-003 | Skills settings usability | Configure any core skill in under 2 minutes | Timed user task completion |
| SC-004 | Skill discovery reliability | All valid skills loaded from `skills/` without errors | Startup validation with diverse skill sets |
| SC-005 | Script execution success | 99% of invocations complete within timeout | Automated: run 1000+ script invocations, measure success rate |
| SC-006 | Graceful degradation | App continues without crash on skill failure; helpful error message | Fault injection tests |

### 10.2 Skill Performance Metrics

| ID | Metric | Target | Test Method |
|----|--------|--------|-------------|
| SC-007 | Memory search speed | <500ms for databases with 1000+ events | Benchmark with synthetic data |
| SC-008 | Reputation query speed | <100ms per query | Benchmark with multiple factions |
| SC-009 | Storyteller fallback | Local LLM fallback within 10 seconds | Network failure simulation |
| SC-010 | Skill installation ease | Install by directory copy + restart, no code changes | Manual test with new skill |
| SC-011 | Config persistence | Config survives restarts; correctly loaded on next invocation | Automated: save, restart, validate |
| SC-012 | Plan failure handling | Logs error, marks step failed, continues remaining steps | Fault injection in plan execution |

### 10.3 Advanced Skill Metrics

| ID | Metric | Target | Test Method |
|----|--------|--------|-------------|
| SC-013 | Choice generation speed | 3-4 options within 3 seconds for 95% of decision points | Automated timing |
| SC-014 | Choice stat relevance | ≥70% of choices mention stat-relevant keywords | (1) Verify Phi-4 prompt template includes `{player_stats}` injection, (2) Automated keyword grep: 20+ choices with stat-variant inputs, verify ≥70% mention stat-relevant keywords, (3) Code review: confirm prompt structures stats for LLM. Pass if all three checks succeed. |
| SC-015 | Portrait generation speed | <15 seconds for 90% of requests (local generation) | Automated timing |
| SC-016 | Portrait cache accuracy | Cached portrait retrieved in 95% of character reappearances | Semantic matching validation |
| SC-017 | NPC perception init speed | <100ms, informed by faction reputation | Benchmark with new NPCs |
| SC-018 | Dice modifier accuracy | Correct perception-based modifiers in 100% of applicable rolls | Deterministic test cases |
| SC-019 | Cross-restart persistence | 100% data integrity across restarts | Save, kill, restart, validate |
| SC-020 | Session stability | 30-minute play session with all skills, no crashes | End-to-end integration test |
| SC-021 | Cross-factor choices | Choices respect both reputation AND perception when applicable | Scenario tests with both factors |
| SC-022 | Portrait cross-session | Images associated correctly across sessions | Multi-session portrait retrieval test |

### 10.4 Persistence Metrics

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

### 10.5 Narrative Engine Metrics

| ID | Metric | Target | Test Method |
|----|--------|--------|-------------|
| SC-033 | Scene transition speed | <3 seconds on 8GB RAM device | Automated timing on target hardware |
| SC-034 | Memory-driven choices | 80%+ reference past events | Automated entity extraction from choice text, cross-referenced against stored memory events in the persistence layer via embedding similarity match. Pass if ≥80% of sampled choices (50+ choices across 3 campaigns) retrieve ≥1 matching memory event with similarity score ≥0.7. |
| SC-035 | Plot beat timing | Trigger within 2 scenes of conditions met, 95% of cases | Automated condition monitoring |
| SC-036 | NPC sentiment accuracy | 95% of interactions reflect correct sentiment | Dialogue sentiment analysis |
| SC-037 | Long-session coherence | Coherent narrative across 100+ consecutive choices | End-to-end session test |
| SC-038 | Episodic memory surfacing | Relevant episodic memories appear 100% of applicable situations | Targeted scenario tests |

> **Note on SC-003 (Removed)**: Previously stated as "Players report feeling the AI remembers their choices in 90% of post-session surveys." Recognized as an emergent property rather than a formal requirement—when SC-034 (memory-driven choices) is achieved, players naturally feel the system remembers because it demonstrably references past events. No player survey required.

### 10.6 Campaign Format Metrics

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

## 11. Appendices

### A. Glossary

See Section 1.5 (Terminology) for the complete glossary of terms used throughout this document.

### B. Contracts Index

All machine-readable contracts are maintained as JSON Schema files:

| Contract | Location | Validates |
|----------|----------|-----------|
| Plan JSON Schema | `contracts/plan-json.schema.json` | Plan documents from Narrator AI |
| Execution Result Schema | `contracts/execution-result.schema.json` | Plan execution traces |
| SKILL.md Frontmatter Schema | `contracts/skill-frontmatter.schema.json` | `SKILL.md` YAML frontmatter (including `x-narratoria` extensions) |
| Config Schema Meta-Schema | `contracts/config-schema-meta.schema.json` | Skill `config-schema.json` files (Narratoria extension) |
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
Layer 5: UI Implementation ───────────┤
    ↑                                  ↑
Layer 7: Campaign Format ─────────────┤
    ↑                                  ↑
Layer 8: Narrative Engine ────────────┘
```

**Reading Order for Understanding:**
1. Start with Layer 1 (Tool Protocol) — foundational communication
2. Read Layers 2 and 3 together — plan execution and skills are co-dependent
3. Read Layers 4 and 6 together — skill interfaces and storage implementation are co-dependent
4. Read Layer 5 — UI implementation ties everything together
5. Read Layers 7 and 8 — campaign content and runtime execution

### E. Future Roadmap

**Phase 1 (MVP)**: Core protocol, plan execution, 4 core skills (storyteller, dice-roller, memory, reputation), basic UI, persistence layer, minimal campaign support.

**Phase 2 (Advanced Skills)**: Player Choices skill, Character Portraits skill, NPC Perception skill, skill integration (cross-skill plans), campaign enrichment pipeline.

**Phase 3 (Polish)**: Error recovery UI, performance optimization, large campaign support, comprehensive testing against success criteria.

**Phase 4 (Ecosystem)**: Campaign authoring tools, skill development SDK, community campaign sharing, documentation site.

**Beyond**: Multiplayer support, voice narration, remote tool execution, model upgrade migration, advanced sandboxing.

