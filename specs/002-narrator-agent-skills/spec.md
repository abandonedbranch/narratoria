# Feature Specification: Narrator Agent & Skills System

**Feature Branch**: `002-narrator-agent-skills`
**Created**: January 23, 2026
**Status**: Draft
**Input**: User description: "I want to create an AI narrator where the narrator has the ability to use agent skills to leverage different tools based on a plan (generated JSON) to do things like maintain game state, player inventory, reputation, quest log, known NPCs and more."


## Scope *(mandatory)*

### In Scope

- Agent-based narrator system that generates JSON plans and executes them via a skill system.
- Planning loop: Context → LLM Plan Generation → JSON Plan → Skill Execution → State Update → Narration.
- Core skills for game state management:
  - **Game State Storage**: Persist and retrieve session state.
  - **Inventory Management**: Add, remove, query player items with quantities and metadata.
  - **Quest Log Management**: Create, update, track, complete quests with stages and objectives.
  - **NPC Tracking**: Remember NPCs with attributes, relationships, dialogue history, last interactions.
  - **Reputation System**: Track faction/NPC relationships with numeric scores and status effects.
  - **Player Choice History**: Record decisions for narrative callbacks and consequences.
  - **World State**: Track locations, time progression, environmental flags.
- Narrator skills:
  - **Generate Narration**: Create story text based on context and plan outcomes.
  - **Rewrite For Style**: Polish grammar, tone, consistency.
  - **Summarize Session**: Create recaps of story progress.
  - **Check Consistency**: Validate state against narrative context.
  - **Generate Player Options**: Create choice branches based on current state.
- Plan JSON schema with action sequences, parameters, conditional logic, dependencies.
- Skill interface contract defining inputs, outputs, side effects, error handling.
- Integration with UnifiedInference (spec 001) for LLM calls.
- Cancellation support throughout agent loop and skill execution.
- State versioning and migration strategy.
- Atomic state transactions for consistency.

### Out of Scope

- UI implementation (deferred to spec 003).
- Real-time voice/audio narration (text-only initially).
- Multi-player shared worlds or collaborative storytelling.
- Character sheet automation (leveling, combat stats, dice rolls).
- Procedural content generation (maps, dungeons, encounters).
- Integration with external game engines (Unity, Unreal).
- Training/fine-tuning custom narrator models.
- Analytics, leaderboards, or social features.

### Assumptions

- Callers will provide context about the player's action or input to drive the agent loop.
- UnifiedInference (spec 001) provides reliable LLM access for plan generation and narration.
- Plans can occasionally fail or be incomplete; the system must degrade gracefully.
- Game state fits in memory for a single session (no distributed state management).
- Skills are synchronous or async but complete within reasonable time (< 10s per skill).
- State persistence is handled by a storage skill but session lifecycle is managed by the caller.

### Open Questions *(mandatory)*

- NEEDS CLARIFICATION: Should the plan generator support multi-turn planning (agent requests more info before finalizing plan)?
- NEEDS CLARIFICATION: Should skills support rollback/undo, or is append-only state sufficient?
- NEEDS CLARIFICATION: What level of narrative autonomy should the agent have (proactive suggestions vs. reactive responses)?
- NEEDS CLARIFICATION: Should reputation effects be numeric-only or support qualitative states (friendly, hostile, neutral)?
- NEEDS CLARIFICATION: How should quest dependencies be modeled (linear, branching, networked)?


## User Scenarios & Testing *(mandatory)*

### User Story 1 - Agent Generates and Executes Plans (Priority: P1)

As a developer integrating the narrator, I want the agent to receive context, generate a plan, and execute skills, so the narrator can autonomously manage game state and narration.

**Why this priority**: This is the core agent loop and must work for any other functionality to be viable.

**Independent Test**: Provide player action context, verify agent generates valid JSON plan, executes skills, and returns narration.

**Acceptance Scenarios**:

