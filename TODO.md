# Narratoria Development TODO

**Last Updated**: 2026-01-31
**Current Branch**: 006-skill-state-persistence
**Status**: Spec analysis complete; planning phase next

---

## 🎯 High Priority - Spec 006 Planning Phase

### Planning & Task Generation
- [ ] Run `/speckit.plan` to generate `specs/006-skill-state-persistence/plan.md`
- [ ] Run `/speckit.tasks` to generate `specs/006-skill-state-persistence/tasks.md`
- [ ] Run `/speckit.analyze` again to validate plan/task consistency
- [ ] Review generated plan for architecture decisions and data model details

### Documentation & References
- [ ] Update [specs/005-dart-implementation/spec.md](specs/005-dart-implementation/spec.md):
  - [ ] Add 006 to parent specs list (currently missing)
  - [ ] Add implementation guidance for ObjectBox persistence backend
  - [ ] Document how Dart implements the persistence layer query interface

### Deferred Design Decisions (for Planning Phase)
- [ ] **U1 - Embedding Model Details**: Specify which models are supported (Ollama, LangChain, etc.)
  - [ ] Document model selection criteria in plan.md
  - [ ] Add fallback strategy if primary embedding service unavailable
  - [ ] Include vector dimension constraints (current: 384-2048)

- [ ] **U2 - Query Vector Terminology**: Review and either remove or use in requirements
  - [ ] Add acceptance scenario for FR-147 (query performance monitoring)
  - [ ] Clarify if "Query Vector" should be formally defined in data model

---

## 📋 Medium Priority - Cross-Spec Alignment

### Spec 005 Updates (Dart Implementation)
- [ ] Add ObjectBox to technology stack table
- [ ] Add LangChain (for Dart) to embedding options
- [ ] Document persistence layer in MVP requirements section (3.2)
- [ ] Add data structure diagrams for:
  - [ ] Memory Event entity
  - [ ] Faction Reputation entity
  - [ ] NPC Perception Record entity
  - [ ] Character Portrait entity

### Spec Reference Chain
- [ ] Verify all specs properly reference their dependencies:
  - [ ] 006 references 003, 004, 005 ✅
  - [ ] 005 references 001, 002, 003, 004, **006** (needs update)
  - [ ] 004 references 002, 003, **006** ✅

### Constitution Compliance
- [ ] Document in plan.md how persistence layer satisfies Principle II exception (like Narrator AI)
- [ ] Clarify skill-private vs shared data in implementation guide
- [ ] Add testing approach for graceful degradation (FR-148 concurrent access)

---

## 🛠️ Implementation Phase (Post-Planning)

### Spec 006 Implementation
- [ ] Implement persistence layer core:
  - [ ] In-process database initialization (ObjectBox)
  - [ ] Memory Event schema and storage
  - [ ] Query interface for all skills
  - [ ] Context augmentation interface (FR-142)

- [ ] Implement per-skill adapters:
  - [ ] Memory skill: store/recall interface
  - [ ] Reputation skill: faction data queries
  - [ ] NPC Perception skill: perception history retrieval
  - [ ] Character Portraits skill: portrait caching

- [ ] Implement advanced features:
  - [ ] Semantic similarity search (FR-133)
  - [ ] Decay calculation for reputation/perception (FR-146)
  - [ ] Data retention policies (FR-145)
  - [ ] Scoped queries by playthrough/session/location (FR-143)
  - [ ] Performance monitoring (FR-147)
  - [ ] Concurrent access handling (FR-148)

### Testing
- [ ] Unit tests for persistence layer (each FR testable)
- [ ] Integration tests for skill-persistence interaction
- [ ] Performance tests:
  - [ ] Semantic search latency (target: <500ms for 1000+ events)
  - [ ] Context augmentation latency (target: <200ms)
  - [ ] Portrait lookup latency (target: <100ms)
- [ ] Data integrity tests:
  - [ ] Concurrent write conflicts
  - [ ] Application restart recovery
  - [ ] Decay calculation accuracy
- [ ] Edge case tests (5 documented edge cases)

---

## 📚 Related Specifications - Updates Needed

### Spec 003 (Skills Framework)
- [ ] Verify skill configuration patterns support persistence backend selection
- [ ] Review if skill discovery needs knowledge of persistence layer

### Spec 004 (Narratoria Skills)
- [ ] ✅ Memory skill priority elevated to P2
- [ ] Verify all skill acceptance scenarios align with spec 006 FRs
- [ ] Document memory skill's context augmentation behavior

### Spec 002 (Plan Execution)
- [ ] Review if context augmentation affects plan execution order
- [ ] Verify concurrent skill access patterns compatible with persistence layer

---

## 🧪 Quality & Testing

### Analysis & Validation
- [ ] Run `/speckit.analyze` after plan/tasks generation
- [ ] Verify FR-to-task mapping is 100% covered
- [ ] Check for unmapped user stories
- [ ] Validate success criteria are measurable and achievable

### Code Review Checklist
- [ ] Constitution compliance verified
- [ ] No implementation details leaked into specs
- [ ] All requirements are testable and unambiguous
- [ ] Acceptance scenarios map to actual code tests
- [ ] Cross-spec consistency maintained

---

## 🔄 Git & Branching

### Current Work
- [x] Create spec 006 draft
- [x] Patch spec 004 for ObjectBox
- [x] Commit spec and checklist
- [x] Run analysis and apply remediation
- [ ] Merge remediation commits

### Next Steps
- [ ] Create plan.md and tasks.md via speckit
- [ ] Commit plan and tasks to 006-skill-state-persistence branch
- [ ] Create PR from 006-skill-state-persistence → main
- [ ] Review PR for constitution compliance
- [ ] Merge to main after approval

---

## 📖 Documentation

### Narrative for Future Developers
- [ ] Add ADR (Architecture Decision Record) explaining:
  - [ ] Why shared persistence layer vs skill-private storage
  - [ ] ObjectBox choice vs alternatives (SQLite, RocksDB, etc.)
  - [ ] LangChain integration strategy for embeddings
  - [ ] Decay algorithm rationale

- [ ] Create data model diagram showing:
  - [ ] Entity relationships
  - [ ] Foreign key constraints
  - [ ] Indexing strategy for performance

- [ ] Document query patterns:
  - [ ] Semantic similarity search algorithm
  - [ ] Scoped query filtering logic
  - [ ] Concurrent access control mechanism

---

## ✅ Completed Items

- [x] Spec 004 patched to reference ObjectBox
- [x] Spec 006 created with 4 user stories, 18 FRs, 10 success criteria
- [x] Quality checklist created
- [x] Specification analysis performed
- [x] FR collision fixed (113-130 → 131-148)
- [x] Memory skill priority elevated to P2
- [x] Architecture clarified for constitution compliance
- [x] Commits created and tracked

---

## 📞 Notes

**Key Decision Points Made:**
- Memory skill is P2 (foundational, not MVP but essential post-MVP)
- Persistence layer is shared infrastructure (exception like Narrator AI)
- ObjectBox for Dart + LangChain for embeddings (planned implementation)
- Playthrough isolation in v1 (cross-playthrough queries future enhancement)

**Assumptions to Validate:**
- Embedding model availability (local Ollama or hosted service)
- Data volume expectations (50-200 events/session, 1000s/playthrough)
- Latency tolerances (500ms search, 200ms augmentation)
- Storage quota policies (configurable retention)

**Risks to Monitor:**
- Embedding quality affecting semantic search relevance
- Concurrent access under load (plan performance tests)
- Storage growth over long campaigns (retention policy testing)
- Migration if embedding model changes (versioning strategy needed)

