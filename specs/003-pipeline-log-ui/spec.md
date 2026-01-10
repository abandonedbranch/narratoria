# Feature Specification: Realtime Pipeline Log UI

**Feature Branch**: `003-pipeline-log-ui`  
**Created**: 2026-01-09  
**Status**: Draft  
**Input**: User description: "002 has been merged. Let us move to specifying 003… System must present user a realtime, streaming log component… Log component must have an area where the user can push new text… execute the pipeline responsively (idle + send)… build pipeline A and pipeline B… pipeline B reserved for transformers defined in 002… pipeline A contains a buffer accumulated from the user input source element, and an LLM sink… LLM used in the sink should be reasonably configurable by the user."


## Scope *(mandatory)*

### In Scope

- Add a user-facing, realtime “pipeline log” component that shows streaming telemetry for pipeline runs.
- Provide an input area within the log component to submit new text into a live user-input source for pipeline execution.
- Execute pipeline runs responsively using two triggers:
  - **Idle**: after the user stops typing for a short period.
  - **Send**: when the user explicitly submits.
- Define two pipeline configurations used by the log component:
  - **Pipeline A (Idle pipeline)**: uses an accumulated input buffer and an LLM sink.
  - **Pipeline B (Send pipeline)**: reserved for LLM-backed transforms defined by spec 002.
- Allow the user to choose the LLM configuration used by the LLM sink (within the set of supported/available options).
- Persist the collected outputs of pipeline runs (and enough session context to resume) to a client-side IndexedDB store so a player can continue the story later.

### Out of Scope

- True per-keystroke diff/incremental processing of editor deltas.
- Hard backpressure guarantees for arbitrary producers outside this UI component.
- Introducing new pipeline chunk types or changing pipeline runner semantics.
- New narrative UI beyond the log + input area (no full “game UI” redesign required to be considered done).
- Server-side persistence for this feature (persistence is client-local).
- Multi-session management UI (naming, selecting, switching between multiple saved sessions).

### Assumptions

- The pipeline runtime emits structured telemetry events per run and per stage that can be surfaced in a UI.
- Spec 002 transforms exist and can be composed as a pipeline segment for “Send” execution.
- A cancelled run is acceptable behavior when a newer run supersedes it (i.e., “latest input wins”).

### Terminology

- **Telemetry**: Structured “what happened” updates during a run (start/end, stages, durations, errors, and optional model usage metrics).
- **Idle trigger**: A run started after the user pauses typing for 500ms.
- **Send trigger**: A run started when the user explicitly submits.
- **Pipeline A**: The idle-triggered pipeline (uses the current accumulated input buffer and an LLM sink).
- **Pipeline B**: The send-triggered pipeline (reserved for spec 002 transforms).
- **LLM profile**: A named preset that trades off speed/cost vs quality (options: Fast, Balanced, Quality; exact tuning parameters are implementation-defined).
- **Story session**: The single current locally persisted context that is restored automatically on load.
- **Story fact**: A key moment in the story (e.g., character development or plot twist) with enough detail for an LLM to maintain useful long-term memory of the story.

### Open Questions *(mandatory)*

- None.


## Clarifications

### Session 2026-01-09

- Q: What should “Idle trigger after a short period of no typing” mean for spec 003? → A: Fixed 500ms idle debounce.
- Q: Should idle-triggered runs be allowed while a Send run is in progress? → A: No. Send suppresses idle runs and disables user input until the Send run completes or errors.
- Q: What does “reasonably configurable” mean for the Pipeline A LLM sink in this spec? → A: Provider + model + a small set of named profile presets (Fast/Balanced/Quality).
- Q: Should the log keep an unlimited run history, or cap it / provide controls? → A: Provide a Clear Log button; otherwise the log continues to build.
- Q: Should Clear Log also clear the current input buffer? → A: No. Clear Log is log-only.
- Q: Where should collected run output/context be stored for continuity and resuming narration? → A: Client-side IndexedDB.
- Q: With IndexedDB persistence required, what should Clear Log do regarding persisted data and story context? → A: Clear Log clears the UI only; persisted history/context remains and will be restored on reload.
- Q: For IndexedDB persistence in 003, what is the session model—single auto-resume session or multiple sessions? → A: Single current session; auto-resume the most recent on load.
- Q: For IndexedDB persistence, should we store full telemetry streams or summarized run records? → A: Persist summarized run records + latest story context (do not persist full per-stage telemetry streams).
- Q: How should persisted run history be retained over time? → A: Cap persisted run records to a fixed maximum count; summarize anything older than the cap into a predictable-length digest and append it to the story context via a transforming pipeline element.
- Q: What does “predictable length” mean for the history digest? → A: Exactly twelve bullets (story facts).


