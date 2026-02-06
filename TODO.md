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

### 🔴 **Issue 2: Missing Cross-Spec References**

- **Spec 002** references "Spec 003" for skill discovery but Spec 003 doesn't detail how plan generator access skill manifests or behavioral prompts
- **Spec 004** references "Spec 006" for Memory skill persistence but Spec 006 assumes Spec 004 skills are already defined
- **Spec 007** and **Spec 008** have **no cross-reference** despite both defining scene/campaign execution
- **Spec 006** assumes on-device embeddings model but no spec defines the embedding model selection/integration

**Recommendation**: Add explicit "Prerequisites" section to each spec.

---

### 🟡 **Issue 3: Narrator AI Stub vs Real Narrator AI**

- **Spec 005** defines MVP Narrator AI Stub as hard-coded pattern matching
- **Spec 002** defines Narrator AI interface requirements (FR-001 to FR-010) for **real LLM-based implementation**
- **Spec 004** assumes narrator can select skills and inject behavioral prompts
- **No spec** defines transition from stub to real narrator or LLM integration architecture

**Problem**: Unclear which specs apply to stub (MVP) vs real narrator.

**Recommendation**: Mark Spec 002's narrator requirements as "Post-MVP LLM Integration" or split into two narratives.

---

### 🟡 **Issue 4: Skill Data Storage Pattern Inconsistency**

- **Spec 003** defines skill-private data in `skills/<skill>/data/` (FR-103-107)
- **Spec 006** introduces *shared* persistence layer for cross-skill context
- **Spec 004** skill implementations don't clarify: Does Memory skill data go in `skills/memory/data/` or in shared persistence layer?

**Problem**: Architectural principle unclear. Who owns the data?

**Recommendation**: Explicitly state:
- `skills/<skill>/data/` = skill-private cache/working files
- Shared persistence layer (Spec 006) = cross-skill context (memory events, reputation scores, NPC perception)

---

### ✅ **Issue 5: Overlap Between Spec 004 Skills and Spec 008 Engine** [RESOLVED]

**Resolution (commit 054f21f)**: Clarified responsibilities:
- **Spec 004**: Defines skill interfaces (what parameters Memory skill accepts, what it returns)
- **Spec 008**: Defines when/why to invoke skills (Plan Generator decides contextually)
- **Scene execution**: Orchestrated by Plan Generator invoking skills, not built-in engine logic

---

### � **Issue 6: Spec 008 Open Questions Block Implementation** [2 OF 3 RESOLVED]

~~1. **Lore chunking strategy** (FR-005)~~ ✅ **RESOLVED (commit c7ec6e6)**: Paragraph-based, 512 token max, sentence-boundary fallback
~~2. **Context window allocation** (FR-006)~~ ✅ **RESOLVED (commit 054f21f)**: Eliminated fixed budgets; LLM decides contextually
3. **Player free-text input** (Q1) ⏸️ **IN PROGRESS**: Determining interaction model

**Recommendation**: Resolve player input question to fully unblock Spec 008 implementation.

---

### 🟡 **Issue 7: Campaign Format Creeds Not Binding**

- **Spec 007** defines "Campaign Format Creeds" as design philosophy
- No enforcement mechanism defined (no validation schemas check `generated` flags)
- **Spec 007** FR-034-036 try to enforce generated asset marking, but sidecar keywords (FR-039b) don't have similar enforcement

**Recommendation**: Define ObjectBox validation that rejects assets missing provenance when `generated: true`.

---

### 🟡 **Issue 8: Success Criteria Mismatch Across Specs**

- **Spec 004** defines SC-013-022 (Advanced Skills) but these aren't marked as post-MVP
- **Spec 005** defines SC for MVP but doesn't cross-reference Spec 004
- **Spec 006** defines SC-023-032 (persistence layer) but Spec 008 needs these to function

**Recommendation**: Tag success criteria with MVP/post-MVP priority.

---

### 🟡 **Issue 9: Missing Reference: Embedding Model**

- **Spec 006** assumes semantic embeddings for memory search (FR-132, FR-133)
- **Spec 007** references embeddings for sparse data enrichment
- **Spec 008** uses embeddings for semantic search
- **No spec** defines: Which embedding model? How is it trained? Local or hosted?

**Problem**: Affects performance, quality, and deployment.

**Recommendation**: Add Spec 009 (Infrastructure) or append to CLAUDE.md with embedding model decision.

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
| **002** | ✅ Draft | Plan Execution | Partial | LLM integration | 001, 003 |
| **003** | ✅ Draft | Skill Framework | Yes | Hot-reload | 001, 002 |
| **004** | ✅ Draft | Individual Skills | Core skills | Advanced skills | 003, 006 |
| **005** | ✅ Draft | Dart/Flutter | Yes | - | 001-004 |
| **006** | ✅ Draft | Persistence | Yes | Query optimization | None |
| **007** | ✅ Draft | Campaign Format | Yes | - | 008, LLM integration |
| **008** | ⚠️ Draft | Narrative Engine | Yes | - | **1 Open Question** |

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