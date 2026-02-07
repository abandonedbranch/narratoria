# Feature Specification: Align Spec 005 Technology Stack

**Feature Branch**: `009-align-005-tech-stack`  
**Created**: 2025-02-07  
**Status**: Draft  
**Input**: User description: "Align Spec 005 with Specs 006/008: replace SQLite with ObjectBox, replace Flutter AI Toolkit + Ollama with Phi-3.5 Mini in-process GGUF, remove MVP language"

## User Scenarios & Testing

### User Story 1 - Consistent Technology References Across Specifications (Priority: P1)

A developer reading Spec 005 (Dart/Flutter Implementation) encounters the technology stack section and sees "SQLite (via sqflite)" listed as the database and "Flutter AI Toolkit + Ollama" as the AI integration. They then read Spec 006 which mandates ObjectBox as the persistence backend, and Spec 008 which specifies Phi-3.5 Mini running in-process via GGUF. The developer is confused about which technology decisions are authoritative and cannot confidently begin implementation.

After this change, the developer reads Spec 005 and sees technology choices that are consistent with all other specifications. The technology stack section references ObjectBox for persistence and Phi-3.5 Mini (GGUF, in-process) for AI, matching what Specs 006 and 008 define. The developer can begin implementation with confidence.

**Why this priority**: Technology stack contradictions block implementation. A developer cannot write code when specifications disagree on fundamental choices like the database engine and AI runtime.

**Independent Test**: Open Spec 005, Spec 006, and Spec 008 side by side. Verify that all technology references (database, AI integration, model loading) are mutually consistent with no contradictions.

**Acceptance Scenarios**:

1. **Given** the Spec 005 technology stack table (Section 5), **When** a developer reads "Database", **Then** the entry reads "ObjectBox (in-process)" with a note about vector search capability, matching Spec 006's persistence backend definition.
2. **Given** the Spec 005 technology stack table (Section 5), **When** a developer reads "AI Integration", **Then** the entry reads "Phi-3.5 Mini (3.8B, GGUF) + sentence-transformers/all-MiniLM-L6-v2" matching the AI model decisions documented in Spec 008 and Spec 005 Section 1.
3. **Given** all eight specifications (001–008), **When** searching for "sqflite", "SQLite", "Flutter AI Toolkit", or "Ollama", **Then** zero matches are found outside of explicitly marked historical/alternative-considered contexts.

---

### User Story 2 - Narrator AI Stub Language Cleanup (Priority: P2)

