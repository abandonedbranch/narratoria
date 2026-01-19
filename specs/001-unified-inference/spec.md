# Feature Specification: UnifiedInference Client

**Feature Branch**: `[001-unified-inference]`  
**Created**: January 19, 2026  
**Status**: Draft  
**Input**: User description summarizing a unified .NET 10 class library offering provider-agnostic inference across OpenAI, Ollama, and Hugging Face for text, image, audio (TTS/STT), video, and optional music, with common generation settings, provider overrides, DI-friendly design, CancellationToken support, capability discovery, and conservative defaults.


## Scope *(mandatory)*

### In Scope

- Provider-agnostic API surfaces for modalities: text (chat/completion), image (prompt-to-image), audio (TTS and STT where supported), video (prompt-to-video where supported), and music (optional hooks for future support).
- Caller supplies: provider identifier (OpenAI/Ollama/HuggingFace), model id/name (string), common generation settings (e.g., temperature, top_p, top_k, max_tokens), and provider-specific overrides (arbitrary map).
- Capability discovery per provider+model to prevent unsupported calls and guide clients.
- Clear error semantics including `NotSupportedException` for unsupported modalities/settings and network/transport exceptions for provider/API failures.
- Dependency-injection-friendly construction (accept preconfigured native clients/HttpClient) and ubiquitous `CancellationToken` usage.
- Stable, minimal unified interfaces that do not attempt full parity with all provider endpoints.

### Out of Scope

- Training/fine-tuning workflows, embeddings, or advanced provider-specific APIs not directly tied to listed modalities.
- UI implementation, CLI tools, or orchestration pipelines beyond the client.
- Data persistence or telemetry storage beyond what is necessary for immediate request execution.
- Non-.NET platforms or language bindings; this specification focuses on .NET consumers.
- Provider account/key management UX.

### Assumptions

- Callers will provide valid credentials/tokens and network connectivity for each provider.
- Model identifiers supplied by callers are valid for the chosen provider.
- Capability discovery is evaluated per model id; when unknown, capabilities default to disabled to avoid surprises.
- DI containers are available in consuming applications but the client can be used without a container as well.
- Providers may differ widely in supported settings; unmapped settings are ignored or cause `NotSupportedException` depending on modality/impact.

### Open Questions *(mandatory)*

- All clarifications resolved: video is best-effort (optional at GA); music is hooks-only with NotSupportedException when invoked; capability discovery is per-model with unknowns treated as disabled by default.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Unified Text Generation (Priority: P1)

As an application developer, I want to generate text (chat/completion) via a single unified API by selecting a provider and model, so I can switch providers without refactoring my code.

**Why this priority**: Text is the most common modality and the foundation for many app experiences.

**Independent Test**: Provide `InferenceProvider`, `model id`, and `GenerationSettings` to produce text across at least two providers; verify identical calling pattern and deterministic handling of unsupported settings.

**Acceptance Scenarios**:

1. Given valid provider credentials and a supported text model, When calling unified text generation, Then a `TextResponse` is returned with content and metadata.
2. Given an unsupported setting for a chosen provider/model, When calling unified text generation, Then either the setting is ignored (with capability disclosure) or a `NotSupportedException` is thrown per rules.

---

### User Story 2 - Capability Discovery and Fallback (Priority: P1)

As a developer, I want to query provider+model capabilities before requests, so I can avoid unsupported calls and implement fallbacks.

**Why this priority**: Prevents runtime failures and improves developer UX.

**Independent Test**: Query capabilities for multiple provider+model combinations, validate modality and setting support, and conditionally select an alternative provider when unsupported.

**Acceptance Scenarios**:

1. Given provider+model inputs, When querying capabilities, Then modalities and supported/ignored settings are returned.
2. Given a requested modality not supported, When checking capabilities, Then client selects a supported provider/model or gracefully disables the feature.

---

### User Story 3 - Images and Audio (Priority: P2)

As a developer, I want prompt-to-image and audio TTS/STT via unified calls, so I can integrate visual and audio features consistently across providers.

**Why this priority**: Images and audio cover common, high-value generation tasks beyond text.

**Independent Test**: Invoke image and audio methods for at least one provider each; verify successful generation and proper fallbacks for unsupported settings.

**Acceptance Scenarios**:

1. Given a provider that supports images, When calling image generation with a prompt, Then `ImageResponse` returns binary/media data or a URI.
2. Given TTS or STT support on a provider, When calling audio generation/recognition, Then `AudioResponse` returns audio/recognized text per modality.

---

### User Story 4 - Video and Optional Music (Priority: P3)

As a developer, I want a unified surface for video generation (where supported) and optional music hooks, so my codebase remains future-proof as providers evolve.

**Why this priority**: Enables early exploration without coupling to a single provider.

**Independent Test**: Call video generation where supported or observe capability-based fallback; invoke music hooks and verify `NotSupportedException` or disabled capability when unavailable.

**Acceptance Scenarios**:

1. Given a provider supporting video, When calling video generation, Then `VideoResponse` returns media or a reference.
2. Given music not supported by the chosen provider/model, When invoking music hooks, Then the client throws `NotSupportedException` or returns disabled capability.

