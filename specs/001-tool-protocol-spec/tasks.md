# Tasks: Tool Protocol Spec 001

**Input**: Design documents from `/specs/001-tool-protocol-spec/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Total Tasks**: 59 (updated after analysis to add protocol error handling, state lifecycle, and enhanced test coverage)

**Tests**: Tests are NOT explicitly requested in the specification, but contract/integration tests are implied by constitutional principle V (Testability). Including minimal test tasks for protocol validation.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Single Flutter project**: `lib/`, `test/` at repository root
- **Example tools**: `tools/` at repository root

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Create Flutter project structure with Material Design 3
- [X] T002 Initialize pubspec.yaml with flutter, flutter_test, integration_test dependencies
- [X] T003 [P] Create directories: lib/models/, lib/services/, lib/ui/screens/, lib/ui/widgets/, test/contract/, test/integration/, test/unit/, tools/
- [X] T004 [P] Configure Material Design 3 dark theme in lib/ui/theme.dart
- [X] T005 [P] Create main.dart entry point with MaterialApp and theme

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T006 Create protocol event models in lib/models/protocol_events.dart (EventEnvelope, LogEvent, StatePatchEvent, AssetEvent, UiEvent, ErrorEvent, DoneEvent)
- [X] T007 Create Plan JSON models in lib/models/plan_json.dart (PlanJson, ToolInvocation)
- [X] T008 [P] Create Asset model in lib/models/asset.dart
- [X] T009 [P] Create SessionState model in lib/models/session_state.dart with deepMerge() utility method (pure function: nested objects merged recursively, arrays replaced, null removes keys)
- [X] T010 Contract test for event envelope schema in test/contract/protocol_events_test.dart
- [X] T011 Contract test for Plan JSON schema validation in test/contract/plan_json_test.dart

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Tool Execution Engine (Priority: P1) 🎯 MVP

**Goal**: Execute a hardcoded plan and display tool logs in real-time

**Independent Test**: Run torch-lighter tool directly, parse NDJSON events, display logs in Tool Execution Panel

### Implementation for User Story 1

- [X] T012 [P] [US1] Implement ToolInvoker service in lib/services/tool_invoker.dart (Process.start, stdin write, stdout NDJSON parsing)
- [X] T012a [US1] Add protocol error handling to ToolInvoker (unknown event types per spec §5.2, malformed JSON, missing done event, timeout handling)
- [X] T013 [P] [US1] Implement PlanExecutor service in lib/services/plan_executor.dart (dependency resolution, sequential/parallel execution, failure handling)
- [X] T014 [US1] Implement ToolExecutionStatus model in lib/models/tool_execution_status.dart (track running tools, events, exit codes)
- [X] T015 [US1] Create Tool Execution Panel in lib/ui/widgets/tool_execution_panel.dart (display tool name, status, streaming logs)
- [X] T016 [US1] Create basic MainScreen with NavigationRail in lib/ui/screens/main_screen.dart
- [X] T017 [US1] Integrate Tool Execution Panel into MainScreen Tools view
- [X] T018 [US1] Integration test: Execute hardcoded Plan JSON with torch-lighter in test/integration/plan_executor_test.dart (verify stderr output doesn't corrupt NDJSON parsing)

**Checkpoint**: At this point, can execute tools programmatically and see logs

---

## Phase 4: User Story 2 - Player Input & Plan Generation (Priority: P2)

**Goal**: Player types natural language prompt, narrator stub generates Plan JSON, tools execute

**Independent Test**: Type "light torch" in input field, see torch-lighter run and complete

### Implementation for User Story 2

- [X] T019 [P] [US2] Implement NarratorAIStub service in lib/services/narrator_ai_stub.dart (hard-coded prompt→Plan JSON mappings)
- [X] T020 [P] [US2] Create PlayerPrompt model in lib/models/player_prompt.dart
- [X] T021 [US2] Create Player Input Field widget in lib/ui/widgets/player_input_field.dart (multiline text, send button)
- [X] T022 [US2] Create Story View widget in lib/ui/widgets/story_view.dart (display narrative text)
- [X] T023 [US2] Integrate Player Input Field and Story View into MainScreen Narrative view
- [X] T024 [US2] Wire player input → narrator stub → plan executor flow in MainScreen
- [X] T025 [US2] Add narrative text display before tool execution

**Checkpoint**: At this point, User Stories 1 AND 2 work - player can submit prompts and see tools execute

---

## Phase 5: User Story 3 - Asset Display & Management (Priority: P3)

**Goal**: Assets generated by tools are registered and displayed with graceful degradation

**Independent Test**: torch-lighter emits asset event, image appears in Asset Gallery

### Implementation for User Story 3

- [X] T026 [P] [US3] Implement AssetRegistry service in lib/services/asset_registry.dart (register assets, validate paths)
- [ ] T027 [US3] Create Asset Gallery widget in lib/ui/widgets/asset_gallery.dart (grid/list of assets)
- [ ] T028 [P] [US3] Create AssetPreview widget in lib/ui/widgets/asset_preview.dart (image, audio, video, placeholder for unknown)
- [ ] T029 [US3] Integrate Asset Gallery into MainScreen Assets view
- [ ] T030 [US3] Wire asset events from PlanExecutor to AssetRegistry
- [ ] T031 [US3] Display registered assets in Asset Gallery with graceful degradation

**Checkpoint**: Assets from tools now visible in UI

---

## Phase 6: User Story 4 - State Management & Display (Priority: P4)

**Goal**: Session state is maintained with deep merge and displayed in Narrative State Panel

**Independent Test**: torch-lighter emits state_patch, torch:{lit:true} appears in state tree

### Implementation for User Story 4

- [X] T032 [US4] Implement StateManager service in lib/services/state_manager.dart (ChangeNotifier pattern; orchestrate state updates via SessionState.deepMerge(), notify listeners)
- [X] T032a [US4] Initialize empty SessionState on app startup in main.dart (wire StateManager to MaterialApp root)
- [X] T032b [US4] Add state clear/reset method to StateManager (for new sessions or testing)
- [ ] T033 [P] [US4] Create Narrative State Panel widget in lib/ui/widgets/narrative_state_panel.dart (expandable tree view, JSON display)
- [X] T034 [US4] Integrate Narrative State Panel into MainScreen State view
- [X] T035 [US4] Wire state_patch events from PlanExecutor to StateManager
- [ ] T036 [US4] Display state tree with highlight on changes
- [X] T037 [US4] Unit test: Deep merge semantics in test/unit/session_state_test.dart (verify SessionState.deepMerge(): nested objects merged recursively, arrays replaced, null removes keys per spec §4.2)

**Checkpoint**: Session state updates are visible and correct

---

## Phase 7: User Story 5 - UI Events & Narrative Choices (Priority: P5)

**Goal**: narrative_choice ui_event displays choice buttons; player can select

**Independent Test**: door-examiner emits narrative_choice, "Open" and "Leave" buttons appear

### Implementation for User Story 5

- [ ] T038 [US5] Implement UiEventHandler service in lib/services/ui_event_handler.dart (dispatch ui_events, handle narrative_choice)
- [ ] T039 [P] [US5] Create NarrativeChoice widget in lib/ui/widgets/narrative_choice.dart (display choice buttons, emit selection)
- [ ] T040 [US5] Wire ui_event events from PlanExecutor to UiEventHandler
- [ ] T041 [US5] Display NarrativeChoice in Story View when narrative_choice event received
- [ ] T042 [US5] Convert choice selection back to player prompt
- [ ] T043 [US5] Implement graceful degradation for unknown ui_event types (placeholder message)

**Checkpoint**: All UI event types handled (narrative_choice + degradation)

---

## Phase 8: Example Tools (Validation)

**Purpose**: Create example tools to validate protocol implementation

- [X] T044 [P] Create torch-lighter tool in tools/torch-lighter/main.dart (emit log, state_patch, asset, done events; depends on T046 for torch_lit.png asset)
- [X] T045 [P] Create door-examiner tool in tools/door-examiner/main.dart (emit log, state_patch, ui_event, done events)
- [ ] T046 [P] Generate 512x512 PNG of lit torch in Material Design icon style at tools/torch-lighter/assets/torch_lit.png
- [X] T047 Compile tools to executables: dart compile exe tools/*/main.dart
- [X] T048 Update NarratorAIStub with mappings for "light torch" and "examine door" prompts

**Checkpoint**: Example tools validate full protocol flow

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T052 [P] Add error display UI for tool failures in Tool Execution Panel
- [ ] T053 [P] Add loading indicators during tool execution
- [ ] T054 Add stderr display in Tool Execution Panel (debug mode)
- [ ] T055 [P] Documentation: Update README with quickstart instructions
- [ ] T056 [P] Documentation: Add screenshots to spec.md
- [ ] T057 Validate against quickstart.md steps
- [ ] T058 [P] Code cleanup and linting (dart analyze)
- [ ] T059 Run full integration test suite

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-7)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3 → P4 → P5)
- **Example Tools (Phase 8)**: Can start after US1 completes (ToolInvoker ready)
- **Polish (Phase 9)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - Integrates with US1 but independently testable
- **User Story 3 (P3)**: Can start after Foundational (Phase 2) - Integrates with US1 but independently testable
- **User Story 4 (P4)**: Can start after Foundational (Phase 2) - Integrates with US1 but independently testable
- **User Story 5 (P5)**: Can start after Foundational (Phase 2) - Integrates with US1/US2 but independently testable

### Within Each User Story

- Models before services
- Services before UI widgets
- UI widgets before integration
- Core implementation before integration tests

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational models marked [P] can run in parallel (within Phase 2)
- Contract tests can run in parallel
- Once Foundational phase completes, all user stories can start in parallel (if team capacity allows)
- Example tools creation can happen in parallel
- Polish tasks can mostly run in parallel

---

## Parallel Example: User Story 1

```bash
# After Foundational phase, launch US1 tasks in parallel:
Task T012: "Implement ToolInvoker service in lib/services/tool_invoker.dart"
Task T013: "Implement PlanExecutor service in lib/services/plan_executor.dart"

