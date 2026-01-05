---

description: "Task list for implementing the streaming narration pipeline"
---

# Tasks: Streaming Narration Pipeline

**Input**: Design documents from `specs/001-streaming-pipeline/`
**Prerequisites**: [plan.md](plan.md) (required), [spec.md](spec.md) (required)

**Tests**: Include deterministic automated tests to cover the spec’s requirements; do not assume existing tests are viable.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the new API surface area in a self-contained way.

- [ ] T001 Create pipeline folder and baseline files in src/Pipeline/
- [ ] T002 Wire pipeline source files into build in src/Narratoria.csproj
- [ ] T003 [P] Create test folder structure in tests/Narratoria.Tests/Pipeline/

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core streaming primitives and typed chunk contract (blocks all user stories).

- [ ] T004 Implement typed payload descriptor types in src/Pipeline/PipelineChunkType.cs
- [ ] T005 [P] Implement chunk metadata container in src/Pipeline/PipelineChunkMetadata.cs
- [ ] T006 Implement typed chunk envelope in src/Pipeline/PipelineChunk.cs
- [ ] T007 Implement run outcome model (completed/failed/canceled/blocked) in src/Pipeline/PipelineOutcome.cs
- [ ] T008 Implement run result container (outcome + collected sink value) in src/Pipeline/PipelineRunResult.cs
- [ ] T009 Define tiny source/transform/sink interfaces in src/Pipeline/IPipelineSource.cs, src/Pipeline/IPipelineTransform.cs, src/Pipeline/IPipelineSink.cs
- [ ] T010 Implement pipeline definition container (ordered stages + compatibility declarations) in src/Pipeline/PipelineDefinition.cs
- [ ] T011 Implement pipeline runner/executor with cancellation support in src/Pipeline/PipelineRunner.cs
- [ ] T012 Implement compatibility checking and failure classification for type mismatch/decode failures in src/Pipeline/PipelineRunner.cs

**Checkpoint**: Foundation ready — typed chunk contract + runner exist and can be tested without any external services.

---

## Phase 3: User Story 1 - Stream narration from user text (Priority: P1) 🎯 MVP

**Goal**: Caller can execute a pipeline with a text-configured source and a sink; sink observes incremental output.

**Independent Test**: In-process test runs source → runner → sink and asserts partial output and terminal outcome.

### Tests for User Story 1

- [ ] T013 [P] [US1] Add unit tests for typed chunk basics in tests/Narratoria.Tests/Pipeline/PipelineChunkTests.cs
- [ ] T014 [P] [US1] Add runner streaming tests (partial output, ordering) in tests/Narratoria.Tests/Pipeline/PipelineRunnerTests.cs

### Implementation for User Story 1

- [ ] T015 [US1] Implement a text-source configuration model in src/Pipeline/Text/TextSourceConfig.cs
- [ ] T016 [US1] Implement a text prompt source supporting complete input in src/Pipeline/Text/TextPromptSource.cs
- [ ] T017 [US1] Implement adapter from complete input to equivalent stream in src/Pipeline/Text/TextInputAdapters.cs
- [ ] T018 [US1] Implement a basic sink that collects streamed text in src/Pipeline/Text/TextCollectingSink.cs
- [ ] T019 [US1] Ensure runner can execute minimal pipeline (source → sink) in src/Pipeline/PipelineRunner.cs

**Checkpoint**: US1 complete — minimal pipeline streams text and produces a terminal outcome.

---

## Phase 4: User Story 2 - Insert transformations without breaking streaming (Priority: P2)

**Goal**: Caller can insert ordered transforms; transforms can rewrite/enrich stream while preserving streaming and order.

**Independent Test**: In-process run with two transforms A then B produces expected modified output in the sink.

### Tests for User Story 2

- [ ] T020 [P] [US2] Add transform-ordering tests in tests/Narratoria.Tests/Pipeline/TransformCompatibilityTests.cs
- [ ] T021 [P] [US2] Add transform rewrite/enrichment tests in tests/Narratoria.Tests/Pipeline/TransformCompatibilityTests.cs
- [ ] T021a [P] [US2] Add text accumulator buffering tests (bytes/chars/chunks thresholds + end-of-stream flush) in tests/Narratoria.Tests/Pipeline/TransformCompatibilityTests.cs

