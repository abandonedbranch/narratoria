# Tasks: UnifiedInference HF-Only

**Input**: Design documents from `/specs/001-unified-inference/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: Required per spec/constitution (deterministic unit/integration; no Playwright).

## Phase 1: Setup (Shared Infrastructure)

- [x] T001 Pin tryAGI/HuggingFace v0.4.0 in src/lib/UnifiedInference/UnifiedInference.csproj
- [x] T002 [P] Capture HF token/config guidance in README or quickstart note if needed (no code change)

---

## Phase 2: Foundational (Blocking Prerequisites)

- [x] T003 Collapse provider enum/abstractions to HuggingFace-only in src/lib/UnifiedInference/Abstractions/RequestsResponses.cs and related types
- [x] T004 Update GenerationSettings model for HF fields (max_new_tokens, do_sample, repetition_penalty, return_full_text, diffusion params) in src/lib/UnifiedInference/Abstractions/GenerationSettings.cs
- [x] T005 Align capability defaults to HF-only in src/lib/UnifiedInference/Core/ModelCapabilitiesDefaults.cs and src/lib/UnifiedInference/Providers/HuggingFace/HuggingFaceCapabilities.cs
- [x] T006 [P] Update quickstart and contracts to reflect HF-only scope in specs/001-unified-inference/quickstart.md and specs/001-unified-inference/contracts/
- [x] T007 [P] Add/adjust DI factory to accept tryAGI/HuggingFace client only in src/lib/UnifiedInference/Factory/InferenceClientFactory.cs

**Checkpoint**: Foundation ready—user story work can begin.

---

## Phase 3: User Story 1 - Unified Text Generation (Priority: P1) 🎯 MVP

**Goal**: Text generation against HF models with unified settings and resilience.
**Independent Test**: Invoke text generation on two HF models with differing support; verify mapping, retries for cold model (503), and NotSupported for unsupported settings.

### Tests
- [x] T008 [P] [US1] Unit test settings mapping for text options (temperature/top_p/top_k/max_new_tokens/do_sample/repetition_penalty/return_full_text/stop) in tests/UnifiedInference.Tests/MappingTests.cs
- [x] T009 [P] [US1] Unit test text response parsing and retry/backoff on simulated 503 in tests/UnifiedInference.Tests/TextRoutingTests.cs

### Implementation
- [x] T010 [P] [US1] Expand HF text settings mapper with do_sample/repetition_penalty/return_full_text/stop/seed in src/lib/UnifiedInference/Core/SettingsMapper.Text.cs
- [x] T011 [US1] Add retry/backoff and wait_for_model/use_cache headers for text calls in src/lib/UnifiedInference/Providers/HuggingFace/HuggingFaceInferenceClient.Text.cs
- [x] T012 [US1] Improve text parsing (multiple candidates, error payloads) and surface HF error context in TextResponse metadata in src/lib/UnifiedInference/Providers/HuggingFace/HuggingFaceInferenceClient.Text.cs

**Checkpoint**: Text generation usable and testable.

---

## Phase 4: User Story 2 - Capability Discovery & Fallback (Priority: P1)

**Goal**: Capability discovery per HF model using HF metadata; gate unsupported/gated/cold models.
**Independent Test**: Query capabilities for multiple HF models (text vs diffusion vs gated); verify gating, pipeline_tag mapping, and disabled unsupported settings.

### Tests
- [x] T013 [P] [US2] Unit test capability mapping from HF model metadata (pipeline_tag/gated/inference status) in tests/UnifiedInference.Tests/CapabilitiesTests.cs
- [x] T014 [US2] Integration-ish test ensuring generation blocks when capabilities disable modality in tests/UnifiedInference.Tests/VideoMusicGatingTests.cs

### Implementation
- [x] T015 [P] [US2] Implement HF model metadata fetch/cache with pipeline_tag/gated/inference_status in src/lib/UnifiedInference/Providers/HuggingFace/HuggingFaceCapabilities.cs
- [x] T016 [US2] Wire capability checks into client entrypoints to throw NotSupported when modality/setting unsupported in src/lib/UnifiedInference/Providers/HuggingFace/UnifiedInferenceClient.Text.cs and related client surfaces
- [x] T017 [US2] Document fallback strategy (choose alternative model) in specs/001-unified-inference/contracts/capabilities.md

**Checkpoint**: Capability discovery prevents unsupported calls.

---

## Phase 5: User Story 3 - Images and Audio (Priority: P2)

**Goal**: Image generation with diffusion options; audio TTS/STT best-effort with clear gating.
**Independent Test**: Generate an image with guidance_scale/steps/size; verify negative prompt and error handling; confirm audio either succeeds when supported or throws NotSupported.

### Tests
- [x] T018 [P] [US3] Unit test image settings mapping (guidance_scale/num_inference_steps/height/width/negative_prompt) in tests/UnifiedInference.Tests/MappingTests.cs
- [x] T019 [US3] Unit test image response handling and error payload parsing in tests/UnifiedInference.Tests/CapabilitiesTests.cs
- [x] T020 [P] [US3] Unit test audio gating (TTS/STT unsupported by default) in tests/UnifiedInference.Tests/VideoMusicGatingTests.cs

### Implementation
- [x] T021 [P] [US3] Implement HF image options mapping in src/lib/UnifiedInference/Core/SettingsMapper.Media.cs
- [x] T022 [US3] Update image client to send diffusion parameters and handle HF error payloads in src/lib/UnifiedInference/Providers/HuggingFace/HuggingFaceInferenceClient.Image.cs
- [x] T023 [US3] Implement audio stubs via tryAGI/HuggingFace if available; otherwise enforce NotSupported with clear messaging in src/lib/UnifiedInference/Providers/HuggingFace

**Checkpoint**: Image generation and audio gating validated.

---

## Phase 6: User Story 4 - Video and Music Hooks (Priority: P3)

**Goal**: Provide video hooks where supported and music hooks that default to NotSupported.
**Independent Test**: Capability check disables video/music by default; calling video/music throws NotSupported or succeeds if capability enabled.

### Tests
- [x] T024 [P] [US4] Unit test video/music gating behavior in tests/UnifiedInference.Tests/VideoMusicGatingTests.cs

### Implementation
- [x] T025 [US4] Add video hook methods with capability gate in src/lib/UnifiedInference/Providers/HuggingFace
- [x] T026 [US4] Add music hook methods that throw NotSupported with clear capability metadata in src/lib/UnifiedInference/Providers/HuggingFace

**Checkpoint**: Video/music hooks are safely gated.

---

## Phase 7: Polish & Cross-Cutting

- [x] T027 [P] Refresh docs to match final HF-only behavior in specs/001-unified-inference/quickstart.md and README
- [x] T028 Add error-handling/cancellation notes and metadata examples to XML docs in src/lib/UnifiedInference
- [x] T029 [P] Run full test suite `dotnet test tests/UnifiedInference.Tests/UnifiedInference.Tests.csproj -c Debug`

---

## Additional Cross-Cutting (Required)

- [x] T030 [P] Enforce `CancellationToken` honoring across all public methods (text/image/audio/video/music) and add tests in tests/UnifiedInference.Tests/CancellationTests.cs
- [x] T031 Expose and document advanced access to underlying tryAGI/HuggingFace client/HttpClient (FR-008) with tests in tests/UnifiedInference.Tests/FactoryTests.cs
- [x] T032 Validate `ProviderOverrides` (type/required keys) and ensure consistent HF error payload surfacing across modalities; add tests in tests/UnifiedInference.Tests/ErrorHandlingTests.cs
- [x] T033 Document and sanity-check performance goals (text p50 < 2s warm, image p50 < 15s warm) with a lightweight perf sanity harness or guidance in specs/001-unified-inference/quickstart.md

---

## Dependencies & Execution Order

- Setup → Foundational → User Stories in priority order (US1 P1, US2 P1, US3 P2, US4 P3) → Polish.
- US1 and US2 both depend on Foundational; US3 depends on US1/US2 mappings; US4 depends on capability plumbing from US2.
- Parallelizable tasks marked [P] (different files, no direct dependencies).

## Parallel Execution Examples

- US1: Run mapping test update (T008) in parallel with text mapper changes (T010) and retry logic (T011) once foundational is done.
- US2: Capability metadata fetch (T015) can proceed in parallel with capability tests (T013) after foundational types are stable.
- US3: Image mapping (T021) and audio gating test (T020) can proceed in parallel; image client update (T022) follows mapping.

## Implementation Strategy

- MVP = Complete Setup + Foundational + US1 (text generation) then validate.
- Incremental = Add US2 (capabilities), then US3 (images/audio), then US4 (video/music hooks), with tests per story.
