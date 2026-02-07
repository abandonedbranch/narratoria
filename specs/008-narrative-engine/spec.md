# Feature Specification: Narrative Engine

**Feature Branch**: `008-narrative-engine`
**Created**: 2026-02-02
**Status**: Draft (Open Questions Pending)
**Input**: User description: "Scene pipeline, 4-tier memory system, and choice generation for executing campaigns"

## Prerequisites

**Read first in this order:**
1. [Spec 001 - Tool Protocol](../001-tool-protocol-spec/spec.md) - Understand tool communication and events
2. [Spec 002 - Plan Execution](../002-plan-execution/spec.md) - Understand plan structure, execution semantics, and Narrator AI role (the core loop)
3. [Spec 003 - Skills Framework](../003-skills-framework/spec.md) - Understand how skills are discovered and executed
4. [Spec 004 - Narratoria Skills](../004-narratoria-skills/spec.md) - Understand individual skills (Storyteller, Dice Roller, Memory, Reputation, NPC Perception, etc.)
5. [Spec 006 - Skill State Persistence](../006-skill-state-persistence/spec.md) - Understand how contextual data (memories, lore, reputation) is stored and retrieved
6. [Spec 007 - Campaign Format](../007-campaign-format/spec.md) - Understand campaign structure and content organization

**Why this order is critical**: Spec 008 orchestrates everything. You cannot understand scene execution without grasping:
- **Plans** (002): how Narrator AI generates execution plans
- **Skills** (003-004): what capabilities are available
- **Persistence** (006): what contextual data is available via queries
- **Campaigns** (007): what content the engine is executing

**Key relationships**:
- Spec 008 inherits the **scene loop from Spec 002**: choice → plan generation → execution → results
- Spec 008 invokes **skills from Spec 004** by name; Narrator AI selects them based on scene context
- Spec 008 queries **persistence from Spec 006**: semantic search for memories, exact match for reputation/NPC perception
- Spec 008 executes **campaign content from Spec 007**: lore provides context, plot beats guide pacing

---

## Overview

The Narrative Engine is the runtime brain of Narratoria—it executes campaigns by managing the scene loop and orchestrating skills. If Spec 007 defines the **static campaign package**, Spec 008 defines the **dynamic execution**.

**Core Responsibilities**:
1. **Scene Loop**: Player choice → Plan generation (Phi-3.5 Mini) → Plan execution → Display results
2. **Plan Generation**: Phi-3.5 Mini analyzes context (including retrieved memories via sentence-transformers semantic search) and decides which skills to invoke with what parameters
3. **Contextual Retrieval**: LLM determines what data to fetch (memories, lore, reputation) based on scene needs—semantic search queries to Spec 006 persistence layer using sentence-transformers embeddings
4. **Skill Orchestration**: Execute plan via Spec 002 execution engine, aggregate results, feed into next scene

**Core Goal**: Make choices feel "perplexingly on-point"—as if the AI truly understands the player's character and situation.

**Architectural Principle**: Phi-3.5 Mini is the LLM brain. It decides contextually what data to retrieve (via semantic search on sentence-transformers embeddings), which skills to invoke, and how to synthesize results into cohesive narration. There are no fixed "memory tier budgets"—retrieval is adaptive based on narrative needs. The LLM maintains context awareness across the entire scene loop.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Scene Loop Execution (Priority: P1)

A player makes a choice in the current scene. The system processes that choice, updates memory, retrieves relevant context, generates the next scene's prose and choices, and displays them to the player. This cycle continues throughout gameplay.

**Why this priority**: This is the fundamental gameplay loop—without it, nothing else works. Every other feature builds on this foundation.

**Independent Test**: Can be fully tested with a minimal campaign (manifest + setting + premise) and verifying the cycle completes: choice → prose → new choices.

**Acceptance Scenarios**:

1. **Given** a player viewing a scene with choices, **When** the player selects a choice, **Then** the system displays a new scene with prose and new choices within 3 seconds.
2. **Given** a player makes a choice, **When** the scene transitions, **Then** the previous choice is stored in memory for future reference.
3. **Given** a player at any point in the story, **When** they make a choice, **Then** the new scene logically follows from that choice.

