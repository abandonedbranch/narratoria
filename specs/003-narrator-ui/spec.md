# Feature Specification: Narrator UI

**Feature Branch**: `003-narrator-ui`
**Created**: January 23, 2026
**Status**: Draft
**Input**: User description: "Create a UI for the AI narrator where users can interact with the narrator, see streaming narration, view and edit game state (inventory, quests, NPCs, reputation), see agent planning and skill execution, configure narrator settings, and save/load sessions."


## Scope *(mandatory)*

### In Scope

- Interactive narrator UI with streaming narration display and player input area.
- Real-time game state visualization:
  - **Inventory viewer**: Grid or list showing items with quantities, descriptions, metadata.
  - **Quest log viewer**: Active and completed quests with objectives and progress.
  - **NPC registry viewer**: Known NPCs with attributes, relationships, dialogue history.
  - **Reputation dashboard**: Faction and NPC standings with status effects.
  - **World state viewer**: Current location, time, environmental flags.
  - **Choice history viewer**: Past player decisions and consequences.
- Agent insight panel (debug/transparency view):
  - Current plan display (JSON or visual representation).
  - Skill execution log with timing and outcomes.
  - Agent reasoning display.
  - State diff viewer (before/after skill execution).
- Narrator configuration panel:
  - LLM selection (provider, model, profile from UnifiedInference spec 001).
  - Narrator personality/style settings (tone, verbosity, agency level).
  - Game rules toggles (e.g., enforce consistency checks, auto-save).
- Session management:
  - Save current session to client-side IndexedDB.
  - Load session from IndexedDB with restore of full state.
  - Multiple save slots with session metadata (title, last played, screenshot/thumbnail).
  - Export session to JSON file.
  - Import session from JSON file.
- Execution modes:
  - **Auto mode**: Agent responds automatically after player input (like "idle trigger" but immediate).
  - **Manual mode**: Player must explicitly submit to trigger agent (like "send trigger").
  - **Turn-based toggle**: Discrete turns vs. continuous interaction.
- State editing (GM mode):
  - Inline editing of inventory, quests, NPCs, reputation.
  - Add/remove/modify state elements directly.
  - Undo/redo support for state edits.
- Responsive UI that works on desktop and tablet.

### Out of Scope

- Mobile phone UI (defer to future spec).
- Voice input/output (text-only).
- Multi-player or collaborative editing (single-player only).
- Cloud-based session storage (client-local only).
- Advanced world-building tools (map editor, NPC relationship graphs, timeline visualizers).
- Integration with external character sheet systems (D&D Beyond, Roll20).
- Procedural content generation UI (random NPC generator, quest templates).
- Achievements, statistics, or analytics dashboards.
- Social features (sharing sessions, community content).

### Assumptions

- Spec 002 (Narrator Agent & Skills) is implemented and provides agent loop, skills, and state management.
- UnifiedInference (spec 001) provides LLM access for narration and planning.
- UI is a web application (likely Blazor, React, or similar SPA framework).
- IndexedDB is available for client-side persistence.
- Sessions are single-player and do not require synchronization.
- Users expect near-instant feedback for UI actions (< 200ms for state updates, < 3s for narration generation).

### Open Questions *(mandatory)*

- NEEDS CLARIFICATION: Should the UI support theming (light/dark mode, color schemes)?
- NEEDS CLARIFICATION: Should state editing (GM mode) be prominently featured or hidden behind an "advanced" toggle?
- NEEDS CLARIFICATION: Should session export include full state or support selective export (e.g., just character data)?
- NEEDS CLARIFICATION: Should the agent insight panel be visible by default or collapsible/hideable?
- NEEDS CLARIFICATION: Should narration history be unlimited or capped with archival/summarization?


## User Scenarios & Testing *(mandatory)*

**Constitution note**: If the feature changes UI components, acceptance scenarios MUST be coverable via Playwright for .NET E2E tests in addition to any applicable unit tests.

### User Story 1 - Interactive Narration with Player Input (Priority: P1)

As a player, I want to read streaming narration and input actions, so I can interact with the story in real-time.

**Why this priority**: This is the core user experience; without it, the UI is non-functional.

**Independent Test**: Open UI, input player action, verify narration streams back and is displayed.

**Acceptance Scenarios**:

1. **Given** the UI is loaded, **When** the player types an action (e.g., "I explore the cave") and submits, **Then** narration begins streaming back and displays in the story area in real-time.
2. **Given** narration is streaming, **When** new text arrives, **Then** the UI auto-scrolls to show the latest text and highlights new content briefly.
3. **Given** the player is in manual mode, **When** the player types without submitting, **Then** no narration is triggered until the player explicitly submits.
4. **Given** the player is in auto mode, **When** the player finishes typing (stops for 500ms), **Then** the agent automatically processes the input and returns narration.
5. **Given** narration is in progress, **When** the player tries to input a new action, **Then** the input area is disabled until narration completes or the player cancels.

---

### User Story 2 - View and Understand Game State (Priority: P1)

As a player, I want to view my inventory, active quests, known NPCs, and reputation, so I understand my current situation.

**Why this priority**: State visibility is essential for informed decision-making.

**Independent Test**: Trigger state changes via agent, verify UI updates reflect changes accurately.

**Acceptance Scenarios**:

1. **Given** the agent updates inventory (adds an item), **When** the narration completes, **Then** the inventory viewer shows the new item with correct details.
2. **Given** the agent creates a quest, **When** the narration completes, **Then** the quest log viewer shows the new quest with objectives and status.
3. **Given** the agent updates NPC relationship, **When** the narration completes, **Then** the NPC registry shows updated relationship status and recent dialogue.
4. **Given** the agent updates reputation, **When** the narration completes, **Then** the reputation dashboard shows new standing and any status effects.
5. **Given** multiple state changes occur in one turn, **When** the narration completes, **Then** all affected state viewers update atomically and consistently.

---

### User Story 3 - Edit Game State (GM Mode) (Priority: P2)

As a player or GM, I want to edit game state directly, so I can correct errors or customize the experience.

**Why this priority**: Enables player agency and error correction without restarting.

**Independent Test**: Open state editor, modify item/quest/NPC, verify changes persist and are visible in narration context.

**Acceptance Scenarios**:

1. **Given** the inventory viewer is open, **When** the player clicks "Add Item" and fills in details, **Then** the item is added to inventory and appears in the viewer.
2. **Given** the player selects an inventory item, **When** the player clicks "Edit" and changes quantity, **Then** the quantity updates and the change is recorded with provenance.
3. **Given** the player edits a quest objective, **When** the player saves the change, **Then** the quest log reflects the update and the agent can reference it in future narration.
4. **Given** the player makes multiple edits, **When** the player clicks "Undo", **Then** the most recent edit is reverted.
5. **Given** state edits have been made, **When** the player saves the session, **Then** edits are included in the saved state.

---

### User Story 4 - Configure Narrator and LLM Settings (Priority: P2)

As a player, I want to configure the narrator's LLM, style, and behavior, so the experience matches my preferences.

**Why this priority**: Customization improves satisfaction and enables experimentation.

**Independent Test**: Change settings, trigger narration, verify narration reflects settings.

**Acceptance Scenarios**:

1. **Given** the settings panel is open, **When** the player selects a different LLM model (e.g., from balanced to quality), **Then** subsequent narration uses the new model and the change is visible in the agent insight panel.
2. **Given** the player adjusts narrator style (e.g., from neutral to dramatic), **When** the agent generates narration, **Then** the tone matches the selected style.
3. **Given** the player enables "enforce consistency checks", **When** the agent detects an inconsistency, **Then** the agent flags it in the insight panel and optionally pauses for player input.
4. **Given** the player changes settings, **When** the session is saved, **Then** settings are persisted and restored on load.

---

### User Story 5 - Save and Load Sessions (Priority: P1)

As a player, I want to save my current session and load it later, so I can continue the story at any time.

**Why this priority**: Continuity is critical for long-form storytelling.

**Independent Test**: Play a session, save it, reload the UI, load the session, verify state and history are restored.

**Acceptance Scenarios**:

1. **Given** the player has interacted with the narrator and state has been updated, **When** the player clicks "Save Session", **Then** the session is saved to IndexedDB with a timestamp and title.
2. **Given** the player refreshes the page, **When** the UI loads, **Then** the most recent session is auto-loaded and state is restored.
3. **Given** multiple save slots exist, **When** the player views save slot list, **Then** each slot shows title, last played timestamp, and a thumbnail or summary.
4. **Given** the player selects a save slot, **When** the player clicks "Load", **Then** the session is loaded and the UI reflects the saved state and history.
5. **Given** the player clicks "Export Session", **When** the export completes, **Then** a JSON file is downloaded containing full session state.
6. **Given** the player has a JSON session file, **When** the player clicks "Import Session" and selects the file, **Then** the session is loaded and the UI reflects the imported state.

---

### User Story 6 - View Agent Insights (Transparency) (Priority: P3)

As a player or developer, I want to see the agent's plan, reasoning, and skill execution details, so I understand what the agent is doing.