A developer reading Spec 005 Section 3.4 encounters the heading "Narrator AI Stub Implementation" with text describing "The MVP Narrator AI Stub" as a primary development artifact. This contradicts the project-wide decision (PR #71) to remove MVP/Post-MVP language and unify all specifications around the full Phi-3.5 Mini implementation.

After this change, Section 3.4 is reframed as a "Testing Stub" — a development and testing convenience that implements the NarratorAI interface with pattern-based plan generation. The language no longer implies the stub is the deliverable; instead it is clearly positioned as a testing tool subordinate to the Phi-3.5 Mini integration described in Section 1 and Section 3.1.

**Why this priority**: While less critical than the technology stack contradiction (which blocks implementation decisions), the MVP language creates confusion about project phasing and deliverables. Developers may incorrectly assume the stub is the product rather than a test harness.

**Independent Test**: Search Spec 005 for "MVP" (case-insensitive). Verify zero matches remain. Verify Section 3.4 heading and body text describe a testing utility, not a product feature.

**Acceptance Scenarios**:

1. **Given** Spec 005 Section 3.4, **When** a developer reads the heading, **Then** it reads "Narrator AI Testing Stub" (or equivalent non-MVP language).
2. **Given** Spec 005 Section 3.4, **When** a developer reads the body text, **Then** no sentence contains "MVP" and the stub is described as a development and testing convenience, not the primary AI implementation.
3. **Given** the full text of Spec 005, **When** searching for "MVP" (case-insensitive), **Then** zero matches are found.

---

### User Story 3 - Cross-Specification Consistency Verification (Priority: P3)

A project maintainer runs a consistency check across all specifications to verify that resolved architectural decisions (ObjectBox, Phi-3.5 Mini, no free-text input, choice-only interaction) are reflected uniformly.

After this change, the maintainer confirms that Spec 005 no longer contains any stale technology references and all cross-references to other specifications are accurate.

**Why this priority**: This is a verification story that confirms the other changes are complete and correct. It has lower priority because it produces no new content — it validates the work from User Stories 1 and 2.

**Independent Test**: Grep all spec files for deprecated technology terms. Verify no contradictions remain.

**Acceptance Scenarios**:

1. **Given** all eight specifications, **When** a maintainer greps for "sqflite", "SQLite", "Flutter AI Toolkit", or "Ollama" across all `specs/*/spec.md` files, **Then** zero matches are found.
2. **Given** Spec 005 cross-references (Section 7), **When** a developer follows each reference, **Then** all linked specifications exist and the described relationships are accurate.

---

### Edge Cases

- **Spec 005 read in isolation**: The Prerequisites section already directs readers to Spec 006 and Spec 008. After this change, the technology stack table in Section 5 is self-consistent, so even isolated reading produces correct understanding.
- **Future technology decisions**: If future specifications introduce additional technology choices, Section 5 of Spec 005 serves as the single authoritative technology reference for the Dart/Flutter implementation. New choices should be reflected there and in the corresponding architecture specification.
- **Partial internal consistency**: Spec 005 Sections 1, 3.1, and 3.2 already correctly reference Phi-3.5 Mini and ObjectBox. Only Section 5 (Technology Stack table) and Section 3.4 (stub heading/body) contain stale references. Changes must be limited to these sections to avoid introducing regressions in already-correct content.

## Requirements

### Functional Requirements

- **FR-001**: Spec 005 Section 5 (Technology Stack) MUST list "ObjectBox (in-process)" as the Database entry, replacing "SQLite (via sqflite)"
- **FR-002**: Spec 005 Section 5 (Technology Stack) MUST list "Phi-3.5 Mini (3.8B, GGUF, in-process) + sentence-transformers/all-MiniLM-L6-v2" as the AI Integration entry, replacing "Flutter AI Toolkit + Ollama"
- **FR-003**: Spec 005 Section 3.4 heading MUST be changed from "Narrator AI Stub Implementation" to "Narrator AI Testing Stub" (or equivalent non-MVP phrasing)
- **FR-004**: Spec 005 Section 3.4 body text MUST NOT contain the term "MVP"
- **FR-005**: The complete text of Spec 005 MUST NOT contain the terms "sqflite", "SQLite", "Flutter AI Toolkit", or "Ollama" as active technology choices
- **FR-006**: All other specifications (001–004, 006–008) MUST NOT reference "sqflite", "SQLite", "Flutter AI Toolkit", or "Ollama" as active technology choices

### Key Entities

- **Spec 005 Technology Stack Table** (Section 5): The authoritative list of implementation technologies for the Dart/Flutter reference implementation. Contains rows for Runtime, UI, State Management, Database, AI Integration, and Embeddings.
- **Spec 005 Section 3.4**: The Narrator AI testing stub section. Currently contains stale "MVP" language that must be reframed as a test utility.
- **Spec 006 (Skill State Persistence)**: The authoritative specification for persistence technology. Defines ObjectBox as the in-process database with vector search capability.
- **Spec 008 (Narrative Engine)**: The authoritative specification for AI model choices. Defines Phi-3.5 Mini (3.8B parameters, 2.5GB GGUF quantized) as the narrator LLM running in-process.

## Success Criteria

### Measurable Outcomes

- **SC-001**: A developer can read Spec 005's technology stack section and begin implementation without encountering contradictions with any other specification. Verified by side-by-side comparison of Spec 005 Section 5 against Spec 006 (persistence) and Spec 008 (AI model).
- **SC-002**: Zero occurrences of deprecated technology terms ("sqflite", "SQLite", "Flutter AI Toolkit", "Ollama") exist across all specification files. Verified by automated text search across all `specs/*/spec.md` files.
- **SC-003**: Zero occurrences of "MVP" exist in Spec 005. Verified by case-insensitive text search.
- **SC-004**: All changes are limited to Spec 005 text corrections — no functional behavior changes, no new features, no modifications to other specification files (unless stale references are discovered there during verification).