---

### User Story 2 - Memory-Driven Choices (Priority: P1)

The choices presented to the player reflect what has happened in the story. Options appear that reference past events, relationships, and knowledge the player has gained. Players feel that the AI "remembers" their journey.

**Why this priority**: Memory-driven choices are the core differentiator. This creates the "perplexingly on-point" experience that defines Narratoria.

**Independent Test**: Play a campaign where the player helps an NPC early on. Verify that a later scene offers a choice referencing that NPC (e.g., "Mention that Marta sent you").

**Acceptance Scenarios**:

1. **Given** a player who helped an NPC in a previous scene, **When** a related situation arises, **Then** at least one choice option references that past interaction.
2. **Given** a player who learned a secret, **When** that secret becomes relevant, **Then** a choice appears allowing the player to use that knowledge.
3. **Given** a player who made an enemy of an NPC, **When** encountering that NPC's faction, **Then** negative sentiment affects available options.

---

### User Story 3 - Sentiment Tracking (Priority: P2)

NPC relationships evolve based on player actions. Helping an NPC improves sentiment; betraying them worsens it. Sentiment affects how NPCs respond and what options are available when interacting with them.

**Why this priority**: Sentiment makes the world feel reactive and alive. It's the mechanic that makes memory feel consequential.

**Independent Test**: Help an NPC, then return to them later. Verify they respond warmly and offer options unavailable to neutral players.

**Acceptance Scenarios**:

1. **Given** a player with positive sentiment toward an NPC, **When** interacting with that NPC, **Then** dialogue reflects their favorable view.
2. **Given** a player with negative sentiment toward an NPC, **When** requesting help from that NPC, **Then** the NPC refuses or demands compensation.
3. **Given** a player's action toward an NPC, **When** the action is positive/negative, **Then** sentiment adjusts accordingly (±0.1 to ±0.5 based on significance).

---

### User Story 4 - Plot Beat Progression (Priority: P2)

The AI guides the story toward campaign-defined plot beats without railroading the player. When conditions for a beat are met, the AI works it into the narrative. Unreachable beats are gracefully skipped.

**Why this priority**: Plot beats allow authors to create intentional story moments while preserving player agency.

**Independent Test**: Create a campaign with a defined beat (e.g., "Player discovers the hidden passage"). Play until conditions are met and verify the beat triggers.

**Acceptance Scenarios**:

1. **Given** a plot beat with conditions, **When** conditions are satisfied, **Then** the beat triggers within 2 scenes.
2. **Given** a plot beat that becomes unreachable, **When** the player's choices make it impossible, **Then** the AI skips the beat without error.
3. **Given** multiple triggerable beats, **When** conditions overlap, **Then** the highest-priority beat triggers first.

---

### User Story 5 - Episodic Memory (Priority: P3)

Major story events (triumphs and failures) are permanently stored with full context. These episodic memories are always retrieved when relevant and may grant mechanical bonuses in related situations.

**Why this priority**: Episodic memory creates the most powerful narrative callbacks but is relatively rare. Core gameplay works without it.

**Independent Test**: Achieve a major triumph (e.g., defeat a dragon). Later, encounter a situation where that triumph is relevant. Verify the prose references it and a mechanical bonus applies.

**Acceptance Scenarios**:

1. **Given** a player achieves a triumph, **When** a related situation arises later, **Then** the prose references the triumph.
2. **Given** a player experienced a failure, **When** a similar situation arises, **Then** the AI offers a chance at redemption.
3. **Given** an episodic memory exists, **When** relevant, **Then** it provides a +1 mechanical modifier.

---

### User Story 6 - Rules Resolution (Priority: P3)

When outcomes are uncertain, the system resolves them using the rules system (default: 2d6 + modifiers). Results affect the narrative appropriately (success, partial success, failure).

**Why this priority**: Rules add tension and unpredictability. Core narrative works without dice, but rules enhance gameplay.

**Independent Test**: Attempt a risky action. Verify a dice roll occurs with appropriate modifiers, and the outcome affects the prose.

