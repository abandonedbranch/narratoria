# Specification 004: Narratoria Skills

**Status**: Draft
**Version**: 0.1.0
**Created**: 2026-01-26
**Parent Specs**: [002-plan-execution](../002-plan-execution/spec.md), [003-skills-framework](../003-skills-framework/spec.md)

## Prerequisites

**Read first**: Specs 001-003 in order
- [Spec 001 - Tool Protocol](../001-tool-protocol-spec/spec.md) - Understand tool communication
- [Spec 002 - Plan Execution](../002-plan-execution/spec.md) and [Spec 003 - Skills Framework](../003-skills-framework/spec.md) together - Understand how skills are selected and orchestrated

**Then read together with**: [Spec 006 - Skill State Persistence](../006-skill-state-persistence/spec.md)

Specs 004 and 006 are **co-dependent** for the Memory, Reputation, NPC Perception, and Character Portrait skills:
- **Spec 004** defines the skill interfaces: what data types each skill stores and retrieves
- **Spec 006** defines the storage implementation: ObjectBox schema, query API, and I/O contracts
- **Reading order**: Read Spec 004 first (understand skill interfaces), then read Spec 006 (understand storage implementation)

**Connection**: Many Spec 004 skills invoke Spec 006 persistence operations (Memory skill stores/recalls via persistence layer; Reputation skill updates faction scores; Character Portrait skill caches images).

**After these**: Specs 005 (implementation) and 008 (narrative engine) use these skills.

---

## RFC 2119 Keywords

