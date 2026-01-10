# Research: Realtime Pipeline Log UI

This document resolves the technical unknowns required to write an implementation plan for spec 003.

## Decisions

### UI + Client Storage Approach

- **Decision**: Implement the “pipeline log UI” as a **browser UI** backed by the existing in-process pipeline runner, and use **client IndexedDB** for persistence.
- **Rationale**: The spec requires client-local IndexedDB persistence and reload/restore semantics; browser execution makes this straightforward and aligns with “player can resume later.”
- **Alternatives considered**:
  - **Server-side persistence**: rejected (explicitly out-of-scope).
  - **In-memory only**: rejected (FR-013/FR-014).

### Telemetry Production

- **Decision**: Add an **optional observer/telemetry sink** around pipeline execution rather than baking UI concerns into core types.
- **Rationale**: Keeps the pipeline core small and reusable; preserves deterministic tests by injecting observers.
- **Alternatives considered**:
  - **Global logging/static events**: rejected (hard to test and violates “pure core” intent).

### Pipeline Composition (“pipeline as a pipeline element”)

- **Decision**: Model “Pipeline A” and “Pipeline B” as **pipeline definitions built from shared transform lists**, not as nested pipeline execution.
  - Composition is expressed as reusable `IReadOnlyList<IPipelineTransform>` (or a small value type like `PipelineSegment`) that can be spliced into a `PipelineDefinition`.
- **Rationale**: The current pipeline model already composes transforms linearly; introducing “nested pipelines” would add new contracts and complexity without clear benefit.
- **Alternatives considered**:
  - **Pipeline-as-transform** (a transform that internally runs another pipeline): rejected for Phase 1 because the current runner/element contracts are stream-based and don’t naturally express “subpipeline runs” without adapter elements and extra cancellation rules.

### Live Input Source

- **Decision**: For idle/send execution, each run uses a **snapshot** of the accumulated buffer, so the source can remain the existing `TextPromptSource` (via `CompleteText`).
- **Rationale**: Matches the spec’s “latest buffer” requirement and avoids inventing a long-lived streaming source contract.
- **Alternatives considered**:
  - **Channel-backed streaming source**: possible later, but not required to satisfy idle/send semantics.

### Persistence + Retention + Compaction

- **Decision**: Persist **summarized run records** + **latest story context** (no full telemetry stream persistence), enforce a **fixed max count** for run records, and compact older history into a **StoryContextDigest** containing **exactly 12 story-fact bullets**.
- **Rationale**: Predictable storage growth and predictable prompt footprint for LLM memory.
- **Alternatives considered**:
  - **Persist full telemetry stream**: rejected (too large/noisy; not needed for resume).
  - **Unbounded history**: rejected (quota risk).
  - **Cap by bytes/days**: workable, but fixed-count is simplest and deterministic.

## Open Items (tracked in plan)

- Concrete UI tech choice (Blazor WASM vs other) depends on repo direction, but the plan will assume a browser UI with IndexedDB access.
- Exact IndexedDB schema/migration strategy.
