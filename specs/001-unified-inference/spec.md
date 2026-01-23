# Feature Specification: UnifiedInference Client (Hugging Face Only)

**Feature Branch**: `[001-unified-inference]`  
**Created**: January 19, 2026  
**Status**: Draft  
**Input**: HF-only .NET 10 class library using tryAGI/HuggingFace v0.4.0 over the Hugging Face Inference API for text, image, best-effort audio/video hooks, and music hooks, with common generation settings, provider overrides, DI-friendly design, CancellationToken support, capability discovery, and conservative defaults.


## Scope *(mandatory)*

### In Scope

- Hugging Face–only API surfaces for modalities: text (chat/completion), image (prompt-to-image/diffusion), audio (TTS/STT where supported), video (best-effort where supported), and music (hooks-only, default unsupported).
- Caller supplies: model id/name (string), common generation settings (e.g., temperature, top_p, top_k, max_new_tokens, diffusion params), and HF-specific overrides (arbitrary map including cache/warmth toggles).
- Capability discovery per model via Hugging Face model metadata (pipeline_tag, gating, inference status) to prevent unsupported calls and guide clients.
- Clear error semantics including `NotSupportedException` for unsupported modalities/settings and network/transport exceptions for HF API failures.
- Dependency-injection-friendly construction (accept preconfigured tryAGI/HuggingFace v0.4.0 client and/or HttpClient) and ubiquitous `CancellationToken` usage.
- Stable, minimal unified interfaces focused on HF Inference API parity without multi-provider abstractions.

### Out of Scope

- Training/fine-tuning workflows, embeddings, or advanced HF APIs beyond listed modalities.
- UI implementation, CLI tools, or orchestration pipelines beyond the client.
- Data persistence or telemetry storage beyond what is necessary for immediate request execution.
- Non-.NET platforms or language bindings; this specification focuses on .NET consumers.
- Account/key management UX.

### Assumptions

- Callers will provide valid HF tokens and network connectivity to HF endpoints.
- Model identifiers supplied by callers are valid HF models and accessible (gated models require allow-list tokens).
- Capability discovery is evaluated per model id; when unknown, capabilities default to disabled to avoid surprises.
- DI containers are available in consuming applications but the client can be used without a container as well.
- HF pipelines differ in supported settings; unmapped settings are ignored or cause `NotSupportedException` depending on modality/impact.

### Open Questions *(mandatory)*

- All clarifications resolved: HF-only scope; video is best-effort; music is hooks-only with NotSupportedException when invoked; capability discovery is per-model with unknowns treated as disabled by default; tryAGI/HuggingFace pinned to v0.4.0.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Unified Text Generation (Priority: P1)

As an application developer, I want to generate text (chat/completion) via a single HF API by selecting an HF model, so I can swap HF models without refactoring my code.

**Why this priority**: Text is the most common modality and the foundation for many app experiences.

**Independent Test**: Provide HF `model id` and `GenerationSettings` to produce text across at least two HF models; verify identical calling pattern and deterministic handling of unsupported settings.

**Acceptance Scenarios**:

1. Given valid HF credentials and a supported text model, When calling text generation, Then a `TextResponse` is returned with content and metadata.
2. Given an unsupported setting for a chosen HF model, When calling text generation, Then either the setting is ignored (with capability disclosure) or a `NotSupportedException` is thrown per rules.

---

### User Story 2 - Capability Discovery and Fallback (Priority: P1)

As a developer, I want to query HF model capabilities before requests, so I can avoid unsupported calls and implement fallbacks.

**Why this priority**: Prevents runtime failures and improves developer UX.

**Independent Test**: Query capabilities for multiple HF models, validate modality and setting support, and conditionally select an alternative HF model when unsupported or gated.

**Acceptance Scenarios**:

1. Given a model id, When querying capabilities, Then modalities and supported/ignored settings are returned using HF metadata.
2. Given a requested modality not supported, When checking capabilities, Then client selects a supported HF model or gracefully disables the feature.

---

### User Story 3 - Images and Audio (Priority: P2)

As a developer, I want prompt-to-image and audio TTS/STT via unified calls, so I can integrate visual and audio features consistently across HF models.

**Why this priority**: Images and audio cover common, high-value generation tasks beyond text.

**Independent Test**: Invoke image and audio methods for at least one HF model each (where supported); verify successful generation and proper fallbacks for unsupported settings.

**Acceptance Scenarios**:

1. Given an HF model that supports images, When calling image generation with a prompt, Then `ImageResponse` returns binary/media data or a URI.
2. Given TTS or STT support on an HF model, When calling audio generation/recognition, Then `AudioResponse` returns audio/recognized text per modality.

---

### User Story 4 - Video and Optional Music (Priority: P3)

As a developer, I want a unified surface for video generation (where supported) and optional music hooks, so my codebase remains future-proof as HF models and pipelines evolve.

**Why this priority**: Enables early exploration without coupling to a single provider.

**Independent Test**: Call video generation where supported or observe capability-based fallback; invoke music hooks and verify `NotSupportedException` or disabled capability when unavailable.

**Acceptance Scenarios**:

1. Given an HF model supporting video, When calling video generation, Then `VideoResponse` returns media or a reference.
2. Given music not supported by the chosen model, When invoking music hooks, Then the client throws `NotSupportedException` or returns disabled capability.

### Edge Cases

- HF returns success with non-standard response shape; client must adapt or surface a stable abstraction.
- Network timeouts or cancellation; calls must respect `CancellationToken` and throw appropriately.
- Invalid HF overrides; client should validate and either ignore or fail fast.
- Model id typo or gated model; capability query and generation calls should surface clear errors.
- Large outputs (images/audio/video) and memory/buffer handling; response abstraction should include references vs. inline payload where applicable.


