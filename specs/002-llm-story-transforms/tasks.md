---

description: "Task list for LLM Story Transforms"

---

# Tasks: LLM Story Transforms

**Input**: Design documents from `/specs/002-llm-story-transforms/`  
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/, quickstart.md

**Tests**: Unit + integration tests are REQUIRED by the spec’s Test Matrix (no live network calls).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `- [ ] T### [P?] [US#?] Description with file path`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[US#]**: Which user story this task belongs to (US1, US2, US3)
- Setup/Foundational/Polish tasks MUST NOT include a story label
- Every task MUST include an exact file path

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add shared folders, dependencies, and common primitives for LLM-backed transforms.

- [X] T001 Create LLM transform folders in src/Pipeline/Transforms/Llm/ (Providers/, Prompts/, StoryState/)
- [X] T002 Add OpenAI SDK NuGet package reference to src/Narratoria.csproj
- [X] T003 Add DI + HttpClient support NuGet packages to src/Narratoria.csproj (Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Http)
- [X] T004 [P] Add provider options records (OpenAI + HuggingFace) in src/Pipeline/Transforms/Llm/Providers/LlmProviderOptions.cs
- [X] T005 [P] Add a single shared abstraction for text generation in src/Pipeline/Transforms/Llm/Providers/ITextGenerationService.cs
- [X] T006 [P] Add deterministic fake provider for tests in tests/Narratoria.Tests/Pipeline/Llm/FakeTextGenerationService.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core implementation pieces that MUST exist before any story-specific transform can be implemented.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T007 [P] Add shared request/response types for generation in src/Pipeline/Transforms/Llm/Providers/TextGenerationModels.cs
- [X] T008 Implement OpenAI provider wrapper (official SDK) in src/Pipeline/Transforms/Llm/Providers/OpenAiTextGenerationService.cs
- [X] T009 Implement Hugging Face REST provider wrapper (HttpClient) in src/Pipeline/Transforms/Llm/Providers/HuggingFaceTextGenerationService.cs
- [X] T010 [P] Add HF request/response DTOs aligned with specs/002-llm-story-transforms/contracts/huggingface-inference.request.schema.json in src/Pipeline/Transforms/Llm/Providers/HuggingFaceDtos.cs
- [X] T011 Add DI registration extensions for providers in src/Pipeline/Transforms/Llm/Providers/LlmServiceCollectionExtensions.cs
- [X] T012 [P] Add prompt templates as constants/helpers in src/Pipeline/Transforms/Llm/Prompts/PromptTemplates.cs
- [X] T013 Add story state JSON models + serializer helpers in src/Pipeline/Transforms/Llm/StoryState/StoryStateModels.cs
- [X] T014 Add story state annotation keys + read/write helpers in src/Pipeline/Transforms/Llm/StoryState/StoryStateAnnotations.cs
- [X] T015 Add merge rules for character/inventory updates (provenance + confidence) in src/Pipeline/Transforms/Llm/StoryState/StoryStateMerge.cs
- [X] T016 [P] Add DTOs for structured character/inventory updates in src/Pipeline/Transforms/Llm/StoryState/StoryStateUpdateDtos.cs
- [X] T017 Add shared error handling helpers (provider failure + parse failure + log error => passthrough input unchanged) in src/Pipeline/Transforms/Llm/LlmTransformErrorHandling.cs
- [X] T018 [P] Add unit tests for StoryState JSON roundtrip in tests/Narratoria.Tests/Pipeline/Llm/StoryStateSerializationTests.cs
- [X] T019 [P] Add unit tests for merge invariants (non-destructive updates + confidence behavior) in tests/Narratoria.Tests/Pipeline/Llm/StoryStateMergeTests.cs
- [X] T020 Add integration test harness that runs a PipelineDefinition with FakeTextGenerationService in tests/Narratoria.Tests/Pipeline/Llm/LlmPipelineHarness.cs

**Checkpoint**: Foundation ready — user story transforms can now be implemented and tested.

---

## Phase 3: User Story 1 - Improved Narration Output (Priority: P1) 🎯 MVP

**Goal**: Rewrite streamed narration text into grammatically-correct, narration-ready prose while preserving meaning.

**Independent Test**: A pipeline with the rewrite transform rewrites bad grammar and preserves key facts using the deterministic fake LLM provider.

### Tests for User Story 1

- [X] T021 [P] [US1] Add unit tests for rewrite transform (passthrough + original text annotation) in tests/Narratoria.Tests/Pipeline/Llm/RewriteTransformTests.cs
- [X] T022 [P] [US1] Add integration test for rewrite transform in a full pipeline in tests/Narratoria.Tests/Pipeline/Llm/RewritePipelineIntegrationTests.cs

### Implementation for User Story 1

- [X] T023 [US1] Add provider-call prompt composition for rewrite in src/Pipeline/Transforms/Llm/Prompts/RewritePromptBuilder.cs
- [X] T024 [US1] Implement rewrite transform in src/Pipeline/Transforms/Llm/RewriteNarrationTransform.cs

**Checkpoint**: US1 is complete and independently testable.

# Tasks: LLM Story Transforms

**Input**: plan.md, spec.md, data-model.md, research.md, quickstart.md, moderation-prompts.md

**Format**: `- [ ] T### [P?] [Story?] Description with file path`

## Phase 1: Setup

- [ ] T001 Create LLM transforms folder scaffold in src/Pipeline/Transforms/Llm/ (Providers/, Prompts/, StoryState/)
- [ ] T002 [P] Add tests scaffold folder for LLM transforms in tests/Narratoria.Tests/Pipeline/Llm/
- [ ] T003 [P] Ensure moderation policy source is pinned at specs/002-llm-story-transforms/moderation-prompts.md and referenced in transforms documentation

---

## Phase 2: Foundational (Blocking)

- [ ] T004 Define shared LLM provider abstraction and request/response DTOs in src/Pipeline/Transforms/Llm/Providers/ITextGenerationService.cs
- [ ] T005 [P] Implement provider wiring stubs (OpenAI/HuggingFace) behind the abstraction in src/Pipeline/Transforms/Llm/Providers/
- [ ] T006 Build StoryState serializer/merger utilities for summary/characters/inventory/reputation in src/Pipeline/Transforms/Llm/StoryState/StoryStateSerializer.cs
- [ ] T007 [P] Add moderation prompt loader and safety annotation models (PolicyFlags/IncidentLog) in src/Pipeline/Transforms/Llm/Prompts/ModerationPolicy.cs (load from specs/002-llm-story-transforms/moderation-prompts.md)
- [ ] T008 [P] Update contracts/story-state.schema.json to include reputation, policy flags, incident log, and trust impacts

---

## Phase 3: User Story 1 - Improved Narration Output (Priority: P1)

**Goal**: Rewrite narration with moderation guardrail while preserving meaning and chunk shape.

**Independent Test**: Input noisy text; expect corrected narration and, when unsafe content is present, safe rewrite plus safety annotations.

### Tests

- [ ] T009 [P] [US1] Add unit tests for rewrite transform (grammar correction, no-op on clean text, moderation rewrite) in tests/Narratoria.Tests/Pipeline/Llm/RewriteNarrationTransformTests.cs

### Implementation

- [ ] T010 [P] [US1] Add rewrite prompt template referencing moderation policy in src/Pipeline/Transforms/Llm/Prompts/RewritePrompts.cs
- [ ] T011 [US1] Implement RewriteNarrationTransform with moderation guardrail, original-text preservation, and safety annotations in src/Pipeline/Transforms/Llm/RewriteNarrationTransform.cs
- [ ] T012 [US1] Wire rewrite transform into pipeline chain defaults in src/Pipeline/Transforms/Llm/PipelineDefinitionExtensions.cs

---

## Phase 4: User Story 2 - Automatic Recap (Priority: P2)

**Goal**: Maintain rolling summary updated per chunk.

**Independent Test**: Multiple chunks produce coherent, updated recap after each chunk.

### Tests

- [ ] T013 [P] [US2] Add unit tests for summary transform (accumulating context, resilience to missing prior state) in tests/Narratoria.Tests/Pipeline/Llm/StorySummaryTransformTests.cs

### Implementation

- [ ] T014 [P] [US2] Add summary prompt template in src/Pipeline/Transforms/Llm/Prompts/SummaryPrompts.cs
- [ ] T015 [US2] Implement StorySummaryTransform updating StoryState summary and annotations in src/Pipeline/Transforms/Llm/StorySummaryTransform.cs

---

## Phase 5: User Story 3 - Track Characters and Inventory (Priority: P3)

**Goal**: Maintain character roster and inventory state with provenance and confidence.

**Independent Test**: Chunk with character intro and item change updates roster/inventory accordingly.

### Tests

- [ ] T016 [P] [US3] Add unit tests for character tracking (new/merge/low-confidence) in tests/Narratoria.Tests/Pipeline/Llm/CharacterTrackerTransformTests.cs
- [ ] T017 [P] [US3] Add unit tests for inventory tracking (add/remove/quantity) in tests/Narratoria.Tests/Pipeline/Llm/InventoryTrackerTransformTests.cs

### Implementation

- [ ] T018 [P] [US3] Add character and inventory prompt templates in src/Pipeline/Transforms/Llm/Prompts/CharacterInventoryPrompts.cs
- [ ] T019 [US3] Implement CharacterTrackerTransform merging roster updates with provenance in src/Pipeline/Transforms/Llm/CharacterTrackerTransform.cs
- [ ] T020 [US3] Implement InventoryTrackerTransform merging inventory updates with provenance in src/Pipeline/Transforms/Llm/InventoryTrackerTransform.cs

---

## Phase 6: User Story 4 - Track Player Reputation (Priority: P3)

**Goal**: Maintain reputation scores (global/faction/NPC) with consequence cues.

**Independent Test**: Positive and negative actions adjust reputation up/down with deterministic merge of conflicting signals.

### Tests

- [ ] T021 [P] [US4] Add unit tests for reputation tracking (positive/negative/conflict aggregation) in tests/Narratoria.Tests/Pipeline/Llm/ReputationTransformTests.cs

### Implementation

- [ ] T022 [P] [US4] Add reputation prompt templates in src/Pipeline/Transforms/Llm/Prompts/ReputationPrompts.cs
- [ ] T023 [US4] Implement ReputationTransform updating ReputationState and consequence cues with provenance in src/Pipeline/Transforms/Llm/ReputationTransform.cs

---

## Phase 7: Polish & Cross-Cutting

- [ ] T024 [P] Refresh quickstart with moderation/reputation usage and pipeline order in specs/002-llm-story-transforms/quickstart.md
- [ ] T025 Add logging/telemetry guidance for safety and reputation events in specs/002-llm-story-transforms/plan.md and src/Pipeline/Transforms/Llm/README.md (create if absent)
- [ ] T026 [P] Add integration smoke tests covering full chain (rewrite→summary→character+inventory+reputation) in tests/Narratoria.Tests/Pipeline/Llm/PipelineIntegrationTests.cs
- [ ] T027 Verify story-state.schema.json and data-model.md remain consistent with implemented fields (reputation, policy flags, incident logs) in specs/002-llm-story-transforms/contracts/story-state.schema.json and specs/002-llm-story-transforms/data-model.md

---

## Dependencies & Execution Order

- Phases: Setup → Foundational → US1 (P1) → US2 (P2) → US3 (P3) → US4 (P3) → Polish.
- Story dependency: US1 must precede US2/US3/US4; US2/US3/US4 can run in parallel once US1+Foundational complete, but chain integration validated in Polish.
- Within each story: Tests before implementation; prompts before transforms; transforms before pipeline wiring.

## Parallel Opportunities (examples)

- Setup: T002 and T003 parallel after T001.
- Foundational: T005, T007, T008 parallel after T004.
- US1: T009 and T010 parallel; T011 after prompts/utilities; T012 after T011.
- US2: T013 and T014 parallel; T015 after prompts/utilities.
- US3: T016 and T017 parallel; T018 after prompts; T019/T020 after prompts/utilities.
- US4: T021 and T022 parallel; T023 after prompts/utilities.
- Polish: T024, T025, T026, T027 can parallel where file paths do not conflict.

## Implementation Strategy

- MVP: Deliver US1 (rewrite + moderation) after Setup/Foundational, then validate chain with summary/trackers before proceeding.
- Incremental: Add US2 recap, then US3 character/inventory, then US4 reputation; validate each independently.
- Deterministic tests: use fake providers; no live network calls.
## Implementation Strategy