1. **Given** the agent receives player action "I pick up the sword", **When** the agent loop runs, **Then** a plan is generated that includes inventory skill execution and narration generation.
2. **Given** a plan with multiple skills, **When** the plan is executed, **Then** skills run in dependency order and state updates are atomic.
3. **Given** a skill fails during execution, **When** the agent handles the error, **Then** the agent either retries, skips to fallback, or reports failure without corrupting state.

---

### User Story 2 - Inventory Management Skill (Priority: P1)

As a narrator agent, I want to add, remove, and query player inventory, so I can track what the player is carrying and reference it in narration.

**Why this priority**: Inventory is fundamental to most interactive narratives and game mechanics.

**Independent Test**: Call inventory skill to add item, verify state update; call query skill, verify item is returned.

**Acceptance Scenarios**:

1. **Given** the agent plan includes "add item: rusty sword", **When** the inventory skill executes, **Then** the item is added to player inventory with metadata (description, quantity, acquired timestamp).
2. **Given** the player has an item, **When** the agent queries inventory, **Then** the skill returns the item with current quantity and metadata.
3. **Given** the agent plan includes "remove item: health potion", **When** the inventory skill executes, **Then** the item quantity decreases or item is removed if quantity reaches zero.
4. **Given** the agent tries to remove an item not in inventory, **When** the inventory skill executes, **Then** the skill returns an error without corrupting state.

---

### User Story 3 - Quest Log Management Skill (Priority: P2)

As a narrator agent, I want to create, update, and complete quests, so I can track the player's objectives and progress.

**Why this priority**: Quest tracking enables structured narrative progression and player goal visibility.

**Independent Test**: Create quest, update objective, verify state; complete quest, verify completion status.

**Acceptance Scenarios**:

1. **Given** the agent plan includes "create quest: Find the Lost Artifact", **When** the quest skill executes, **Then** a new quest is created with title, description, objectives, and status (active).
2. **Given** an active quest exists, **When** the agent updates an objective (e.g., "Discovered artifact location"), **Then** the objective is marked completed and quest progress is updated.
3. **Given** all quest objectives are completed, **When** the agent completes the quest, **Then** the quest status changes to completed with completion timestamp.
4. **Given** the agent tries to update a non-existent quest, **When** the quest skill executes, **Then** the skill returns an error without creating invalid state.

---

### User Story 4 - NPC Tracking Skill (Priority: P2)

As a narrator agent, I want to remember NPCs with attributes, relationships, and dialogue history, so I can create consistent character interactions.

**Why this priority**: NPC continuity is essential for believable worlds and character-driven stories.

**Independent Test**: Add NPC, query NPC, update relationship, verify state changes.

**Acceptance Scenarios**:

1. **Given** the agent plan includes "remember NPC: Eldara the Merchant", **When** the NPC skill executes, **Then** the NPC is added with name, description, attributes, and last interaction timestamp.
2. **Given** an NPC exists, **When** the agent queries the NPC, **Then** the skill returns NPC details including dialogue history and relationship status.
3. **Given** the agent updates NPC relationship (e.g., "player helped Eldara"), **When** the NPC skill executes, **Then** the relationship score or status is updated with provenance (why it changed).
4. **Given** an interaction with an NPC occurs, **When** the agent records dialogue, **Then** the dialogue is appended to NPC history with context and timestamp.

---

### User Story 5 - Reputation System Skill (Priority: P3)

As a narrator agent, I want to track faction and NPC reputation, so I can adjust narrative tone and available options based on relationships.

**Why this priority**: Reputation adds depth and consequences to player choices.

**Independent Test**: Update reputation, query reputation, verify faction standing changes.

**Acceptance Scenarios**:

1. **Given** the agent plan includes "increase reputation with Thieves Guild", **When** the reputation skill executes, **Then** the reputation score increases and status effects are recalculated.
2. **Given** the player has negative reputation with a faction, **When** the agent queries reputation, **Then** the skill returns current standing and active status effects (e.g., "hostile", "unwelcome").
3. **Given** reputation crosses a threshold, **When** the reputation skill updates, **Then** status effects change (e.g., "neutral" becomes "friendly") and are recorded with provenance.

