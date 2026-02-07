# Specification 001: Tool Protocol (Version 0.0.1)

**Status:** Draft
**Audience:** Narratoria Core Runtime, Tool Developers
**Scope:** Defines the communication protocol between external tools and the Narratoria application.
**Version:** 0.0.1 (document version)
**Protocol Version:** "0" (value of `version` field in event envelopes; incremented only on breaking protocol changes)

## Terminology

The key words "MUST", "MUST NOT", "REQUIRED", "SHALL", "SHALL NOT", "SHOULD", "SHOULD NOT", "RECOMMENDED", "MAY", and "OPTIONAL" in this document are to be interpreted as described in [RFC 2119](https://datatracker.ietf.org/doc/html/rfc2119).

## Clarifications

### Session 2026-01-24

- Q: How should the Narrator AI Stub be implemented in the MVP? → A: In-process Dart function/class that returns hard-coded Plan JSON for known prompts
- Q: What merge semantics should state_patch events use? → A: Deep merge (nested objects merged recursively; arrays replaced)
- Q: Should independent tools continue when a sibling tool fails? → A: Continue automatically (independent tools proceed unless they also fail)
- Q: Which ui_event types must the MVP implement? → A: narrative_choice only (display choice buttons/list from payload)
- Q: Who creates asset files and determines paths? → A: Tools generate and write files independently; provide absolute paths in asset events

---

## Prerequisites

**Read order**: This is the foundational spec; no prerequisites required.

**Dependency chain**: Specs 002, 003, 005 build directly on this protocol. Specs 004, 006, 007, 008 assume this protocol is understood.

---

## Glossary

- **Session State**: The runtime data model containing narrative state accumulated from `state_patch` events (e.g., `{"inventory": {"torch": {"lit": true}}}`)
- **Plan JSON**: Structured document produced by narrator AI describing which tools to execute, their inputs, dependencies, and execution strategy (parallel/sequential). See [Spec 002](../002-plan-execution/spec.md) for plan execution semantics and schema.
- **Deep Merge**: State patch merge semantics where nested objects are merged recursively, arrays replaced entirely, and null values remove keys

> **Note:** For UI component definitions (Narrative State Panel, Tool Execution Panel, Tools View), see [Spec 005](../005-dart-implementation/spec.md). For Narrator AI Stub implementation, see [Spec 002](../002-plan-execution/spec.md).

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

2. **Player Interaction Flow**: The mechanism by which player input (natural language prompts) is converted into executable plans that invoke tools.

This protocol ensures backward and forward compatibility between Narratoria and tools, while maintaining a testable, composable UI architecture.

> **Note:** For UI implementation requirements (Material Design 3, Flutter widgets), see [Spec 005: Dart Implementation](../005-dart-implementation/spec.md).

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

A compliant minimal tool emits NDJSON events to stdout:

```
{"version":"0","type":"log","level":"info","message":"Starting"}
{"version":"0","type":"state_patch","patch":{"flags":{"torchLit":true}}}
{"version":"0","type":"done","ok":true,"summary":"Torch lit."}
```

For skill implementation examples and the Agent Skills Standard, see [Spec 003](../003-skills-framework/spec.md).

---

## 10. Non-Goals for Spec 001

Spec 001 does NOT define:
- UI rendering specifics (see [Spec 005](../005-dart-implementation/spec.md) for Material Design 3 Flutter implementation)
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

## 12. Related Specifications

| Specification | Relationship |
|---------------|--------------|
| [002: Plan Execution](../002-plan-execution/spec.md) | Defines Plan JSON schema, execution semantics, replan loop |
| [003: Skills Framework](../003-skills-framework/spec.md) | Defines skill discovery, configuration, and Agent Skills Standard |
| [004: Narratoria Skills](../004-narratoria-skills/spec.md) | Defines individual skill specifications (storyteller, dice-roller, etc.) |
| [005: Dart Implementation](../005-dart-implementation/spec.md) | Dart+Flutter reference implementation including UI requirements |

---

## Contracts

This specification defines the following machine-readable contracts in `contracts/`:

- **tool-protocol.openapi.yaml**: OpenAPI 3.1.0 schema for all protocol event types

For Plan JSON and execution result schemas, see [Spec 002 contracts](../002-plan-execution/contracts/). For skill manifest schemas, see [Spec 003 contracts](../003-skills-framework/contracts/).