# Then sequential:
Task T014: "Implement ToolExecutionStatus model" (needs ToolInvoker)
Task T015: "Create Tool Execution Panel widget"
Task T016: "Create MainScreen with NavigationRail"
Task T017: "Integrate Tool Execution Panel"
Task T018: "Integration test"
```

---

## Implementation Strategy

### MVP First (User Story 1 + 2 Only)

1. Complete Phase 1: Setup (5 tasks)
2. Complete Phase 2: Foundational (6 tasks - CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (8 tasks - includes protocol error handling)
4. Complete Phase 4: User Story 2 (7 tasks)
5. Complete Phase 8: Example Tools (5 tasks)
6. **STOP and VALIDATE**: Can player type "light torch" and see it execute?
7. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Can execute hardcoded plan
3. Add User Story 2 → Test independently → Player prompts work (MVP!)
4. Add User Story 3 → Test independently → Assets display
5. Add User Story 4 → Test independently → State visible
6. Add User Story 5 → Test independently → Narrative choices work
7. Polish and release

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (Tool Execution)
   - Developer B: User Story 2 (Player Input) - starts after US1 T012-T013
   - Developer C: User Story 3 (Assets)
   - Developer D: Example Tools
3. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files, no dependencies
- [US#] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence
- Contract tests validate protocol compliance, not tool behavior
- Integration tests validate end-to-end flows with real tool processes