---

### User Story 6 - Generate Narration Skill (Priority: P1)

As a narrator agent, I want to generate story text based on context and plan outcomes, so I can present coherent narrative to the player.

**Why this priority**: Narration is the primary output of the narrator system.

**Independent Test**: Provide context and plan outcomes, verify narration is generated and coherent.

**Acceptance Scenarios**:

1. **Given** the agent has executed skills (e.g., inventory update, NPC interaction), **When** the narration skill runs, **Then** narrative text is generated that reflects skill outcomes and current state.
2. **Given** an error occurred during skill execution, **When** the narration skill runs, **Then** the narrative gracefully incorporates the failure or omits it without breaking immersion.
3. **Given** the player's action context includes specific tone or style preferences, **When** the narration skill runs, **Then** the narrative matches the requested tone (e.g., dramatic, humorous, terse).

---

### User Story 7 - Plan Validation and Error Recovery (Priority: P2)

As a narrator agent, I want to validate plans before execution and recover from failures, so the system remains stable and predictable.

**Why this priority**: Robustness is critical for a good developer experience and prevents state corruption.

**Independent Test**: Provide invalid plan, verify validation catches errors; cause skill failure, verify agent recovers gracefully.

**Acceptance Scenarios**:

1. **Given** the agent generates a plan with invalid JSON syntax, **When** the plan is validated, **Then** the validation fails and a fallback plan or error is returned without executing any skills.
2. **Given** the agent generates a plan referencing non-existent skills, **When** the plan is validated, **Then** the validation fails with clear error details.
3. **Given** a skill fails during execution, **When** the error is caught, **Then** the agent either retries (for transient errors) or proceeds with a partial plan while logging the failure.
4. **Given** cancellation is requested mid-execution, **When** the agent loop processes cancellation, **Then** in-flight skills are cancelled, state updates are rolled back or left consistent, and cancellation propagates correctly.

---

### User Story 8 - Check Consistency Skill (Priority: P3)

As a narrator agent, I want to validate current state against narrative context, so I can catch and resolve continuity errors.

**Why this priority**: Ensures narrative quality and reduces immersion-breaking contradictions.

**Independent Test**: Introduce a state inconsistency (e.g., item used but still in inventory), verify consistency skill detects it.

**Acceptance Scenarios**:

1. **Given** the agent has recorded conflicting information (e.g., NPC marked dead but later referenced alive), **When** the consistency skill runs, **Then** the inconsistency is flagged with details and suggested resolutions.
2. **Given** state is consistent with narrative history, **When** the consistency skill runs, **Then** no issues are reported.

---

### Edge Cases

- LLM generates malformed JSON plan (syntax errors, missing required fields).
- Plan references skills that don't exist or have incorrect parameter types.
- Skill execution times out or hangs.
- State update conflicts (concurrent modifications).
- Player action is ambiguous or context is insufficient for plan generation.
- Plan includes circular dependencies between skills.
- Reputation or quest state reaches boundary values (e.g., max reputation, completed quest archive overflow).
- State serialization/deserialization fails due to schema version mismatch.


## Interface Contract *(mandatory)*

### New/Changed Public APIs

- **Agent Loop**: Entry point for narrator execution. Accepts context, returns narration and updated state.
  - `ExecuteAgentLoop(context: NarratorContext, cancellationToken: CancellationToken): Task<NarratorResult>`
- **Plan Generator**: LLM-based planning service.
  - `GeneratePlan(context: NarratorContext, cancellationToken: CancellationToken): Task<NarratorPlan>`
- **Skill Executor**: Interprets and executes plan actions.
  - `ExecutePlan(plan: NarratorPlan, state: GameState, cancellationToken: CancellationToken): Task<SkillExecutionResult>`
- **Skill Interface**: Common contract for all skills.
  - `ISkill.Execute(parameters: SkillParameters, state: GameState, cancellationToken: CancellationToken): Task<SkillResult>`

### Events / Messages *(if applicable)*