### Edge Cases

- Provider returns a success but with non-standard response shape; client must adapt or surface a stable abstraction.
- Network timeouts or cancellation; calls must respect `CancellationToken` and throw appropriately.
- Invalid provider overrides; client should validate and either ignore or fail fast.
- Model id typo; capability query and generation calls should surface clear errors.
- Large outputs (images/audio/video) and memory/buffer handling; response abstraction should include references vs. inline payload where applicable.


## Interface Contract *(mandatory)*

List the externally observable surface area this feature introduces or changes. Avoid implementation details.

### New/Changed Public APIs

- Unified client surface — Methods to generate: Text, Image, Audio (TTS/STT), Video, Music (hooks), and to query `ModelCapabilities`.
- Common `GenerationSettings` with conservative mapping rules and a `ProviderOverrides` bag for per-provider specifics.
- Request/Response abstractions per modality: `TextRequest`/`TextResponse`, `ImageRequest`/`ImageResponse`, `AudioRequest`/`AudioResponse` (TTS/STT context), `VideoRequest`/`VideoResponse`, `MusicRequest`/`MusicResponse`.

### Events / Messages *(if applicable)*

- None mandated; implementations may internally emit logs/telemetry but that is out of scope for this spec.

### Data Contracts *(if applicable)*

- `GenerationSettings` — fields: temperature, top_p, top_k, max_tokens, presence/frequency penalties (if applicable), and `ProviderOverrides` (opaque map).
- `ModelCapabilities` — declares supported modalities and which settings are supported/ignored by provider+model.
- Modality-specific request/response abstractions — prompt/input content, optional stream flags, and output content/metadata.


## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Provide provider-agnostic methods for text, image, audio (TTS/STT), video, and optional music hooks.
- **FR-002**: Accept `InferenceProvider`, `model id`, `GenerationSettings`, and `ProviderOverrides` on each call.
- **FR-003**: Support DI-friendly construction by accepting native provider clients/`HttpClient`.
- **FR-004**: Use `CancellationToken` in all public methods.
- **FR-005**: Offer capability discovery per provider+model via a `ModelCapabilities` query.
- **FR-006**: Apply conservative mapping for `GenerationSettings`; unsupported/mismatched fields MUST be ignored or cause `NotSupportedException` per rules.
- **FR-007**: Provide stable request/response abstractions for each modality, decoupled from provider specifics.
- **FR-008**: Expose a mechanism for advanced callers to access underlying native clients/transports without breaking the unified surface.
- **FR-009**: Video support is best-effort/optional; if unsupported for a provider/model, capabilities must mark it disabled and calls must throw `NotSupportedException`.
- **FR-010**: Music is hooks-only; capabilities must mark it unsupported by default and invocations must throw `NotSupportedException`.
- **FR-011**: Capability discovery must operate per model id; unknown models default to all modalities/settings disabled until confirmed.

### Error Handling *(mandatory)*

- **EH-001**: If a modality is unsupported by the selected provider/model, the client MUST throw `NotSupportedException` (unless capability queries are used to avoid the call).
- **EH-002**: Network/transport failures MUST surface as `HttpRequestException` (or provider SDK-specific exceptions) without leaking internal implementation details.
- **EH-003**: Provider-specific error payloads MUST be captured and surfaced via a stable error structure or nested exception details where applicable.
- **EH-004**: Cancellation MUST be honored, resulting in task cancellation exceptions consistent with .NET semantics.
- **EH-005**: Invalid `ProviderOverrides` MUST either be ignored (if benign) or fail fast with clear error messages.

### State & Data *(mandatory if feature involves data)*

- **Persistence**: No persistent storage required; all operations are request-scoped.
- **Invariants**: Capability discovery and generation settings mapping MUST remain consistent and deterministic across calls for the same provider+model.
- **Migration/Compatibility**: Unified abstractions MUST remain stable even as providers evolve; introduce new optional fields conservatively to avoid breaking changes.

### Key Entities *(include if feature involves data)*

- **GenerationSettings**: Common tuning parameters; includes `ProviderOverrides`.
- **ModelCapabilities**: Supported modalities and settings per provider+model.
- **Modality Requests/Responses**: Canonical abstractions for each supported modality.


## Test Matrix *(mandatory)*

| Requirement ID | Unit Tests | Integration Tests | E2E (Playwright) | Notes |
|---|---|---|---|---|
| FR-001 | Y | Y | N | Methods exist and route correctly per provider |
| FR-005 | Y | Y | N | Capability queries return expected support/ignore flags |
| EH-001 | Y | Y | N | Throws `NotSupportedException` for unsupported modality |
| EH-004 | Y | Y | N | Honors cancellation across all modality calls |


## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Developers integrate at least two providers for text in under 2 hours using the unified client.
- **SC-002**: 95% of supported requests complete successfully without provider-specific code in the caller.
- **SC-003**: Capability queries prevent 90% of unsupported calls (exceptions avoided) in sample apps.
- **SC-004**: For supported modalities, 95% of responses are returned within user-acceptable latency (e.g., perceived as snappy under typical network conditions).
