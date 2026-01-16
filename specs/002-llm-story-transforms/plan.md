# Implementation Plan: LLM Story Transforms

**Branch**: `002-llm-story-transforms` | **Date**: 2026-01-08 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-llm-story-transforms/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.github/agents/speckit.plan.agent.md` for the execution workflow.

## Summary

Add a set of `IPipelineTransform` implementations that enrich streamed `TextChunk` content using LLM calls:

- Rewrite narration text (grammar/voice normalization) with a moderation guardrail sourced from [moderation-prompts.md](moderation-prompts.md)
- Maintain a rolling story summary
- Maintain character roster/state
- Maintain player inventory state
- Maintain player reputation/faction standing with consequence cues

Design centers on small injectable LLM provider services:

- OpenAI provider: uses the official OpenAI .NET library
- Hugging Face provider: uses `HttpClient` against the Hugging Face Inference REST API

Transforms are chained so downstream state trackers consume the best available text and context:

`Rewrite (with moderation guardrail) -> Summary -> (Character + Inventory + Reputation)`

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`)  
**Primary Dependencies**: Official OpenAI .NET library (NuGet), `HttpClient`, `System.Text.Json`  
**Storage**: N/A (story state stored as structured JSON in `PipelineChunkMetadata.Annotations`)  
**Testing**: MSTest (`tests/Narratoria.Tests`)  
**Target Platform**: .NET class library (cross-platform)
**Project Type**: Single library (`src/`) + test project (`tests/`)  
**Performance Goals**: Stream-friendly; minimize LLM calls per input; avoid unbounded buffering  
**Constraints**: Deterministic tests (no live network calls); all async accepts `CancellationToken` and is cancellation-correct; transforms remain stream-safe; graceful degradation on provider failures; moderation policy is loaded from `specs/002-llm-story-transforms/moderation-prompts.md` and applied in the rewrite transform; safety flags/logs must not block downstream consumption  
**Scale/Scope**: Per-session story state updated incrementally as text streams (summary, characters, inventory, reputation, safety flags)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Specs are authoritative; no silent drift (update spec or record in `TODO`).
- Interfaces are tiny; composition over inheritance.
- Prefer immutability; isolate IO/time/random behind interfaces.
- Async/cancellation is explicit; no fire-and-forget.
- Tests are deterministic; UI changes include Playwright E2E coverage.

Status: PASS (no planned violations).

## Project Structure

### Documentation (this feature)

```text
specs/002-llm-story-transforms/
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
└── Pipeline/
  ├── IPipelineSource.cs
  ├── IPipelineSink.cs
  ├── IPipelineTransform.cs
  ├── PipelineChunk*.cs
  ├── PipelineRunner*.cs
  ├── Text/
  └── Transforms/
    └── (new) Llm/
      ├── (new) Providers/
      ├── (new) Prompts/
      └── (new) StoryState/

tests/
└── Narratoria.Tests/
  └── Pipeline/
    └── (new) Llm/
```

**Structure Decision**: Single library + single MSTest project. New LLM transforms live under `src/Pipeline/Transforms/Llm/` to match existing `Transforms/` organization.

## Phase 0: Research (output: research.md)

Goal: resolve provider/API decisions and establish a stable contract for calling OpenAI and Hugging Face from transforms.

Deliverable: [research.md](research.md)

## Phase 1: Design & Contracts (outputs: data-model.md, contracts/, quickstart.md)

Goal: define story-state schema, provider abstractions, prompt strategy, and transform chaining semantics.

Deliverables:

- [data-model.md](data-model.md)
- [quickstart.md](quickstart.md)
- contracts/ (see [contracts/](contracts/))
- Update moderation prompt reference to [moderation-prompts.md](moderation-prompts.md) for rewrite guardrail inputs

Constitution Re-check (post-design): PASS (design keeps IO behind small interfaces; transforms remain composable; tests remain deterministic).

## Phase 2: Implementation Planning (output: tasks.md via /speckit.tasks)

This plan intentionally stops before enumerating implementation tasks. Run `/speckit.tasks` to generate `tasks.md` from this plan and the spec.
