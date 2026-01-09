# Feature Specification: LLM Story Transforms

**Feature Branch**: `002-llm-story-transforms`  
**Created**: 2026-01-08  
**Status**: Draft  
**Input**: User description: "I want to build transformation elements for the pipeline that send prompts to a large language model service, so that each element may: (a) rewrite incoming text to correct grammar and fit narration prose, (b) create a summary of incoming text, (c) keep track of characters in the story, (d) keep track of the player's inventory. Elements must be chained logically so character and inventory tracking has as much information as possible."


## Scope *(mandatory)*

### In Scope

- Add a set of pipeline transformation elements that invoke an external language model service to enrich streamed story text.
- Provide distinct transform behaviors:
  - Rewrite narration text (grammar + voice/style normalization).
  - Generate an up-to-date story summary/recap.
  - Extract and maintain a structured character roster and character facts.
  - Extract and maintain the player’s inventory state.
- Define a required logical chaining/order so character and inventory tracking can leverage rewritten text and summary.
- Ensure transforms are safe to run repeatedly across streamed chunks and across multiple turns within the same story session.

### Out of Scope

- UI changes (no new screens required to be considered “done”).
- Image/audio generation.
- Long-term analytics, leaderboards, or sharing.
- Human-in-the-loop editing workflows.
- Multi-player shared worlds.
- Pipeline engine changes to support per-keystroke diffs, hard backpressure guarantees, new chunk types, or new runner semantics (reserved for a future UI/editor spec).

### Assumptions

- The pipeline already has a concept of a “session” so character and inventory state can evolve over time.
- The external language model service can be called on demand and returns text that can be parsed into the required outputs (rewritten prose, summary, structured updates).
- When the language model service is unavailable, the system can continue producing narration without corrupting story state.

### Open Questions *(mandatory)*

- None.


## User Scenarios & Testing *(mandatory)*

**Constitution note**: If the feature changes UI components, acceptance scenarios MUST be coverable via end-to-end tests in addition to any applicable unit tests.

### User Story 1 - Improved Narration Output (Priority: P1)

As a player, I want the story narration to read smoothly (correct grammar, consistent voice) so that the experience feels polished.

**Why this priority**: It directly improves the primary output users consume.

**Independent Test**: Provide an input text chunk with errors and verify the output narration is corrected while preserving meaning.

**Acceptance Scenarios**:

1. **Given** a story session with an incoming narration text chunk containing grammar issues, **When** the rewrite transform runs, **Then** the output narration is grammatically corrected and stylistically consistent without removing key facts.
2. **Given** a story session with an incoming narration text chunk that already matches the desired style, **When** the rewrite transform runs, **Then** the output narration remains materially unchanged (no unnecessary rewrites).

---

### User Story 2 - Automatic Recap (Priority: P2)

As a player, I want an up-to-date recap of what has happened so far so that I can quickly re-orient after a break.

**Why this priority**: Improves continuity and reduces confusion in longer sessions.

**Independent Test**: Feed multiple chunks and verify a coherent summary exists and is updated after each chunk.

**Acceptance Scenarios**:

1. **Given** a story session with multiple narration chunks processed, **When** the summary transform runs after new content arrives, **Then** the story summary is updated to reflect new events while retaining prior important context.

---

### User Story 3 - Track Characters and Inventory (Priority: P3)

As a player, I want the system to remember characters I’ve met and what I’m carrying so that the narrative stays consistent and reactive.

**Why this priority**: State tracking enables richer downstream narration and fewer continuity errors.

**Independent Test**: Provide chunks that introduce a character and add/remove an item; verify the maintained state changes accordingly.

**Acceptance Scenarios**:

1. **Given** a story session where a new character is introduced, **When** the character tracking transform runs, **Then** the character roster includes the new character with key known facts captured.
2. **Given** a story session where an item is acquired or consumed, **When** the inventory tracking transform runs, **Then** the inventory state reflects the update and maintains a clear current set of items.

### Edge Cases

- A narration chunk contains contradictory statements about a character or inventory.
- A narration chunk is extremely short (e.g., a single sentence) or extremely long (e.g., a long paragraph).
- The language model output is incomplete, non-parseable, or omits required sections.
- The language model “hallucinates” new characters/items not supported by the input text.
- The language model service is slow or temporarily unavailable.
- Streaming stops early (consumer stops reading) and the session later resumes.


## Interface Contract *(mandatory)*

List the externally observable surface area this feature introduces or changes. Avoid implementation details.

### New/Changed Public APIs

- Pipeline transform: **Rewrite Narration** — Accepts incoming story text and outputs a rewritten narration text while preserving meaning.
- Pipeline transform: **Story Summary** — Accepts story text (and prior summary) and outputs an updated summary.
- Pipeline transform: **Character Tracker** — Accepts story text (and optional summary) and outputs updated character state.
- Pipeline transform: **Inventory Tracker** — Accepts story text (and optional summary) and outputs updated inventory state.

### Data Contracts *(if applicable)*

- **StoryState** — Holds current story facts: summary, characters, inventory, and per-session metadata needed to continue.
- **CharacterRecord** — Name/identifier, known traits, relationships, last-seen context, and confidence/source references.
- **InventoryState** — Current items, quantities (when applicable), and notes.
- **TransformProvenance** — For any updated field: source snippet reference (from input), timestamp/order, and confidence.


## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST support a rewrite transform that converts incoming story text into corrected, narration-ready prose while preserving intended meaning.
- **FR-002**: Rewrite transform MUST provide output that is safe for downstream transforms to consume (preserves expected chunk type/shape; e.g., `TextChunk -> TextChunk`).
- **FR-003**: System MUST support a summary transform that maintains an up-to-date summary across a session.
- **FR-004**: Summary transform MUST incorporate newly received events while retaining prior key facts (recency + continuity).
- **FR-005**: System MUST support a character tracking transform that updates a structured character roster over time.
- **FR-006**: Character tracking MUST avoid inventing new characters/facts that are not supported by the incoming content; uncertain inferences MUST be flagged as low confidence.
- **FR-007**: System MUST support an inventory tracking transform that updates a structured inventory state over time.
- **FR-008**: Inventory tracking MUST avoid inventing items not supported by the incoming content; uncertain inferences MUST be flagged as low confidence.
- **FR-009**: Transforms MUST be chainable in a deterministic order such that:
  - Rewrite runs before Summary.
  - Summary runs before Character and Inventory tracking.
  - Character and Inventory tracking may run in either order but MUST each be able to use the rewritten text and latest summary.
- **FR-010**: System MUST preserve the original incoming text so that downstream components can compare/trace differences between original and rewritten outputs.
- **FR-011**: System MUST update the per-session story state (summary/characters/inventory) after each processed chunk or turn.
- **FR-012**: System MUST include provenance for any character/inventory updates (at minimum: reference to supporting input text and the time/order it was observed).
- **FR-013**: All transforms and provider calls MUST be cancellation-correct: they MUST honor the provided `CancellationToken`, stop work promptly when cancelled, and propagate cancellation (no swallowing `OperationCanceledException`).
- **FR-014**: All transforms MUST be stream-safe: they MUST avoid unbounded buffering and MUST not require full input enumeration before producing any output.

### Error Handling *(mandatory)*

- **EH-001**: If the language model service call fails, the system MUST continue the pipeline using the best available inputs (e.g., original text) and MUST NOT corrupt existing story state.
- **EH-002**: If the language model response is missing required parts (e.g., cannot derive structured updates), the system MUST keep prior state unchanged and log an observable failure reason.
- **EH-003**: If the language model output conflicts with existing state, the system MUST prefer evidence-backed updates and MUST not discard prior state without justification/provenance.
- **EH-004**: System MUST record enough diagnostic context to understand failures and quality regressions (at minimum: which transform failed, and which session/turn it applied to). Log via `ILogger<T>` (terminal output buffer is sufficient).

### State & Data *(mandatory if feature involves data)*

- **Persistence**: Summary, character roster, and inventory state MUST be maintained across the lifetime of a story session.
- **Invariants**:
  - Original incoming text is never overwritten or lost.
  - Character and inventory state updates are append/merge operations with provenance; no “silent” destructive edits.
  - Low-confidence inferences are explicitly labeled as such.
- **Migration/Compatibility**: Existing sessions without these fields MUST still load and run; missing state initializes to empty defaults.

### Optional Metadata Conventions *(forward compatibility)*

To enable a future UI/editor spec to implement “latest-wins” execution and input de-duplication without changing transform logic, the system MAY attach optional run metadata to `PipelineChunkMetadata.Annotations`.

- **Producer**: These values are expected to be set by the caller/orchestrator (and/or a pipeline source/runner wrapper) that initiates a run. Transforms MUST NOT create, modify, or reinterpret these fields.

- **Reserved keys**:
  - `narratoria.run_id`: string. Stable identifier for one pipeline run invocation.
  - `narratoria.run_sequence`: integer. Monotonic sequence number within a run.
  - `narratoria.input_snapshot_sha256`: lowercase hex string. SHA-256 of the UTF-8 bytes of the full input text snapshot used to produce this output.
- **Rule**: If these keys are present on an incoming chunk, transforms MUST pass them through unchanged.

### Key Entities *(include if feature involves data)*

- **StoryState**: The canonical per-session representation of summary + characters + inventory.
- **NarrationChunk**: A single unit of incoming story text processed by the pipeline.
- **CharacterRecord**: A structured representation of a story character.
- **InventoryItem**: A structured representation of a player-held item.
- **TransformProvenance**: Evidence trail for updates.


## Test Matrix *(mandatory)*

Map each requirement to the minimum required test coverage. If UI behavior changes, include end-to-end coverage.

| Requirement ID | Unit Tests | Integration Tests | E2E | Notes |
|---|---|---|---|---|
| FR-001 | Y | Y | N | Rewrite preserves meaning and improves grammar/style |
| FR-003 | Y | Y | N | Summary evolves with new chunks |
| FR-005 | Y | Y | N | Character roster updates from text + summary |
| FR-007 | Y | Y | N | Inventory updates from text + summary |
| FR-009 | Y | N | N | Order is deterministic and documented |
| FR-010 | Y | N | N | Original text preserved |
| FR-013 | Y | N | N | Cancellation is honored and propagated |
| FR-014 | Y | N | N | Transforms remain streaming-friendly |
| EH-001 | Y | Y | N | Service failure degrades gracefully |
| EH-002 | Y | Y | N | Non-parseable output does not corrupt state |
| EH-003 | Y | N | N | Conflict handling preserves evidence and avoids destructive edits |


## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In a representative test set of narration inputs, at least 90% of rewritten outputs are rated “ready to read aloud” (no obvious grammar errors) without losing key story facts.
- **SC-002**: After each new story chunk, an updated recap is available and includes the latest major events with no more than 5% critical omissions (as judged against the same test set).
- **SC-003**: Character tracking correctly identifies and maintains character entries for at least 90% of explicit introductions and named references in the test set.
- **SC-004**: Inventory tracking correctly adds/removes items for at least 90% of explicit acquisitions/consumptions in the test set.
