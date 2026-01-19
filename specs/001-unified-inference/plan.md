# Implementation Plan: UnifiedInference Client

**Branch**: `[001-unified-inference]` | **Date**: January 19, 2026 | **Spec**: [specs/001-unified-inference/spec.md](specs/001-unified-inference/spec.md)
**Input**: Feature specification from `specs/001-unified-inference/spec.md`

## Summary

Production-grade .NET class library delivering a provider-agnostic unified inference client across OpenAI, Ollama, and Hugging Face for text, image, audio (TTS/STT), and optional video/music hooks. Uses DI-friendly construction, honors `CancellationToken`, provides capability discovery per provider+model with conservative defaults, and exposes native clients where appropriate. Code lives in `src/lib`; project may be named `UnifiedInference` but the output assembly MUST be `inference.dll`.

## Technical Context

**Language/Version**: .NET 10 (TFM `net10.0`); if unavailable in toolchain locally, use `net8.0` during development and plan upgrade.
**Primary Dependencies**:
- Official OpenAI .NET SDK (wrap directly; expose native client)
- `HttpClient` for Hugging Face generic inference and configurable Inference Endpoints
- Ollama: first-party/de-facto .NET client if exists; otherwise HTTP API via `HttpClient`
**Storage**: N/A (stateless request execution)
**Testing**: xUnit for unit/integration; deterministic tests by injecting transports and time/random; no UI hence no Playwright needed
**Target Platform**: Cross-platform .NET library
**Project Type**: Single class library
**Performance Goals**: Minimal overhead vs native SDKs/HTTP; streaming-friendly where provider supports it
**Constraints**:
- Source code MUST live under `/Users/djlawhead/Developer/forkedagain/projects/narratoria/src/lib`
- Project CAN be named `UnifiedInference`, but assembly output MUST be `inference.dll` (`<AssemblyName>inference</AssemblyName>`)
- Small stable interfaces; DI-friendly constructors accepting native clients/HttpClient; `CancellationToken` everywhere
- Video is best-effort/optional; music is hooks-only; per-model capabilities default disabled when unknown
**Scale/Scope**: Library consumed by applications integrating multiple AI providers; no UI, no persistence

## Constitution Check

GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.

- Spec-first: Implementation will follow [specs/001-unified-inference/spec.md](specs/001-unified-inference/spec.md); any ambiguity is resolved in spec.
- Small interfaces: `IUnifiedInferenceClient` with focused modality methods and capability query.
- Immutability: Use records for settings/requests/responses; IO isolated in provider clients.
- Async/cancellation: All public methods accept and honor `CancellationToken`.
- Deterministic tests: Provider transports injected/mocked; no UI, so Playwright not applicable.

## Project Structure

### Documentation (this feature)

```text
specs/001-unified-inference/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
└── tasks.md           # Created by /speckit.tasks (not in this plan)
```

### Source Code (repository root)

```text
src/
└── lib/
    ├── UnifiedInference.csproj            # Project name UnifiedInference, assembly name 'inference'
    ├── Abstractions/                      # Interfaces, enums, request/response records
    ├── Core/                              # Common mapping, capability discovery
    ├── Providers/
    │   ├── OpenAI/                        # Wrap official SDK; expose NativeClient
    │   ├── Ollama/                        # HTTP client/transport; expose native if exists
    │   └── HuggingFace/                   # HTTP client for generic + endpoints; robust parsing
    └── Factory/                           # Optional InferenceClientFactory

tests/
└── UnifiedInference.Tests/
    ├── Unit/
    └── Integration/
```

**Structure Decision**: Single-project library under `src/lib` with assembly name set to `inference`. Tests colocated under `tests/UnifiedInference.Tests`.

## Complexity Tracking

No constitution violations anticipated. If `net10.0` is unavailable locally, we will temporarily target `net8.0` and record a TODO to upgrade when CI/tooling supports `net10.0`.