## User Scenarios & Testing *(mandatory)*

**Constitution note**: If the feature changes UI components, acceptance scenarios MUST be coverable via Playwright for .NET E2E tests in addition to any applicable unit tests.

### User Story 1 - Realtime Pipeline Telemetry Log (Priority: P2)

As a user, I want to see a realtime log of what the pipeline is doing so I can understand execution, performance, and failures.

**Why this priority**: Without the log, the rest of the feature is not observable and cannot be debugged or trusted.

**Independent Test**: Trigger a pipeline run from the UI and verify that the log displays a streaming set of telemetry entries for that run.

**Acceptance Scenarios**:

1. **Given** the pipeline log component is open, **When** a pipeline run starts, **Then** a new run entry appears immediately and receives streaming telemetry updates until completion.
2. **Given** a pipeline run fails, **When** telemetry reports an error, **Then** the log shows the run as failed with enough context to identify the stage and the failure category.
3. **Given** the log contains prior run entries, **When** the user clicks Clear Log, **Then** prior log entries are cleared from the UI without cancelling any in-progress run.
4. **Given** the user clicks Clear Log, **When** the log is cleared, **Then** the current input buffer and LLM selection remain unchanged.
5. **Given** the user has cleared the log, **When** the user refreshes the page, **Then** the log and latest story context are restored from client IndexedDB.

---

### User Story 2 - Responsive Execution from Live Input (Priority: P3)

As a user, I want to type text and have the system run the pipeline responsively on idle and on send, so I can iterate quickly and deliberately.

**Why this priority**: The feature is centered on interactive experimentation and immediate feedback.

**Independent Test**: Type into the input area and verify that idle-triggered runs occur; click send and verify a send-triggered run occurs.

**Acceptance Scenarios**:

1. **Given** the input area has focus, **When** the user types and then pauses for 500ms, **Then** Pipeline A runs using the latest accumulated input.
2. **Given** the user presses Send, **When** the Send action is received, **Then** Pipeline B runs using the latest accumulated input.
3. **Given** the user continues typing while an idle-triggered run is in progress, **When** a newer idle trigger occurs, **Then** the prior idle run is cancelled and the newest run becomes the active one (“latest input wins”).
4. **Given** a Send-triggered run is in progress, **When** the user attempts to type, **Then** user input is disabled until the Send-triggered run completes or errors.
5. **Given** a Send-triggered run is in progress, **When** the user pauses typing (no further input events occur), **Then** no idle-triggered runs start until the Send-triggered run completes or errors.

---

### User Story 3 - User-Configurable LLM Sink (Priority: P4)

As a user, I want to adjust the LLM used by the interactive sink so I can trade off quality, cost, and speed.

**Why this priority**: Different tasks and environments require different model behavior and performance.

**Independent Test**: Change the LLM configuration in the UI and verify that subsequent runs reflect the new selection.

**Acceptance Scenarios**:

1. **Given** the user changes the LLM selection (provider, model, or profile), **When** the next run starts, **Then** the run uses the updated LLM configuration and the log reflects the effective selection.

---

### User Story 4 - Resume a Story Session (Priority: P1)

As a player, I want the story context from prior runs to be saved locally so that I can resume the narration later without losing progress.

**Why this priority**: Continuity is a core promise of the experience (a continuous story the player can resume at any time).

**Independent Test**: Execute a run that produces narration + updated state, reload the page, and verify the prior session’s latest context and recent run history are restored from IndexedDB.

**Acceptance Scenarios**:

1. **Given** the user has executed one or more runs, **When** the user refreshes the page, **Then** the log component restores the prior run history and the latest story context from client IndexedDB.
2. **Given** a new run completes, **When** its output is produced, **Then** the run’s stored outputs and latest story context are written to client IndexedDB.
3. **Given** the user refreshes the page, **When** the prior session is restored, **Then** the input buffer and LLM selection are restored to their last persisted values.

### Edge Cases

