# Tasks: Dart/Flutter Implementation

**Source Specs**: [001-tool-protocol-spec](../001-tool-protocol-spec/spec.md), [002-plan-generation-skills](../002-plan-generation-skills/spec.md)
**Prerequisites**: spec.md, data-model.md, quickstart.md, contracts/

**Organization**: Tasks are organized in two tracks:
- **Protocol Track (P-xxx)**: From Spec 001 - Tool execution, UI components, protocol compliance
- **Skills Track (S-xxx)**: From Spec 002 - Plan generation, skill discovery, memory, reputation

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Single Flutter project**: `lib/`, `test/` at repository root
- **Example tools**: `tools/` at repository root
- **Skills**: `skills/` at repository root

---

# PROTOCOL TRACK (from Spec 001)

## Phase P1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] P-001 Create Flutter project structure with Material Design 3
- [X] P-002 Initialize pubspec.yaml with flutter, flutter_test, integration_test dependencies
- [X] P-003 [P] Create directories: lib/models/, lib/services/, lib/ui/screens/, lib/ui/widgets/, test/contract/, test/integration/, test/unit/, tools/
- [X] P-004 [P] Configure Material Design 3 dark theme in lib/ui/theme.dart
- [X] P-005 [P] Create main.dart entry point with MaterialApp and theme

---

## Phase P2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] P-006 Create protocol event models in lib/models/protocol_events.dart (EventEnvelope, LogEvent, StatePatchEvent, AssetEvent, UiEvent, ErrorEvent, DoneEvent)
- [X] P-007 Create Plan JSON models in lib/models/plan_json.dart (PlanJson, ToolInvocation)
- [X] P-008 [P] Create Asset model in lib/models/asset.dart
- [X] P-009 [P] Create SessionState model in lib/models/session_state.dart with deepMerge() utility method (pure function: nested objects merged recursively, arrays replaced, null removes keys)
- [X] P-010 Contract test for event envelope schema in test/contract/protocol_events_test.dart
- [X] P-011 Contract test for Plan JSON schema validation in test/contract/plan_json_test.dart

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase P3: User Story 1 - Tool Execution Engine (Priority: P1) 🎯 MVP

**Goal**: Execute a hardcoded plan and display tool logs in real-time

**Independent Test**: Run torch-lighter tool directly, parse NDJSON events, display logs in Tool Execution Panel

### Implementation for User Story 1

