ios/ or android/
# Implementation Plan: UnifiedInference HF-Only

**Branch**: `[001-unified-inference]` | **Date**: 2026-01-22 | **Spec**: [specs/001-unified-inference/spec.md](specs/001-unified-inference/spec.md)
**Input**: Feature specification from `/specs/001-unified-inference/spec.md`, revised to focus solely on Hugging Face via tryAGI/HuggingFace.

**Note**: This template is filled in by the `/speckit.plan` command. See `.github/agents/speckit.plan.agent.md` for the execution workflow.

## Summary

Shift the unified inference client to a Hugging Face–only implementation using the tryAGI/HuggingFace .NET client and HF Inference API. Deprecate OpenAI and Ollama support, remap `GenerationSettings` to HF parameters (text + diffusion), and align capability discovery with HF model metadata so callers can pick supported models and avoid gated/cold endpoints.

## Technical Context

**Language/Version**: C# / .NET 10  
**Primary Dependencies**: tryAGI/HuggingFace client library v0.4.0 (NuGet), `System.Net.Http`  
**Storage**: N/A (request-scoped operations only)  
**Testing**: xUnit via `dotnet test` (deterministic unit/integration); no UI/Playwright needed for library  
**Target Platform**: .NET class library consumed by server/desktop workloads  
**Project Type**: Single library (`src/lib/UnifiedInference`) with provider-specific folder (`Providers/HuggingFace`)  
**Performance Goals**: HF warm-path targets — text p50 < 2s and image p50 < 15s (no unnecessary retries; backoff only on 503 with Retry-After); avoid added client-side latency beyond HTTP round trips  
**Constraints**: Respect HF rate limits, gating, and model cold starts; cancellation honored; NO external state  
**Scale/Scope**: API surface limited to HF text/image/audio/video where supported; music hooks stay disabled

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Specs stay authoritative; spec updates required to remove OpenAI/Ollama (recorded in this plan).
- Interfaces remain small (unified client + per-modality requests/responses); composition preserved.
- Data favors immutability; side effects isolated to HTTP calls.
- Async with `CancellationToken` everywhere; no fire-and-forget.
- Tests deterministic; no UI so Playwright not applicable.

## Project Structure

### Documentation (this feature)

```text
specs/001-unified-inference/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
└── lib/
    └── UnifiedInference/
        ├── Abstractions/
        ├── Core/
        ├── Factory/
        └── Providers/
            └── HuggingFace/

tests/
└── UnifiedInference.Tests/
```

**Structure Decision**: Single library with provider-specific subfolder for Hugging Face; tests live under `tests/UnifiedInference.Tests`.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| (none) | N/A | N/A |

## Constitution Check (Post-Design)

- HF-only scope aligns spec and plan; no silent drift recorded.
- Interfaces remain small; composition preserved; no inheritance introduced.
- Data stays immutable-first; side effects limited to HTTP calls.
- Async + cancellation required for all flows; streaming best-effort.
- Tests stay deterministic; no UI → Playwright not applicable.