**Acceptance Scenarios**:

1. **Given** an uncertain outcome, **When** the player attempts the action, **Then** a dice roll determines success/partial/failure.
2. **Given** relevant modifiers (skill, sentiment, episodic), **When** rolling, **Then** modifiers are applied correctly.
3. **Given** a roll result, **When** generating the next scene, **Then** prose reflects the outcome appropriately.

---

### Edge Cases

- What happens when the Plan Generator cannot generate valid choices for a scene?
  - Fallback to generic choices: "Wait and observe", "Look around", "Continue forward". Log warning for debugging.
- How does the system handle very long play sessions (1000+ choices)?
  - Summarize older scene summaries to compress context while preserving key information. Store summaries in persistence layer.
- What happens when the LLM generates invalid/inappropriate content?
  - Retry generation with adjusted prompt (max 3 attempts), then fall back to generic safe content.
- What happens when all choices lead to undesirable outcomes?
  - System presents choices honestly; player agency includes accepting consequences. No "bail-out" mechanism.

---

## Requirements *(mandatory)*

### Functional Requirements

#### Plan Generation and Contextual Retrieval

- **FR-001**: Plan Generator (local LLM) MUST analyze current scene context and generate Plan JSON that invokes relevant skills with appropriate input parameters
- **FR-002**: Plan Generator MUST have access to campaign metadata (from Spec 007): world constraints, NPC profiles, plot beats, available skills
- **FR-003**: Plan Generator MUST decide contextually which data to retrieve by invoking skills (Memory, Reputation, NPC Perception) with semantic queries in the generated plan
- **FR-004**: Plan Generator MUST respect campaign constraints (from Spec 007 `world/constraints.md`) when generating plans—e.g., "no resurrection" means never generate plans involving revival
- **FR-005**: Scene summaries MUST be stored after each player choice via the Memory skill (from Spec 004) for future retrieval
- **FR-006**: Plan Generator MAY query lore, recent events, NPC relationships, faction reputation, or episodic memories based on scene needs—retrieval is adaptive, not fixed percentages

#### Scene Transition Pipeline

- **FR-007**: System MUST execute scene transition pipeline: Player Choice → Plan Generation → Plan Execution (via Spec 002) → Aggregate Results → Display Prose and New Choices
- **FR-008**: After each choice, Plan Generator MUST invoke Memory skill to store scene summary with: choice made, outcome, characters involved, location, and significance
- **FR-009**: Plan Generator MUST determine scene type (travel, dialogue, danger, resolution) based on narrative context and generate appropriate skill invocations
- **FR-010**: Plan Generator decides what to retrieve by generating Plan JSON that invokes skills with queries—e.g., `{toolId: "recall-memory", toolPath: "skills/memory/recall-memory.dart", input: {query: "past betrayals", limit: 3}}`
- **FR-011**: Storyteller skill (from Spec 004) MUST produce 2-3 paragraphs of scene-setting narrative when invoked by the plan
- **FR-012**: Player-Choices skill (from Spec 004) MUST produce 3-4 contextually relevant options when invoked by the plan

#### Choice Generation