- **PlanGenerated** — Agent Loop → Observers — Emitted when LLM generates a plan (for logging/debugging).
- **SkillExecuted** — Skill Executor → Observers — Emitted when a skill completes (for telemetry).
- **StateUpdated** — Skill Executor → Observers — Emitted when game state changes (for persistence triggers).
- **AgentError** — Agent Loop → Observers — Emitted when the agent encounters an error (for alerting).

### Data Contracts *(if applicable)*

- **NarratorContext**: Input context for agent loop.
  - Fields: `playerAction` (string), `currentState` (GameState), `sessionId` (string), `preferences` (NarratorPreferences).
- **NarratorResult**: Output of agent loop.
  - Fields: `narration` (string), `updatedState` (GameState), `planSummary` (string), `errors` (list of errors).
- **NarratorPlan**: JSON plan structure.
  - Fields: `actions` (list of SkillAction), `reasoning` (string), `dependencies` (map of action dependencies).
- **SkillAction**: Single action in a plan.
  - Fields: `skillName` (string), `parameters` (map), `actionId` (string).
- **GameState**: Unified game state container.
  - Fields: `inventory` (InventoryState), `quests` (QuestLog), `npcs` (NpcRegistry), `reputation` (ReputationMap), `worldState` (WorldState), `playerChoices` (ChoiceHistory), `metadata` (StateMetadata).
- **InventoryState**: Player inventory.
  - Fields: `items` (map of item id → InventoryItem).
- **InventoryItem**: Single inventory item.
  - Fields: `id` (string), `name` (string), `description` (string), `quantity` (int), `metadata` (map), `acquiredAt` (timestamp).
- **QuestLog**: Active and completed quests.
  - Fields: `activeQuests` (list of Quest), `completedQuests` (list of Quest).
- **Quest**: Single quest.
  - Fields: `id` (string), `title` (string), `description` (string), `objectives` (list of Objective), `status` (enum: Active, Completed, Failed), `createdAt` (timestamp), `completedAt` (timestamp?).
- **Objective**: Quest objective.
  - Fields: `id` (string), `description` (string), `completed` (bool), `completedAt` (timestamp?).
- **NpcRegistry**: Known NPCs.
  - Fields: `npcs` (map of npc id → Npc).
- **Npc**: Single NPC.
  - Fields: `id` (string), `name` (string), `description` (string), `attributes` (map), `relationship` (RelationshipStatus), `dialogueHistory` (list of DialogueEntry), `lastInteractionAt` (timestamp).
- **DialogueEntry**: Single dialogue record.
  - Fields: `speaker` (string), `text` (string), `context` (string), `timestamp` (timestamp).
- **ReputationMap**: Reputation scores.
  - Fields: `factions` (map of faction id → ReputationScore), `npcs` (map of npc id → ReputationScore).
- **ReputationScore**: Reputation with faction or NPC.
  - Fields: `score` (int), `status` (enum: Hostile, Unfriendly, Neutral, Friendly, Allied), `effects` (list of status effects), `provenance` (list of ReputationChange).
- **ReputationChange**: Record of reputation change.
  - Fields: `reason` (string), `delta` (int), `timestamp` (timestamp).
- **WorldState**: World flags and time.
  - Fields: `currentLocation` (string), `timeOfDay` (string), `flags` (map of flag id → bool), `environmentalState` (map).
- **ChoiceHistory**: Player decisions.
  - Fields: `choices` (list of Choice).
- **Choice**: Single player choice.
  - Fields: `id` (string), `description` (string), `selected` (string), `alternatives` (list of strings), `consequences` (list of strings), `timestamp` (timestamp).
- **StateMetadata**: State versioning and session info.
  - Fields: `version` (string), `sessionId` (string), `createdAt` (timestamp), `lastUpdatedAt` (timestamp).
- **SkillParameters**: Input to a skill.
  - Fields: `parameters` (map), `context` (string).
- **SkillResult**: Output from a skill.
  - Fields: `success` (bool), `output` (map), `stateChanges` (list of StateChange), `errors` (list of errors).
