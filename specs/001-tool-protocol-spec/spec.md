# Specification 001: Tool Protocol (Version 0.0.1)

**Status:** Draft  
**Audience:** Narratoria Core Runtime, Tool Developers  
**Scope:** Defines the communication protocol between external tools and the Narratoria application.  
**Version:** 0.0.1 (protocol envelope property `version`: "0")

## Clarifications

### Session 2026-01-24

- Q: How should the Narrator AI Stub be implemented in the MVP? → A: In-process Dart function/class that returns hard-coded Plan JSON for known prompts
- Q: What merge semantics should state_patch events use? → A: Deep merge (nested objects merged recursively; arrays replaced)
- Q: Should independent tools continue when a sibling tool fails? → A: Continue automatically (independent tools proceed unless they also fail)
- Q: Which ui_event types must the MVP implement? → A: narrative_choice only (display choice buttons/list from payload)
- Q: Who creates asset files and determines paths? → A: Tools generate and write files independently; provide absolute paths in asset events

---

## Glossary

- **Session State**: The runtime data model containing narrative state accumulated from `state_patch` events (e.g., `{"inventory": {"torch": {"lit": true}}}`)
- **Narrative State Panel**: UI component displaying session state as expandable tree view with JSON inspector
- **Tool Execution Panel**: UI component (widget) displaying active tool invocations with logs, progress, and completion status
- **Tools View**: MainScreen navigation destination containing the Tool Execution Panel widget
- **Narrator AI Stub**: Simplified in-process implementation that converts player prompts to Plan JSON using hard-coded mappings (not a test mock; intended for MVP functionality before LLM integration)
- **Plan JSON**: Structured document produced by narrator AI describing which tools to execute, their inputs, dependencies, and execution strategy (parallel/sequential)
- **Deep Merge**: State patch merge semantics where nested objects are merged recursively, arrays replaced entirely, and null values remove keys

---

## 1. Purpose

This specification establishes:

1. **Tool Protocol**: A minimal, extensible, language-agnostic protocol for external tool processes communicating with the Narratoria runtime. Tools may be authored in any programming language and executed as independent OS processes. The protocol enables:
   - state updates
   - asset generation
   - UI event requests
   - structured errors
   - progress logs
   - streaming incremental output

2. **Client UI Requirements**: Design patterns and UI components for the Narratoria Flutter client to execute tools and present results to players.

3. **Player Interaction Flow**: The mechanism by which player input (natural language prompts) is converted into executable plans that invoke tools.

This protocol ensures backward and forward compatibility between Narratoria and tools, while maintaining a testable, composable UI architecture.

---

## 2. Transport Model

### 2.1 Invocation

Narratoria launches a tool as an external process using platform-native mechanisms (fork/exec on Unix, CreateProcess on Windows). Narratoria MAY supply arguments and/or JSON input via stdin.

### 2.2 Tool Output

Tools MUST write output to stdout using NDJSON (one JSON object per line). Tools MAY write additional human-readable diagnostics to stderr without restriction.

### 2.3 Encoding

All stdout content MUST be UTF-8 encoded text with Unix-style newlines (\n). Each output line MUST represent a complete, valid JSON object.

---

## 3. Event Envelope

Every JSON object emitted by a tool MUST include:

```
{
  "version": "0",
  "type": "<event-type>",
  ...
}
```

**Required fields**
- `version`: string. For this spec, MUST equal "0".
- `type`: string. One of: `log`, `state_patch`, `asset`, `ui_event`, `error`, `done`.

**Optional envelope fields**
- `requestId`: string. Opaque identifier provided by Narratoria.
- `timestamp`: string. ISO-8601 datetime.
- Additional fields MAY be included and MUST be ignored unless otherwise specified.

---

## 4. Event Types

### 4.1 log Event

**Purpose:** communicate progress or diagnostic information.

**Structure:**

```
{
  "version": "0",
  "type": "log",
  "level": "debug" | "info" | "warn" | "error",
  "message": "<string>",
  "fields": { ...optional key/value data... }
}
```

Narratoria requirements:
- MUST NOT treat log events as errors.
- SHOULD display logs in a developer or debug console.

### 4.2 state_patch Event

**Purpose:** update the Narratoria session state.

**Structure:**

```
{
  "version": "0",
  "type": "state_patch",
  "patch": { ...arbitrary JSON object... }
}
```