- The user types rapidly (bursty input) and multiple idle triggers occur.
- The user uses input methods that temporarily compose text (intermediate composition states) and the system should not spam runs for unstable intermediate states.
- The user pastes a large block of text.
- A run is cancelled mid-flight (newer input supersedes it or the user navigates away).
- The user presses Send while an idle-triggered run is in progress.
- The user clears the log while a run is in progress.
- The LLM service is slow or unavailable.
- Telemetry volume is high; the log must remain usable and not degrade the UI.
- The persisted run history grows beyond the retention cap and must be compacted; compaction should not block the user from continuing.


## Interface Contract *(mandatory)*

List the externally observable surface area this feature introduces or changes. Avoid implementation details.

### New/Changed Public APIs

- Pipeline log component — Presents streaming pipeline telemetry and controls to trigger pipeline execution from live input.

### Events / Messages *(if applicable)*

- Pipeline run telemetry update — pipeline runtime -> pipeline log component — communicates run lifecycle, stage progression, timings, and errors.
- User input changed — input area -> orchestrator — updates accumulated buffer state.
- Idle trigger — orchestrator -> pipeline executor — initiates Pipeline A using the latest buffer.
- Send trigger — orchestrator -> pipeline executor — initiates Pipeline B using the latest buffer.

### Data Contracts *(if applicable)*

- **RunTelemetryEntry** — UI-facing telemetry item (run identifier, trigger type, stage name, status, timestamps/durations, and optional provider/model metrics).
- **PersistedRunRecord** — stored summary record per run (run identifier, trigger type, start/end timestamps, final status, and optional key stage summary + provider/model metrics when available).
- **StoryContextDigest** — exactly twelve **story fact** bullet items summarizing older run history; appended into persisted story context when old run records are compacted.
- **UserInputBuffer** — current accumulated text and basic metadata (e.g., last-updated time).
- **LlmSelection** — user-chosen LLM configuration (at a high level: provider, model, and profile preset).


## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a realtime, streaming log component that displays telemetry for every pipeline run initiated by the component.
- **FR-002**: The log component MUST allow the user to push new text into a live user-input source used to run the pipeline.
- **FR-003**: The log component MUST execute pipelines responsively using both triggers:
  - Idle trigger after 500ms of no typing.
  - Send trigger on explicit user action.
- **FR-004**: The log component MUST build and execute two pipelines:
  - **Pipeline A** (Idle pipeline) contains an accumulated input buffer from the user input source and an LLM sink.
  - **Pipeline B** (Send pipeline) is reserved for LLM-backed transforms defined by spec 002.
- **FR-005**: The LLM used by the Pipeline A sink MUST be reasonably configurable by the user (selection among supported provider/model/profile presets and clearly visible effective choice per run).
- **FR-006**: System MUST implement “latest input wins” for idle-triggered execution: when a newer idle-triggered run starts, any prior in-flight idle-triggered run MUST be cancelled.
- **FR-007**: The log MUST display enough per-run context to correlate telemetry entries to a specific run and trigger type.
- **FR-008**: The system SHOULD avoid triggering idle runs for unstable intermediate text composition states and SHOULD prioritize stable text snapshots.
- **FR-009**: The system MUST support run identification metadata for correlation and de-duplication (consistent with spec 002 optional run metadata conventions, including a run identifier, a monotonic sequence, and an input snapshot hash).
- **FR-010**: While a Send-triggered run is in progress, the log component MUST suppress idle-triggered runs.
- **FR-011**: While a Send-triggered run is in progress, the log component MUST disable user input until the Send-triggered run completes or errors.
- **FR-012**: The log component MUST provide a Clear Log action that clears prior log entries from the UI only (does not clear the input buffer, LLM selection, or persisted history/context); absent user action, the log continues to accumulate entries.
- **FR-013**: The system MUST persist summarized run records (not full per-stage telemetry streams) and the latest story context needed to continue narration to client IndexedDB.
- **FR-014**: On load, the system MUST restore the latest persisted story context and recent run history from client IndexedDB.
- **FR-015**: The system MUST treat the latest persisted story context as the single current story session and auto-resume it on load (no multi-session selection UI).
- **FR-016**: The system MUST enforce a fixed maximum count of persisted run records in IndexedDB (configurable; default 200).
- **FR-017**: When persisted run records would exceed the maximum count, the system MUST compact the oldest run records by producing a **StoryContextDigest** via a transforming pipeline element and appending that digest to the persisted story context before deleting the compacted run records.
- **FR-018**: The **StoryContextDigest** produced by compaction MUST contain exactly twelve bullet items (“story facts”).