- **FR-013**: Choices MUST be contextually grounded (reference relevant memories).
- **FR-014**: Choices MUST be character-appropriate (match player's established personality and abilities).
- **FR-015**: Choices MUST be narratively interesting (driven by Storyteller and Player-Choices skills from Spec 004 to nudge toward compelling outcomes).
- **FR-016**: Choices MUST be mechanically valid (respect game rules and world constraints).
- **FR-017**: Player interaction MUST be exclusively choice-based—players select from AI-generated options only. Free-text input is NOT supported. This ensures all player actions are contextually valid and narratively coherent.

#### Rules System

- **FR-017**: System MUST implement default rules: 2d6 + modifiers, with 2-6 = failure, 7-9 = partial, 10-12 = success.
- **FR-018**: Modifiers MUST include: +1 relevant skill/trait, ±1 sentiment, +1 episodic callback.
- **FR-019**: System MUST support custom rules defined in `world/rules.md` (from Spec 007).

#### Plot Integration

- **FR-020**: System MUST check plot beat conditions during Scene Rules step.
- **FR-021**: System MUST trigger satisfied beats within 2 scenes of conditions being met.
- **FR-022**: System MUST gracefully skip unreachable beats.
- **FR-023**: System MUST guide narrative toward defined endings when player choices trend that direction.

---

### Key Entities

- **Scene**: A narrative moment with prose and choices. The atomic unit of gameplay. Generated by executing a plan that orchestrates multiple skills.
- **Plan JSON**: Generated by the LLM Plan Generator (Spec 002), defines which skills to invoke and with what parameters. Skills may query persistence layer (Spec 006) for data.
- **Scene Summary**: Compressed representation of what happened in a scene. Stored via Memory skill after each choice for future semantic retrieval.
- **Sentiment Value**: Numeric score (-1.0 to +1.0) representing an NPC's attitude toward the player. Stored in persistence layer (Spec 006), queried by NPC Perception skill (Spec 004).
- **Plot Beat**: Campaign-defined story moment with trigger conditions and priority (from Spec 007). Plan Generator checks beat conditions and works beats into narrative when conditions met.
- **Choice**: Player-facing option with text, underlying intent, and potential outcomes. Generated by Player-Choices skill (Spec 004) or Storyteller skill based on plan.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Scene transitions complete within 3 seconds on target hardware (8GB RAM device).
- **SC-002**: 80% of generated choices reference relevant past events or player knowledge (memory-driven). **Test method**: Automated entity extraction from choice text, cross-referenced against stored memory events in ObjectBox via embedding similarity match. Pass if ≥80% of sampled choices (50+ choices across 3 campaigns) retrieve ≥1 matching memory event with similarity score ≥0.7.
- **SC-004**: Plot beats trigger within 2 scenes of conditions being met in 95% of cases.
- **SC-005**: NPC dialogue reflects correct sentiment (positive/negative/neutral) in 95% of interactions.
- **SC-006**: System maintains coherent narrative across 100+ consecutive choices without context degradation.
- **SC-007**: Episodic memories surface when relevant in 100% of applicable situations.

> **Note on SC-003 (Removed)**: Previously stated as "Players report feeling the AI remembers their choices in 90% of post-session surveys." This is recognized as an emergent property rather than a formal requirement. When SC-002 (memory-driven choices) is achieved, players naturally feel the system remembers because it will demonstrably reference past events in its narrative. No player survey required—the behavior speaks for itself.

---

## Assumptions

- Spec 007 (Campaign Format) is implemented—campaigns exist with defined structure.
- Spec 003-004 (Skills Framework) is implemented—narrator, choice-generator, and game-master skills are available.
- Spec 006 (State Persistence) is implemented—memory can be saved/restored across sessions.
- Target LLMs (2B-3B parameter) can generate coherent prose and choices within the context window budget.
- On-device vector storage is available for semantic search (RAG retrieval).
- Players interact primarily through selecting presented choices (not extensive free-text input).

---

## Open Questions

~~All critical open questions have been resolved. Spec 008 is ready for implementation.~~

**Note**: Previous open questions resolved:
- Q1 (Lore chunking): Resolved in favor of paragraph-based chunking (commit c7ec6e6)
- Q2 (Context window budget): Eliminated in favor of LLM-driven contextual retrieval (commit 054f21f)  
- Q3 (Player input model): Resolved in favor of structured choices only (see FR-017)

---

## Dependencies

- **Spec 007: Campaign Format** - Provides campaign structure, NPC profiles, plot beats, lore files.
- **Spec 003: Skills Framework** - Provides skill invocation for narrator, choice-generator, game-master.
- **Spec 004: Narratoria Skills** - Defines individual skill specifications.
- **Spec 006: Skill State Persistence** - Enables saving/restoring memory across sessions.

---

## Out of Scope

- Campaign authoring tools (this spec covers runtime execution, not creation).
- Multiplayer narrative coordination.
- Voice synthesis or audio narration.
- Real-time streaming prose generation (batch generation is sufficient).
- AI-generated imagery (explicitly excluded per project vision).
