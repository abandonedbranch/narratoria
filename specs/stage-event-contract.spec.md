## spec: stage-event-contract

mode:
  - compositional (defines shared contracts; no owned state)

behavior:
  - what: Define a shared stage-event contract for UI-visible pipelines so ingestion (source stages) and narration (transform chains) emit compatible start/progress/complete events.
  - input:
      - StageEvent : event record emitted by a stage execution
      - IStageEventSink : receiver for StageEvent
      - StageExecutionContext : identifiers and policy for one execution
  - output:
      - IReadOnlyList<StageEvent> : consumed by UI adapters to update chips/hover metadata
  - caller_obligations:
      - ensure `StageId` values match UI `NarrationStageKind.Name` when the stage is rendered as a chip
      - emit events in-order per (ExecutionId, StageId)

stage_id_conventions:
  - reserved_stage_ids:
      - attachment_ingestion
        - meaning: attachment ingestion pipeline stage id (source element) for converting a newly-dropped file into a persisted processed attachment summary
        - ui_surface: attachment chips (not narration turn stage chips)
      - attachment_context_injection
        - meaning: narration pipeline stage id for injecting staged processed attachments into the prompt/context used by provider dispatch
        - ui_surface: narration turn stage chips
  - rule:
      - `attachment_ingestion` MUST NOT appear in narration turn StageOrder
      - `attachment_context_injection` MUST NOT be used to report ingestion work
  - side_effects_allowed:
      - none (contract only)

state:
  - none

preconditions:
  - StageId is non-empty
  - ExecutionId is non-empty

postconditions:
  - for every stage that starts, there is exactly one terminal event (Completed|Failed|Canceled)

invariants:
  - stage identity is stable: StageId is an identity string, not a display label
  - per-stage lifecycle is monotonic:
      - Pending (implicit) → Running → Completed | Failed | Canceled
  - events are idempotent under replay when (ExecutionId, StageId, Sequence) are stable

failure_modes:
  - invalid_event :: missing identifiers or invalid transition :: sink drops event; logs warning

policies:
  - cancellation:
      - when CancellationToken is signaled, stages SHOULD emit a terminal Canceled event promptly
  - concurrency:
      - events may be emitted concurrently across different ExecutionId values
      - events for the same (ExecutionId, StageId) MUST be serialized

never:
  - include raw prompt text, system prompt text, or raw attachment bytes in StageEvent payloads
  - emit provider secrets or credentials

non_goals:
  - defining storage schemas for events
  - defining UI layout

performance:
  - event emission overhead under 1ms per event

observability:
  - logs:
      - trace_id, request_id, execution_id, stage_id, status, elapsed_ms, error_class
  - metrics:
      - stage_event_emitted_count (by stage_id/status), stage_event_dropped_count

output:
  - minimal implementation only (no commentary, no TODOs)

---

types:
  - StageStatus: enum { Running, Completed, Failed, Canceled }

  - StageExecutionContext: record {
      Guid ExecutionId;
      TraceMetadata Trace;
      Guid? SessionId;
      Guid? TurnId;
      string? AttachmentId;
      DateTimeOffset StartedAt;
    }

  - StageEvent: record {
      Guid ExecutionId;
      string StageId;
      StageStatus Status;
      int Sequence;
      DateTimeOffset At;
      long? ElapsedMs;
      string? ErrorClass;
      string? ErrorMessage;
      string? Model;
      int? PromptTokens;
      int? CompletionTokens;
      string? AttachmentId;
      Guid? SessionId;
      Guid? TurnId;
      TraceMetadata Trace;
    }

interfaces:
  - IStageEventSink:
      - ValueTask EmitAsync(StageEvent e, CancellationToken cancellationToken)

  - IStageSource<TOut>:
      - ValueTask<TOut> RunAsync(StageExecutionContext context, IStageEventSink sink, CancellationToken cancellationToken)

  - IStageTransform<TIn, TOut>:
      - ValueTask<TOut> RunAsync(StageExecutionContext context, TIn input, IStageEventSink sink, CancellationToken cancellationToken)

  - IStageSink<TIn>:
      - ValueTask RunAsync(StageExecutionContext context, TIn input, IStageEventSink sink, CancellationToken cancellationToken)