Rules:
- `patch` MUST be a JSON object.
- Narratoria MUST merge this patch into session state using **deep merge** semantics:
  - Nested objects are merged recursively (keys added/updated, not replaced entirely)
  - Arrays are replaced entirely (not merged element-by-element)
  - Null values remove keys from the state tree
  - Example: existing state `{"a": {"b": 1, "c": 2}}` + patch `{"a": {"c": 3, "d": 4}}` → `{"a": {"b": 1, "c": 3, "d": 4}}`
- Tools SHOULD only express state changes and MUST NOT assume implementation details beyond deep merge behavior.

### 4.3 asset Event

**Purpose:** notify Narratoria that a new asset has been created.

**Structure:**

```
{
  "version": "0",
  "type": "asset",
  "assetId": "<string>",
  "kind": "<string>",
  "mediaType": "<MIME-type>",
  "path": "<filesystem-path>",
  "metadata": { ...optional... }
}
```

Rules:
- `assetId` MUST be unique within the tool invocation.
- `kind` SHOULD describe a broad category (e.g., "image", "audio", "video", "model").
- `mediaType` MUST be a valid MIME type string.
- `path` MUST refer to a file readable by Narratoria.
- **Tools are responsible for creating and writing asset files** before emitting the asset event. Tools SHOULD use platform-standard temporary directories (e.g., `/tmp` on Unix, `%TEMP%` on Windows) or other writable locations.
- `path` MUST be an absolute path or a path resolvable by Narratoria's current working directory.
- `metadata` MAY include arbitrary details (width, height, framerate, camera data, etc.).

Narratoria MUST:
- Register the asset.
- Defer rendering until referenced by UI logic or plan execution.
- Display degraded or placeholder UI for unsupported `mediaType` values.

### 4.4 ui_event Event

**Purpose:** request a UI action within Narratoria.

**Structure:**

```
{
  "version": "0",
  "type": "ui_event",
  "event": "<string>",
  "payload": { ...event-specific parameters... }
}
```

Rules:
- `event` MUST be a string identifying an action known to Narratoria.
- `payload` MAY be omitted or MAY contain arbitrary event parameters.
- Tools MUST NOT assume that Narratoria will support all events.

Narratoria MUST:
- Dispatch supported events to their handlers.
- For MVP (Spec 001), the `narrative_choice` event MUST be supported:
  - Payload format: `{"choices": ["string", "string", ...]}`
  - Display as clickable buttons or list in Story View
  - Player selection becomes next prompt
- Gracefully degrade unsupported events via placeholder messages with event name and payload details.

### 4.5 error Event

**Purpose:** represent a structured error detected by the tool.

**Structure:**

```
{
  "version": "0",
  "type": "error",
  "errorCode": "<string>",
  "errorMessage": "<string>",
  "details": { ...optional structured data... }
}
```

Rules:
- Error events MUST NOT terminate the invocation by themselves.
- The tool SHOULD emit a subsequent `done` event with `ok`: false.

Narratoria MUST:
- Surface error information in logs and UI (as appropriate).
- Continue to process events until `done` or process exit.

### 4.6 done Event

**Purpose:** signal completion of the entire tool invocation.

**Structure:**

```
{
  "version": "0",
  "type": "done",
  "ok": <true | false>,
  "summary": "<string, optional>"
}
```

Rules:
- Each invocation MUST end with exactly one `done` event OR a premature process termination.
- `ok: true` indicates successful logical completion.
- `ok: false` indicates controlled logical failure.
- Tools SHOULD avoid emitting events after `done`.

Narratoria MUST:
- Stop accepting further events from the tool after processing `done`.
- Treat missing `done` plus a non-zero exit code as a protocol-level failure.

---

## 5. Protocol Semantics

### 5.1 Event Ordering

Tools MAY emit events in any order. Typical flow: logs -> assets -> ui events -> state patches -> done.

### 5.2 Unknown Event Types

If `type` is unknown, Narratoria MUST treat it as a protocol error and terminate processing for the invocation.

### 5.3 Process Exit
- Exit code 0: protocol intact. `done.ok` determines logical success.
- Exit code != 0: protocol-level failure, regardless of any emitted events.

### 5.4 Streaming Behavior

Tools MAY produce incremental events during long-running operations. Narratoria MUST process each JSON object line-by-line as received.

---

## 6. Tool Input (Optional)