- [X] P-012 [P] [US1] Implement ToolInvoker service in lib/services/tool_invoker.dart (Process.start, stdin write, stdout NDJSON parsing)
- [X] P-012a [US1] Add protocol error handling to ToolInvoker (unknown event types per spec §5.2, malformed JSON, missing done event, timeout handling)
- [X] P-013 [P] [US1] Implement PlanExecutor service in lib/services/plan_executor.dart (dependency resolution, sequential/parallel execution, failure handling)
- [X] P-014 [US1] Implement ToolExecutionStatus model in lib/models/tool_execution_status.dart (track running tools, events, exit codes)
- [X] P-015 [US1] Create Tool Execution Panel in lib/ui/widgets/tool_execution_panel.dart (display tool name, status, streaming logs)
- [X] P-016 [US1] Create basic MainScreen with NavigationRail in lib/ui/screens/main_screen.dart
- [X] P-017 [US1] Integrate Tool Execution Panel into MainScreen Tools view
- [X] P-018 [US1] Integration test: Execute hardcoded Plan JSON with torch-lighter in test/integration/plan_executor_test.dart (verify stderr output doesn't corrupt NDJSON parsing)

**Checkpoint**: At this point, can execute tools programmatically and see logs

---

## Phase P4: User Story 2 - Player Input & Plan Generation (Priority: P2)

**Goal**: Player types natural language prompt, narrator stub generates Plan JSON, tools execute

**Independent Test**: Type "light torch" in input field, see torch-lighter run and complete

### Implementation for User Story 2

- [X] P-019 [P] [US2] Implement NarratorAIStub service in lib/services/narrator_ai_stub.dart (hard-coded prompt→Plan JSON mappings)
- [X] P-020 [P] [US2] Create PlayerPrompt model in lib/models/player_prompt.dart
- [X] P-021 [US2] Create Player Input Field widget in lib/ui/widgets/player_input_field.dart (multiline text, send button)
- [X] P-022 [US2] Create Story View widget in lib/ui/widgets/story_view.dart (display narrative text)
- [X] P-023 [US2] Integrate Player Input Field and Story View into MainScreen Narrative view
- [X] P-024 [US2] Wire player input → narrator stub → plan executor flow in MainScreen
- [X] P-025 [US2] Add narrative text display before tool execution

**Checkpoint**: At this point, User Stories 1 AND 2 work - player can submit prompts and see tools execute

---

## Phase P5: User Story 3 - Asset Display & Management (Priority: P3)

**Goal**: Assets generated by tools are registered and displayed with graceful degradation

**Independent Test**: torch-lighter emits asset event, image appears in Asset Gallery

### Implementation for User Story 3

- [X] P-026 [P] [US3] Implement AssetRegistry service in lib/services/asset_registry.dart (register assets, validate paths)
- [ ] P-027 [US3] Create Asset Gallery widget in lib/ui/widgets/asset_gallery.dart (grid/list of assets)
- [ ] P-028 [P] [US3] Create AssetPreview widget in lib/ui/widgets/asset_preview.dart (image, audio, video, placeholder for unknown)
- [ ] P-029 [US3] Integrate Asset Gallery into MainScreen Assets view
- [ ] P-030 [US3] Wire asset events from PlanExecutor to AssetRegistry
- [ ] P-031 [US3] Display registered assets in Asset Gallery with graceful degradation

**Checkpoint**: Assets from tools now visible in UI

---

## Phase P6: User Story 4 - State Management & Display (Priority: P4)

**Goal**: Session state is maintained with deep merge and displayed in Narrative State Panel

**Independent Test**: torch-lighter emits state_patch, torch:{lit:true} appears in state tree

### Implementation for User Story 4

- [X] P-032 [US4] Implement StateManager service in lib/services/state_manager.dart (ChangeNotifier pattern; orchestrate state updates via SessionState.deepMerge(), notify listeners)
- [X] P-032a [US4] Initialize empty SessionState on app startup in main.dart (wire StateManager to MaterialApp root)
- [X] P-032b [US4] Add state clear/reset method to StateManager (for new sessions or testing)
- [ ] P-033 [P] [US4] Create Narrative State Panel widget in lib/ui/widgets/narrative_state_panel.dart (expandable tree view, JSON display)
- [X] P-034 [US4] Integrate Narrative State Panel into MainScreen State view
- [X] P-035 [US4] Wire state_patch events from PlanExecutor to StateManager
- [ ] P-036 [US4] Display state tree with highlight on changes
- [X] P-037 [US4] Unit test: Deep merge semantics in test/unit/session_state_test.dart (verify SessionState.deepMerge(): nested objects merged recursively, arrays replaced, null removes keys per spec §4.2)

**Checkpoint**: Session state updates are visible and correct

---

## Phase P7: User Story 5 - UI Events & Narrative Choices (Priority: P5)

**Goal**: narrative_choice ui_event displays choice buttons; player can select

**Independent Test**: door-examiner emits narrative_choice, "Open" and "Leave" buttons appear

### Implementation for User Story 5

- [ ] P-038 [US5] Implement UiEventHandler service in lib/services/ui_event_handler.dart (dispatch ui_events, handle narrative_choice)
- [ ] P-039 [P] [US5] Create NarrativeChoice widget in lib/ui/widgets/narrative_choice.dart (display choice buttons, emit selection)
- [ ] P-040 [US5] Wire ui_event events from PlanExecutor to UiEventHandler
- [ ] P-041 [US5] Display NarrativeChoice in Story View when narrative_choice event received
- [ ] P-042 [US5] Convert choice selection back to player prompt
- [ ] P-043 [US5] Implement graceful degradation for unknown ui_event types (placeholder message)

**Checkpoint**: All UI event types handled (narrative_choice + degradation)

---

## Phase P8: Example Tools (Validation)

**Purpose**: Create example tools to validate protocol implementation

- [X] P-044 [P] Create torch-lighter tool in tools/torch-lighter/main.dart (emit log, state_patch, asset, done events; depends on P-046 for torch_lit.png asset)
- [X] P-045 [P] Create door-examiner tool in tools/door-examiner/main.dart (emit log, state_patch, ui_event, done events)
- [ ] P-046 [P] Generate 512x512 PNG of lit torch in Material Design icon style at tools/torch-lighter/assets/torch_lit.png
- [X] P-047 Compile tools to executables: dart compile exe tools/*/main.dart
- [X] P-048 Update NarratorAIStub with mappings for "light torch" and "examine door" prompts

**Checkpoint**: Example tools validate full protocol flow

---

## Phase P9: Protocol Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] P-052 [P] Add error display UI for tool failures in Tool Execution Panel
- [ ] P-053 [P] Add loading indicators during tool execution
- [ ] P-054 Add stderr display in Tool Execution Panel (debug mode)
- [ ] P-055 [P] Documentation: Update README with quickstart instructions
- [ ] P-056 [P] Documentation: Add screenshots to spec.md
- [ ] P-057 Validate against quickstart.md steps
- [ ] P-058 [P] Code cleanup and linting (dart analyze)
- [ ] P-059 Run full integration test suite

---

# SKILLS TRACK (from Spec 002)

## Phase S1: Skills Setup

- [ ] S-001 Update dependencies (flutter_ai_toolkit, sqlite3/sqflite, json_schema, path_provider, uuid) in [pubspec.yaml](pubspec.yaml)
- [ ] S-002 [P] Add gitignore entries for skill configs and data in [.gitignore](.gitignore) (root repo gitignore, not src/)
- [ ] S-003 [P] Scaffold skills directory structure (storyteller, memory, reputation, dice-roller) under [skills/](skills/)
- [ ] S-004 [P] Create developer samples directory for mock scripts in [test/contract/](test/contract/) for protocol tests

---

## Phase S2: Skills Foundational (Blocking Prerequisites)

- [ ] S-005 Define PlanJson extended schema model in [lib/models/plan_json.dart](lib/models/plan_json.dart)
- [ ] S-006 [P] Define protocol event types (log, state_patch, asset, ui_event, error, done) in [lib/models/protocol_events.dart](lib/models/protocol_events.dart)
- [ ] S-007 [P] Implement SessionState with deep merge algorithm in [lib/models/session_state.dart](lib/models/session_state.dart)
- [ ] S-008 [P] Implement execution result and trace types in [lib/models/tool_execution_status.dart](lib/models/tool_execution_status.dart)
- [ ] S-009 [P] Extend ToolInvoker to parse NDJSON events and enforce single done event in [lib/services/tool_invoker.dart](lib/services/tool_invoker.dart)
- [ ] S-010 Implement SkillDiscovery service (scan skills/, parse manifests, collect prompts/scripts) in [lib/services/skill_discovery.dart](lib/services/skill_discovery.dart)
- [ ] S-011 Implement SkillConfig loader (schema validation, env substitution) in [lib/services/skill_config.dart](lib/services/skill_config.dart)
- [ ] S-012 Implement NarratorAI interface (LLM abstraction + prompt assembly hooks) in [lib/services/narrator_ai.dart](lib/services/narrator_ai.dart)
- [ ] S-013 Implement PlanExecutor skeleton (dependency graph, in-degree calc, cycle detection) in [lib/services/plan_executor.dart](lib/services/plan_executor.dart)

**Checkpoint**: Foundation ready—user stories can proceed in parallel.

---

## Phase S3: User Story 1 - Basic Interactive Storytelling (Priority: P1) 🎯 MVP

**Goal**: Player enters an action; narrator AI generates Plan JSON; executor runs tools; narration returned with graceful fallbacks.
**Independent Test**: Launch app, type "I roll to pick the lock", observe plan with dice-roller, narration returned; if LLM unavailable, template narration appears.

- [ ] S-014 [US1] Implement topological execution with retries/timeouts and execution trace in [lib/services/plan_executor.dart](lib/services/plan_executor.dart)
- [ ] S-015 [P] [US1] Implement replan loop controller (3 plan executions, 5 generations) returning disabledSkills and canReplan in [lib/services/plan_executor.dart](lib/services/plan_executor.dart)
- [ ] S-016 [P] [US1] Implement narrator plan generation (Flutter AI Toolkit + Ollama) with behavioral prompt injection in [lib/services/narrator_ai.dart](lib/services/narrator_ai.dart)
- [ ] S-017 [P] [US1] Wire storytelling screen to narrator + executor pipeline in [lib/ui/screens/storytelling_screen.dart](lib/ui/screens/storytelling_screen.dart)
- [ ] S-018 [P] [US1] Build storyteller skill (prompt.md, config-schema.json, skill.json, narrate.dart with fallbacks) in [skills/storyteller/](skills/storyteller/)
- [ ] S-019 [US1] Add execution trace viewer widget for debugging in [lib/ui/widgets/execution_trace_viewer.dart](lib/ui/widgets/execution_trace_viewer.dart)

**Checkpoint**: P1 storytelling end-to-end works with template fallback.

---

## Phase S4: User Story 2 - Skill Configuration (Priority: P2)

**Goal**: Configure storyteller (and other skills) via UI forms generated from JSON Schema, persisting to config.json with validation.
**Independent Test**: Open Skills settings, edit storyteller provider/model/apiKey, save, reload app, plan generation uses new config.

- [ ] S-020 [US2] Implement SkillsSettingsScreen listing discovered skills with enable/disable toggle in [lib/ui/screens/skills_settings_screen.dart](lib/ui/screens/skills_settings_screen.dart)
- [ ] S-021 [P] [US2] Implement dynamic SkillConfigForm (string/number/boolean/enum/password) in [lib/ui/widgets/skill_config_form.dart](lib/ui/widgets/skill_config_form.dart)
- [ ] S-022 [P] [US2] Persist validated configs to skills/<skill>/config.json with env substitution in [lib/services/skill_config.dart](lib/services/skill_config.dart)
- [ ] S-023 [US2] Surface validation and error messages inline in [lib/ui/widgets/skill_config_form.dart](lib/ui/widgets/skill_config_form.dart)

**Checkpoint**: Skill configuration editable and persisted; storyteller can switch providers.

---

## Phase S5: User Story 3 - Skill Discovery and Installation (Priority: P2)

**Goal**: Drop-in skills discovered at startup; prompts loaded; scripts registered for plan generation and execution.
**Independent Test**: Add a new skill directory with valid skill.json and script; restart app; skill appears in settings and is selectable by narrator.

- [ ] S-024 [US3] Validate skill.json against schema and skip invalid skills with warnings in [lib/services/skill_discovery.dart](lib/services/skill_discovery.dart)
- [ ] S-025 [P] [US3] Load prompt.md and inject into narrator system context in [lib/services/narrator_ai.dart](lib/services/narrator_ai.dart)
- [ ] S-026 [P] [US3] Register scripts with tool registry including executable checks in [lib/services/skill_discovery.dart](lib/services/skill_discovery.dart)
- [ ] S-027 [US3] Expose discovered skills to SkillsSettingsScreen with metadata in [lib/ui/screens/skills_settings_screen.dart](lib/ui/screens/skills_settings_screen.dart)

**Checkpoint**: New skills discoverable and usable after restart.

---

## Phase S6: User Story 4 - Memory and Continuity (Priority: P3)

**Goal**: Store and recall past events via memory skill with fast vector search.
**Independent Test**: Play a session, store events, restart, ask "What happened last time?" and receive recalled context under 500ms for 1000 events.

- [ ] S-028 [US4] Implement memory skill manifest, prompt, and config schema in [skills/memory/](skills/memory/)
- [ ] S-029 [P] [US4] Implement store-memory.dart (embedding + SQLite insert) in [skills/memory/scripts/store-memory.dart](skills/memory/scripts/store-memory.dart)
- [ ] S-030 [P] [US4] Implement recall-memory.dart (vector search + top-K return) in [skills/memory/scripts/recall-memory.dart](skills/memory/scripts/recall-memory.dart)
- [ ] S-031 [US4] Add memory skill wiring to narrator prompt context and plan generation in [lib/services/narrator_ai.dart](lib/services/narrator_ai.dart)

**Checkpoint**: Memory recall integrated; performance goal met.

---

## Phase S7: User Story 5 - Reputation and Consequence Tracking (Priority: P3)

**Goal**: Track faction reputation and influence narration.
**Independent Test**: Perform action that adjusts faction score, then interact; narration reflects updated reputation; decay applied over time.

- [ ] S-032 [US5] Implement reputation skill manifest and config schema in [skills/reputation/](skills/reputation/)
- [ ] S-033 [P] [US5] Implement update-reputation.dart (delta + log) in [skills/reputation/scripts/update-reputation.dart](skills/reputation/scripts/update-reputation.dart)
- [ ] S-034 [P] [US5] Implement query-reputation.dart with decay handling in [skills/reputation/scripts/query-reputation.dart](skills/reputation/scripts/query-reputation.dart)
- [ ] S-035 [US5] Integrate reputation context into narrator prompts and plan selection in [lib/services/narrator_ai.dart](lib/services/narrator_ai.dart)

**Checkpoint**: Reputation affects narration tone and outcomes.

---

## Phase S8: Data Persistence and Resilience

- [ ] S-036 [P] Implement skill data directory creation on first use in [lib/services/skill_discovery.dart](lib/services/skill_discovery.dart) (FR-062)
- [ ] S-037 [P] Enforce skill data isolation: skills cannot access other skills' data/ directories, verified via unit test in [test/unit/](test/unit/)
- [ ] S-038 [P] Verify skill data persistence: create test skill that writes to data/, restart app, confirm data still present in [test/integration/](test/integration/) (FR-059)
- [ ] S-039 Implement fallback narration template in [lib/services/narrator_ai.dart](lib/services/narrator_ai.dart) for when plan generation fails (FR-066)
- [ ] S-040 [P] Implement graceful continue-on-failure for non-required tools in plan executor in [lib/services/plan_executor.dart](lib/services/plan_executor.dart) (FR-067)
- [ ] S-041 [P] Surface user-friendly warnings for misconfigured skills in [lib/ui/screens/skills_settings_screen.dart](lib/ui/screens/skills_settings_screen.dart) (FR-064)
- [ ] S-042 Add integration test for API failure fallback (hosted provider -> local model) in [test/integration/](test/integration/) (FR-065)

---

## Phase S9: Performance & SLO Validation

- [ ] S-043 [P] Benchmark plan generation latency (<5s for typical input) and add automated test gate in [test/integration/](test/integration/) (FR-006, SC-001)
- [ ] S-044 [P] Benchmark per-tool timeout enforcement (30s default) and plan-level timeout (60s default) in executor in [test/integration/](test/integration/) (FR-017/FR-018)
- [ ] S-045 Benchmark memory skill vector search (<500ms for 1000 events) and add perf test in [test/integration/](test/integration/) (SC-007)
- [ ] S-046 Benchmark reputation skill queries (<100ms) and add perf test in [test/integration/](test/integration/) (SC-008)
- [ ] S-047 Benchmark storyteller fallback latency (<10s when API unavailable) in [test/integration/](test/integration/) (SC-009)
- [ ] S-048 [P] Add contract test for NDJSON protocol compliance (all events well-formed, exactly one done) in [test/contract/](test/contract/) (FR-048)

---

## Phase S10: Verification & Documentation

- [ ] S-049 [P] Integration test: skill discovery loads all valid skills on startup without errors (SC-004)
- [ ] S-050 [P] Integration test: drop-in skill install (add skill to skills/ and restart) is discoverable and usable (SC-010)
- [ ] S-051 [P] Integration test: skill configuration persists across restart (SC-011)
- [ ] S-052 [P] Guard narrator AI to disallow network calls via unit/integration test in [test/unit/](test/unit/) and [test/integration/](test/integration/) (FR-007, C4)
- [ ] S-053 [P] Document skill packaging and install steps in [README.md](README.md)
- [ ] S-054 [P] Optimize execution trace logging and viewer UX in [lib/ui/widgets/execution_trace_viewer.dart](lib/ui/widgets/execution_trace_viewer.dart)
- [ ] S-055 [P] Validate quickstart steps end-to-end using dice-roller sample in quickstart.md
- [ ] S-056 Harden timeout/backoff defaults and configuration surfacing in [lib/services/plan_executor.dart](lib/services/plan_executor.dart)

---

# Dependencies & Execution Order

## Track Dependencies

- **Protocol Track (P-xxx)** establishes foundational Flutter app and tool execution
- **Skills Track (S-xxx)** builds on Protocol Track for plan generation and skill ecosystem
- Complete Protocol Phases P1-P4 (MVP) before starting Skills Track

## Within Protocol Track

- Setup (Phase P1) → Foundational (Phase P2) → User Stories in priority order
- All user stories depend on Phase P2 completion
- User stories can proceed in parallel after Phase P2

## Within Skills Track

- Setup (Phase S1) → Foundational (Phase S2) → User Stories in priority order
- US1 (S3) is prerequisite for end-to-end narration validation
- US2/US3 (S4/S5) can start after Foundational
- US4/US5 (S6/S7) depend on narrator/discovery from US1-US3
- Phase S8 (resilience) depends on user stories
- Phase S9 (perf) depends on Phase S8
- Phase S10 (verification) depends on Phase S9

---

# Implementation Strategy

## MVP First (Protocol + Basic Skills)

1. Complete Protocol Phases P1-P4 (tool execution, player input)
2. Complete Protocol Phase P8 (example tools)
3. **STOP and VALIDATE**: Can player type "light torch" and see it execute?
4. Complete Skills Phases S1-S3 (skill discovery, basic storytelling)
5. **STOP and VALIDATE**: Does narrator AI generate plans from skills?

## Incremental Delivery

1. Protocol MVP → Test → Deploy/demo
2. Add Skills S1-S3 → Test → Enhanced narration
3. Add Skills S4-S5 → Test → Configurable skills
4. Add Skills S6-S7 → Test → Memory and reputation
5. Polish and release

---

# Task Summary

| Track | Phases | Tasks | Status |
|-------|--------|-------|--------|
| Protocol | P1-P9 | ~59 | MVP complete |
| Skills | S1-S10 | ~56 | Pending |
| **Total** | 19 | **~115** | |
