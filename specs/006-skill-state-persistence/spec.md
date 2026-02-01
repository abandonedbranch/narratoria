# Specification 006: Skill State Persistence

**Status**: Draft
**Version**: 0.1.0
**Created**: 2026-01-31
**Parent Specs**: [003-skills-framework](../003-skills-framework/spec.md), [004-narratoria-skills](../004-narratoria-skills/spec.md), [005-dart-implementation](../005-dart-implementation/spec.md)

## RFC 2119 Keywords

The key words "MUST", "MUST NOT", "REQUIRED", "SHALL", "SHALL NOT", "SHOULD", "SHOULD NOT", "RECOMMENDED", "MAY", and "OPTIONAL" in this document are to be interpreted as described in [RFC 2119](https://datatracker.ietf.org/doc/html/rfc2119).

## 1. Purpose

This specification defines the persistent data storage layer that enables skills to store and retrieve contextual information across story sessions and story playthroughs. The persistence layer serves two primary purposes:

1. **Skill Memory**: Allow the Memory skill to embed and store significant narrative events with semantic context, enabling retrieval of relevant memories in future sessions
2. **Context Augmentation**: Enable other skills (Reputation, NPC Perception, Character Portraits) to query persisted data and use it to inform their decisions during plan execution

**Core Skills Using This Layer**:
- Memory - Stores narrative events and interactions with embeddings
- Reputation - Persists faction standing data across sessions
- NPC Perception - Maintains individual NPC relationship data
- Character Portraits - Caches generated character images

**Scope excludes:**
- Individual skill implementations (see [Spec 004](../004-narratoria-skills/spec.md))
- Skill discovery and configuration (see [Spec 003](../003-skills-framework/spec.md))
- Tool protocol and execution semantics (see [Spec 001](../001-tool-protocol-spec/spec.md) and [Spec 002](../002-plan-execution/spec.md))
- User interface layers (see [Spec 005](../005-dart-implementation/spec.md) for Flutter implementation details)

---

## 2. Terminology

- **Memory Event**: A recorded narrative occurrence (e.g., "player befriends blacksmith", "betrays thieves guild") with timestamp and semantic embedding
- **Semantic Embedding**: Numerical vector representation of a memory event for similarity-based retrieval
- **Context Augmentation**: The process of retrieving relevant stored data and injecting it into a skill's execution context
- **Story Session**: A single continuous play session within a narrative playthrough
- **Story Playthrough**: A complete or ongoing narrative arc spanning multiple sessions
- **Skill Context**: The set of relevant stored data provided to a skill during plan execution
- **Persistence Backend**: The data storage system (e.g., in-process database, file-based storage)
- **Query Vector**: Embedding representation of a search query for finding relevant memories
- **Decay Rate**: Time-based degradation of data relevance (e.g., older memories become less prominent in searches)

---

## 3. User Scenarios

### User Story 1 - Cross-Session Memory Continuity (Priority: P1)

A player engages in a story over multiple days. In session 1, they befriend the blacksmith and steal from a merchant. In session 2 (days later), they return to the story. The Memory skill retrieves the previous interactions and augments the narrator's context, enabling the narrator to provide continuity without the player having to recap their actions.

**Why this priority**: Cross-session continuity is essential for long-form narrative experiences. Without it, players feel their story doesn't persist between sessions.

**Independent Test**: Can be fully tested by: Starting a story, recording events, ending session, starting a new session, and verifying Memory skill retrieves and provides prior events to narrator. Delivers: Seamless story continuity across days/weeks.

**Acceptance Scenarios**:

1. **Given** player completed session with recorded interactions, **When** narrator initializes plan for new session, **Then** Memory skill retrieves 3-5 most relevant prior events and augments narrator context
2. **Given** player asks "What happened last time?", **When** Memory skill searches stored memories, **Then** narrator receives relevant historical context ranked by recency and relevance
3. **Given** player has 100+ stored memory events across multiple playthroughs, **When** new session begins, **Then** only contextually relevant memories are provided (not all stored data)
4. **Given** narrative context changes significantly (new location, new arc), **When** Memory skill performs search, **Then** search correctly returns memories relevant to new context, not just recent memories

---

### User Story 2 - Context-Aware Skill Augmentation (Priority: P1)

The narrator is preparing to generate player choices for an NPC interaction. The NPC Perception skill needs to know: (1) Has the player met this NPC before? (2) What was the prior interaction? (3) Does the player have faction reputation that influences initial perception? The persistence layer provides this data, enabling NPC Perception to make informed decisions.

**Why this priority**: Without persisted context, skills cannot make intelligent decisions. Every interaction would be treated as first-contact, breaking story continuity and dynamic NPC behavior.

**Independent Test**: Can be fully tested by: Recording NPC interaction data, requesting context for that NPC, and verifying complete relationship history is provided. Delivers: Skills receive necessary historical context for decision-making.

**Acceptance Scenarios**:

1. **Given** player previously helped NPC "Aldric", **When** NPC Perception skill queries for Aldric context, **Then** prior interactions and accumulated perception score are returned
2. **Given** multiple skills need context simultaneously, **When** plan includes multiple skills accessing persistence layer, **Then** all queries complete within performance targets without blocking each other
3. **Given** persisted data exists for player's actions, **When** Reputation skill queries faction standing, **Then** all prior faction updates are summed and decay applied correctly
4. **Given** player interacted with NPC 50 days ago in-game, **When** NPC Perception applies decay, **Then** perception value has degraded toward neutral by configured decay rate

---

### User Story 3 - Semantic Memory Search (Priority: P2)

The player is in a new location and the narrator wants context about similar past experiences. Instead of exact keyword matching ("find all memories with 'blacksmith'"), the narrator queries with semantic intent ("what do I know about craftspeople?"). The persistence layer returns relevant memories ordered by semantic similarity, not just exact matches.

**Why this priority**: Semantic search enables the narrator to leverage past context intelligently, even when exact terms don't match. This significantly enhances narrative quality and reduces narrator confusion.

**Independent Test**: Can be fully tested by: Storing diverse memories, querying with semantic intent, and verifying results match narrative intent rather than keyword match. Delivers: Rich, intent-based memory retrieval.

**Acceptance Scenarios**:

1. **Given** stored memories include interactions with "blacksmith", "weaponsmith", "armorsmith", **When** narrator queries "craftspeople interactions", **Then** all three are returned in order of relevance
2. **Given** memory database contains 1000+ events, **When** semantic search is performed, **Then** results are returned in under 500ms and ranked by relevance
3. **Given** player has memories from multiple story arcs, **When** narrator performs scoped search (e.g., "current location" memories only), **Then** search respects scope constraints
4. **Given** narration style or context changes, **When** Memory skill performs new search with updated query intent, **Then** different results are returned reflecting the new narrative direction

---

### User Story 4 - Portrait Caching and Reuse (Priority: P2)

The narrator describes a character portrait and the system generates an image. Later in the story, the same character appears. Instead of regenerating the expensive portrait, the system retrieves the cached version. The persistence layer maintains the character-to-portrait mapping and handles portrait lifecycle.

**Why this priority**: Portrait generation is computationally expensive. Caching prevents redundant generation and ensures visual consistency for recurring characters.

**Independent Test**: Can be fully tested by: Generating a portrait, storing it, retrieving it later, and verifying the same image is used. Delivers: Efficient portrait reuse and visual consistency.

**Acceptance Scenarios**:

1. **Given** portrait generated for character "Aldric", **When** Aldric reappears 30 minutes later, **Then** stored portrait is retrieved and displayed (no regeneration)
2. **Given** character description changes significantly, **When** Character Portrait skill detects description change, **Then** portrait is regenerated and new version replaces old
3. **Given** portrait storage grows large (100+ portraits per session), **When** portrait retrieval is requested, **Then** lookup completes in under 100ms
4. **Given** game session ends, **When** new session begins with same playthrough, **Then** all prior portraits remain accessible and consistent

---

### Edge Cases

- **What happens when persistence backend becomes unavailable?** System degrades gracefully; skills function without historical context but narrator warns that continuity is compromised
- **How does system handle storage quota exhaustion?** Configurable retention policies (e.g., keep most recent 10,000 memories or memories from last 90 days); oldest memories are pruned automatically
- **What if player manually edits persisted story data?** System assumes manual edits are authoritative; no validation or rollback occurs, but integrity is maintained
- **How are embeddings versioned if embedding model changes?** Old embeddings remain; new memories use new model; query vector uses current model; results include both but may show degraded relevance for old memories
- **What happens when multiple story playthroughs have conflicting data?** Data is scoped by playthrough ID; queries automatically filter to current playthrough unless explicitly requesting cross-playthrough search

---

## 4. Requirements

### Functional Requirements

- **FR-113**: System MUST provide a persistence layer accessible to skills via query interface defined in this spec
- **FR-114**: System MUST store memory events with the following minimum attributes: event summary, timestamp, story session ID, embedding vector, narrative context tags
- **FR-115**: System MUST support semantic similarity search: given a query, return memories ranked by embedding similarity above configured threshold
- **FR-116**: System MUST support exact-match filtering: query by timestamp range, story session, playthrough ID, or character identifier
- **FR-117**: Memory skill MUST be able to store new memory events with generated embeddings into the persistence layer
- **FR-118**: Memory skill MUST be able to query persisted memories and return ranked results (by recency and semantic relevance)
- **FR-119**: Reputation skill MUST query persisted faction data and retrieve current standing for any faction
- **FR-120**: Reputation skill MUST record reputation changes (delta, timestamp, faction ID) and persist them atomically
- **FR-121**: NPC Perception skill MUST retrieve perception history for any NPC identifier
- **FR-122**: NPC Perception skill MUST persist perception changes and calculate current perception from accumulated history
- **FR-123**: Character Portraits skill MUST store generated portraits with character identifier and retrieve by character ID or semantic description match
- **FR-124**: System MUST provide a context augmentation interface: given a skill execution context, return relevant persisted data to inject into that skill's input
- **FR-125**: System MUST support scoped queries: memories/data filtered by current playthrough, session, location, or custom tags
- **FR-126**: System MUST persist all skill data across application restarts with zero data loss
- **FR-127**: System MUST support configurable data retention policies (time-based, count-based, storage-based)
- **FR-128**: System MUST apply decay to time-sensitive data (reputation, perception) at configured rates
- **FR-129**: System MUST provide query performance monitoring: track query latency, result count, search scope for debugging
- **FR-130**: System MUST handle concurrent read access from multiple skills without blocking or data corruption

### Key Entities

- **Memory Event**: Represents a recorded narrative occurrence. Attributes: ID, summary text, embedding vector, timestamp, story session ID, playthrough ID, character identifiers (involved parties), action type (befriend/betray/help/harm), semantic tags (location, theme, consequence), relevance score
  - Relationships: Associated with one playthrough and session; referenced by narrator for context augmentation

- **Faction Reputation**: Represents player standing with a faction. Attributes: faction ID, current reputation score, last update timestamp, playthrough ID
  - Relationships: Updated by Reputation skill; queried by Choice skill for option filtering; influences NPC Perception initialization

- **NPC Perception Record**: Represents individual NPC's opinion of player. Attributes: NPC ID, perception score (-100 to +100), playthrough ID, perception events (history), decay-adjusted score
  - Relationships: Updated by Perception skill; influences Choice and Dice Roller skill outcomes; seeded from Faction Reputation

- **Character Portrait**: Represents cached character image. Attributes: character ID, image data/path, description hash, generation timestamp, style preset, playthrough ID
  - Relationships: Associated with NPC or player character; retrieved by Narrator for display; regenerated when description changes significantly

- **Playthrough Session**: Represents a single continuous play session. Attributes: session ID, playthrough ID, start timestamp, end timestamp, location/arc, session summary
  - Relationships: Contains zero or more memory events; establishes scope for queries

---

## 5. Success Criteria

### Measurable Outcomes

- **SC-023**: Memory skill can store and retrieve memories with embeddings for databases containing 1000+ events with 99% accuracy
- **SC-024**: Semantic similarity search returns relevant memories (verified by human review) in under 500ms for databases with 1000+ events
- **SC-025**: Context augmentation for a skill completes within 200ms, retrieving up to 5 relevant memories/data points
- **SC-026**: All skill data persists correctly across application restarts with zero data loss (100% recovery rate)
- **SC-027**: Cross-session continuity works as expected: player completes action in session 1, memory is retrieved and narrator references it in session 2 (verified in acceptance test 1 of User Story 1)
- **SC-028**: NPC Perception correctly reflects prior interactions: perceived score for an NPC who received help is higher than an NPC who was betrayed (delta > 30 points)
- **SC-029**: Reputation decay works correctly: faction reputation loses 10% per configured time unit for neutral relationships, slower for strong opinions (+/-50 points)
- **SC-030**: Character portraits are correctly retrieved and reused: same character in session reuses prior portrait (semantic match) in 90% of cases
- **SC-031**: Multiple skills can query persistence layer simultaneously without blocking or deadlock (tested with 5+ concurrent queries)
- **SC-032**: Memory queries correctly filter by scope (playthrough, session, location, tags) and return no out-of-scope results

---

## 6. Related Specifications

| Specification | Relationship |
|---------------|--------------|
| [001: Tool Protocol](../001-tool-protocol-spec/spec.md) | Defines NDJSON protocol for skill communication |
| [002: Plan Execution](../002-plan-execution/spec.md) | Defines plan structure; persistence layer provides context for plan generation |
| [003: Skills Framework](../003-skills-framework/spec.md) | Skills access persistence layer through standard interface |
| [004: Narratoria Skills](../004-narratoria-skills/spec.md) | Memory, Reputation, NPC Perception, and Portrait skills depend on this layer |
| [005: Dart Implementation](../005-dart-implementation/spec.md) | Dart implementation provides concrete persistence backend |

---

## 7. Assumptions

1. **Embedding Model**: System assumes access to a semantic embedding model (local or hosted) that can generate 384-2048 dimensional vectors. Embedding quality directly impacts semantic search effectiveness.

2. **Data Volume**: Specifications assume typical narrative sessions generate 50-200 memory events. Storage layer is designed for 1000s of events per playthrough, not millions.

3. **Latency Tolerance**: Skills can tolerate up to 500ms for memory retrieval and 200ms for context augmentation. Real-time synchronous retrieval is expected (not asynchronous).

4. **Data Consistency**: System prioritizes availability over strict consistency. Skills may occasionally receive stale data (< 1 second old) but data corruption is not acceptable.

5. **Playthrough Isolation**: Each playthrough has its own data scope. Cross-playthrough queries are not supported in this version (possible future enhancement).

6. **Decay Semantics**: Time-based decay applies only to perception and reputation data. Memory events do not decay; they remain accessible indefinitely (archival system handles long-term storage).

