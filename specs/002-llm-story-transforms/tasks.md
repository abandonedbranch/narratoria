---
---

description: "Task list for LLM Story Transforms"

---

# Tasks: LLM Story Transforms

**Input**: plan.md, spec.md, data-model.md, research.md, contracts/, quickstart.md, moderation-prompts.md

**Format**: `- [ ] T### [P?] [US#?] Description with file path`

## Phase 1: Setup (Shared Infrastructure)

- [ ] T001 Create LLM transforms scaffold folders in src/Pipeline/Transforms/Llm/ (Providers/, Prompts/, StoryState/)
- [ ] T002 [P] Add tests scaffold folder for LLM transforms in tests/Narratoria.Tests/Pipeline/Llm/
- [ ] T003 [P] Pin moderation policy source reference in specs/002-llm-story-transforms/moderation-prompts.md and cite it in transform docs/README

---

## Phase 2: Foundational (Blocking)

- [ ] T004 Define shared LLM provider abstraction and request/response DTOs in src/Pipeline/Transforms/Llm/Providers/ITextGenerationService.cs
- [ ] T005 [P] Implement provider wiring stubs (OpenAI/HF) behind the abstraction in src/Pipeline/Transforms/Llm/Providers/
- [ ] T006 Build StoryState serializer/merger utilities for summary/characters/inventory/reputation in src/Pipeline/Transforms/Llm/StoryState/StoryStateSerializer.cs
- [ ] T007 [P] Add moderation prompt loader and safety annotation models (PolicyFlags/IncidentLog) loading from specs/002-llm-story-transforms/moderation-prompts.md in src/Pipeline/Transforms/Llm/Prompts/ModerationPolicy.cs
- [ ] T008 [P] Update specs/002-llm-story-transforms/contracts/story-state.schema.json and data-model.md to include reputation, policy flags, incident log, trust impacts, and moderation prompt source
- [ ] T009 [P] Add cancellation/stream harness for transforms (bounded buffering + CancellationToken propagation) in tests/Narratoria.Tests/Pipeline/Llm/StreamingCancellationTests.cs
- [ ] T010 Add integration harness ensuring chunk type/shape is preserved through transforms in tests/Narratoria.Tests/Pipeline/Llm/ChunkShapeIntegrationTests.cs (covers FR-002)

---

## Phase 3: User Story 1 - Improved Narration Output (P1)

**Goal**: Rewrite narration with moderation guardrail while preserving meaning and chunk shape.

**Independent Test**: Noisy input is corrected; unsafe input is safely rewritten with safety annotations; original text preserved.

### Tests

- [ ] T011 [P] [US1] Unit tests for rewrite transform (grammar correction, no-op on clean text, safe rewrite for prohibited content, original-text preservation, output shape) in tests/Narratoria.Tests/Pipeline/Llm/RewriteNarrationTransformTests.cs

### Implementation

- [ ] T012 [P] [US1] Add rewrite prompt templates referencing moderation policy in src/Pipeline/Transforms/Llm/Prompts/RewritePrompts.cs
- [ ] T013 [US1] Implement RewriteNarrationTransform with moderation guardrail, original-text preservation, safety annotations in src/Pipeline/Transforms/Llm/RewriteNarrationTransform.cs
- [ ] T014 [US1] Wire rewrite transform into pipeline defaults in src/Pipeline/Transforms/Llm/PipelineDefinitionExtensions.cs

---

## Phase 4: User Story 2 - Automatic Recap (P2)

**Goal**: Maintain rolling summary updated per chunk.

**Independent Test**: Multiple chunks produce coherent, updated recap after each chunk.

### Tests

- [ ] T015 [P] [US2] Unit tests for summary transform (accumulating context, resilience to missing prior state, chunk shape preserved) in tests/Narratoria.Tests/Pipeline/Llm/StorySummaryTransformTests.cs

### Implementation

- [ ] T016 [P] [US2] Add summary prompt templates in src/Pipeline/Transforms/Llm/Prompts/SummaryPrompts.cs
- [ ] T017 [US2] Implement StorySummaryTransform updating StoryState summary and annotations in src/Pipeline/Transforms/Llm/StorySummaryTransform.cs

---

## Phase 5: User Story 3 - Track Characters and Inventory (P3)

**Goal**: Maintain character roster and inventory with provenance/confidence.

**Independent Test**: Chunk with character intro and item change updates roster/inventory accordingly with provenance.

### Tests

- [ ] T018 [P] [US3] Unit tests for character tracking (new/merge/low-confidence, provenance) in tests/Narratoria.Tests/Pipeline/Llm/CharacterTrackerTransformTests.cs
- [ ] T019 [P] [US3] Unit tests for inventory tracking (add/remove/quantity, provenance) in tests/Narratoria.Tests/Pipeline/Llm/InventoryTrackerTransformTests.cs

### Implementation