Narratoria MAY send structured input to the tool via stdin in JSON format:

```
{
  "requestId": "<string>",
  "tool": "<toolName>",
  "operation": "<string>",
  "input": { ...arbitrary data... }
}
```

Tools MAY ignore any fields they do not recognize.

---

## 7. File System Layout (Guidance Only)

Tools have full autonomy over asset file creation and storage. Tools MAY write assets to:
- platform-standard temporary directories (e.g., `/tmp`, `%TEMP%`)
- application data directories
- any writable location accessible to the Narratoria process

Tools MUST provide absolute paths (or paths resolvable from Narratoria's working directory) in asset events.

Narrratoria MUST validate that asset files exist and are readable before registering them. Narratoria MAY copy or move assets to managed storage but MUST NOT require tools to use specific output directories in Spec 001.

Future specifications may define managed asset directories or content streaming mechanisms.

---

## 8. Forward Compatibility Requirements

Tools MUST:
- include a stable `version`: "0" field
- only use defined `type` strings
- tolerate additional fields from future Narratoria versions

Narratoria MUST:
- ignore unknown fields within known event types
- not require any event types other than `done` for correctness
- degrade gracefully on unknown MIME types or unknown `ui_event` names

---

## 9. Minimum Viable Tool Example

A compliant minimal tool:

```
{"version":"0","type":"log","level":"info","message":"Starting"}
{"version":"0","type":"state_patch","patch":{"flags":{"torchLit":true}}}
{"version":"0","type":"done","ok":true,"summary":"Torch lit."}
```

---

## 10. Non-Goals for Spec 001

Spec 001 does NOT define:
- UI rendering specifics beyond the requirements in section 12 (widget implementation details remain implementation-specific)
- Shader or 3D scene protocols
- Tool discovery or installation mechanisms (tools paths must be known in advance)
- Narrator AI implementation or LLM integration (Plan JSON generation is external)
- Authentication or sandboxing
- Threading, cancellation, or lifecycle semantics within tools
- Schema of Narratoria internal state (tools use opaque state_patch objects)
- Network protocols for remote tool execution
- Tool versioning or capability negotiation

These will be defined in later specifications.

---

## 11. Versioning

The protocol `version` string in the event envelope MUST remain "0" until Spec 002 changes it. Spec 001 establishes baseline compatibility. Future specs MUST commit to backwards compatibility unless a major version change is declared.

---

## 12. Client UI Requirements

### 12.1 Design System

Narratoria MUST use **Material Design 3** for its Flutter UI. This provides:
- First-class Flutter support with extensive widget library
- Cross-platform consistency (macOS, Windows, Linux)
- Excellent testability via `flutter_test`
- Mature theming and customization

### 12.2 Core UI Components

The Narratoria client MUST implement these components:

#### Tool Execution Panel
Displays active tool invocations with real-time progress:
- Tool name and current status
- Streaming log output (from `log` events)
- Progress indicators for long-running operations
- Error display (from `error` events)
- Completion status (from `done` events)

#### Asset Gallery
Displays assets generated by tools (from `asset` events):
- Image preview with metadata
- Audio player controls
- Video player controls
- **Graceful degradation**: Placeholder cards for unsupported `mediaType` values showing asset details

#### Narrative State Panel
Displays current session state:
- Expandable tree view of state objects
- State changes highlighted (from `state_patch` events)
- JSON inspector for debugging

#### Player Input Field
Natural language textarea for player prompts:
- Multiline text input
- Send button to submit prompt
- Visual feedback during processing

### 12.3 UI Layout

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
│                   │  │  Player Input    │  │
│                   │  │  [text field]    │  │
│                   │  └──────────────────┘  │
└─────────────────────────────────────────────┘
```

### 12.4 Theming

The client MUST implement a dark-themed Material Design 3 scheme suitable for immersive storytelling:

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

---

## 13. Player Interaction Flow

### 13.1 Overview

Players interact with Narratoria by submitting natural language prompts (e.g., "I light the torch" or "I examine the mysterious door"). The narrator AI converts these prompts into executable plans that invoke tools via the protocol defined in sections 2-11.

### 13.2 Flow Diagram

```
┌──────────────┐
│ Player types │
│   prompt     │
└──────┬───────┘
       │
       ▼
┌──────────────┐
│ Narrator AI  │ (external LLM/agent service)
│ analyzes     │
│   prompt     │
└──────┬───────┘
       │
       ▼
┌──────────────┐
│  Plan JSON   │ {tools: [...], parallel: bool}
└──────┬───────┘
       │
       ▼
┌──────────────┐
│ Narratoria   │ executes tools per plan
│  Runtime     │ collects events via protocol
└──────┬───────┘
       │
       ▼
┌──────────────┐
│ UI updates   │ display results, assets, state
│   (Material) │
└──────────────┘
```

### 13.3 Plan JSON Schema (Extended)

The narrator AI MUST produce a **Plan JSON** document with this structure:

```json
{
  "requestId": "<uuid>",
  "narrative": "<string, optional narrator response>",
  "tools": [
    {
      "toolId": "<string>",
      "toolPath": "<filesystem-path-to-executable>",
      "input": { ...arbitrary JSON... },
      "dependencies": ["<toolId>", ...],
      "required": <boolean, default true>,
      "async": <boolean, default false>,
      "retryPolicy": {
        "maxRetries": <integer, default 3>,
        "backoffMs": <integer, default 100>
      }
    }
  ],
  "parallel": <boolean, default false>,
  "disabledSkills": ["<skillName>", ...],
  "metadata": {
    "generationAttempt": <integer>,
    "parentPlanId": "<uuid, or null>"
  }
}
```

**Fields**:
- `requestId`: Unique identifier for this plan execution
- `narrative`: Optional narrative text to display before or during tool execution
- `tools`: Array of tool invocation descriptors
  - `toolId`: Unique ID for this tool within the plan (for dependency tracking)
  - `toolPath`: Absolute or relative path to the tool executable
  - `input`: JSON object passed to the tool via stdin (as described in section 6)
  - `dependencies`: Array of `toolId` values that must complete before this tool runs
  - `required`: (NEW) If true, tool failure aborts dependent tools and plan execution fails; if false, tool failure is non-blocking and dependent tools may still execute
  - `async`: (NEW) If true, tool may run in parallel with unrelated tools and siblings (respecting `dependencies`); if false, tool runs sequentially
  - `retryPolicy`: (NEW) Configures retry behavior for this specific tool
    - `maxRetries`: Maximum retry attempts before marking tool as failed (default 3)
    - `backoffMs`: Milliseconds between retries with exponential backoff
- `parallel`: If true and dependencies allow, tools run concurrently; if false, tools run sequentially
- `disabledSkills`: (NEW) Array of skill names that failed in previous execution attempts (plan generator MUST NOT select these skills)
- `metadata`: (NEW) Plan metadata for debugging and replan tracking
  - `generationAttempt`: Which attempt this plan represents (1, 2, 3...)
  - `parentPlanId`: If this is a replan, the UUID of the previous plan that failed

### 13.4 Plan Execution Rules (Extended)

The runtime MUST execute plans according to these behavioral requirements. Implementation details (algorithms, data structures) are left to the client; see §13.7 for reference implementation guidance.

1. **Circular Dependency Detection**: Before execution, the runtime MUST detect any circular dependencies among tools (direct or transitive). If detected, the runtime MUST reject the plan and request a new plan from the narrator AI.
   
   *Note: Reference implementation uses topological sort (Kahn's algorithm). See Spec 002 data-model.md §3 for algorithm details.*

2. **Topological Execution Order**: Tools MUST execute in dependency-respecting order. A tool MUST NOT begin execution until all tools listed in its `dependencies` array have completed successfully.

3. **Parallel Execution**: 
   - If `parallel: true` in the plan AND `async: true` for a tool, tools with satisfied dependencies MAY run concurrently
   - Concurrent execution MUST NOT exceed the number of available CPU cores (implementation-specific limit)
   - Tools with no dependencies MAY run in parallel if both plan and tool have `parallel`/`async: true`

4. **Sequential Fallback**: If `parallel: false`, tools MUST run in topological order, waiting for each to complete before starting the next.

5. **Retry Logic**:
   - If a tool fails (emits `done.ok: false` or exits non-zero), the runtime MUST retry up to `retryPolicy.maxRetries` times
   - The runtime MUST apply exponential backoff between retries with minimum delay `retryPolicy.backoffMs` milliseconds
   - After exhausting retries, the runtime MUST mark the tool as failed and proceed according to the tool's `required` flag
   - The runtime MUST record retry count in the execution trace
   
   *Note: Reference backoff formula: delay = backoffMs × 2^(attempt-1). See Spec 002 data-model.md §2 RetryPolicy.calculateBackoff().*

6. **Failure Handling (by `required` flag)**:
   - **If `required: true` and tool fails**:
     - Dependent tools (listing failed tool in `dependencies`) MUST NOT execute
     - Plan execution stops; all remaining non-blocking tasks MAY continue
     - Tool failure counts against plan execution attempt limit (max 3 attempts)
   - **If `required: false` and tool fails**:
     - Dependent tools MAY execute (they receive null/empty for failed tool inputs)
     - Plan continues executing remaining tasks
     - Tool failure is logged but does not abort plan
   - Independent tools (no dependency on failed tool) continue execution automatically in all cases

7. **Event Aggregation**: Narratoria MUST collect all events from all tools in the plan and merge:
   - `log` events → displayed in Tool Execution Panel
   - `state_patch` events → merged into session state
   - `asset` events → registered and displayed in Asset Gallery
   - `ui_event` events → dispatched to UI handlers
   - `error` events → displayed with context

8. **Execution Trace**: Narratoria MUST maintain a full execution trace with results for each tool (see §13.6)

### 13.5 Plan Executor Output (NEW)

After executing a plan, Narratoria MUST return an execution result with full trace:

```json
{
  "planId": "<uuid, matches plan.requestId>",
  "success": <boolean>,
  "narrative": "<final narrative string>",
  "executionTime": <milliseconds>,
  "toolResults": [
    {
      "toolId": "<string>",
      "ok": <boolean>,
      "state": "completed" | "failed" | "skipped" | "timeout",
      "output": { ...aggregated state_patch events... },
      "executionTime": <milliseconds>,
      "retryCount": <integer>,
      "error": "<string, if ok=false>",
      "events": [ ...all events from this tool... ]
    }
  ],
  "failedTools": ["<toolId>", ...],
  "generationAttempt": <integer>,
  "canReplan": <boolean>
}
```

**Purpose**: This trace allows the narrator AI to:
- Understand which tools failed and why
- Disable failed skills for the next plan via `disabledSkills` field
- Determine if replanning is possible (see §13.7)
- Debug execution issues

### 13.6 Narrator AI Interface (Extended Requirements)

While tool discovery/installation is a non-goal for Spec 001, the narrator AI service integration is normative per Constitution Principle IV.A:

**Required Behavior**:
- The narrator AI MUST return Plan JSON in the format specified in §13.3
- The narrator AI MUST implement bounded replan loop: maximum 5 plan generation attempts before graceful fallback (Constitution IV.A)
- The narrator AI MUST consult `disabledSkills` in execution results to avoid selecting failed skills in subsequent plans
- The narrator AI MUST track generation attempt count in `metadata.generationAttempt`
- The narrator AI MUST set `metadata.parentPlanId` to the previous plan's UUID when replanning
- After 5 failed plan generation attempts, the narrator AI MUST provide template-based fallback narration

**Implementation Flexibility**:
- The narrator AI MAY be a separate process, remote service, or in-process module
- Tool capability discovery mechanisms are implementation-specific (see Spec 002 for Agent Skills Standard integration)
- Plan generation strategy (prompt engineering, model selection) is implementation-specific

*Note: For complete replan loop implementation including per-tool and per-plan-execution retry bounds, see Constitution §IV.A and Spec 002 plan.md.*

Future specifications will define tool discovery protocols and capability negotiation.

---

### 13.7 Implementation Guidance

This specification defines the protocol contract and behavioral requirements that any Narratoria-compatible runtime must satisfy. Implementation details (algorithms, data structures, performance optimizations) are left to individual clients.

**Reference Implementation**: For a complete reference implementation in Dart+Flutter, see:

- **Spec 002: Plan Generation and Skill Discovery**
  - `data-model.md` §3: PlanExecutionContext (dependency graph operations, topological sort, cycle detection)
  - `data-model.md` §2: RetryPolicy (backoff calculation formula)
  - `data-model.md` §11: DeepMerge extension (state merge algorithm)
  - `plan.md`: Architecture diagrams (topological sort flow, replan loop state machine, tool execution lifecycle)
  - `tasks.md`: Implementation tasks (T013: circular dependency detection, T014: topological execution, T015: replan loop)

**Cross-Language Compatibility**: Other language implementations (Rust, Go, Python) MAY use different algorithms or data structures as long as they satisfy the behavioral requirements defined in §13.4. For example:
- Cycle detection: DFS-based algorithms are acceptable alternatives to topological sort
- State merge: Implementations may optimize for specific use cases while preserving deep merge semantics
- Parallel execution: Thread pools, work-stealing, or actor models are all valid strategies

**Protocol Compliance Testing**: Implementations SHOULD validate against:
- `contracts/plan-json.schema.json`: Plan JSON structure
- `contracts/execution-result.schema.json`: Execution result structure
- `contracts/tool-protocol.openapi.yaml`: Event schema validation

---

## 14. MVP Requirements

To deliver a minimum viable product that demonstrates the protocol and player interaction flow, the Narratoria client MUST implement:

### 14.1 Core Features (MUST)

1. **Player Input**: Text field accepting natural language prompts
2. **Narrator AI Stub**: In-process Dart service that converts prompts to Plan JSON using hard-coded mappings (e.g., "light torch" → torch-lighter tool invocation). This mock implementation can be replaced with actual LLM integration in future iterations without changing the PlanExecutor interface.
3. **Tool Invocation**: Execute tools per Plan JSON using process launch and stdin/stdout pipes
4. **Event Processing**: Parse NDJSON from tool stdout and dispatch to handlers
5. **UI Event Support**: Implement `narrative_choice` handler (display choice buttons; other events degrade gracefully)
6. **State Management**: Maintain session state, apply `state_patch` events using deep merge
6. **Asset Registry**: Store asset metadata from `asset` events
7. **UI Panels**: 
   - Story View (narrative text + rendered assets)
   - Tool Execution Panel (logs, progress)
   - Asset Gallery (images, audio, video with graceful degradation)
   - Narrative State Panel (JSON inspector)

### 14.2 Example Tools (SHOULD)

For MVP validation, provide at least two example tools:

1. **torch-lighter**: Receives `{action: "light_torch"}`, emits:
   - `log` event: "Lighting torch..."
   - `state_patch` event: `{"inventory": {"torch": {"lit": true}}}`
   - `asset` event: image of lit torch (PNG file)
   - `done` event: `{"ok": true, "summary": "Torch lit."}`

2. **door-examiner**: Receives `{target: "mysterious_door"}`, emits:
   - `log` event: "Examining door..."
   - `state_patch` event: `{"discovered": {"door_inscription": "Ancient runes"}}`
   - `ui_event` event: `{"event": "narrative_choice", "payload": {"choices": ["Open", "Leave"]}}`
   - `done` event: `{"ok": true, "summary": "Door examined."}`

### 14.3 Sample Plan JSON (Extended)

For prompt "I light the torch and examine the door" (first attempt):

```json
{
  "requestId": "550e8400-e29b-41d4-a716-446655440000",
  "narrative": "You reach for the torch on the wall.",
  "tools": [
    {
      "toolId": "light1",
      "toolPath": "tools/torch-lighter",
      "input": {"action": "light_torch"},
      "dependencies": [],
      "required": true,
      "async": false,
      "retryPolicy": {"maxRetries": 3, "backoffMs": 100}
    },
    {
      "toolId": "examine1",
      "toolPath": "tools/door-examiner",
      "input": {"target": "mysterious_door"},
      "dependencies": ["light1"],
      "required": true,
      "async": false,
      "retryPolicy": {"maxRetries": 3, "backoffMs": 100}
    }
  ],
  "parallel": false,
  "disabledSkills": [],
  "metadata": {
    "generationAttempt": 1,
    "parentPlanId": null
  }
}
```

If torch-lighter fails after 3 retries, execution trace shows failure. Narrator AI generates Plan 2:

```json
{
  "requestId": "550e8400-e29b-41d4-a716-446655440001",
  "narrative": "The torch is beyond reach. You examine the mysterious door instead.",
  "tools": [
    {
      "toolId": "examine2",
      "toolPath": "tools/door-examiner",
      "input": {"target": "mysterious_door"},
      "dependencies": [],
      "required": true,
      "async": false
    }
  ],
  "parallel": false,
  "disabledSkills": ["torch-lighter"],
  "metadata": {
    "generationAttempt": 2,
    "parentPlanId": "550e8400-e29b-41d4-a716-446655440000"
  }
}
```

This demonstrates sequential execution with dependency tracking, retry policies, and replan logic. Tool specifications are defined in §14.2 above.
