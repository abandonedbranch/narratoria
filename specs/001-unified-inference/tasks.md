---

description: "Task list for UnifiedInference implementation"
---

# Tasks: UnifiedInference Client

**Input**: Design documents from `/specs/001-unified-inference/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: Only if requested; this feature is a library (no UI), so no Playwright requirements. Deterministic unit/integration tests recommended but optional per spec.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- [P]: Can run in parallel (different files, no dependencies)
- [Story]: Which user story this task belongs to (US1, US2, US3, US4)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure in `src/lib` with `inference.dll` output.

- [ ] T001 Create source structure in src/lib (Abstractions, Core, Providers/{OpenAI,Ollama,HuggingFace}, Factory)
- [ ] T002 Initialize class library in src/lib via `dotnet new classlib -n UnifiedInference`
- [ ] T003 Set assembly name to inference in src/lib/UnifiedInference.csproj
- [ ] T004 Set target framework to net10.0 (or net8.0 fallback) in src/lib/UnifiedInference.csproj
- [ ] T005 [P] Add OpenAI official SDK dependency in src/lib/UnifiedInference.csproj
- [ ] T006 [P] Add EditorConfig/nullable/implicit usings settings in src/lib/UnifiedInference.csproj

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core abstractions and contracts that all user stories depend on.

- [ ] T007 Define `InferenceProvider` enum in src/lib/Abstractions/InferenceProvider.cs
- [ ] T008 Define `GenerationSettings` record with ProviderOverrides in src/lib/Abstractions/GenerationSettings.cs
- [ ] T009 Define requests/responses: Text/Image/Audio/Video/Music in src/lib/Abstractions/RequestsResponses.cs
- [ ] T010 Define `IModelCapabilities` in src/lib/Abstractions/IModelCapabilities.cs
- [ ] T011 Define `IUnifiedInferenceClient` interface in src/lib/Abstractions/IUnifiedInferenceClient.cs
- [ ] T012 Implement common mapping helpers (settings → provider params) in src/lib/Core/SettingsMapper.cs
- [ ] T013 Implement capability model + defaults (conservative) in src/lib/Core/ModelCapabilities.cs
- [ ] T014 Implement error helpers and NotSupported rules in src/lib/Core/Errors.cs

**Checkpoint**: Foundation ready - user stories can start.

---

## Phase 3: User Story 1 - Unified Text Generation (Priority: P1) 🎯 MVP

**Goal**: Generate text (chat/completion) across providers via unified API.
**Independent Test**: Issue text requests to at least two providers using identical call shape; verify handling of unsupported settings.

### Implementation

- [ ] T015 [P] [US1] Implement OpenAI text flow in src/lib/Providers/OpenAI/OpenAiInferenceClient.Text.cs
- [ ] T016 [P] [US1] Implement Ollama text flow (HTTP) in src/lib/Providers/Ollama/OllamaInferenceClient.Text.cs
- [ ] T017 [P] [US1] Implement Hugging Face text flow (HTTP) in src/lib/Providers/HuggingFace/HuggingFaceInferenceClient.Text.cs
- [ ] T018 [US1] Implement unified routing in src/lib/Core/UnifiedInferenceClient.Text.cs
- [ ] T019 [US1] Map GenerationSettings → provider params (text) in src/lib/Core/SettingsMapper.Text.cs
- [ ] T020 [US1] Wire DI constructors (OpenAI client, HttpClient, transports) in src/lib/Factory/InferenceClientFactory.cs

**Checkpoint**: Text generation works across providers with unified calls.

---

## Phase 4: User Story 2 - Capability Discovery and Fallback (Priority: P1)

**Goal**: Query per-model capabilities and avoid unsupported calls.
**Independent Test**: Query capabilities across providers/models; ensure modalities/settings reflect support and unknowns are disabled.

### Implementation

- [ ] T021 [P] [US2] Implement OpenAI capability probe in src/lib/Providers/OpenAI/OpenAiCapabilities.cs
- [ ] T022 [P] [US2] Implement Ollama capability probe in src/lib/Providers/Ollama/OllamaCapabilities.cs
- [ ] T023 [P] [US2] Implement Hugging Face capability probe in src/lib/Providers/HuggingFace/HuggingFaceCapabilities.cs
- [ ] T024 [US2] Implement unified `GetCapabilitiesAsync` in src/lib/Core/UnifiedInferenceClient.Capabilities.cs
- [ ] T025 [US2] Document capability rules and defaults in specs/001-unified-inference/contracts/capabilities.md

**Checkpoint**: Callers can gate behavior using capabilities and avoid exceptions.

---

## Phase 5: User Story 3 - Images and Audio (Priority: P2)

**Goal**: Prompt-to-image and audio TTS/STT via unified calls.
**Independent Test**: Execute image + one audio flow with supported providers.

### Implementation

- [ ] T026 [P] [US3] Implement image generation (OpenAI where applicable) in src/lib/Providers/OpenAI/OpenAiInferenceClient.Image.cs
- [ ] T027 [P] [US3] Implement image generation (Hugging Face) in src/lib/Providers/HuggingFace/HuggingFaceInferenceClient.Image.cs
- [ ] T028 [P] [US3] Implement image generation (Ollama if supported) in src/lib/Providers/Ollama/OllamaInferenceClient.Image.cs
- [ ] T029 [US3] Implement unified image routing in src/lib/Core/UnifiedInferenceClient.Image.cs
- [ ] T030 [P] [US3] Implement STT (OpenAI Whisper/HF model) in src/lib/Providers/*/*InferenceClient.AudioStt.cs
- [ ] T031 [P] [US3] Implement TTS (provider-supported) in src/lib/Providers/*/*InferenceClient.AudioTts.cs
- [ ] T032 [US3] Implement unified audio routing in src/lib/Core/UnifiedInferenceClient.Audio.cs
- [ ] T033 [US3] Map settings → provider params (image/audio) in src/lib/Core/SettingsMapper.Media.cs

**Checkpoint**: Image + audio functionality operational with capability-gated behavior.

---

## Phase 6: User Story 4 - Video and Optional Music (Priority: P3)

**Goal**: Provide hooks for video (best-effort) and music (hooks-only) in unified API.
**Independent Test**: If supported, video returns media/URI; music hooks throw `NotSupportedException`.

### Implementation

- [ ] T034 [P] [US4] Implement video (supported providers only) in src/lib/Providers/*/*InferenceClient.Video.cs
- [ ] T035 [US4] Implement unified video routing in src/lib/Core/UnifiedInferenceClient.Video.cs
- [ ] T036 [US4] Add music hooks in src/lib/Core/UnifiedInferenceClient.Music.cs (throw NotSupportedException)
- [ ] T037 [US4] Capability flags for video/music (default disabled) in src/lib/Core/ModelCapabilities.cs

**Checkpoint**: Video best-effort; music hooks present and safe.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Hardening, docs, and packaging.

- [ ] T038 [P] Update mapping documentation in specs/001-unified-inference/contracts/mapping.md
- [ ] T039 Validate quickstart steps and build output `inference.dll`
- [ ] T040 Performance and memory pass for large media payloads
- [ ] T041 Security review for HTTP calls (timeouts, retries, headers)
- [ ] T042 [P] Add minimal unit tests for core mapping/capabilities in tests/UnifiedInference.Tests/Unit/
- [ ] T043 Final README update under specs/001-unified-inference/quickstart.md (examples per provider)

---

## Dependencies & Execution Order

### Phase Dependencies

- Setup (Phase 1): No dependencies
- Foundational (Phase 2): Depends on Setup completion – BLOCKS all user stories
- US1 (Phase 3): Depends on Phase 2
- US2 (Phase 4): Depends on Phase 2 (can proceed in parallel with US1)
- US3 (Phase 5): Depends on Phase 2 (ideally after US1 for reuse)
- US4 (Phase 6): Depends on Phase 2 (can be parallel; best-effort)
- Polish (Phase 7): Depends on desired user stories completion

### User Story Dependencies

- US1 (Text): None beyond foundation; forms MVP
- US2 (Capabilities): Independent of US1, but complementary
- US3 (Image/Audio): May reuse US1 mapping patterns but independently testable
- US4 (Video/Music): Independent hooks with capability gating

### Parallel Opportunities

- T005/T006; provider-specific client implementations (T015–T017, T021–T023, T026–T028, T030–T031, T034) can run concurrently.
- Capability probes per provider can run in parallel with mapping work.
- Documentation and mapping docs can run concurrently with non-conflicting code tasks.

## Implementation Strategy

- MVP: Complete Phases 1–3 (Setup, Foundation, US1). Stop and validate.
- Incremental: Add US2 next to reduce runtime errors; then US3 for media; US4 for future-proof hooks.
