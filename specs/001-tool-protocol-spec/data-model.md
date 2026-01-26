# Data Model

## Protocol Event Entities

### EventEnvelope
- **Fields:**
  - `version` (string, required): MUST equal "0" for Spec 001.
  - `type` (string, required): One of `log`, `state_patch`, `asset`, `ui_event`, `error`, `done`.
  - `requestId` (string, optional): Opaque identifier supplied by Narratoria.
  - `timestamp` (string, optional, ISO-8601): Optional event timestamp.
  - `...` (object, optional): Additional fields are allowed and MUST be ignored by the runtime unless defined.
- **Validation rules:** `version` must be "0"; `type` must be in the allowed set.
- **Relationships:** Acts as the envelope for all event subtypes below.

### LogEvent (extends EventEnvelope)
- **Fields:**
  - `level` (string, required): `debug` | `info` | `warn` | `error`.
  - `message` (string, required): Human-readable text.
  - `fields` (object, optional): Arbitrary key/value data.
- **Validation rules:** `message` non-empty string; `level` in allowed set.

### StatePatchEvent (extends EventEnvelope)
- **Fields:**
  - `patch` (object, required): Arbitrary JSON object representing state changes.
- **Validation rules:** `patch` must be a JSON object (not null/array/primitive).

### AssetEvent (extends EventEnvelope)
- **Fields:**
  - `assetId` (string, required): Unique per tool invocation.
  - `kind` (string, required): Broad category (e.g., image, audio, video, model).
  - `mediaType` (string, required): MIME type.
  - `path` (string, required): File system path accessible to Narratoria.
  - `metadata` (object, optional): Arbitrary key/value details (dimensions, rate, etc.).
- **Validation rules:** `assetId`, `kind`, `mediaType`, `path` non-empty strings; `mediaType` must be valid MIME string.

### UiEvent (extends EventEnvelope)
- **Fields:**
  - `event` (string, required): UI action identifier.
  - `payload` (object, optional): Event parameters.
- **Validation rules:** `event` non-empty string.

### ErrorEvent (extends EventEnvelope)
- **Fields:**
  - `errorCode` (string, required): Machine-consumable code.
  - `errorMessage` (string, required): Human-readable message.
  - `details` (object, optional): Structured data for debugging.
- **Validation rules:** `errorCode` and `errorMessage` non-empty strings.
- **Behavioral rule:** Error events do not terminate the invocation; a `done` event should follow.

### DoneEvent (extends EventEnvelope)
- **Fields:**
  - `ok` (boolean, required): True for success, false for controlled failure.
  - `summary` (string, optional): Human-readable completion summary.
- **Validation rules:** `ok` boolean required.
- **Behavioral rule:** Exactly one `done` event per invocation; tools should not emit further events after `done`.

## State Transitions

- Event ordering is flexible; typical flow: `log` -> `asset`/`ui_event`/`state_patch` (any mix/any order) -> `done`.
- Protocol-level completion requires `done` with `ok` plus process exit code 0; non-zero exit codes indicate protocol failure regardless of events.

---

## Player Interaction Entities

### PlanJson
- **Fields:**
  - `requestId` (string, required): Unique identifier for plan execution.
  - `narrative` (string, optional): Narrator text to display to player.
  - `tools` (array, required): Array of ToolInvocation objects.
  - `parallel` (boolean, optional, default false): Whether tools can run concurrently.
- **Validation rules:** `requestId` non-empty string; `tools` non-empty array.
- **Relationships:** Contains multiple ToolInvocation entities.

### ToolInvocation
- **Fields:**
  - `toolId` (string, required): Unique identifier within this plan.
  - `toolPath` (string, required): Filesystem path to tool executable.
  - `input` (object, required): JSON object passed via stdin to tool.
  - `dependencies` (array of string, optional): Array of `toolId` values that must complete first.
- **Validation rules:** `toolId` and `toolPath` non-empty strings; `input` must be JSON object.
- **Behavioral rules:** Tool cannot execute until all dependencies complete with `done.ok: true`.

### PlayerPrompt
- **Fields:**
  - `text` (string, required): Natural language input from player.
  - `timestamp` (ISO-8601 string, required): When prompt was submitted.
  - `sessionId` (string, required): Current game session identifier.
- **Validation rules:** `text` non-empty string.
- **Relationships:** Generates one PlanJson via narrator AI service.

---

## UI Component Entities

### Asset (from protocol)
- Already defined in protocol events section as AssetEvent payload
- Used by UI Asset Gallery component for rendering

### SessionState
- **Fields:**
  - `stateTree` (object, required): Current game state as nested JSON object.
  - `patches` (array, optional): History of StatePatchEvent applications.
- **Validation rules:** `stateTree` must be valid JSON object.
- **Behavioral rules:** Merged incrementally via `state_patch` events; displayed in Narrative State Panel.

### ToolExecutionStatus
- **Fields:**
  - `toolId` (string, required): From ToolInvocation.
  - `status` (enum, required): `pending`, `running`, `completed`, `failed`.
  - `events` (array, required): Collected protocol events from this tool.
  - `exitCode` (integer, optional): Process exit code when completed/failed.
- **Validation rules:** `status` in allowed set.
- **Behavioral rules:** Displayed in Tool Execution Panel; status updates as events received.
