# Feature Specification: Narrative Engine

**Feature Branch**: `008-narrative-engine`
**Created**: 2026-02-02
**Status**: Draft (Open Questions Pending)
**Input**: User description: "Scene pipeline, 4-tier memory system, and choice generation for executing campaigns"

## Overview

The Narrative Engine is the runtime brain of Narratoria—it executes campaigns by managing the scene loop, memory retrieval, and choice generation. If Spec 007 defines the **static campaign package**, Spec 008 defines the **dynamic execution**.

**Core Goal**: Make choices feel "perplexingly on-point"—as if the AI truly understands the player's character and situation.

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

- What happens when the context window is too small to fit all relevant memories?
  - Prioritize: episodic > recent incremental > high-sentiment NPCs > static lore. Truncate oldest/lowest-priority content.
- How does the system handle player free-text input (custom actions)?
  - [NEEDS CLARIFICATION: Should players be allowed to type custom actions, or only select from presented choices?]
- What happens when the LLM generates invalid/inappropriate content?
  - Retry generation with adjusted prompt (max 3 attempts), then fall back to generic safe content.
- How does the system handle very long play sessions (1000+ choices)?
  - Summarize older incremental memories to compress context while preserving key information.

---

## Requirements *(mandatory)*

### Functional Requirements

#### Memory System (4 Tiers)

- **FR-001**: System MUST maintain Tier 1 (Static) memory containing campaign lore, NPC profiles, and world rules, indexed for semantic retrieval.
- **FR-002**: System MUST maintain Tier 2 (Incremental) memory that appends a scene summary after each player choice.
- **FR-003**: System MUST maintain Tier 3 (Weighted) memory storing NPC sentiment values that bias retrieval of related content.
- **FR-004**: System MUST maintain Tier 4 (Episodic) memory storing rare triumph/failure events with full context, always retrieved when relevant.
- **FR-005**: Lore files MUST be chunked by paragraph (split on `\n\n`) with a maximum of 512 tokens per chunk. If a single paragraph exceeds 512 tokens, it MUST be split on sentence boundaries (`.`, `!`, `?`). Each chunk MUST be stored with metadata including original file path, chunk index, and paragraph ID.
- **FR-006**: System MUST allocate context window budget across memory tiers. [NEEDS CLARIFICATION: What percentage of context should be allocated to each tier? e.g., 30% static, 40% incremental, 20% episodic, 10% rules/prompt]

#### Scene Transition Pipeline

- **FR-007**: System MUST execute a 7-step pipeline: Choice → Memory Update → Scene Rules → Memory Retrieval → Prose Generation → Choice Generation → Display.
- **FR-008**: Memory Update step MUST store scene summary, update sentiment, and check for episodic triggers.
- **FR-009**: Scene Rules step MUST determine scene type (travel, dialogue, danger, resolution) based on narrative context.
- **FR-010**: Memory Retrieval step MUST query static lore, recent incremental memories, sentiment-weighted content, and episodic memories.
- **FR-011**: Prose Generation step MUST produce 2-3 paragraphs of scene-setting narrative.
- **FR-012**: Choice Generation step MUST produce 3-4 contextually relevant options.

#### Choice Generation

- **FR-013**: Choices MUST be contextually grounded (reference relevant memories).
- **FR-014**: Choices MUST be character-appropriate (match player's established personality and abilities).
- **FR-015**: Choices MUST be narratively interesting (Story Director skill nudges toward compelling outcomes).
- **FR-016**: Choices MUST be mechanically valid (respect game rules and world constraints).

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

- **Scene**: A narrative moment with prose and choices. The atomic unit of gameplay.
- **Memory Tier**: One of four storage layers with different persistence and retrieval characteristics.
- **Scene Summary**: Compressed representation of what happened in a scene, stored in Tier 2.
- **Sentiment Value**: Numeric score (-1.0 to +1.0) representing an NPC's attitude toward the player.
- **Episodic Event**: Rare, significant story moment (triumph/failure) stored with full context.
- **Plot Beat**: Campaign-defined story moment with trigger conditions and priority.
- **Choice**: Player-facing option with text, underlying intent, and potential outcomes.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Scene transitions complete within 3 seconds on target hardware (8GB RAM device).
- **SC-002**: 80% of generated choices reference relevant past events or player knowledge (memory-driven).
- **SC-003**: Players report feeling the AI "remembers" their choices in 90% of post-session surveys.
- **SC-004**: Plot beats trigger within 2 scenes of conditions being met in 95% of cases.
- **SC-005**: NPC dialogue reflects correct sentiment (positive/negative/neutral) in 95% of interactions.
- **SC-006**: System maintains coherent narrative across 100+ consecutive choices without context degradation.
- **SC-007**: Episodic memories surface when relevant in 100% of applicable situations.

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

These questions significantly impact implementation scope and should be resolved before planning:

### Q1: Context Window Budget

**Context**: FR-006 requires allocating context across memory tiers.

**Question**: What percentage of context window should each tier receive?

| Option | Allocation | Implications |
|--------|------------|--------------|
| A | 40% static, 30% incremental, 20% episodic, 10% system | Heavy lore focus, less room for recent history |
| B | 25% static, 45% incremental, 20% episodic, 10% system | Heavy recent focus, less lore depth |
| C | 30% static, 35% incremental, 25% episodic, 10% system | Balanced approach |
| D | Dynamic: adjust based on available content | Most flexible, most complex |

---

### Q3: Player Free-Text Input

**Context**: Edge case asks about custom player actions.

**Question**: Should players be able to type custom actions beyond presented choices?

| Option | Approach | Implications |
|--------|----------|--------------|
| A | Structured choices only | Simpler, guaranteed valid options, less player freedom |
| B | Free-text always available | Maximum freedom, risk of invalid/game-breaking input |
| C | Free-text as 4th "Other" option | Balanced—clear choices plus escape hatch |
| D | Free-text unlocked by setting/campaign flag | Author controls complexity |

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
