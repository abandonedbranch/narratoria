# Implementation Plan: Realtime Pipeline Log UI

**Branch**: `003-pipeline-log-ui` | **Date**: 2026-01-09 | **Spec**: [specs/003-pipeline-log-ui/spec.md](specs/003-pipeline-log-ui/spec.md)
**Input**: Feature specification from `/specs/003-pipeline-log-ui/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.github/agents/speckit.plan.agent.md` for the execution workflow.

## Summary

Implement a realtime pipeline telemetry log with embedded input that runs the existing streaming pipeline on idle (500ms) and send triggers, persists a single auto-resume story session to client IndexedDB, and bounds persisted run history via fixed-count retention with compaction into a 12-bullet story-fact digest.

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: C# / .NET 10  
**Primary Dependencies**: Existing `Narratoria.Pipeline` core; UI framework TBD (browser UI required for IndexedDB); JSON serialization (System.Text.Json)  
**Storage**: Client IndexedDB (single session auto-resume; bounded run-record store + compaction digest)  
**Testing**: MSTest for unit/integration; Playwright E2E required for UI behaviors (per constitution/spec)  
**Target Platform**: Browser (required for IndexedDB); runs pipeline in-process  
**Project Type**: Web app + library (pipeline core remains in `src/Narratoria.csproj`)  
**Performance Goals**: No UI freezes during typing; idle debounce 500ms; cancellation “latest-wins” for idle runs  
**Constraints**: Deterministic tests; explicit cancellation; fixed storage growth; no new chunk types  
**Scale/Scope**: Single-user local session; bounded run history; high-frequency input events

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Specs are authoritative; no silent drift (update spec or record in `TODO`).
- Interfaces are tiny; composition over inheritance.
- Prefer immutability; isolate IO/time/random behind interfaces.
- Async/cancellation is explicit; no fire-and-forget.
- Tests are deterministic; UI changes include Playwright E2E coverage.

Status: PASS (plan follows spec; adds IO behind small interfaces; cancellation/testing called out explicitly).

## Project Structure

### Documentation (this feature)

```text
specs/003-pipeline-log-ui/
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
├── Narratoria.csproj
└── Pipeline/
    ├── IPipelineSink.cs
    ├── IPipelineSource.cs
    ├── IPipelineTransform.cs
    ├── PipelineChunk*.cs
    ├── PipelineDefinition.cs
    ├── PipelineOutcome*.cs
    ├── PipelineRunner.cs
    ├── Text/
    └── Transforms/

tests/
└── Narratoria.Tests/
    └── Pipeline/

# Planned additions for this feature (to satisfy UI + IndexedDB requirements):
src/
└── Narratoria.Web/                    # browser UI host

tests/
└── Narratoria.PlaywrightTests/        # E2E coverage for UI behaviors
```

**Structure Decision**: Keep the existing pipeline library in `src/Narratoria.csproj`, and add a thin browser UI host project plus Playwright E2E tests to cover the UI behaviors required by the spec.

## Design Outline

### Execution Orchestrator (Idle + Send)

- Maintain a single in-memory `UserInputBuffer` and `LlmSelection`.
- Idle trigger: debounce input changes for 500ms; start a Pipeline A run using the latest snapshot.
- Send trigger: start a Pipeline B run using the latest snapshot; suppress idle runs and disable input until completion/error.
- Cancellation: “latest input wins” for idle runs by canceling the prior idle run via a `CancellationTokenSource`.

### Pipeline A / Pipeline B composition

- Represent the “pipeline split” as two `PipelineDefinition<TSinkResult>` builders.
- Reuse existing transform lists (and spec 002 transforms for Pipeline B) by splicing `IReadOnlyList<IPipelineTransform>` into definitions.
- **No pipeline-as-element in Phase 1**: composition is “list of transforms,” which matches the current runner contract and avoids nested streaming semantics.

### Source element choice

- Default: use `TextPromptSource` with `TextSourceConfig.CompleteText` set to the current buffer snapshot.
- Optional (only if needed later): add a channel-backed `IPipelineSource` for truly incremental sources, but the spec’s idle/send semantics do not require it.

### Telemetry

- Add a small observer surface around pipeline execution to emit run lifecycle + stage events.
- UI receives telemetry updates and renders them as `RunTelemetryEntry` items.
- Persisted data remains summarized run records + latest story context; telemetry streams are not persisted.

### Persistence “sink” + retention/compaction

- Persist `PersistedRunRecord` + latest story context to IndexedDB after each run completion.
- Enforce a fixed max count of run records (make this a configuration value; default 200).
- When compacting:
  - Select oldest records beyond the cap.
  - Run a compaction pipeline that produces a `StoryContextDigest` with exactly 12 story-fact bullets.
  - Append digest into story context; delete compacted run records.

### Pipeline elements to add (expected)

- A persistence adapter (conceptually a sink) for IndexedDB IO.
- A compaction transform (LLM-backed) used only during retention compaction.

## Post-Design Constitution Check

- No spec drift: plan follows spec 003 clarifications for persistence/retention/digest.
- Small interfaces: observer + persistence abstractions remain narrow.
- Cancellation explicit: orchestrator owns CTS and passes tokens to runner.
- Deterministic tests: compaction can be tested with a fake summarizer.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