- **StateChange**: Description of a state modification.
  - Fields: `path` (string, e.g., "inventory.items.sword"), `operation` (enum: Add, Update, Remove), `value` (object), `provenance` (string).


## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide an agent loop that accepts context and returns narration and updated state.
- **FR-002**: Agent loop MUST generate plans via LLM using UnifiedInference (spec 001).
- **FR-003**: Plans MUST be valid JSON conforming to a documented schema (see contracts/plan-schema.json).
- **FR-004**: System MUST validate plans before execution and reject invalid plans with clear errors.
- **FR-005**: System MUST execute skills in dependency order as specified by the plan.
- **FR-006**: Skills MUST implement a common interface (ISkill) with Execute method accepting parameters, state, and cancellationToken.
- **FR-007**: System MUST support inventory management skill with add, remove, query operations.
- **FR-008**: System MUST support quest log management skill with create, update, complete, query operations.
- **FR-009**: System MUST support NPC tracking skill with add, update, query operations including dialogue history.
- **FR-010**: System MUST support reputation system skill with update, query operations for factions and NPCs.
- **FR-011**: System MUST support player choice history skill with record and query operations.
- **FR-012**: System MUST support world state skill with location, time, and flag management.
- **FR-013**: System MUST support narration generation skill that produces text based on context and skill outcomes.
- **FR-014**: System MUST support rewrite-for-style skill that polishes narration text.
- **FR-015**: System MUST support summarize session skill that creates recaps.
- **FR-016**: System MUST support check consistency skill that validates state against narrative.
- **FR-017**: System MUST support generate player options skill that creates choice branches.
- **FR-018**: State updates MUST be atomic within a single skill execution.
- **FR-019**: State updates across multiple skills in a plan MUST either all succeed or leave state in a consistent intermediate state with provenance.
- **FR-020**: All skills and agent loop operations MUST honor CancellationToken and propagate cancellation exceptions.
- **FR-021**: State MUST include versioning metadata (schema version, session id, timestamps).
- **FR-022**: State serialization MUST use a stable JSON schema (see contracts/game-state-schema.json).
- **FR-023**: System MUST support state migration for schema version changes.
- **FR-024**: System MUST record provenance for all state changes (which skill, why, when).
- **FR-025**: Skills MUST return SkillResult with success status, outputs, state changes, and errors.
- **FR-026**: Plans MAY include reasoning text explaining the plan (for debugging/transparency).
- **FR-027**: System MUST support parallel skill execution when plan indicates no dependencies between skills.
- **FR-028**: System MUST limit plan execution time with a configurable timeout (default: 30s per plan).
- **FR-029**: System MUST limit individual skill execution time with a configurable timeout (default: 10s per skill).

### Error Handling *(mandatory)*

- **EH-001**: If LLM fails to generate a plan, agent loop MUST return an error result with fallback narration (e.g., "The narrator pauses to think...").
- **EH-002**: If LLM generates invalid JSON, system MUST log the raw output, attempt to repair common issues (missing braces, trailing commas), and if repair fails, return validation error.
- **EH-003**: If plan validation fails, system MUST return validation errors with actionable messages (e.g., "Skill 'unknown_skill' not found").
- **EH-004**: If a skill fails during execution, system MUST catch the error, log it with context (skill name, parameters, state snapshot), and either retry (for transient errors) or proceed with partial results.
- **EH-005**: If a skill times out, system MUST cancel the skill, log the timeout, and mark the skill as failed without blocking other skills.
- **EH-006**: If state serialization fails, system MUST log the error and throw an exception (state corruption is unacceptable).
- **EH-007**: If state deserialization fails due to version mismatch, system MUST attempt migration and log the outcome; if migration fails, throw an exception.
- **EH-008**: If a skill attempts an invalid state change (e.g., remove non-existent item), skill MUST return error in SkillResult without throwing exception or corrupting state.
- **EH-009**: All errors MUST be logged with ILogger<T> including context (session id, action id, skill name) when available.
- **EH-010**: Transient errors (network, timeout) MUST be retried up to 3 times with exponential backoff before failing.

