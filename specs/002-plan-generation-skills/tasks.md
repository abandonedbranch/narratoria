# Tasks: Plan Generation and Skill Discovery

**Input**: Design documents from `/specs/002-plan-generation-skills/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

## Phase 1: Setup (Shared Infrastructure)

- [ ] T001 Update dependencies (flutter_ai_toolkit, sqlite3/sqflite, json_schema, path_provider, uuid) in [src/pubspec.yaml](src/pubspec.yaml)
- [ ] T002 [P] Add gitignore entries for skill configs and data in [.gitignore](.gitignore) (root repo gitignore, not src/)
- [ ] T003 [P] Scaffold skills directory structure (storyteller, memory, reputation, dice-roller) under [skills/](skills/)
- [ ] T004 [P] Create developer samples directory for mock scripts in [test/contract/](test/contract/) for protocol tests

---

## Phase 2: Foundational (Blocking Prerequisites)

- [ ] T005 Define PlanJson extended schema model in [src/lib/models/plan_json.dart](src/lib/models/plan_json.dart)
- [ ] T006 [P] Define protocol event types (log, state_patch, asset, ui_event, error, done) in [src/lib/models/protocol_events.dart](src/lib/models/protocol_events.dart)
- [ ] T007 [P] Implement SessionState with deep merge algorithm in [src/lib/models/session_state.dart](src/lib/models/session_state.dart)
- [ ] T008 [P] Implement execution result and trace types in [src/lib/models/tool_execution_status.dart](src/lib/models/tool_execution_status.dart)
- [ ] T009 [P] Extend ToolInvoker to parse NDJSON events and enforce single done event in [src/lib/services/tool_invoker.dart](src/lib/services/tool_invoker.dart)
- [ ] T010 Implement SkillDiscovery service (scan skills/, parse manifests, collect prompts/scripts) in [src/lib/services/skill_discovery.dart](src/lib/services/skill_discovery.dart)
- [ ] T011 Implement SkillConfig loader (schema validation, env substitution) in [src/lib/services/skill_config.dart](src/lib/services/skill_config.dart)
- [ ] T012 Implement NarratorAI interface (LLM abstraction + prompt assembly hooks) in [src/lib/services/narrator_ai.dart](src/lib/services/narrator_ai.dart)
- [ ] T013 Implement PlanExecutor skeleton (dependency graph, in-degree calc, cycle detection) in [src/lib/services/plan_executor.dart](src/lib/services/plan_executor.dart)

**Checkpoint**: Foundation ready—user stories can proceed in parallel.

---

## Phase 3: User Story 1 - Basic Interactive Storytelling (Priority: P1) 🎯 MVP

**Goal**: Player enters an action; narrator AI generates Plan JSON; executor runs tools; narration returned with graceful fallbacks.
**Independent Test**: Launch app, type "I roll to pick the lock", observe plan with dice-roller, narration returned; if LLM unavailable, template narration appears.

- [ ] T014 [US1] Implement topological execution with retries/timeouts and execution trace in [src/lib/services/plan_executor.dart](src/lib/services/plan_executor.dart)
- [ ] T015 [P] [US1] Implement replan loop controller (3 plan executions, 5 generations) returning disabledSkills and canReplan in [src/lib/services/plan_executor.dart](src/lib/services/plan_executor.dart)
- [ ] T016 [P] [US1] Implement narrator plan generation (Flutter AI Toolkit + Ollama) with behavioral prompt injection in [src/lib/services/narrator_ai.dart](src/lib/services/narrator_ai.dart)
- [ ] T017 [P] [US1] Wire storytelling screen to narrator + executor pipeline in [src/lib/ui/screens/storytelling_screen.dart](src/lib/ui/screens/storytelling_screen.dart)
- [ ] T018 [P] [US1] Build storyteller skill (prompt.md, config-schema.json, skill.json, narrate.dart with fallbacks) in [skills/storyteller/](skills/storyteller/)
- [ ] T019 [US1] Add execution trace viewer widget for debugging in [src/lib/ui/widgets/execution_trace_viewer.dart](src/lib/ui/widgets/execution_trace_viewer.dart)

**Checkpoint**: P1 storytelling end-to-end works with template fallback.

---

## Phase 4: User Story 2 - Skill Configuration (Priority: P2)

**Goal**: Configure storyteller (and other skills) via UI forms generated from JSON Schema, persisting to config.json with validation.
**Independent Test**: Open Skills settings, edit storyteller provider/model/apiKey, save, reload app, plan generation uses new config.

- [ ] T020 [US2] Implement SkillsSettingsScreen listing discovered skills with enable/disable toggle in [src/lib/ui/screens/skills_settings_screen.dart](src/lib/ui/screens/skills_settings_screen.dart)
- [ ] T021 [P] [US2] Implement dynamic SkillConfigForm (string/number/boolean/enum/password) in [src/lib/ui/widgets/skill_config_form.dart](src/lib/ui/widgets/skill_config_form.dart)
- [ ] T022 [P] [US2] Persist validated configs to skills/<skill>/config.json with env substitution in [src/lib/services/skill_config.dart](src/lib/services/skill_config.dart)
- [ ] T023 [US2] Surface validation and error messages inline in [src/lib/ui/widgets/skill_config_form.dart](src/lib/ui/widgets/skill_config_form.dart)

**Checkpoint**: Skill configuration editable and persisted; storyteller can switch providers.

---

## Phase 5: User Story 3 - Skill Discovery and Installation (Priority: P2)

**Goal**: Drop-in skills discovered at startup; prompts loaded; scripts registered for plan generation and execution.
**Independent Test**: Add a new skill directory with valid skill.json and script; restart app; skill appears in settings and is selectable by narrator.

- [ ] T024 [US3] Validate skill.json against schema and skip invalid skills with warnings in [src/lib/services/skill_discovery.dart](src/lib/services/skill_discovery.dart)
- [ ] T025 [P] [US3] Load prompt.md and inject into narrator system context in [src/lib/services/narrator_ai.dart](src/lib/services/narrator_ai.dart)
- [ ] T026 [P] [US3] Register scripts with tool registry including executable checks in [src/lib/services/skill_discovery.dart](src/lib/services/skill_discovery.dart)
- [ ] T027 [US3] Expose discovered skills to SkillsSettingsScreen with metadata in [src/lib/ui/screens/skills_settings_screen.dart](src/lib/ui/screens/skills_settings_screen.dart)

**Checkpoint**: New skills discoverable and usable after restart.

---

## Phase 6: User Story 4 - Memory and Continuity (Priority: P3)

**Goal**: Store and recall past events via memory skill with fast vector search.
**Independent Test**: Play a session, store events, restart, ask "What happened last time?" and receive recalled context under 500ms for 1000 events.

- [ ] T028 [US4] Implement memory skill manifest, prompt, and config schema in [skills/memory/](skills/memory/)
- [ ] T029 [P] [US4] Implement store-memory.dart (embedding + SQLite insert) in [skills/memory/scripts/store-memory.dart](skills/memory/scripts/store-memory.dart)
- [ ] T030 [P] [US4] Implement recall-memory.dart (vector search + top-K return) in [skills/memory/scripts/recall-memory.dart](skills/memory/scripts/recall-memory.dart)
- [ ] T031 [US4] Add memory skill wiring to narrator prompt context and plan generation in [src/lib/services/narrator_ai.dart](src/lib/services/narrator_ai.dart)

**Checkpoint**: Memory recall integrated; performance goal met.

---

## Phase 7: User Story 5 - Reputation and Consequence Tracking (Priority: P3)

**Goal**: Track faction reputation and influence narration.
**Independent Test**: Perform action that adjusts faction score, then interact; narration reflects updated reputation; decay applied over time.

- [ ] T032 [US5] Implement reputation skill manifest and config schema in [skills/reputation/](skills/reputation/)
- [ ] T033 [P] [US5] Implement update-reputation.dart (delta + log) in [skills/reputation/scripts/update-reputation.dart](skills/reputation/scripts/update-reputation.dart)
- [ ] T034 [P] [US5] Implement query-reputation.dart with decay handling in [skills/reputation/scripts/query-reputation.dart](skills/reputation/scripts/query-reputation.dart)
- [ ] T035 [US5] Integrate reputation context into narrator prompts and plan selection in [src/lib/services/narrator_ai.dart](src/lib/services/narrator_ai.dart)

**Checkpoint**: Reputation affects narration tone and outcomes.

---

## Phase 8: Data Persistence and Resilience

- [ ] T036 [P] Implement skill data directory creation on first use in [src/lib/services/skill_discovery.dart](src/lib/services/skill_discovery.dart) (FR-062)
- [ ] T037 [P] Enforce skill data isolation: skills cannot access other skills' data/ directories, verified via unit test in [test/unit/](test/unit/)
- [ ] T038 [P] Verify skill data persistence: create test skill that writes to data/, restart app, confirm data still present in [test/integration/](test/integration/) (FR-059)
- [ ] T039 Implement fallback narration template in [src/lib/services/narrator_ai.dart](src/lib/services/narrator_ai.dart) for when plan generation fails (FR-066)
- [ ] T040 [P] Implement graceful continue-on-failure for non-required tools in plan executor in [src/lib/services/plan_executor.dart](src/lib/services/plan_executor.dart) (FR-067)
- [ ] T041 [P] Surface user-friendly warnings for misconfigured skills in [src/lib/ui/screens/skills_settings_screen.dart](src/lib/ui/screens/skills_settings_screen.dart) (FR-064)
- [ ] T042 Add integration test for API failure fallback (hosted provider -> local model) in [test/integration/](test/integration/) (FR-065)

---

## Phase 9: Performance & SLO Validation

- [ ] T043 [P] Benchmark plan generation latency (<5s for typical input) and add automated test gate in [test/integration/](test/integration/) (FR-006, SC-001)
- [ ] T044 [P] Benchmark per-tool timeout enforcement (30s default) and plan-level timeout (60s default) in executor in [test/integration/](test/integration/) (FR-017/FR-018)
- [ ] T045 Benchmark memory skill vector search (<500ms for 1000 events) and add perf test in [test/integration/](test/integration/) (SC-007)
- [ ] T046 Benchmark reputation skill queries (<100ms) and add perf test in [test/integration/](test/integration/) (SC-008)
- [ ] T047 Benchmark storyteller fallback latency (<10s when API unavailable) in [test/integration/](test/integration/) (SC-009)
- [ ] T048 [P] Add contract test for NDJSON protocol compliance (all events well-formed, exactly one done) in [test/contract/](test/contract/) (FR-048)

---

## Phase 10: Verification & Documentation

- [ ] T049 [P] Integration test: skill discovery loads all valid skills on startup without errors (SC-004)
- [ ] T056 [P] Integration test: drop-in skill install (add skill to skills/ and restart) is discoverable and usable (SC-010)
- [ ] T050 [P] Integration test: skill configuration persists across restart (SC-011)
- [ ] T051 [P] Guard narrator AI to disallow network calls via unit/integration test in [test/unit/](test/unit/) and [test/integration/](test/integration/) (FR-007, C4)
- [ ] T052 [P] Document skill packaging and install steps in [src/README.md](src/README.md)
- [ ] T053 [P] Optimize execution trace logging and viewer UX in [src/lib/ui/widgets/execution_trace_viewer.dart](src/lib/ui/widgets/execution_trace_viewer.dart)
- [ ] T054 [P] Validate quickstart steps end-to-end using dice-roller sample in [specs/002-plan-generation-skills/quickstart.md](specs/002-plan-generation-skills/quickstart.md)
- [ ] T055 Harden timeout/backoff defaults and configuration surfacing in [src/lib/services/plan_executor.dart](src/lib/services/plan_executor.dart)

---

## Dependencies & Execution Order

- Setup (Phase 1) → Foundational (Phase 2) → User Stories in priority order (US1 P1 → US2/US3 P2 → US4/US5 P3) → Resilience (Phase 8) → Performance (Phase 9) → Verification (Phase 10).
- US1 is a prerequisite for validating end-to-end narration; US2 and US3 can start after Foundational but assume narrator/executor scaffolding exists.
- US4 (memory) and US5 (reputation) depend on narrator prompt/context wiring from US1 and discovery/config from US2/US3.
- Phase 8 (resilience) depends on user stories; Phase 9 (perf) depends on Phase 8; Phase 10 (verification) depends on Phase 9.

## Parallel Execution Examples

- **Phase 2**: T006, T007, T008, T009 can run in parallel; T010–T013 follow once models are ready.
- **US1**: T015, T016, T018 can proceed in parallel; T014 depends on T013; T017 depends on T014/T016; T019 can follow T014.
- **US2**: T021 and T022 can run in parallel after T020 scaffolds the screen.
- **US3**: T024 and T026 in parallel; T025 after T024; T027 after T024/T026.
- **US4**: T029 and T030 in parallel after T028; T031 after T029/T030.
- **US5**: T033 and T034 in parallel after T032; T035 after T033/T034.
- **Phase 8**: T036–T038 (data) in parallel; T039–T042 (graceful degradation) in parallel after T014 completes.
- **Phase 9**: T043–T048 (perf benchmarks) can run in parallel after respective features complete (e.g., T043 after US1 narrator/executor, T045 after US4, T046 after US5).
- **Phase 10**: T049–T051 (verification) depend on corresponding features; T052–T055 (docs/polish) can run in parallel at end.

## Implementation Strategy

- MVP first: Complete Phases 1–4 (through US1), demo storytelling with fallback (approx. 2–3 weeks).
- Resilience: Add Phase 8 (data persistence + graceful degradation) to ensure robustness and Constitution IV.A compliance.
- Incremental: Add US2 and US3 (Phase 4–5) to unlock configurable and discoverable skills.
- Extend: Add US4/US5 (Phase 6–7) for continuity and consequence depth.
- Performance: Run Phase 9 benchmarks and perf validation throughout (can run in parallel with feature work).
- Polish: Run Phase 10 verification, documentation, and quickstart validation before release.
- Total estimated duration: 7–8 weeks for full implementation (can be compressed with 2 developers).
