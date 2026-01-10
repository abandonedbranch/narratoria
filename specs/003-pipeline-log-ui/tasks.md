---

description: "Task list for feature implementation"

---

# Tasks: Realtime Pipeline Log UI

**Input**: Design documents from `/specs/003-pipeline-log-ui/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: Include test tasks per spec.md AND constitution/CONTRIB. UI component changes MUST include Playwright for .NET E2E coverage (in addition to applicable unit tests).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Phase 1: Setup (Shared Infrastructure)

- [ ] T001 Confirm UI host choice matches plan/spec (Blazor Server) and keep docs consistent
- [ ] T002 [P] Create web host project in src/Narratoria.Web/Narratoria.Web.csproj (ASP.NET Core + Blazor Server)
- [ ] T003 [P] Add web host entrypoint and routing in src/Narratoria.Web/Program.cs (include route `/pipeline-log`)
- [ ] T004 [P] Add launch profile URLs for local dev/E2E in src/Narratoria.Web/Properties/launchSettings.json
- [ ] T005 [P] Create Playwright E2E test project in tests/Narratoria.PlaywrightTests/PlaywrightTests.csproj
- [ ] T006 Configure Playwright test settings and base URL handling in tests/Narratoria.PlaywrightTests/playwright.runsettings
- [ ] T007 Update quickstart run instructions for web + E2E in specs/003-pipeline-log-ui/quickstart.md

**Checkpoint**: Web project builds; Playwright project builds.

---

## Phase 2: Foundational (Blocking Prerequisites)

- [ ] T008 Create core models for spec 003 in src/PipelineLog/Models/StorySession.cs
- [ ] T009 [P] Create run record and digest models in src/PipelineLog/Models/PersistedRunRecord.cs
- [ ] T010 [P] Create LLM selection + input buffer models in src/PipelineLog/Models/LlmSelection.cs
- [ ] T011 [P] Define persistence interfaces in src/PipelineLog/Persistence/IStorySessionStore.cs
- [ ] T012 [P] Define run record store interface in src/PipelineLog/Persistence/IRunRecordStore.cs
- [ ] T013 [P] Define telemetry types in src/PipelineLog/Telemetry/RunTelemetryEntry.cs
- [ ] T014 [P] Define run identity helpers (run_id, run_sequence, snapshot hash) in src/PipelineLog/Telemetry/RunIdentity.cs
- [ ] T015 Define compaction abstraction (12 story-facts) in src/PipelineLog/Compaction/IHistoryCompactor.cs
- [ ] T016 Implement coordinator state machine (idle debounce, send suppression, latest-wins cancellation) in src/PipelineLog/PipelineExecutionCoordinator.cs
- [ ] T017 Implement per-run telemetry aggregation to PersistedRunRecord in src/PipelineLog/Telemetry/RunRecordBuilder.cs
- [ ] T018 Add deterministic unit tests for coordinator semantics in tests/Narratoria.Tests/PipelineLog/PipelineExecutionCoordinatorTests.cs

**Checkpoint**: Coordinator behavior testable without UI; core models compile.

---

## Phase 3: User Story 4 - Resume a Story Session (Priority: P1) 🎯 MVP

**Goal**: Persist summarized run records + latest story context in client IndexedDB; auto-resume single session on load; enforce retention cap with compaction digest.

**Independent Test**: Execute a run, reload, and verify restored latest story context + recent run history; exceed retention cap and verify compaction produces exactly 12 bullets.

- [ ] T019 [P] [US4] Add IndexedDB JS module in src/Narratoria.Web/wwwroot/indexedDb.js (stores: story_session, run_records)
- [ ] T020 [P] [US4] Add JS interop wrapper in src/Narratoria.Web/Services/IndexedDbInterop.cs
- [ ] T021 [US4] Implement StorySession store in src/Narratoria.Web/Persistence/IndexedDbStorySessionStore.cs
- [ ] T022 [US4] Implement RunRecord store with fixed-cap retention in src/Narratoria.Web/Persistence/IndexedDbRunRecordStore.cs
- [ ] T023 [US4] Implement retention policy config (default 200) in src/Narratoria.Web/Options/PipelineLogPersistenceOptions.cs
- [ ] T024 [US4] Implement compaction workflow (select old records, summarize, append digest, delete old) in src/Narratoria.Web/Persistence/RunHistoryCompactionService.cs
- [ ] T025 [US4] Implement deterministic fake compactor for tests in tests/Narratoria.Tests/PipelineLog/FakeHistoryCompactor.cs
- [ ] T026 [US4] Add unit tests for retention + compaction (exactly 12 bullets) in tests/Narratoria.Tests/PipelineLog/RunHistoryCompactionTests.cs
- [ ] T027 [US4] Wire persistence into app startup DI in src/Narratoria.Web/Program.cs
- [ ] T028 [US4] Restore on load (read IndexedDB and hydrate UI state) in src/Narratoria.Web/Pages/PipelineLogPage.razor
- [ ] T029 [P] [US4] Playwright E2E: persistence + restore on reload in tests/Narratoria.PlaywrightTests/ResumeSessionE2E.cs
- [ ] T030 [P] [US4] Playwright E2E: exceed cap triggers compaction and preserves 12 bullets in tests/Narratoria.PlaywrightTests/RetentionCompactionE2E.cs

**Checkpoint**: US4 complete — reload restores session; retention cap + 12-bullet digest enforced.

---

## Phase 4: User Story 1 - Realtime Pipeline Telemetry Log (Priority: P2)

**Goal**: UI shows streaming telemetry per run; Clear Log clears UI only.

**Independent Test**: Trigger a run and observe live entries; Clear Log clears UI but refresh restores from IndexedDB.

- [ ] T031 [P] [US1] Create pipeline log UI state model in src/Narratoria.Web/State/PipelineLogState.cs
- [ ] T032 [US1] Implement log rendering component in src/Narratoria.Web/Components/PipelineLog.razor
- [ ] T033 [US1] Implement telemetry-to-UI adapter in src/Narratoria.Web/Telemetry/PipelineLogTelemetryAdapter.cs
- [ ] T034 [US1] Implement Clear Log UI-only behavior in src/Narratoria.Web/Components/PipelineLog.razor
- [ ] T035 [P] [US1] Playwright E2E: run entry appears and streams updates in tests/Narratoria.PlaywrightTests/TelemetryLogE2E.cs
- [ ] T036 [P] [US1] Playwright E2E: Clear Log clears UI only; reload restores persisted history in tests/Narratoria.PlaywrightTests/ClearLogE2E.cs

**Checkpoint**: US1 complete — streaming log works; Clear Log UI-only.

---

## Phase 5: User Story 2 - Responsive Execution from Live Input (Priority: P3)

**Goal**: Idle debounce runs Pipeline A; Send runs Pipeline B; send suppresses idle and disables input; idle latest-wins cancellation.

**Independent Test**: Type/pause triggers idle run; Send triggers send run; input is disabled during send; earlier idle runs cancel.

- [ ] T037 [US2] Implement input area and Send control in src/Narratoria.Web/Components/PipelineLogInput.razor
- [ ] T038 [US2] Wire coordinator to input events (500ms debounce) in src/Narratoria.Web/Pages/PipelineLogPage.razor
- [ ] T039 [US2] Implement input-disable semantics during Send runs in src/Narratoria.Web/Components/PipelineLogInput.razor
- [ ] T040 [US2] Implement idle-run cancellation surfaced as cancelled in src/Narratoria.Web/Telemetry/PipelineLogTelemetryAdapter.cs
- [ ] T041 [P] [US2] Add unit tests for “send suppresses idle” in tests/Narratoria.Tests/PipelineLog/PipelineExecutionCoordinatorTests.cs
- [ ] T042 [P] [US2] Playwright E2E: idle debounce triggers run in tests/Narratoria.PlaywrightTests/IdleDebounceE2E.cs
- [ ] T043 [P] [US2] Playwright E2E: send disables input and suppresses idle in tests/Narratoria.PlaywrightTests/SendSuppressesIdleE2E.cs
- [ ] T044 [P] [US2] Playwright E2E: latest-wins cancels prior idle run in tests/Narratoria.PlaywrightTests/LatestWinsE2E.cs

**Checkpoint**: US2 complete — idle/send execution policies match spec.

---

## Phase 6: User Story 3 - User-Configurable LLM Sink (Priority: P4)

**Goal**: User can select provider/model/profile presets; selection is visible per run and affects pipeline A sink.

**Independent Test**: Change selection; next run reflects new selection in persisted run record + UI log.

- [ ] T045 [P] [US3] Define LLM sink abstraction in src/PipelineLog/Llm/ILlmTextSink.cs
- [ ] T046 [P] [US3] Implement deterministic local LLM sink (dev/test) in src/Narratoria.Web/Llm/FakeLlmTextSink.cs
- [ ] T047 [US3] Implement UI selector for provider/model/profile in src/Narratoria.Web/Components/LlmSelectionPicker.razor
- [ ] T048 [US3] Apply effective selection per run and persist it in src/PipelineLog/Telemetry/RunRecordBuilder.cs
- [ ] T049 [P] [US3] Unit test: selection change affects next run record in tests/Narratoria.Tests/PipelineLog/LlmSelectionTests.cs
- [ ] T050 [P] [US3] Playwright E2E: selection change reflected in next run in tests/Narratoria.PlaywrightTests/LlmSelectionE2E.cs

**Checkpoint**: US3 complete — selection changes are observable and persisted.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [ ] T051 [P] Add non-fatal error surfacing for IndexedDB failures in src/Narratoria.Web/Telemetry/PipelineLogTelemetryAdapter.cs
- [ ] T052 Add schema_version migration handling in src/Narratoria.Web/Services/IndexedDbInterop.cs
- [ ] T053 Validate quickstart steps end-to-end and update specs/003-pipeline-log-ui/quickstart.md
- [ ] T054 [P] Update TODO with any known drift/gaps discovered during implementation in TODO

---

## CRITICAL/HIGH Coverage Additions (from consistency review)

These tasks close the highest-impact gaps identified during the spec/plan/tasks consistency audit.

### Telemetry emission (pipeline runtime -> UI)

- [ ] T055 [P] Add a pipeline telemetry observer contract in src/Pipeline/Telemetry/IPipelineTelemetryObserver.cs (run start/end; per-stage start/end; outcome)
- [ ] T056 [P] Add an observable runner in src/Pipeline/Telemetry/ObservablePipelineRunner.cs that wraps PipelineRunner and emits telemetry (no behavior changes to runner semantics)
- [ ] T057 [P] Add stage naming rules for telemetry (transform type name + optional label) in src/Pipeline/Telemetry/PipelineStageNamer.cs
- [ ] T058 [P] Unit tests: telemetry event ordering + cancellation behavior in tests/Narratoria.Tests/Pipeline/ObservablePipelineRunnerTests.cs

### Pipeline A / Pipeline B definition wiring

- [ ] T059 [P] Implement Pipeline A/B builders in src/PipelineLog/Pipelines/PipelineDefinitionFactory.cs (Pipeline A = input snapshot + LLM sink; Pipeline B = spec 002 transform segment)
- [ ] T060 [P] Wire coordinator execution to PipelineDefinitionFactory in src/Narratoria.Web/Pages/PipelineLogPage.razor (Idle -> Pipeline A, Send -> Pipeline B)
- [ ] T061 [P] Unit tests: Pipeline A/B composition uses correct transforms/sinks and is type-compatible in tests/Narratoria.Tests/PipelineLog/PipelineDefinitionFactoryTests.cs

### FR-008 IME / composition handling

- [ ] T062 [P] Add composition-aware input handling in src/Narratoria.Web/Components/PipelineLogInput.razor (ignore unstable composition events; schedule idle on stable snapshot)
- [ ] T063 [P] Unit tests: coordinator does not schedule idle runs during composition in tests/Narratoria.Tests/PipelineLog/PipelineExecutionCoordinatorTests.cs
- [ ] T064 [P] Playwright E2E: composition events do not spam idle runs (simulate compositionstart/update/end) in tests/Narratoria.PlaywrightTests/ImeCompositionE2E.cs

### EH-003 structured logging + EH-004 send-error unlock

- [ ] T065 [P] Add structured logging (ILogger) for run failures with required fields (run_id, trigger, stage, session) in src/PipelineLog/PipelineExecutionCoordinator.cs
- [ ] T066 [P] Unit tests: failure logging includes required correlation fields in tests/Narratoria.Tests/PipelineLog/FailureLoggingTests.cs
- [ ] T067 [P] Ensure Send-run error path re-enables input and surfaces error state in src/Narratoria.Web/Pages/PipelineLogPage.razor
- [ ] T068 [P] Playwright E2E: Send error re-enables input (uses a deterministic failing pipeline configuration) in tests/Narratoria.PlaywrightTests/SendErrorUnlocksInputE2E.cs

---

## Dependencies & Execution Order

### User Story completion order

1. **US4 (P1)** Resume session + persistence (blocks the “resume at any time” promise)
2. **US1 (P2)** Telemetry log UI
3. **US2 (P3)** Responsive execution policy (idle/send)
4. **US3 (P4)** User-configurable LLM sink

### Parallel opportunities (examples)

- Setup: T002–T007 are mostly parallel once T001 is done.
- Foundational: Models/interfaces/telemetry (T008–T015) can be parallelized.
- E2E tests: Most Playwright tasks are parallel once pages/routes exist.

## Parallel Example: US4

- Workstream A (storage): T019–T023 in src/Narratoria.Web/
- Workstream B (compaction): T024–T026 in src/Narratoria.Web/ and tests/Narratoria.Tests/
- Workstream C (E2E): T029–T030 in tests/Narratoria.PlaywrightTests/

## Implementation Strategy

- MVP = **US4 only**: persistence + restore + retention/compaction digest.
- Then add US1/US2 to make the system observable and responsive.
- Add US3 last; keep a deterministic fake sink until a real provider is introduced.