**Why this priority**: Transparency builds trust and aids debugging.

**Independent Test**: Trigger agent execution, open insight panel, verify plan and skill logs are displayed.

**Acceptance Scenarios**:

1. **Given** the agent generates a plan, **When** the insight panel is open, **Then** the panel displays the plan JSON or a visual representation (flowchart, tree).
2. **Given** the agent executes skills, **When** skills complete, **Then** the insight panel logs each skill execution with name, parameters, result, and timing.
3. **Given** the agent updates state, **When** the insight panel is open, **Then** a state diff is shown (before vs. after) highlighting changes.
4. **Given** the agent encounters an error, **When** the error occurs, **Then** the insight panel shows error details, stack trace (if available), and suggested actions.
5. **Given** the player is debugging, **When** the player hovers over a skill in the plan, **Then** a tooltip shows skill description and expected behavior.

---

### User Story 7 - Responsive and Accessible UI (Priority: P2)

As a player, I want the UI to work smoothly on desktop and tablet, so I can play anywhere.

**Why this priority**: Usability and reach.

**Independent Test**: Load UI on different screen sizes, verify layout adapts and remains usable.

**Acceptance Scenarios**:

1. **Given** the UI is loaded on a desktop, **When** the window is resized, **Then** panels adjust layout and remain readable.
2. **Given** the UI is loaded on a tablet, **When** the player interacts, **Then** touch gestures work and text is readable without zooming.
3. **Given** the player uses keyboard navigation, **When** the player tabs through UI elements, **Then** focus is visible and logical.
4. **Given** the player uses a screen reader, **When** the player navigates, **Then** UI elements are announced with meaningful labels.

---

### User Story 8 - Handle Long Sessions and History (Priority: P3)

As a player, I want the UI to handle long sessions with many turns without slowing down or losing history, so I can play extended campaigns.

**Why this priority**: Performance and reliability for dedicated users.

**Independent Test**: Simulate 100+ turn session, verify UI remains responsive and history is accessible.

**Acceptance Scenarios**:

1. **Given** the player has played 100+ turns, **When** the UI is loaded, **Then** the narration history is capped and older turns are summarized or archived.
2. **Given** the player wants to review old narration, **When** the player scrolls to the top of the story area, **Then** older turns load progressively (infinite scroll or pagination).
3. **Given** the session has 100+ turns, **When** the player saves the session, **Then** save time is reasonable (< 5s) and state is not corrupted.
4. **Given** a large session is loaded, **When** the UI renders, **Then** initial load time is acceptable (< 3s) and UI is responsive.

---

### Edge Cases

- Player types very long input (> 10,000 characters).
- LLM takes a long time to respond (> 10s).
- IndexedDB quota is exceeded (too many sessions or large state).
- Player attempts to load a corrupted or incompatible session file.
- Player rapidly submits multiple actions while narration is in progress.
- Network is unavailable (LLM calls fail).
- Browser tab is backgrounded or device sleeps during narration generation.
- Player edits state while agent is processing a plan (concurrent modification).


## Interface Contract *(mandatory)*

### New/Changed Public APIs

- **UI Component Library**: React/Blazor components for story display, input, state viewers, insight panel, settings, session management.
  - `StoryDisplay`: Renders streaming narration with auto-scroll and highlighting.
  - `PlayerInput`: Text area with submit button and auto/manual mode toggle.
  - `InventoryViewer`: Grid/list of items with add/edit/remove actions.
  - `QuestLogViewer`: List of quests with objectives and status.
  - `NpcRegistryViewer`: List of NPCs with relationship and dialogue tabs.
  - `ReputationDashboard`: Bars or gauges showing faction/NPC standings.
  - `WorldStateViewer`: Location, time, flags display.
  - `ChoiceHistoryViewer`: Timeline or list of past choices.
  - `AgentInsightPanel`: Plan display, skill log, state diff viewer.
  - `SettingsPanel`: LLM selection, narrator style, game rules.
  - `SessionManager`: Save/load/export/import session controls.

### Events / Messages *(if applicable)*

- **PlayerActionSubmitted** — PlayerInput → Agent Loop — Emitted when player submits an action.
- **NarrationChunkReceived** — Agent Loop → StoryDisplay — Emitted when narration text arrives.
- **StateUpdated** — Agent Loop → State Viewers — Emitted when game state changes.
- **PlanGenerated** — Agent Loop → AgentInsightPanel — Emitted when agent creates a plan.
- **SkillExecuted** — Skill Executor → AgentInsightPanel — Emitted when a skill completes.
- **SessionSaved** — SessionManager → Storage — Emitted when session is saved.
- **SessionLoaded** — SessionManager → UI Components — Emitted when session is loaded.