### State & Data *(mandatory if feature involves data)*

- **Persistence**: Game state must be persistable to JSON for storage. Persistence mechanism (file, database, IndexedDB) is out of scope but state must be serialization-ready.
- **Invariants**:
  - State version must always be present and valid.
  - All state changes must have provenance (skill name, timestamp).
  - Inventory quantities must be non-negative.
  - Quest status transitions must follow valid paths (Active → Completed/Failed, not Completed → Active).
  - NPC relationship scores must be within valid range (implementation-defined, e.g., -100 to 100).
  - Dialogue history must be append-only (no deletions except for archival).
- **Migration/Compatibility**:
  - Each state schema version must have a migration path to the next version.
  - Missing fields in older versions must be initialized with safe defaults.
  - New fields must be optional or have defaults to avoid breaking existing states.
  - Schema version identifier must follow semantic versioning (major.minor.patch).

### Key Entities *(include if feature involves data)*

- **Agent Loop**: Orchestrates plan generation and skill execution.
- **Plan Generator**: LLM-powered planning service.
- **Skill Executor**: Executes plans by invoking skills.
- **Skill Registry**: Maintains available skills and resolves skill names to implementations.
- **Game State**: Unified container for all session state.
- **Skill**: Unit of functionality (inventory, quests, NPCs, narration, etc.).


## Test Matrix *(mandatory)*

| Requirement ID | Unit Tests | Integration Tests | E2E (Playwright) | Notes |
|---|---|---|---|---|
| FR-001 | Y | Y | N | Agent loop accepts context and returns result |
| FR-002 | Y | Y | N | Plan generation calls UnifiedInference |
| FR-003 | Y | N | N | Plans conform to JSON schema |
| FR-004 | Y | Y | N | Invalid plans are rejected |
| FR-005 | Y | Y | N | Skills execute in dependency order |
| FR-006 | Y | N | N | Skills implement ISkill interface |
| FR-007 | Y | Y | N | Inventory skill add/remove/query |
| FR-008 | Y | Y | N | Quest skill create/update/complete |
| FR-009 | Y | Y | N | NPC skill add/update/query |
| FR-010 | Y | Y | N | Reputation skill update/query |
| FR-013 | Y | Y | N | Narration generation skill |
| FR-016 | Y | Y | N | Consistency checking skill |
| FR-018 | Y | N | N | State updates are atomic |
| FR-020 | Y | Y | N | Cancellation is honored |
| FR-022 | Y | N | N | State serialization matches schema |
| FR-023 | Y | Y | N | State migration works |
| FR-027 | Y | Y | N | Parallel skill execution when possible |
| EH-002 | Y | Y | N | Malformed JSON handling |
| EH-004 | Y | Y | N | Skill failure recovery |
| EH-005 | Y | Y | N | Skill timeout handling |
| EH-007 | Y | Y | N | State version migration |


## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Agent generates valid executable plans 95%+ of the time for common player actions in test scenarios.
- **SC-002**: Skill execution succeeds 98%+ of the time (with retries) in integration tests.
- **SC-003**: Game state remains consistent across 100+ turn test sessions with no state corruption.
- **SC-004**: State updates are atomic and traceable; every change has clear provenance.
- **SC-005**: Plan execution completes within 3 seconds for typical actions (90th percentile).
- **SC-006**: Narration quality is coherent and relevant to context in 90%+ of test scenarios (manual review).
- **SC-007**: Consistency check skill detects 95%+ of intentionally introduced contradictions in test data.
- **SC-008**: State serialization/deserialization round-trips without data loss for all test states.
- **SC-009**: Schema migration succeeds for all version transitions in compatibility test matrix.
- **SC-010**: Cancellation stops agent loop and skills within 500ms in 99%+ of test cases.


## Clarifications

### Session 2026-01-23

- None yet; open questions await stakeholder input.