## Interface Contract *(mandatory)*

List the externally observable surface area this feature introduces or changes. Avoid implementation details.

### New/Changed Public APIs

- HF-only client surface — Methods to generate: Text, Image, Audio (TTS/STT where supported), Video (best-effort), Music (hooks), and to query `ModelCapabilities` per model.
- Common `GenerationSettings` with HF-focused mapping rules and a `ProviderOverrides` bag for HF-specific parameters (e.g., cache, wait_for_model, scheduler).
- Request/Response abstractions per modality: `TextRequest`/`TextResponse`, `ImageRequest`/`ImageResponse`, `AudioRequest`/`AudioResponse` (TTS/STT context), `VideoRequest`/`VideoResponse`, `MusicRequest`/`MusicResponse`.

### Events / Messages *(if applicable)*

- None mandated; implementations may internally emit logs/telemetry but that is out of scope for this spec.

### Data Contracts *(if applicable)*

- `GenerationSettings` — fields: temperature, top_p, top_k, max_new_tokens, do_sample, repetition_penalty, return_full_text, diffusion params (guidance_scale, num_inference_steps, height/width, scheduler, negative_prompt), seed when supported, and `ProviderOverrides` (opaque map).
- `ModelCapabilities` — declares supported modalities and which settings are supported/ignored by HF model; includes pipeline_tag, gating, and inference status.
- Modality-specific request/response abstractions — prompt/input content, optional stream flags, and output content/metadata.


## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Provide HF-only methods for text, image, audio (TTS/STT), video (best-effort), and optional music hooks.
- **FR-002**: Accept `model id`, `GenerationSettings`, and `ProviderOverrides` on each call.
- **FR-003**: Support DI-friendly construction by accepting the tryAGI/HuggingFace v0.4.0 client and/or `HttpClient`.
- **FR-004**: Use `CancellationToken` in all public methods.
- **FR-005**: Offer capability discovery per model via a `ModelCapabilities` query using HF metadata (pipeline_tag, gating, inference status).
- **FR-006**: Apply conservative mapping for `GenerationSettings`; unsupported/mismatched fields MUST be ignored or cause `NotSupportedException` per rules.
- **FR-007**: Provide stable request/response abstractions for each modality, decoupled from transport specifics.
- **FR-008**: Expose a mechanism for advanced callers to access underlying tryAGI/HuggingFace client or HttpClient without breaking the unified surface.
- **FR-009**: Video support is best-effort/optional; if unsupported for a model, capabilities must mark it disabled and calls must throw `NotSupportedException`.
- **FR-010**: Music is hooks-only; capabilities must mark it unsupported by default and invocations must throw `NotSupportedException`.
- **FR-011**: Capability discovery must operate per model id; unknown models default to all modalities/settings disabled until confirmed.

### Error Handling *(mandatory)*

- **EH-001**: If a modality is unsupported by the selected provider/model, the client MUST throw `NotSupportedException` (unless capability queries are used to avoid the call).
- **EH-002**: Network/transport failures MUST surface as `HttpRequestException` (or HF client exceptions) without leaking internal implementation details.
- **EH-003**: HF error payloads MUST be captured and surfaced via a stable error structure or nested exception details where applicable. On HF 503/cold responses, the client MUST set `wait_for_model=true`, honor `Retry-After`, and retry with a small capped backoff (about 2–3 attempts) while keeping `use_cache` true unless overridden.
- **EH-004**: Cancellation MUST be honored, resulting in task cancellation exceptions consistent with .NET semantics.
- **EH-005**: Invalid `ProviderOverrides` MUST either be ignored (if benign) or fail fast with clear error messages.

### State & Data *(mandatory if feature involves data)*

- **Persistence**: No persistent storage required; all operations are request-scoped.
- **Invariants**: Capability discovery and generation settings mapping MUST remain consistent and deterministic across calls for the same HF model.
- **Migration/Compatibility**: Unified abstractions MUST remain stable even as HF APIs evolve; introduce new optional fields conservatively to avoid breaking changes.

### Key Entities *(include if feature involves data)*

- **GenerationSettings**: Common tuning parameters; includes `ProviderOverrides`.
- **ModelCapabilities**: Supported modalities and settings per provider+model.
- **Modality Requests/Responses**: Canonical abstractions for each supported modality.


## Test Matrix *(mandatory)*

| Requirement ID | Unit Tests | Integration Tests | E2E (Playwright) | Notes |
|---|---|---|---|---|
| FR-001 | Y | Y | N | Methods exist and route correctly for HF models |
| FR-005 | Y | Y | N | Capability queries return expected support/ignore flags from HF metadata |
| EH-001 | Y | Y | N | Throws `NotSupportedException` for unsupported modality |
| EH-004 | Y | Y | N | Honors cancellation across all modality calls |


## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Developers integrate at least two HF text models in under 2 hours using the unified client.
- **SC-002**: 95% of supported requests complete successfully without caller-side HF-specific branching beyond model choice.
- **SC-003**: Capability queries prevent 90% of unsupported/gated calls (exceptions avoided) in sample apps.
- **SC-004**: For supported modalities, 95% of responses are returned within user-acceptable latency (e.g., HF warm-path expectations for text/image).

## Clarifications

### Session 2026-01-22

- Q: How should the client behave on HF 503/cold responses? → A: On 503/cold, set `wait_for_model=true`, honor `Retry-After`, back off with a small capped retry budget (about 2–3 attempts), and keep `use_cache` true unless overridden.