### Data Contracts *(if applicable)*

- **UiState**: UI-specific state (panel visibility, scroll position, selected tab).
  - Fields: `storyScrollPosition` (int), `activeTab` (enum), `panelStates` (map of panel id → visibility).
- **SessionMetadata**: Metadata for save slots.
  - Fields: `slotId` (string), `title` (string), `lastPlayed` (timestamp), `thumbnail` (base64 image or URL), `turnCount` (int).
- **NarrationChunk**: Streaming narration piece.
  - Fields: `text` (string), `isComplete` (bool), `timestamp` (timestamp).
- **StateViewerFilter**: Filter for state viewers.
  - Fields: `showCompleted` (bool, for quests), `sortBy` (enum), `searchQuery` (string).


## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a web-based UI for narrator interaction.
- **FR-002**: UI MUST display streaming narration in a story area with auto-scroll and highlighting of new text.
- **FR-003**: UI MUST provide a player input area with text entry and submit button.
- **FR-004**: UI MUST support auto mode (immediate agent response) and manual mode (explicit submit required).
- **FR-005**: UI MUST disable player input while narration is in progress (in manual mode).
- **FR-006**: UI MUST provide inventory viewer showing items with name, description, quantity, metadata.
- **FR-007**: Inventory viewer MUST support add, edit, remove actions for items (GM mode).
- **FR-008**: UI MUST provide quest log viewer showing active and completed quests with objectives.
- **FR-009**: Quest log viewer MUST support add, edit, complete actions for quests (GM mode).
- **FR-010**: UI MUST provide NPC registry viewer showing NPCs with attributes, relationships, dialogue history.
- **FR-011**: NPC registry viewer MUST support add, edit actions for NPCs (GM mode).
- **FR-012**: UI MUST provide reputation dashboard showing faction and NPC standings with status effects.
- **FR-013**: Reputation dashboard MUST support editing reputation scores (GM mode).
- **FR-014**: UI MUST provide world state viewer showing location, time, flags.
- **FR-015**: World state viewer MUST support editing location, time, flags (GM mode).
- **FR-016**: UI MUST provide choice history viewer showing past player decisions and consequences.
- **FR-017**: UI MUST provide agent insight panel showing plan, skill log, state diff.
- **FR-018**: Agent insight panel MUST update in real-time as agent executes plan and skills.
- **FR-019**: UI MUST provide settings panel for LLM selection (provider, model, profile).
- **FR-020**: UI MUST provide settings panel for narrator style (tone, verbosity, agency level).
- **FR-021**: UI MUST provide settings panel for game rules (consistency checks, auto-save).
- **FR-022**: UI MUST save current session to IndexedDB with session metadata (title, timestamp, thumbnail).
- **FR-023**: UI MUST load session from IndexedDB and restore full state and history.
- **FR-024**: UI MUST support multiple save slots with session metadata display.
- **FR-025**: UI MUST export session to JSON file.
- **FR-026**: UI MUST import session from JSON file with validation.
- **FR-027**: UI MUST auto-load most recent session on page load.
- **FR-028**: UI MUST support undo/redo for state edits (GM mode).
- **FR-029**: UI MUST be responsive and adapt layout for desktop and tablet screen sizes.
- **FR-030**: UI MUST support keyboard navigation for all interactive elements.
- **FR-031**: UI MUST provide accessible labels and ARIA attributes for screen readers.
- **FR-032**: UI MUST cap narration history display and archive or summarize older turns (default: show last 50 turns, summarize older).
- **FR-033**: UI MUST support progressive loading of older narration history (infinite scroll or pagination).
- **FR-034**: UI MUST render initial load in < 3s for typical sessions (< 100 turns).
- **FR-035**: UI MUST update state viewers in < 200ms after state change event.
- **FR-036**: UI MUST disable concurrent state edits while agent is processing to prevent conflicts.

### Error Handling *(mandatory)*