- [ ] T020 [P] [US3] Add character/inventory prompt templates in src/Pipeline/Transforms/Llm/Prompts/CharacterInventoryPrompts.cs
- [ ] T021 [US3] Implement CharacterTrackerTransform merging roster updates with provenance in src/Pipeline/Transforms/Llm/CharacterTrackerTransform.cs
- [ ] T022 [US3] Implement InventoryTrackerTransform merging inventory updates with provenance in src/Pipeline/Transforms/Llm/InventoryTrackerTransform.cs

---

## Phase 6: User Story 4 - Track Player Reputation (P3)

**Goal**: Maintain reputation scores (global/faction/NPC) with consequence cues.

**Independent Test**: Positive/negative actions adjust reputation; conflicting signals merge deterministically with provenance.

### Tests

- [ ] T023 [P] [US4] Unit tests for reputation tracking (positive/negative/conflict aggregation, provenance) in tests/Narratoria.Tests/Pipeline/Llm/ReputationTransformTests.cs
- [ ] T026 [P] [US4] Add reputation threshold/decay tests for accumulated minor offenses triggering consequence cues in tests/Narratoria.Tests/Pipeline/Llm/ReputationThresholdTests.cs

### Implementation

- [ ] T024 [P] [US4] Add reputation prompt templates in src/Pipeline/Transforms/Llm/Prompts/ReputationPrompts.cs
- [ ] T025 [US4] Implement ReputationTransform updating ReputationState and consequence cues with provenance in src/Pipeline/Transforms/Llm/ReputationTransform.cs
- [ ] T027 [US4] Extend ReputationTransform to aggregate minor offenses over time and emit threshold-based consequence cues with provenance in src/Pipeline/Transforms/Llm/ReputationTransform.cs

---

## Phase 7: Error Handling, Safety, and Cross-Cutting

- [ ] T028 [P] Add error-handling tests for provider failure and parse failure (EH-001, EH-002) ensuring passthrough + prior state retained in tests/Narratoria.Tests/Pipeline/Llm/ErrorHandlingProviderTests.cs
- [ ] T029 [P] Add conflict-handling tests for state merges (EH-003) covering character/inventory/reputation in tests/Narratoria.Tests/Pipeline/Llm/StateConflictTests.cs
- [ ] T030 [P] Add safety rewrite tests for prohibited/ambiguous sexual content with logging/annotations (EH-005) in tests/Narratoria.Tests/Pipeline/Llm/ModerationSafetyTests.cs
- [ ] T031 [P] Add reputation fallback/merge tests for missing/contradictory signals (EH-006) in tests/Narratoria.Tests/Pipeline/Llm/ReputationFallbackTests.cs
- [ ] T032 Add cancellation propagation + bounded buffering integration test (FR-013, FR-014) in tests/Narratoria.Tests/Pipeline/Llm/StreamingCancellationTests.cs
- [ ] T033 [P] Add logging/telemetry verification for safety/reputation events (EH-004, FR-016/018) in tests/Narratoria.Tests/Pipeline/Llm/SafetyLoggingTests.cs
- [ ] T034 [P] Add full-chain integration smoke test (rewrite→summary→character+inventory+reputation) verifying chunk shape, annotations, and original-text preservation (FR-002, FR-010) in tests/Narratoria.Tests/Pipeline/Llm/PipelineIntegrationTests.cs
- [ ] T035 Refresh quickstart with moderation/reputation usage and pipeline order in specs/002-llm-story-transforms/quickstart.md
- [ ] T036 Add logging/telemetry guidance for safety and reputation events in specs/002-llm-story-transforms/plan.md and src/Pipeline/Transforms/Llm/README.md (create if absent)
- [ ] T037 Verify story-state.schema.json and data-model.md remain consistent with implemented fields (reputation, policy flags, incident logs) in specs/002-llm-story-transforms/contracts/story-state.schema.json and specs/002-llm-story-transforms/data-model.md

---

## Dependencies & Execution Order

- Phases: Setup → Foundational → US1 (P1) → US2 (P2) → US3 (P3) → US4 (P3) → Error Handling/Polish.
- US1 precedes US2/US3/US4; US2/US3/US4 can run in parallel after US1 + Foundational. Error-handling tasks rely on core transforms existing.
- Within each story: Tests before implementation; prompts before transforms; transforms before pipeline wiring.

## Parallel Opportunities (examples)

- Setup: T002 and T003 parallel after T001.
- Foundational: T005, T007, T008, T009 parallel after T004; T010 can run after harness basics.
- US1: T011 and T012 parallel; T013 after prompts/utilities; T014 after T013.
- US2: T015 and T016 parallel; T017 after prompts/utilities.
- US3: T018 and T019 parallel; T020 after prompts; T021/T022 after prompts/utilities.
- US4: T023, T024, T026 parallel; T025/T027 after prompts/utilities.
- Error/Polish: T028–T037 can parallel where file paths don’t conflict; T035–T037 can parallel with tests.

## Implementation Strategy

- MVP: Deliver US1 (rewrite + moderation) after Setup/Foundational, then validate chain before proceeding.
- Incremental: Add US2 recap, then US3 character/inventory, then US4 reputation; validate each independently.
- Deterministic tests: use fake providers; no live network calls.