The key words "MUST", "MUST NOT", "REQUIRED", "SHALL", "SHALL NOT", "SHOULD", "SHOULD NOT", "RECOMMENDED", "MAY", and "OPTIONAL" in this document are to be interpreted as described in [RFC 2119](https://datatracker.ietf.org/doc/html/rfc2119).

## 1. Purpose

This specification defines the individual skills that ship with Narratoria:

**Core Skills (MVP)**:
- Storyteller - Rich narrative enhancement
- Dice Roller - Randomness and game mechanics
- Memory - Semantic memory and continuity
- Reputation - Faction standing tracking

**Advanced Skills**:
- Player Choices - Contextual multiple-choice options
- Character Portraits - Visual character generation
- NPC Perception - Individual NPC relationship tracking

**Scope excludes:**
- Skills framework and discovery (see [Spec 003](../003-skills-framework/spec.md))
- Plan execution semantics (see [Spec 002](../002-plan-execution/spec.md))
- Dart/Flutter implementation (see [Spec 005](../005-dart-implementation/spec.md))
- Skill state persistence (see [Spec 006](../006-skill-state-persistence/spec.md))

---

## 2. Terminology

- **Persona Profile**: Player's character data including stats, traits, background, and preferences that influence available choices
- **Player Choice**: Generated option for player decision point with difficulty indicators
- **Character Portrait**: Generated or cached image for a character
- **NPC Perception Record**: Individual NPC's opinion of the player (-100 to +100)
- **Perception Score**: Numerical value representing an NPC's opinion, distinct from faction reputation
- **Portrait Cache**: Persistent storage of generated character images for reuse within a story session
- **Perception Event**: Record of action that modified perception

---

## 3. User Scenarios

### User Story 4 - Memory and Continuity (Priority: P2)

A player engages in a long storytelling session over multiple days. The memory skill tracks significant events, character interactions, and world changes. When the player returns and types "What happened last time?", the narrator uses the memory skill to recall key events without sending the entire conversation history to the LLM.

**Why this priority**: Memory is foundational for meaningful narrative experiences. Without continuity, each session feels disconnected and players cannot build long-term story arcs. The persistence layer (Spec 006) enables this capability.

**Acceptance Scenarios**:

1. **Given** memory skill is enabled, **When** player action causes significant event (e.g., "befriends the blacksmith"), **Then** memory skill stores event summary with semantic embedding
2. **Given** memory skill has stored events, **When** narrator generates new plan, **Then** memory-recall script is invoked to fetch relevant context from past events
3. **Given** player asks "What happened with the blacksmith?", **When** memory skill searches stored events, **Then** narrator incorporates relevant past context into response
4. **Given** memory database grows large (>1000 events), **When** memory skill performs search, **Then** search completes in under 500ms using vector similarity

---

### User Story 5 - Reputation and Consequence Tracking (Priority: P3)

A player's actions have consequences. When the player steals from a merchant in town, the reputation skill records decreased standing with the Merchants Guild. Later, when attempting to trade with another merchant, the narrator checks reputation and generates narration reflecting the player's negative reputation.

**Why this priority**: Reputation tracking creates a living world where actions have lasting consequences.

**Acceptance Scenarios**:

1. **Given** reputation skill is enabled and tracks "Merchants Guild", **When** player steals from merchant, **Then** reputation skill records -20 reputation with Merchants Guild
2. **Given** player has negative reputation with faction, **When** narrator generates plan for interaction with that faction, **Then** reputation skill is queried and result influences narration tone
3. **Given** reputation skill tracks multiple factions, **When** player action affects multiple factions simultaneously, **Then** all relevant faction reputations are updated in single transaction
4. **Given** time passes in-game, **When** reputation skill applies decay (configured decay rate), **Then** old reputation modifications fade gradually toward neutral

---

### User Story 6 - Contextual Player Choices (Priority: P2)

A player reaches a decision point in the narrative. Instead of typing freeform text, they are presented with 3-5 contextual multiple-choice options that reflect their character's abilities, past choices, and current reputation.

**Why this priority**: This enhances the game master experience by providing intelligent, contextual suggestions.

**Acceptance Scenarios**:

1. **Given** player character has high Stealth stat (>15), **When** narrator reaches a locked door scenario, **Then** player choices include stealth-related options like "Pick the lock" or "Find another way around"
2. **Given** player previously betrayed the Thieves Guild, **When** player encounters a guild member, **Then** choices do NOT include "Ask for guild assistance" and MAY include "Attempt to make amends"
3. **Given** player has low Charisma stat (<8), **When** social encounter requires persuasion, **Then** persuasion-based choices are either hidden or marked as "unlikely to succeed"
4. **Given** decision point requires immediate action, **When** choice skill is invoked, **Then** 3-5 contextual options are generated and displayed within 3 seconds

---

### User Story 7 - Character Portrait Generation (Priority: P3)

During narrative play, the narrator describes a new character. The character portrait skill generates a visual image based on the narrative description and stores it. When this character appears again later in the story, their portrait is retrieved and displayed.

**Why this priority**: Visual elements significantly enhance immersion.

**Acceptance Scenarios**:

1. **Given** narrator describes a new character with physical details, **When** portrait skill is invoked, **Then** an image is generated matching the description and displayed in the narrative
2. **Given** character portrait was previously generated, **When** same character appears in narrative, **Then** stored portrait is retrieved and displayed (no regeneration)
3. **Given** player character has a description in their persona profile, **When** player views their character sheet, **Then** player portrait is displayed
4. **Given** image generation service is unavailable, **When** portrait skill is invoked, **Then** system displays placeholder silhouette and logs warning (graceful degradation)

---

### User Story 8 - NPC Perception and Reactions (Priority: P3)

A player who previously helped the town blacksmith returns to request a favor. The NPC perception skill calculates that the blacksmith has a positive perception (+45) of the player based on past interactions. This positive perception grants a +2 bonus to any persuasion rolls.

**Why this priority**: Individual NPC relationships create a living world distinct from faction reputation.

**Acceptance Scenarios**:

1. **Given** player previously helped NPC "Aldric the Blacksmith", **When** perception skill calculates NPC's view of player, **Then** Aldric has positive perception score reflecting past aid
2. **Given** NPC has positive perception (>30) of player, **When** dice-roller skill calculates social roll, **Then** roll receives positive modifier (+1 to +3 based on perception level)
3. **Given** NPC has negative perception (<-30) of player, **When** choice skill generates options, **Then** friendly cooperation options are hidden or marked as very difficult
4. **Given** player action affects NPC (help/harm/insult/compliment), **When** action completes, **Then** NPC perception score is updated and persisted

---

## 4. Core Skills (MVP)

### 4.1 Storyteller Skill

**FR-054**: System MUST ship with a `storyteller` skill for rich narrative enhancement

**Components**:
- Behavioral prompt for evocative narration
- `narrate.dart` script that calls LLM (local or hosted) for detailed prose

**Configuration**:
- `provider`: ollama | claude | openai
- `model`: Model identifier
- `apiKey`: API key for hosted providers (sensitive)
- `style`: terse | vivid | poetic
- `fallbackProvider`: Provider to use when primary fails

### 4.2 Dice Roller Skill

**FR-055**: System MUST ship with a `dice-roller` skill for randomness

**Components**:
- `roll-dice.dart` script that parses dice formulas (e.g., "1d20+5", "3d6")
- Emits `ui_event` with roll results for display to player

**Configuration**:
- `showIndividualRolls`: boolean - Display each die result
- `randomSource`: crypto | pseudo - Random number source

### 4.3 Memory Skill

**FR-056**: System MUST ship with a `memory` skill for semantic memory and continuity

**Purpose**: Allows the Plan Generator to store and retrieve narrative events across sessions using semantic search.

**Components**:
- `store-memory.dart` script that receives event summaries, generates embeddings, and stores via Spec 006 persistence layer
- `recall-memory.dart` script that receives semantic queries, calls Spec 006 `semanticSearch()`, and returns ranked results

**Input (store-memory)**:
```json
{
  "summary": "Player helped blacksmith repair anvil",
  "characters": ["player", "blacksmith_aldric"],
  "location": "blacksmith_shop",
  "significance": "high"
}
```

**Input (recall-memory)**:
```json
{
  "query": "interactions with blacksmith",
  "limit": 3,
  "filters": {"location": "blacksmith_shop"}
}
```

**Output (recall-memory)**:
```json
{
  "memories": [
    {"summary": "...", "timestamp": "...", "relevance": 0.92},
    {"summary": "...", "timestamp": "...", "relevance": 0.85}
  ]
}
```

**Configuration**:
- `embeddingModel`: MUST be `sentence-transformers/all-MiniLM-L6-v2` (33MB, 384-dimensional semantic embeddings, downloads from HuggingFace on first use). This model is optimized for semantic similarity and runs locally for privacy
- `similarityThreshold`: Minimum similarity score for results (default: 0.7)

**Embedding Model Details**:
- **Model**: sentence-transformers/all-MiniLM-L6-v2 (Hugging Face: `sentence-transformers/all-MiniLM-L6-v2`)
- **Size**: 33MB (~60MB on disk with dependencies)
- **Dimensions**: 384-dimensional vectors
- **Latency**: ~10-50ms per sentence on typical mobile hardware
- **Coverage**: All stored memories, lore chunks, and semantic queries use this model for embeddings

### 4.4 Reputation Skill

**FR-057**: System MUST ship with a `reputation` skill for tracking player standing

**Components**:
- `update-reputation.dart` script that records reputation changes by faction
- `query-reputation.dart` script that returns current reputation values

**Configuration**:
- `factionList`: Array of faction names
- `reputationScale`: min/max values
- `decayRate`: Reputation decay per in-game time unit
- `storageBackend`: objectbox | files

---

## 5. Advanced Skills

### 5.1 Player Choices Skill

**FR-068**: System MUST ship with a `player-choices` skill for generating contextual multiple-choice options

**Components**:
- `generate-choices.dart` script that analyzes context and produces 3-5 choices
- `evaluate-choice.dart` script that determines outcome modifiers for selected choice

**Configuration**:
- `minOptions`: Minimum choices to generate (default: 3)
- `maxOptions`: Maximum choices to generate (default: 5)
- `showDifficultyThreshold`: Show difficulty for options below this success probability
- `consequenceHintVerbosity`: none | brief | detailed

**Requirements**:
- **FR-069**: Choice skill MUST consider player persona profile (stats, traits, background) when generating options
- **FR-070**: Choice skill MUST consider player's past choices and narrative history when filtering options
- **FR-071**: Choice skill MUST consider faction reputation (from `reputation` skill) when determining available options
- **FR-072**: Choice skill MUST consider individual NPC perception (from `npc-perception` skill) when applicable
- **FR-073**: Choice skill MUST mark options with difficulty indicators when player stats suggest low success probability
- **FR-074**: Choice skill MUST complete option generation within 3 seconds for typical scenarios
- **FR-075**: Choice skill MUST emit `ui_event` with event type `narrative_choice` containing generated options
- **FR-076**: Choice skill MUST allow freeform player input as alternative to selecting generated options
- **FR-077**: Choice skill MUST include brief consequence hints for each option (without spoiling outcomes)

### 5.2 Character Portraits Skill

**FR-078**: System MUST ship with a `character-portraits` skill for generating and managing character images

**Components**:
- `generate-portrait.dart` script that creates character images from descriptions
- `lookup-portrait.dart` script that retrieves cached portraits by character identifier
- `update-portrait.dart` script that regenerates portrait for existing character

**Configuration**:
- `imageProvider`: local | hosted
- `stylePreset`: realistic | stylized | anime | pixel
- `resolution`: 256 | 512 | 1024
- `timeout`: Generation timeout in seconds
- `storageLocation`: Path for portrait cache

**Requirements**:
- **FR-079**: Portrait skill MUST generate images based on narrative character descriptions
- **FR-080**: Portrait skill MUST store generated portraits in persistent cache with character identifier
- **FR-081**: Portrait skill MUST retrieve cached portraits when same character reappears (semantic matching)
- **FR-082**: Portrait skill MUST support player character portrait generation from persona profile
- **FR-083**: Portrait skill MUST emit `asset` events containing generated portrait image data
- **FR-084**: Portrait skill MUST gracefully degrade to placeholder silhouette when generation fails
- **FR-085**: Portrait skill MUST complete image generation within 15 seconds (configurable timeout)
- **FR-086**: Portrait skill MUST support regeneration when character description significantly changes

### 5.3 NPC Perception Skill

**FR-087**: System MUST ship with an `npc-perception` skill for tracking individual NPC opinions of the player

**Components**:
- `update-perception.dart` script that records perception changes
- `query-perception.dart` script that returns current perception value and modifiers
- `initialize-perception.dart` script that seeds perception for new NPCs

**Configuration**:
- `decayRate`: Perception decay per in-game time unit
- `factionInfluenceWeight`: How much faction reputation affects initial perception (0-1)
- `modifierScale`: Dice roll modifier per perception tier
- `storageBackend`: objectbox | files

**Requirements**:
- **FR-088**: Perception skill MUST maintain perception scores (-100 to +100) per NPC identifier
- **FR-089**: Perception skill MUST initialize new NPC perception based on faction reputation and visible player traits
- **FR-090**: Perception skill MUST update perception scores based on player actions affecting that NPC
- **FR-091**: Perception skill MUST calculate dice roll modifiers based on perception level:
  - Positive perception (>30): +1 to +3 bonus
  - Neutral perception (-30 to +30): no modifier
  - Negative perception (<-30): -1 to -3 penalty
- **FR-092**: Perception skill MUST provide perception data to choice skill for option filtering
- **FR-093**: Perception skill MUST support perception decay over in-game time (configurable rate)
- **FR-094**: Perception skill MUST distinguish between perception (individual NPC) and reputation (faction)
- **FR-095**: Perception skill MUST persist perception data across sessions
- **FR-096**: Perception skill MUST complete perception queries within 100ms

---

## 6. Skill Integration

- **FR-097**: Player choice skill MUST query NPC perception skill when generating options for NPC interactions
- **FR-098**: Player choice skill MUST query reputation skill for faction-based option filtering
- **FR-099**: NPC perception skill MUST consult reputation skill when initializing perception for new NPCs
- **FR-100**: Portrait skill MUST associate portraits with NPC identifiers used by perception skill
- **FR-101**: All skills MUST communicate via Plan JSON tool invocations (no direct skill-to-skill calls)
- **FR-102**: Narrator AI MUST orchestrate skill interactions through multi-step plans when needed

---

## 7. Data Management (Skill-Specific)

- **FR-108**: Player choice skill MUST access player persona profile from session state
- **FR-109**: Portrait skill MUST store images in `skills/character-portraits/data/` directory
- **FR-110**: Portrait skill MUST maintain character-to-portrait mapping in local database
- **FR-111**: NPC perception skill MUST store perception data in `skills/npc-perception/data/` directory
- **FR-112**: Skills MUST support data export for story archival purposes

---

## 8. Graceful Degradation (Skill-Specific)

- **FR-119**: If portrait generation fails, system MUST display placeholder and continue narrative
- **FR-120**: If perception skill unavailable, choice skill MUST generate options without perception filtering
- **FR-121**: If choice skill fails, narrator MUST fall back to freeform text input (always available)

---

## 9. Key Entities

### Persona Profile

Player character definition used for choice generation.

**Attributes**: stats (strength, dexterity, charisma, etc.), traits, background, equipment, notable past choices
**Relationships**: Referenced by choice skill, portrait skill (player portrait)

### Player Choice

Generated option for player decision point.

**Attributes**: choice ID, display text, difficulty indicator, consequence hint, required conditions
**Relationships**: Generated by choice skill, selected by player, evaluated for outcome modifiers

### Character Portrait

Generated or cached image for a character.

**Attributes**: character ID, image data (or path), description hash, generation timestamp, style preset
**Relationships**: Associated with NPC identifier or player profile

### NPC Perception Record

Individual NPC's opinion of the player.

**Attributes**: NPC identifier, perception score (-100 to +100), last interaction timestamp, interaction history summary
**Relationships**: Influences choice generation, dice roll modifiers; seeded from faction reputation

### Perception Event

Record of action that modified perception.

**Attributes**: NPC identifier, action type, perception delta, timestamp, narrative context
**Relationships**: Accumulated to calculate current perception score

---

## 10. Edge Cases

### Skill-Specific Edge Cases

- **What happens when player has no persona profile defined?**
  - Choice skill uses default "average adventurer" profile; all options available but not optimized

- **How does system handle conflicting signals (high stat but low reputation)?**
  - Options appear but are marked with difficulty indicators; player can still attempt unlikely actions

- **What happens when portrait generation times out or fails?**
  - Placeholder silhouette displayed; retry attempted in background; narrative continues uninterrupted

- **How does NPC perception decay over time?**
  - Configurable decay rate (default: 10% per in-game week); strong impressions (+/-50) decay slower

- **What happens when player tries action not in the generated choices?**
  - Freeform input always accepted; narrator evaluates against same criteria used for choice generation

- **How are NPC perceptions seeded for NPCs the player has never met?**
  - Initial perception based on: faction reputation (50%), player's visible traits/equipment (30%), random variance (20%)

- **What happens when the same character description varies between mentions?**
  - Portrait skill uses semantic similarity to match existing portraits; significant changes trigger regeneration with warning

---

## 11. Success Criteria

### Core Skills

- **SC-007**: Memory skill stores and recalls story events with semantic search completing in under 500ms for databases with up to 1000 events
- **SC-008**: Reputation skill tracks and persists multiple faction standings, allowing queries to return current reputation within 100ms
- **SC-009**: Storyteller skill falls back to local LLM when hosted API is unavailable, with fallback completing within 10 seconds

### Advanced Skills

- **SC-013**: Choice skill generates 3-5 contextual options within 3 seconds for 95% of decision points
- **SC-014**: Generated choices correctly reflect player stats by incorporating campaign-defined stat schema (campaign manifest defines stat types: relationships/reputation for dating sims, armor/strength for combat-heavy campaigns, etc.). Test method: (1) Verify Phi-3.5 prompt template includes `{player_stats}` injection, (2) Automated keyword grep: generate 20 choices with stat-variant inputs, verify ≥70% mention stat-relevant keywords (relationship/romance, armor/combat, etc.), (3) Code review: confirm prompt properly structures stats for LLM decision-making. Pass if all three checks succeed.
- **SC-015**: Portrait skill generates character images within 15 seconds for 90% of requests when using local generation
- **SC-016**: Portrait skill correctly retrieves cached portraits (semantic match) in 95% of character reappearances
- **SC-017**: NPC perception skill initializes perception for new NPCs within 100ms, informed by faction reputation
- **SC-018**: Dice roll modifiers correctly reflect perception scores (positive perception = bonus, negative = penalty) in 100% of applicable rolls
- **SC-019**: All skill data persists correctly across application restarts with 100% data integrity
- **SC-020**: Players can complete a 30-minute play session using all skills without application crashes
- **SC-021**: Choice generation respects both faction reputation AND individual NPC perception when both are applicable
- **SC-022**: Portrait cache correctly associates images with character identifiers across multiple sessions

---

## 12. Related Specifications

| Specification | Relationship |
|---------------|--------------|
| [001: Tool Protocol](../001-tool-protocol-spec/spec.md) | Defines NDJSON protocol for skill scripts |
| [002: Plan Execution](../002-plan-execution/spec.md) | Orchestrates skill execution via plans |
| [003: Skills Framework](../003-skills-framework/spec.md) | Defines discovery, configuration, and execution |
| [005: Dart Implementation](../005-dart-implementation/spec.md) | Dart+Flutter reference implementation |
| [006: Skill State Persistence](../006-skill-state-persistence/spec.md) | ObjectBox-based in-process data storage for skills |
