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

---

## Phase 4: User Story 2 - Automatic Recap (Priority: P2)

**Goal**: Maintain a rolling story summary updated after new narration arrives.

**Independent Test**: Feeding multiple chunks yields an updated summary annotation after each chunk/batch.

### Tests for User Story 2

- [X] T025 [P] [US2] Add unit tests for summary transform (updates + provider failure degrade) in tests/Narratoria.Tests/Pipeline/Llm/SummaryTransformTests.cs
- [X] T026 [P] [US2] Add integration test for summary updates across multiple chunks in tests/Narratoria.Tests/Pipeline/Llm/SummaryPipelineIntegrationTests.cs

### Implementation for User Story 2

- [X] T027 [US2] Add prompt composition for summary (prior summary + new text) in src/Pipeline/Transforms/Llm/Prompts/SummaryPromptBuilder.cs
- [X] T028 [US2] Implement summary transform in src/Pipeline/Transforms/Llm/StorySummaryTransform.cs
- [X] T029 [US2] Write updated summary to StoryState JSON annotations in src/Pipeline/Transforms/Llm/StoryState/StoryStateAnnotations.cs

**Checkpoint**: US2 is complete and independently testable.

---

## Phase 5: User Story 3 - Track Characters and Inventory (Priority: P3)

**Goal**: Maintain structured character roster and inventory state, using rewritten text + latest summary.

**Independent Test**: Provide chunks that introduce a character and add/remove an item; verify merged StoryState reflects updates and retains provenance.

### Tests for User Story 3

- [X] T030 [P] [US3] Add unit tests for character tracker extracting a new character in tests/Narratoria.Tests/Pipeline/Llm/CharacterTrackerTransformTests.cs
- [X] T031 [P] [US3] Add unit tests for inventory tracker add/remove behavior in tests/Narratoria.Tests/Pipeline/Llm/InventoryTrackerTransformTests.cs
- [X] T032 [P] [US3] Add unit tests for parsing structured tracker updates (JSON -> DTOs) in tests/Narratoria.Tests/Pipeline/Llm/TrackerOutputParsingTests.cs
- [X] T033 [P] [US3] Add integration test ensuring tracker transforms can read latest summary + rewritten text in tests/Narratoria.Tests/Pipeline/Llm/TrackerPipelineIntegrationTests.cs

### Implementation for User Story 3

- [X] T034 [US3] Add prompt composition for character extraction (text + summary) in src/Pipeline/Transforms/Llm/Prompts/CharacterPromptBuilder.cs
- [X] T035 [US3] Add prompt composition for inventory extraction (text + summary) in src/Pipeline/Transforms/Llm/Prompts/InventoryPromptBuilder.cs
- [X] T036 [US3] Implement character tracker transform in src/Pipeline/Transforms/Llm/CharacterTrackerTransform.cs
- [X] T037 [US3] Implement inventory tracker transform in src/Pipeline/Transforms/Llm/InventoryTrackerTransform.cs
- [X] T038 [US3] Ensure transforms merge structured updates into StoryState JSON annotations in src/Pipeline/Transforms/Llm/StoryState/StoryStateMerge.cs

**Checkpoint**: US3 is complete and independently testable.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Tighten correctness, resilience, and documentation across all stories.

- [X] T039 [P] Add unit tests for cancellation propagation (provider call honors CancellationToken) in tests/Narratoria.Tests/Pipeline/Llm/CancellationTests.cs
- [X] T040 Add transform chaining example in specs/002-llm-story-transforms/quickstart.md
- [X] T041 Run quickstart validation by adding a runnable example test in tests/Narratoria.Tests/Pipeline/Llm/QuickstartExampleTests.cs
- [X] T042 [P] Add unit tests that failure paths emit expected logs (transform + session/turn) in tests/Narratoria.Tests/Pipeline/Llm/LoggingTests.cs
- [X] T043 [P] Add unit tests ensuring transforms are stream-safe and preserve pass-through annotations (including optional run metadata keys) in tests/Narratoria.Tests/Pipeline/Llm/StreamingAndMetadataTests.cs

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories.
- **User Stories (Phase 3–5)**: Depend on Foundational phase completion.
- **Polish (Phase 6)**: Depends on desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: Depends on Phase 2 only.
- **US2 (P2)**: Depends on Phase 2; benefits from US1 but must be independently testable.
- **US3 (P3)**: Depends on Phase 2; MUST run after summary within a pipeline execution order.

### Required Transform Order

- Rewrite -> Summary -> (Character, Inventory)

### Dependency Graph (User Story Completion Order)

`Phase 1 (Setup) -> Phase 2 (Foundational) -> US1 (Rewrite) -> US2 (Summary) -> US3 (Trackers) -> Phase 6 (Polish)`

---

## Parallel Opportunities

- Phase 1: T004–T006 can run in parallel.
- Phase 2: T007, T010, T012, T016, T018, and T019 can run in parallel.
- US1 tests: T021–T022 can run in parallel.
- US2 tests: T025–T026 can run in parallel.
- US3 tests: T030–T033 can run in parallel.
- Phase 6: T039, T042, and T043 can run in parallel.

---

## Parallel Example: User Story 3

```bash
Task: "T030 Add unit tests for character tracker extracting a new character in tests/Narratoria.Tests/Pipeline/Llm/CharacterTrackerTransformTests.cs"
Task: "T031 Add unit tests for inventory tracker add/remove behavior in tests/Narratoria.Tests/Pipeline/Llm/InventoryTrackerTransformTests.cs"
Task: "T032 Add unit tests for parsing structured tracker updates (JSON -> DTOs) in tests/Narratoria.Tests/Pipeline/Llm/TrackerOutputParsingTests.cs"
Task: "T033 Add integration test ensuring tracker transforms can read latest summary + rewritten text in tests/Narratoria.Tests/Pipeline/Llm/TrackerPipelineIntegrationTests.cs"
```

---

## Parallel Example: User Story 1

```bash
Task: "T021 Add unit tests for rewrite transform (passthrough + original text annotation) in tests/Narratoria.Tests/Pipeline/Llm/RewriteTransformTests.cs"
Task: "T022 Add integration test for rewrite transform in a full pipeline in tests/Narratoria.Tests/Pipeline/Llm/RewritePipelineIntegrationTests.cs"
```

---

## Parallel Example: User Story 2

```bash
Task: "T025 Add unit tests for summary transform (updates + provider failure degrade) in tests/Narratoria.Tests/Pipeline/Llm/SummaryTransformTests.cs"
Task: "T026 Add integration test for summary updates across multiple chunks in tests/Narratoria.Tests/Pipeline/Llm/SummaryPipelineIntegrationTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 (Setup)
2. Complete Phase 2 (Foundational)
3. Complete Phase 3 (US1)
4. Validate US1 independently using `RewritePipelineIntegrationTests`

### Incremental Delivery

1. Setup + Foundational
2. US1 (rewrite)
3. US2 (summary)
4. US3 (character + inventory)
5. Polish
