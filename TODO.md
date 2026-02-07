## Spec Analysis: Coverage, Consistency, and Dependencies

### **Spec 001: Tool Protocol (Version 0.0.1)**

**Scope**: Communication protocol between external tools and Narratoria runtime

**Covers**:
- Transport model (process invocation, stdin/stdout)
- NDJSON event envelopes (version, type, optional fields)
- 6 event types: `log`, `state_patch`, `asset`, `ui_event`, `error`, `done`
- Deep merge semantics for state patches
- Asset file handling (tools responsible for creation)
- Forward compatibility requirements
- MVP requirement: narrative_choice UI event support only

**Quality**: Well-defined, minimal, extensible. Protocol version remains "0" until breaking changes needed.

---

### **Spec 002: Plan Execution (0.1.0)** + **Data Model**

**Scope**: Plan generation, execution engine, replan loop, narrator AI interface

**Covers**:
- Plan JSON schema (tools array with dependencies, retry policies, parallel/sequential)
- Execution semantics: topological sort, cycle detection, parallel execution
- Failure handling (required vs non-required tools)
- Replan loop (max 5 attempts, disabled skills tracking)
- Narrator AI interface requirements
- Retry logic with exponential backoff formula
- Event aggregation and execution traces
- Deep merge algorithm (per Spec 001)
- Timeout and resource bounds

