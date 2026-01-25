# Specification 001: Tool Protocol (Version 0.0.1)

**Status:** Draft  
**Audience:** Narratoria Core Runtime, Tool Developers  
**Scope:** Defines the communication protocol between external tools and the Narratoria application.  
**Version:** 0.0.1 (protocol envelope property `version`: "0")

---

## 1. Purpose

This specification establishes a minimal, extensible, language-agnostic protocol for external tool processes communicating with the Narratoria runtime. Tools may be authored in any programming language and executed as independent OS processes. The protocol enables:
- state updates
- asset generation
- UI event requests
- structured errors
- progress logs
- streaming incremental output

This protocol ensures backward and forward compatibility between Narratoria and tools.

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
- Narratoria MUST validate and merge this patch into session state using Narratoria-defined rules.
- Tools SHOULD only express state changes and MUST NOT assume how they are applied.

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
- Gracefully degrade unsupported events via placeholder messages.

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

Tools MAY write assets to:
- platform-standard application data directories, or
- paths supplied by Narratoria at invocation time.

Tools MUST return correct absolute or relative paths in asset events.

Narratoria MUST NOT enforce a global schema for per-tool storage in Spec 001.

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
- UI rendering specifics
- Shader or 3D scene protocols
- Tool discovery or installation
- Authentication or sandboxing
- Threading, cancellation, or lifecycle semantics within tools
- Schema of Narratoria internal state

These will be defined in later specifications.

---

## 11. Versioning

The protocol `version` string in the event envelope MUST remain "0" until Spec 002 changes it. Spec 001 establishes baseline compatibility. Future specs MUST commit to backwards compatibility unless a major version change is declared.