### Implementation for User Story 2

- [ ] T022 [US2] Implement a simple prefix transform in src/Pipeline/Transforms/PrefixTextTransform.cs
- [ ] T023 [US2] Implement a simple enrichment transform that adds annotations/metadata in src/Pipeline/Transforms/AnnotateTransform.cs
- [ ] T023a [US2] Implement text accumulator transform (bytes/chars/chunks thresholds + end-of-stream flush) in src/Pipeline/Transforms/TextAccumulatorTransform.cs
- [ ] T024 [US2] Ensure transform ordering is deterministic and enforced in src/Pipeline/PipelineRunner.cs

**Checkpoint**: US2 complete — transforms compose predictably without breaking streaming.

---

## Phase 5: User Story 3 - Handle cancellation and failure predictably (Priority: P3)

**Goal**: Cancellation, early termination, and failures are observable and stop streaming promptly.

**Independent Test**: Tests cancel mid-stream and assert no further chunks; tests incompatible types and decode failures produce classified failure outcomes.

### Tests for User Story 3

- [ ] T025 [P] [US3] Add cancellation/early termination tests in tests/Narratoria.Tests/Pipeline/PipelineRunnerTests.cs
- [ ] T026 [P] [US3] Add type incompatibility failure tests in tests/Narratoria.Tests/Pipeline/TransformCompatibilityTests.cs
- [ ] T027 [P] [US3] Add bytes→text decode contract tests in tests/Narratoria.Tests/Pipeline/StreamingInputAdapterTests.cs

### Implementation for User Story 3

- [ ] T028 [US3] Implement streaming byte-input support for text source in src/Pipeline/Text/TextPromptSource.cs
- [ ] T029 [US3] Implement bytes→text transform that requires declared encoding contract in src/Pipeline/Transforms/DecodeBytesToTextTransform.cs
- [ ] T030 [US3] Enforce that bytes→text only runs when chunk metadata declares decodability in src/Pipeline/Transforms/DecodeBytesToTextTransform.cs
- [ ] T031 [US3] Ensure runner cancels upstream promptly when downstream stops consuming in src/Pipeline/PipelineRunner.cs
- [ ] T032 [US3] Ensure runner emits explicit classified failures for type incompatibility/decode failure in src/Pipeline/PipelineRunner.cs

**Checkpoint**: US3 complete — cancellation/failure behavior is predictable and observable.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T033 [P] Update documentation entry for the pipeline API surface in README
- [ ] T034 Add a minimal usage example (non-UI) in specs/001-streaming-pipeline/quickstart.md
- [ ] T035 [P] Run and fix deterministic unit tests for the new pipeline in tests/Narratoria.Tests/Narratoria.Tests.csproj

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup — blocks all user stories
- **User Stories (Phases 3–5)**: Depend on Foundational phase completion
- **Polish (Phase 6)**: Depends on desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Depends on Foundational only
- **User Story 2 (P2)**: Depends on Foundational only
- **User Story 3 (P3)**: Depends on Foundational only

---

## Parallel Opportunities

- Setup: T003 is parallel
- Foundational: T005 is parallel
- US1: T013 and T014 can run in parallel
- US2: T020 and T021 can run in parallel
- US3: T025–T027 can run in parallel
- Polish: T033 and T035 are parallel

---

## Parallel Example: User Story 3

Task: "Add cancellation/early termination tests in tests/Narratoria.Tests/Pipeline/PipelineRunnerTests.cs"
Task: "Add type incompatibility failure tests in tests/Narratoria.Tests/Pipeline/TransformCompatibilityTests.cs"
Task: "Add bytes→text decode contract tests in tests/Narratoria.Tests/Pipeline/StreamingInputAdapterTests.cs"

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. Validate the US1 checkpoint via automated tests

### Incremental Delivery

- Add US2 (transforms) after US1 is solid
- Add US3 (cancellation/errors/typed decode) after US2
- Finish with Polish tasks