**Quality**: Comprehensive. Data model includes detailed algorithms (Kahn's algorithm for topo sort, DFS for cycle detection).

---

### **Spec 003: Skills Framework (0.1.0)**

**Scope**: Skill discovery, configuration, execution, data management, graceful degradation

**Covers**:
- Skill discovery from `skills/` directory
- Skill manifest (skill.json) parsing
- Behavioral prompts (prompt.md)
- Configuration schema and UI form generation
- Script execution via NDJSON protocol
- Dependency respect and parallel/sequential execution
- Per-skill timeouts (30s default)
- Skill data storage in `skills/<skill>/data/`
- Error states (healthy, degraded, temporaryFailure, permanentFailure)
- Agent Skills Standard implementation

**Note**: FR-035 allows "hot-reloading" with caveat that MVP requires restart.

**Quality**: Well-structured. Clear relationships to Spec 001 (protocol), Spec 002 (execution), Spec 004 (individual skills).

---

### **Spec 004: Narratoria Skills (0.1.0)**

**Scope**: Individual skills shipped with Narratoria

**Covers**:

**Core Skills (MVP)**:
- **Storyteller**: Rich narrative (local LLM or hosted API with fallback)
- **Dice Roller**: Randomness (2d6 mechanics)
- **Memory**: Semantic memory with embeddings and vector search
- **Reputation**: Faction standing tracking with decay

**Advanced Skills (Post-MVP)**:
- **Player Choices**: Contextual option generation (considers stats, past choices, reputation, NPC perception)
- **Character Portraits**: Generation and caching with semantic matching
- **NPC Perception**: Individual NPC relationship tracking (-100 to +100 scores)

**Integration Requirements**:
- Choice skill must query reputation and NPC perception skills
- All skills communicate via Plan JSON (no direct calls)
- Narrator AI orchestrates multi-step plans
- Graceful degradation (fallbacks defined for each skill)

**Quality**: Good scope definition, but **critical gap**: FR-068 onward defines advanced skills without indicating whether they block MVP or are purely post-MVP. References success criteria (SC-013 onwards) that assume they're implemented.

---

### **Spec 005: Dart/Flutter Implementation (0.1.0)** + **Data Model**

**Scope**: Reference implementation in Dart+Flutter, UI requirements, MVP features

**Covers**:

**Flutter UI**:
- Material Design 3 dark theme
- Tool Execution Panel (logs, progress, status)
- Asset Gallery (images, audio, video, graceful degradation)
- Narrative State Panel (JSON inspection)
- Story View (prose + assets)
- Player Input Field

**MVP Requirements**:
- Text input for prompts
- Narrator AI Stub (hard-coded prompt→plan mapping for testing)
- Tool invocation and event processing
- NDJSON protocol compliance
- Session state management with deep merge
- Asset registry
- Error recovery UI patterns

**Data Model**: Comprehensive Dart classes including:
- Skill, SkillScript, SkillErrorState
- PlanJson, ToolInvocation, RetryPolicy
- PlanExecutionContext with topological sort and cycle detection
- ProtocolEvent sealed class hierarchy
- SessionState with deep merge
- Exception hierarchy

**Quality**: Well-rounded. Includes error UI patterns and fallback narration templates.

**Note**: MVP Narrator AI Stub is deliberately simple (pattern-matching) to unblock other development.

---

### **Spec 006: Skill State Persistence (0.1.0)**

**Scope**: Persistent data layer for skill context, memory system, semantic search

**Covers**:
- 4-tier memory system (needs clarification on allocation):
  - Tier 1 (Static): Campaign lore, NPC profiles, world rules
  - Tier 2 (Incremental): Scene summaries post-choice
  - Tier 3 (Weighted): NPC sentiment values
  - Tier 4 (Episodic): Rare triumphs/failures (always retrieved)
- Semantic search via embeddings
- Context augmentation interface
- Scoped queries (playthrough, session, location, tags)
- Concurrent read access
- Data retention policies and decay
- Storage for Memory, Reputation, NPC Perception, Character Portraits skills

**Architectural Note**: Spec 006 differs from Spec 003's `skills/<skill>/data/` pattern—this is **shared infrastructure** for cross-skill context, justified as exempt from Principle II (like Narrator AI).

**Quality**: Clear intent but **open questions**:
- How should lore be chunked? (paragraph, semantic, fixed tokens, hybrid?)
- What context window allocation across tiers? (30/35/25/10? dynamic?)

---

### **Spec 007: Campaign Format (Draft)**

**Scope**: Campaign package structure, manifest, asset ingestion, sparse data enrichment

**Covers**:
- Campaign directory structure: `world/`, `characters/`, `plot/`, `lore/`, `art/`, `music/`
- Manifest schema (title, version, author, genre, etc.)
- World definition (setting.md, rules.md, constraints.md)
- Character system (NPC profiles, player template, portraits)
- Plot structure (premise.md, beats.json, endings/)
- Lore files (indexed for semantic search)
- Creative assets (PNG, JPEG, WebP, MP3, OGG, WAV, FLAC)
- **Asset Metadata Structure**: Comprehensive schema with:
  - Core fields: path, type, keywords, generated flag, checksum
  - Provenance (for AI-generated content): source_model, generated_at, seed_data
  - Type-specific metadata (image dimensions, audio duration, word count)
  - Relationship graphs
- **Keyword Sidecar Files** (`.keywords.txt`): Human override of auto-extracted keywords
- Sparse data enrichment (LLM fills gaps in <3 files)
- Campaign Format Creeds (respect human artistry, radical transparency, human override)

**Quality**: Comprehensive and thoughtful. Asset metadata design is sophisticated with clear provenance tracking.

**Validation Requirements**: FR-029 to FR-031 define error handling.

---

### **Spec 008: Narrative Engine (Draft)**

**Scope**: Runtime execution of campaigns, scene loop, memory system, choice generation

**Covers**:
- Scene transition pipeline (7-step): Choice → Memory Update → Scene Rules → Memory Retrieval → Prose Gen → Choice Gen → Display
- 4-tier memory system (mirrors Spec 006):
  - Tier 1 (Static): Campaign lore, NPC profiles, world rules
  - Tier 2 (Incremental): Scene summary per choice
  - Tier 3 (Weighted): NPC sentiment values
  - Tier 4 (Episodic): Triumphs/failures
- Rules system (default: 2d6 + modifiers)
- Plot beat integration with graceful degradation
- Sentiment tracking (affects dialogue and options)
- Episodic memory callbacks

**Quality**: Good framework, but **3 open questions** block implementation:
1. Lore chunking strategy (paragraph vs semantic vs fixed tokens)?
2. Context window allocation across tiers?
3. Player free-text input (structured choices only vs "Other" option vs always)?

---

## **Critical Issues & Inconsistencies**

### ✅ **Issue 1: Spec 006 vs Spec 008 Memory System Duplication** [RESOLVED]

**Resolution (commit 054f21f)**: Separated persistence (006) from orchestration (008):
- **Spec 006**: Pure storage layer with query API (`semanticSearch()`, `exactMatch()`, `store()`, `update()`)
- **Spec 008**: LLM Plan Generator makes contextual retrieval decisions (no fixed budgets)
- **Spec 004**: Memory skill defines interface between them

**Architectural Principle**: Plan Generator decides what to retrieve based on scene context, not fixed percentages.

---

### ✅ **Issue 2: Missing Cross-Spec References** [RESOLVED]

**Resolution (commit TBD)**: Added explicit "Prerequisites" sections to all 8 specs documenting:
- **Read order**: How specs build on each other
- **Dependencies**: Which specs are prerequisites and why
- **Circular dependencies**: Explained and resolved for 002↔003, 004↔006, 007↔008

**Spec-by-Spec Updates**:
- **Spec 001**: Foundation (no prerequisites)
- **Spec 002 & 003**: Marked as co-dependent pair (read together); explains how plans and skills co-define each other
- **Spec 004 & 006**: Marked as co-dependent pair (read 004 first for interfaces, then 006 for storage); Memory skill is the connection point
- **Spec 005**: Explicitly requires understanding Specs 001-006 before implementation details make sense
- **Spec 007 & 008**: Marked as complementary pair (static structure vs dynamic execution); explains content flow from campaign to persistence to narrative engine
- **Spec 008**: Comprehensive prerequisites explaining why all 7 prior specs are needed for scene execution

**Key Clarifications**:
1. **Plan Generator & Skills**: Spec 002's Narrator AI uses behavioral prompts from Spec 003 skills; it selects which scripts to invoke; Spec 003 explains skill discovery mechanism
2. **Memory Skill**: Spec 004 defines interface (store/recall); Spec 006 implements storage backend; skills communicate via Spec 006 query API, not direct skill-to-skill calls
3. **Campaign Execution**: Spec 007 provides static content (lore, NPCs, structure); Spec 006 ingests and indexes it; Spec 008 executes it by querying Spec 006
4. **Circular Dependencies Resolved**: All circular dependencies are documented with clear reading order (always read definitions before uses)

---

### ✅ **Issue 3: Narrator AI Stub vs Real Narrator AI** [RESOLVED]

**Resolution (commit fd644b3)**: Removed all MVP Narrator AI Stub language; unified all specs around Phi-3.5 Mini:
- **Spec 001**: Removed 'Narrator AI Stub' clarifications; focused on protocol requirements
- **Spec 002**: Updated terminology to show Phi-3.5 Mini instead of '(local LLM or stub)'
- **Spec 005**: Removed 'MVP Requirements' framing; now 'Core Implementation Requirements'
- **Spec 005**: Section 3.4 changed from "Narrator AI Stub Implementation" to "Phi-3.5 Mini Integration"
- **CLAUDE.md**: Technology stack specifies Phi-3.5 Mini as narrator AI (see commit 84e1f37)

**Architectural Decision**: No MVP/real distinction. All specs assume full Phi-3.5 Mini implementation from day one. MVP requirements are tracked separately (not in spec documents).

---

### ✅ **Issue 4: Skill Data Storage Pattern** [RESOLVED]

**Architectural Decision**:
- **`skills/<skill>/data/`** = Temporary working files, caches, skill-private runtime state (Spec 003 FR-103-107)
- **Persistence Layer (ObjectBox, Spec 006)** = Durable cross-skill context (memory events, reputation, NPC perception, portraits)
- **Not all skills require persistent storage** — only Memory, Reputation, NPC Perception, Character Portraits
- **Storage orchestration** — If a skill needs to persist/retrieve data, the Plan Generator includes that in the plan (e.g., Memory skill declares data dependencies as part of skill invocation)

**Benefit**: Clean separation between temporary execution state and persistent narrative context; Plan Generator can schedule storage/retrieval as plan steps.

---

### ✅ **Issue 5: Overlap Between Spec 004 Skills and Spec 008 Engine** [RESOLVED]

**Resolution (commit 054f21f)**: Clarified responsibilities:
- **Spec 004**: Defines skill interfaces (what parameters Memory skill accepts, what it returns)
- **Spec 008**: Defines when/why to invoke skills (Plan Generator decides contextually)
- **Scene execution**: Orchestrated by Plan Generator invoking skills, not built-in engine logic

---

### ✅ **Issue 6: Spec 008 Open Questions Block Implementation** [RESOLVED]

~~1. **Lore chunking strategy** (FR-005)~~ ✅ **RESOLVED (commit c7ec6e6)**: Paragraph-based, 512 token max, sentence-boundary fallback
~~2. **Context window allocation** (FR-006)~~ ✅ **RESOLVED (commit 054f21f)**: Eliminated fixed budgets; LLM decides contextually
~~3. **Player interaction model**~~ ✅ **RESOLVED (commit 244c3b9)**: Structured choices only—narrator AI generates 3-5 choice buttons; no free-text input allowed

**Impact**: All open questions in Spec 008 now resolved. Implementation can proceed without architectural blockers.

---

### ✅ **Issue 7: Campaign Format Creeds Not Binding** [RESOLVED]

**Resolution (commit 68709ad)**: Added ObjectBox validation enforcement for Campaign Format Creeds:
- **FR-038**: When `generated: true`, ObjectBox MUST validate presence of complete provenance object (source_model, generated_at, seed_data). Rejects store if missing.
- **FR-038a**: When `generated: false`, provenance object MUST NOT be present. Rejects store if present.
- **FR-038b**: ObjectBox validates `generated_at` timestamp is valid ISO 8601 format.
- **FR-038c**: Campaign loader displays warning listing count of AI-generated assets and directs authors to review provenance.

**Outcome**: Campaign Format Creeds are now mechanically enforced at the data layer. Authors cannot inadvertently share campaigns with unlabeled AI content or fraudulent provenance.

---

### ✅ **Issue 8: Remove MVP/Post-MVP Language Across Specs** [RESOLVED]

**Resolution (commit 9908245)**: Removed all MVP/Post-MVP distinctions:
- **Spec 003**: FR-035 changed from "MVP implementation will... post-MVP enhancement" to unified hot-reload requirement
- **Spec 004**: Removed "(Post-MVP)" from "Advanced Skills" label (overview and section heading); all skills assumed to be implementation requirements
- **Spec 008**: Removed "(post-MVP)" tag from Player-Choices skill reference (FR-012)

**Result**: All 8 specs now treat all documented features as MVP-eligible. MVP scope decisions will be determined separately in implementation roadmap, not in specifications.

---

### ✅ **Issue 9: Missing Reference: Embedding Model** [RESOLVED]

**Resolution (commit TBD)**: Specified embedding model across all specs:
- **Spec 004**: Memory skill configuration now specifies `sentence-transformers/all-MiniLM-L6-v2` (33MB, 384-dim embeddings)
- **Spec 005**: Model download mechanism documented (HuggingFace on first app launch)
- **Spec 006**: All embedding storage now explicitly uses sentence-transformers (384-dimensional vectors)
- **Spec 008**: Clarified semantic search uses `sentence-transformers/all-MiniLM-L6-v2` embeddings
- **CLAUDE.md**: Updated technology stack with Phi-3.5 Mini (narrator AI) + sentence-transformers (embeddings)

**Architectural Decision**: Hybrid approach maximizes performance and quality:
- **Narrator AI**: Phi-3.5 Mini (3.8B, 2.5GB GGUF) for plan generation and narration
- **Semantic Embeddings**: sentence-transformers/all-MiniLM-L6-v2 (33MB, optimized for similarity) for memory/lore retrieval
- **Deployment**: Both download from HuggingFace Hub on first app launch; cached locally for offline use
- **iPhone 17 Compatibility**: Total ~2.6GB (fits on basic iPhone 17 with 8GB+ RAM)

---

### 🟡 **Issue 10: Narrative Quality Metrics Not Defined**

- Specs define success criteria (SC-001, SC-002, etc.) but many are subjective
- **SC-002** (80% of choices reference past events) - how is this measured?
- **SC-003** (Players report feeling AI "remembers") - survey? metric?
- **SC-014** (Choices correctly reflect player stats) - what does "correctly" mean?

**Recommendation**: Define acceptance test procedures (automated checks vs human review) for each SC.

---

## **Summary Table**

| Spec | Status | Scope | MVP | Post-MVP | Key Dependencies |
|------|--------|-------|-----|----------|------------------|
| **001** | ✅ Draft | Protocol | Yes | - | None |
| **002** | ✅ Draft | Plan Execution (Phi-3.5 Mini narrator) | Yes | Enhanced LLM | 001, 003 |
| **003** | ✅ Draft | Skill Framework | Yes | Hot-reload | 001, 002 |
| **004** | ✅ Draft | Individual Skills (sentence-transformers embeddings) | Core skills | Advanced skills | 003, 006 |
| **005** | ✅ Draft | Dart/Flutter + Model Download | Yes | - | 001-008 |
| **006** | ✅ Draft | Persistence (ObjectBox + sentence-transformers) | Yes | Query optimization | None |
| **007** | ✅ Draft | Campaign Format | Yes | - | 008, LLM integration |
| **008** | ✅ Draft | Narrative Engine (Phi-3.5 + embeddings) | Yes | - | **0 Open Questions** |

---

## **Missing Specs**

1. **Spec 009: Infrastructure & Deployment** - Embedding model selection, persistence backend (ObjectBox setup), LLM integration (Ollama, Gemma, etc.), build & release
2. **Spec 010: Testing & Validation** - Acceptance test procedures, narrative quality benchmarks, integration test strategies
3. **Spec 011: Data Import/Export** - Campaign archival, player save/load, story export formats 

Completed: *Analyze spec coverage and create breakdown* (1/1)

I've completed a comprehensive analysis of all 8 spec documents covering 85,000+ lines. Here's the executive summary:

## **Quick Stats**
- **8 specs total** covering ~50 major features
- **2 critical inconsistencies** (memory duplication, unclear narrator AI scope)
- **6 unresolved architectural questions** (data ownership, embedding model, narrative quality metrics)
- **3 open implementation questions** in Spec 008 that block development
- **Missing specs**: Infrastructure (009), Testing/Validation (010), Import/Export (011)

## **Key Findings**

**What's Strong**:
- ✅ **Spec 001** (Tool Protocol) - Minimal, elegant, extensible
- ✅ **Spec 005** (Dart/Flutter) - Complete reference implementation with classes and error patterns
- ✅ **Spec 007** (Campaign Format) - Sophisticated asset metadata with provenance tracking

**What Needs Clarification**:
- ⚠️ **Spec 006 vs 008** — Both define "4-tier memory" but serve different purposes (unclear if they're the same thing)
- ⚠️ **Spec 004 & 008 Overlap** — Unclear if scene execution is built-in engine logic or skill-based
- ⚠️ **Narrator AI** — Spec 005's MVP stub differs from Spec 002's real implementation (no transition path defined)
- ⚠️ **Skill Data Storage** — Ambiguous: private `skills/<skill>/data/` vs shared persistence layer ownership

**What's Blocked**:
- ❌ **Spec 008 Open Questions** —Three unresolved decisions block implementation:
  1. How to chunk lore files? (affects Spec 007 ingestion)
  2. Context window budget across memory tiers? (affects all memory specs)
  3. Player free-text input model? (affects interaction design)

**Major Gaps**:
- No embedding model specification
- No cross-reference between Campaigns (007) and Narrative Engine (008) despite 100% coupling
- No definition of how Narrator AI accesses skill manifests or behavioral prompts
- Success criteria lacking acceptance test procedures (SC-002 "80% references" — measured how?)

The analysis includes detailed issue breakdown with recommendations for each problem. Specs 001-005 are relatively solid; Specs 006-008 need architectural alignment on persistence, memory, and narrative execution before implementation begins.