### Error Handling *(mandatory)*

- **EH-001**: If a pipeline run fails, the system MUST surface the failure in the log and allow the user to continue typing and sending new runs.
- **EH-002**: If an idle-triggered run is cancelled due to newer input, the system MUST represent it as cancelled (not failed) in the log.
- **EH-003**: System MUST log failures at an appropriate level including run identifier, trigger type, stage name (when applicable), and any available session/turn context.
- **EH-004**: If a Send-triggered run errors, the system MUST re-enable user input after surfacing the error in the log.


### State & Data *(mandatory if feature involves data)*

- **Persistence**:
  - The user’s current input buffer and LLM selection MUST be stored in the single current story session and restored on load.
  - Summarized run records and latest story context MUST be stored in client IndexedDB so the session can be resumed.
  - Persisted run records MUST be capped; older run records beyond the cap are compacted into a predictable-length digest that is appended into the persisted story context.
- **Invariants**:
  - The log remains usable under frequent input changes.
  - Idle-triggered runs do not accumulate unbounded backlog; newer input supersedes older work.
- **Migration/Compatibility**: Existing usage without this component remains unaffected.

### Key Entities *(include if feature involves data)*

- **PipelineRun**: One execution initiated from the log component (triggered by Idle or Send).
- **Pipeline A**: Idle pipeline (input buffer + LLM sink).
- **Pipeline B**: Send pipeline (reserved for spec 002 transforms).
- **RunTelemetryEntry**: A single displayable telemetry item associated with a run.
- **UserInputBuffer**: The latest accumulated text snapshot provided by the user.
- **LlmSelection**: The user-selected LLM configuration used by the Pipeline A sink.
- **HistoryCompactionTransform**: A transforming pipeline element that compacts older persisted run records into a predictable-length digest and appends it into the story context.


## Test Matrix *(mandatory)*

Map each requirement to the minimum required test coverage. If UI behavior changes, include Playwright E2E coverage.

| Requirement ID | Unit Tests | Integration Tests | E2E (Playwright) | Notes |
|---|---|---|---|---|
| FR-001 | N | Y | Y | Log shows streaming telemetry per run |
| FR-002 | N | N | Y | Input text feeds the live input source |
| FR-003 | Y | N | Y | Debounce/idle vs send triggering |
| FR-004 | Y | Y | Y | Pipeline A/B are wired correctly |
| FR-005 | Y | N | Y | LLM selection affects subsequent runs |
| FR-006 | Y | Y | Y | “Latest input wins” cancellation behavior |
| FR-008 | Y | N | Y | IME/composition does not spam unstable idle runs |
| EH-001 | N | Y | Y | Failures surfaced; UI remains usable |
| EH-002 | Y | N | Y | Cancellations are shown as cancelled |
| EH-003 | Y | N | N | Logs contain required correlation context |
| FR-010 | Y | N | Y | Send suppresses idle-triggered runs |
| FR-011 | N | N | Y | Input disabled during Send runs |
| EH-004 | N | N | Y | Input is re-enabled after Send errors |
| FR-012 | N | N | Y | Clear Log clears UI only; persisted history remains |
| FR-013 | N | Y | Y | Run outputs + context persisted to IndexedDB |
| FR-014 | N | N | Y | Restores persisted context on load |
| FR-015 | N | N | Y | Auto-resumes single current session |
| FR-016 | Y | Y | Y | Retention cap enforced for persisted run records |
| FR-017 | Y | Y | Y | Old run records compacted into digest appended to story context |
| FR-018 | Y | Y | Y | Digest is exactly twelve story-fact bullets |


## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For at least 95% of pipeline runs initiated from the component, a corresponding run entry appears in the log within 250ms of initiation.
- **SC-002**: When a newer idle-triggered run supersedes an older one, the older run transitions to a cancelled state and stops producing UI-visible updates within 500ms.
- **SC-003**: Users can perform the primary workflow (type → idle run → send run → observe results) without UI freezes or missed keystrokes in typical usage.
- **SC-004**: Users can change the LLM selection and observe the updated effective selection reflected in the next run’s log entry.
- **SC-005**: After a page reload, the latest persisted story context is restored and a user can continue with no manual recovery steps.