- **EH-001**: If agent loop fails to generate narration, UI MUST display an error message with retry option and preserve player input.
- **EH-002**: If LLM call times out, UI MUST display a timeout message and offer to retry or cancel.
- **EH-003**: If state viewer fails to render (e.g., due to invalid state), UI MUST display a fallback error message and log the issue.
- **EH-004**: If session save fails (e.g., IndexedDB quota exceeded), UI MUST notify the player and offer to export session to file.
- **EH-005**: If session load fails (e.g., corrupted data), UI MUST notify the player, log the error, and offer to start a new session or import from file.
- **EH-006**: If session import fails (e.g., invalid JSON), UI MUST display validation errors and preserve current session.
- **EH-007**: If state edit is invalid (e.g., negative quantity), UI MUST display validation error inline and prevent save.
- **EH-008**: If player attempts to submit input while narration is in progress, UI MUST show a message explaining input is disabled until narration completes.
- **EH-009**: If network is unavailable during LLM call, UI MUST display offline message and queue action for retry when network returns (if supported) or allow manual retry.
- **EH-010**: All errors MUST be logged to browser console with context (session id, action, component name).

### State & Data *(mandatory if feature involves data)*

- **Persistence**: Sessions are persisted to client-side IndexedDB with the following structure:
  - `sessions` object store: key = session id, value = full session state (from spec 002) + UI state + session metadata.
  - `settings` object store: key = "default", value = narrator settings (LLM selection, style, game rules).
- **Invariants**:
  - Session metadata (title, timestamp) must always be present.
  - Session state must conform to spec 002 GameState schema.
  - UI state must be serializable to JSON.
  - Save/load must be idempotent (save → load → save produces identical state).
- **Migration/Compatibility**:
  - If IndexedDB schema changes, migration must occur transparently on load.
  - If session state schema (from spec 002) changes, migration is handled by spec 002 logic.
  - UI state schema changes must be backward-compatible (missing fields default to safe values).

### Key Entities *(include if feature involves data)*

- **Session**: Container for game state (spec 002) + UI state + metadata.
- **UiState**: UI-specific ephemeral state (scroll position, panel visibility).
- **SessionMetadata**: Save slot display information (title, timestamp, thumbnail).
- **NarratorSettings**: Persistent user preferences (LLM, style, rules).


## Test Matrix *(mandatory)*

| Requirement ID | Unit Tests | Integration Tests | E2E (Playwright) | Notes |
|---|---|---|---|---|
| FR-001 | N | N | Y | UI loads and renders |
| FR-002 | N | Y | Y | Streaming narration displays |
| FR-003 | N | N | Y | Player input area works |
| FR-004 | N | N | Y | Auto/manual mode toggle |
| FR-006 | N | Y | Y | Inventory viewer displays items |
| FR-007 | N | Y | Y | Inventory editing (GM mode) |
| FR-008 | N | Y | Y | Quest log viewer displays quests |
| FR-010 | N | Y | Y | NPC registry viewer displays NPCs |
| FR-012 | N | Y | Y | Reputation dashboard displays standings |
| FR-017 | N | Y | Y | Agent insight panel displays plan |
| FR-022 | Y | Y | Y | Session save to IndexedDB |
| FR-023 | Y | Y | Y | Session load from IndexedDB |
| FR-025 | Y | N | Y | Session export to JSON |
| FR-026 | Y | Y | Y | Session import from JSON |
| FR-028 | N | Y | Y | Undo/redo for state edits |
| FR-029 | N | N | Y | Responsive layout |
| FR-030 | N | N | Y | Keyboard navigation |
| FR-034 | N | Y | Y | Initial load performance |
| FR-035 | N | Y | Y | State viewer update latency |
| EH-001 | N | Y | Y | Agent failure handling |
| EH-004 | Y | Y | Y | Session save failure handling |
| EH-005 | Y | Y | Y | Session load failure handling |
| EH-006 | Y | Y | Y | Session import validation |


## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: UI loads and renders initial view in < 3s for typical sessions (< 100 turns) in 95%+ of cases.
- **SC-002**: Narration streaming latency is < 100ms from agent output to UI display in 90%+ of cases.
- **SC-003**: State viewer updates reflect changes in < 200ms in 95%+ of cases.
- **SC-004**: Session save/load completes in < 2s for typical sessions in 95%+ of cases.
- **SC-005**: UI remains responsive (no frozen frames) during narration streaming in 99%+ of cases.
- **SC-006**: All interactive elements are keyboard-accessible and pass automated accessibility checks (e.g., axe-core).
- **SC-007**: UI adapts correctly to desktop (1920x1080) and tablet (768x1024) screen sizes without horizontal scrolling or unreadable text.
- **SC-008**: Session export/import round-trips without data loss for all test sessions.
- **SC-009**: Undo/redo works correctly for all state edit types in 100% of test cases.
- **SC-010**: Playwright E2E tests cover all critical user flows (narration, state viewing, session save/load, settings) with 95%+ pass rate.


## Clarifications

### Session 2026-01-23

- None yet; open questions await stakeholder input.
