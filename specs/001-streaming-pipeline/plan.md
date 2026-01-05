# Implementation Plan: Streaming Narration Pipeline

**Branch**: `001-streaming-pipeline` | **Date**: 2026-01-05 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-streaming-pipeline/spec.md`

## Summary

Deliver a reusable, streaming narration pipeline API surface (source → transforms → sink) with a typed chunk contract that enables safe composition across different payload types (e.g., bytes vs text). The pipeline supports incremental input (including byte-by-byte), deterministic transform ordering, cancellation/early termination, and explicit failure outcomes.

## Technical Context

**Language/Version**: C# on .NET 10
**Primary Dependencies**: .NET BCL only (no new third-party dependencies required for core pipeline)
**Storage**: N/A for this feature (no new persistence required)
**Testing**: MSTest (existing repo convention), deterministic unit tests; integration tests limited to in-process composition
**Target Platform**: ASP.NET Core / Blazor Server hosting environment
**Project Type**: Single project (`src/`) with test projects in `tests/`
**Performance Goals**:
- Minimal overhead for chunk forwarding (streaming should be incremental)
- Cancellation is observed quickly (sub-second in-process)
**Constraints**:
- No assumption that existing source code or existing tests are viable; implement feature in a self-contained manner with new or revised automated tests.
- Interfaces must remain small and composable per `CONTRIB`.

## Constitution Check

- Specs are authoritative; no silent drift (update spec or record in `TODO`).
- Interfaces are tiny; composition over inheritance.
- Prefer immutability; isolate IO/time/random behind interfaces.
- Async/cancellation is explicit; no fire-and-forget.
- Tests are deterministic; UI changes include end-to-end coverage (not expected for this API-only feature).

## Project Structure

### Documentation (this feature)

```text
specs/001-streaming-pipeline/
├── spec.md
├── plan.md
├── tasks.md
└── checklists/
    └── requirements.md
```

### Source Code (repository root)

```text
src/
├── Pipeline/
│   ├── PipelineChunk.cs
│   ├── PipelineChunkMetadata.cs
│   ├── PipelineChunkType.cs
│   ├── PipelineOutcome.cs
│   ├── PipelineRunResult.cs
│   ├── IPipelineSource.cs
│   ├── IPipelineTransform.cs
│   ├── IPipelineSink.cs
│   ├── PipelineDefinition.cs
│   └── PipelineRunner.cs
│   ├── Transforms/
│   │   ├── DecodeBytesToTextTransform.cs
│   │   └── TextAccumulatorTransform.cs
│   └── Text/
│       ├── TextSourceConfig.cs
│       ├── TextPromptSource.cs
│       ├── TextInputAdapters.cs
│       └── TextCollectingSink.cs
└── Narration/
    └── (optional) adapters that let existing narration flows consume the new pipeline API

tests/
└── Narratoria.Tests/
    └── Pipeline/
        ├── PipelineChunkTests.cs
        ├── PipelineRunnerTests.cs
        ├── StreamingInputAdapterTests.cs
        └── TransformCompatibilityTests.cs
```

**Structure Decision**: Implement the new pipeline as a self-contained API under `src/Pipeline/` with a minimal surface area. Any integration with existing narration components is optional and should be done via adapters to avoid coupling.

## Design Decisions

### Typed Chunk Contract

- Define a typed chunk envelope (conceptually `PipelineChunk<TPayload>`) with:
  - An explicit payload type descriptor (bytes vs text vs other future payloads)
  - Metadata that can declare interpretation rules (e.g., UTF-8 decode contract for byte payloads that represent text)
  - Optional annotations for downstream transforms/sinks

### Composition and Compatibility

- Define small interfaces for source/transform/sink.
- Require transforms to declare accepted and produced chunk types.
- Fail fast if the pipeline graph is incompatible.

### Streaming Input

- Support both:
  - Streaming input (bytes/chunks arriving incrementally)
  - Complete input values adapted into an equivalent stream

### Execution Model

- Use `IAsyncEnumerable<PipelineChunk>` as the streaming primitive.
- Cancellation is propagated via `CancellationToken`.
- Early termination is supported when downstream stops consumption.

## Risk & Mitigations

- **Risk**: Over-generalizing too early.
  - **Mitigation**: Start with bytes/text chunk types only; keep interfaces minimal.
- **Risk**: Tight coupling to existing code.
  - **Mitigation**: Place the pipeline in `src/Pipeline/` and integrate via adapters only if needed.
