# Specification 006: Skill State Persistence

**Status**: Draft
**Version**: 0.1.0
**Created**: 2026-01-31
**Parent Specs**: [003-skills-framework](../003-skills-framework/spec.md), [004-narratoria-skills](../004-narratoria-skills/spec.md), [005-dart-implementation](../005-dart-implementation/spec.md)

## Prerequisites

**Read first:**
1. [Spec 003 - Skills Framework](../003-skills-framework/spec.md) - Understand what skills are
2. [Spec 004 - Narratoria Skills](../004-narratoria-skills/spec.md) - Understand which skills need persistent storage (Memory, Reputation, NPC Perception, Character Portraits)

**Key relationship**: Specs 004 and 006 are **co-dependent**:
- **Spec 004** defines skill interfaces: what data types are stored/retrieved (memory events, faction reputation, NPC perception, character portraits)
- **Spec 006** defines storage implementation: ObjectBox schema, query API, and performance semantics
- **Reading order**: Read Spec 004 first (understand what needs to be stored), then read Spec 006 (understand how it's stored)

**Connection**: Spec 006's query interface (FR-133-137) is called by Spec 004 skills: Memory skill calls `semanticSearch()` and `store()`; Reputation/NPC Perception skills call `update()` and `exactMatch()`. Spec 006 storage schema (FR-132-132d) stores the data types defined in Spec 004.

**After this**: Specs 005 (implementation), 007 (campaign content with lore chunks to store), and 008 (narrative engine queries this layer) all use persistence.

---

## RFC 2119 Keywords

The key words "MUST", "MUST NOT", "REQUIRED", "SHALL", "SHALL NOT", "SHOULD", "SHOULD NOT", "RECOMMENDED", "MAY", and "OPTIONAL" in this document are to be interpreted as described in [RFC 2119](https://datatracker.ietf.org/doc/html/rfc2119).

## 1. Purpose

This specification defines the persistent data storage layer that provides a unified ObjectBox-based storage and retrieval interface for narrative data. The persistence layer is **infrastructure**, not business logic—it stores data and answers queries, but does not decide when or why data is retrieved (that's the responsibility of the Plan Generator in Spec 008 and individual skills in Spec 004).

**Primary Responsibilities**:
1. **Store Narrative Data**: Memory events, lore chunks, faction reputation, NPC perception, character portraits
2. **Semantic Search**: Vector similarity search for context-relevant retrieval
3. **Query Interface**: Fast, filtered access to stored data (<200ms latency)
4. **Persistence**: Data survives application restarts and session boundaries

**Core Skills Using This Layer**:
- Memory - Stores narrative events and interactions with embeddings
- Reputation - Persists faction standing data across sessions
- NPC Perception - Maintains individual NPC relationship data
- Character Portraits - Caches generated character images

**Architectural Note (Constitution Compliance):**
The persistence layer is **shared infrastructure**, not skill-owned data directories. Like the Narrator AI (which is an in-process Dart service exempt from Principle II's out-of-process requirement), the persistence layer runs within the Dart runtime and provides a common query interface for all skills. This differs from the `skills/<skill-name>/data/` pattern described in the constitution, which applies to skill-private caches and working files. The shared persistence layer enables cross-skill data access (e.g., Memory skill stores events that can be queried by any other skill) without violating the "no direct skill-to-skill calls" rule—skills communicate through the persistence layer's query interface, not by accessing each other's private directories.

**What This Spec Does NOT Define**:
- When/why to retrieve memories (decided by Plan Generator in Spec 008)
- How much context to allocate to different data types (decided by LLM contextually)
- Business logic for memory relevance (handled by semantic search ranking)

**Scope excludes:**
- Individual skill implementations (see [Spec 004](../004-narratoria-skills/spec.md))
- Skill discovery and configuration (see [Spec 003](../003-skills-framework/spec.md))
- Tool protocol and execution semantics (see [Spec 001](../001-tool-protocol-spec/spec.md) and [Spec 002](../002-plan-execution/spec.md))
- User interface layers (see [Spec 005](../005-dart-implementation/spec.md) for Flutter implementation details)

---

## 2. Terminology

- **Memory Event**: A recorded narrative occurrence (e.g., "player befriends blacksmith", "betrays thieves guild") with timestamp and semantic embedding
- **Semantic Embedding**: Numerical vector representation of text via sentence-transformers/all-MiniLM-L6-v2 for similarity-based retrieval (384 dimensions)
- **Semantic Search**: Finding stored data by vector similarity to a query embedding using sentence-transformers embeddings
- **Story Session**: A single continuous play session within a narrative playthrough
- **Story Playthrough**: A complete or ongoing narrative arc spanning multiple sessions
- **Persistence Backend**: ObjectBox database running in-process (shared infrastructure)
- **Query Interface**: The API skills use to retrieve data: `semanticSearch()`, `exactMatch()`, `store()`, `update()`
- **Lore Chunk**: A paragraph-sized segment of campaign lore stored with embedding (see Spec 007 for chunking strategy)
- **Decay Rate**: Time-based degradation of numeric values (e.g., reputation, perception scores)

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

### User Story 2 - Query Interface for Skills (Priority: P1)

The Plan Generator (Spec 008) determines that the NPC Perception skill should be invoked to check the player's relationship with an NPC. The skill receives input from the plan, queries the persistence layer with: `exactMatch(npcId: "blacksmith_aldric")`, and receives the perception history. The skill processes this data and returns results to the Plan Executor.

**Why this priority**: Without a query interface, skills cannot access stored data. This is the fundamental contract between skills and persistence.

**Independent Test**: Can be fully tested by: Storing NPC perception data, invoking a skill that queries for it, and verifying the correct data is returned. Delivers: Working query interface for skill access.

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

- **FR-131**: System MUST provide an ObjectBox-based persistence layer accessible to skills via the query interface defined in this spec. Semantic embeddings are generated using sentence-transformers/all-MiniLM-L6-v2 (384-dimensional vectors) and cached with each record to avoid re-embedding on retrieval
- **FR-132**: System MUST store memory events with the following minimum attributes: event summary, timestamp, story session ID, playthrough ID, embedding vector (384-dim via sentence-transformers), character identifiers
- **FR-132a**: System MUST store lore chunks with metadata: original file path, chunk index, paragraph ID, token count, chunk content, and embedding vector
- **FR-132b**: System MUST store faction reputation records with: faction ID, playthrough ID, current score, last update timestamp, decay rate
- **FR-132c**: System MUST store NPC perception records with: NPC identifier, playthrough ID, perception score, last interaction timestamp, event history
- **FR-132d**: System MUST store character portrait records with: character identifier, image path/data, description hash, generation timestamp
- **FR-133**: System MUST provide `semanticSearch(query, dataType, limit, filters)` method that returns data ranked by embedding similarity above configured threshold (default: 0.7)
- **FR-134**: System MUST provide `exactMatch(filters)` method supporting filters: timestamp range, story session, playthrough ID, character identifier, NPC identifier, faction ID, source file path
- **FR-135**: System MUST provide `store(dataType, record)` method for persisting new records atomically
- **FR-136**: System MUST provide `update(dataType, identifier, changes)` method for modifying existing records atomically
- **FR-137**: System MUST provide `delete(dataType, identifier)` method for removing records (used for data retention policies)
- **FR-138**: System MUST support scoped queries: data filtered by current playthrough, session, location, or custom tags
- **FR-139**: System MUST persist all data across application restarts with zero data loss (ACID compliance)
- **FR-140**: System MUST support configurable data retention policies (time-based, count-based, storage-based) executed during idle periods
- **FR-141**: System MUST apply decay to time-sensitive numeric data (reputation scores, perception scores) based on configured decay rates when queried
- **FR-142**: All query methods MUST complete within 200ms for databases containing up to 10,000 records
- **FR-143**: System MUST handle concurrent read access from multiple skills without blocking or data corruption (read-optimized with write locks)
- **FR-144**: System MUST provide query performance monitoring: log query latency, result count, search scope for debugging

### Key Entities

**Note**: These entities define storage schema, not retrieval logic. Skills query this data via the interface defined in FR-133-144.

- **Memory Event**: Stores a recorded narrative occurrence. Attributes: ID, summary text, embedding vector, timestamp, story session ID, playthrough ID, character identifiers, action type, semantic tags
  - Query methods: `semanticSearch(query)`, `exactMatch(sessionId)`, `exactMatch(characterId)`

- **Lore Chunk**: Stores a paragraph of campaign lore. Attributes: ID, original file path, chunk index, paragraph ID, content, embedding vector, token count
  - Query methods: `semanticSearch(query)`, `exactMatch(filePath)`

- **Faction Reputation**: Stores player standing with a faction. Attributes: faction ID, playthrough ID, current score, last update timestamp, decay rate
  - Query methods: `exactMatch(factionId, playthroughId)`, `update(factionId, scoreDelta)`

- **NPC Perception**: Stores individual NPC's opinion of player. Attributes: NPC ID, playthrough ID, perception score (-100 to +100), last interaction timestamp, event history
  - Query methods: `exactMatch(npcId, playthroughId)`, `update(npcId, scoreDelta, event)`

- **Character Portrait**: Stores cached character image. Attributes: character ID, image path, description hash, generation timestamp, playthrough ID
  - Query methods: `exactMatch(characterId)`, `semanticSearch(description)` for semantic matching

- **Playthrough Session**: Metadata for session scope. Attributes: session ID, playthrough ID, start timestamp, end timestamp, current location
  - Query methods: `exactMatch(sessionId)`, used as filter in scoped queries

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

7. **Lore Chunking**: Campaign lore files are chunked by paragraph (split on `\n\n`) with max 512 tokens per chunk. Each chunk is stored with metadata: original file path, chunk index, paragraph ID, and token count. This enables efficient semantic retrieval without loading entire lore files into context.

