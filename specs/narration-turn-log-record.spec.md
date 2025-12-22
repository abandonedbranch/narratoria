## spec: narration turn log record

mode:
  - isolated (defines persisted record shape and replay semantics; does not own persistence)

behavior:
  - what: Define a deterministic, persisted representation of a narration “turn” such that a UI transcript can be reconstructed from storage without re-running provider dispatch.
  - input:
      - Guid SessionId: session identifier
      - Guid TurnId: per-turn identifier
      - DateTimeOffset CreatedAt: timestamp of turn creation
      - DateTimeOffset UpdatedAt: timestamp of last persisted update for this turn
      - string Prompt: user prompt text for the turn
  - NarrationTurnOutcome Outcome: final turn outcome (Succeeded|Failed|Canceled)
      - IReadOnlyList<NarrationStageKind> StageOrder: stage order used to render chips for this turn
      - IReadOnlyList<NarrationStageSnapshot> Stages: persisted stage snapshots
      - IReadOnlyList<string> OutputSegments: persisted narration output segments (ordered)
      - bool IsFinal: whether the persisted record represents a completed turn (no further changes)
  - string? FailureClass: optional structured error class for a failed turn (final only; null unless Outcome=Failed)
      - TraceMetadata Trace: trace identifiers
  - output:
      - NarrationTurnRecord: immutable record suitable for storage and replay
      - NarrationPipelineTurnView: reconstructed in-memory UI view model for transcript rendering
  - caller_obligations:
      - Persist and load NarrationTurnRecord via a session-scoped store.
      - Use TurnId as the stable key for both persistence and UI lookup.
      - Ensure StageOrder values match middleware telemetry stage ids (NarrationStageKind.Name) used by the configured pipeline.
      - Never attempt to “replay” by calling providers; replay is pure transformation from NarrationTurnRecord → view.
  - side_effects_allowed:
      - none (spec defines data + mapping only)

state:
  - none: pure record definition and mapping rules

preconditions:
  - SessionId and TurnId are non-empty GUIDs.
  - StageOrder is non-empty and contains unique NarrationStageKind.Name values.
  - For any entry in Stages, StageId MUST exist exactly once in StageOrder.

postconditions:
  - Replay produces a NarrationPipelineTurnView where stage chips render in StageOrder and status fields match persisted stage snapshots.
  - Replay produces output text as the concatenation of OutputSegments in order.

invariants:
  - Determinism: given the same NarrationTurnRecord, replay MUST produce identical NarrationPipelineTurnView.
  - Append-only output: OutputSegments ordering is stable; replay MUST preserve ordering.
  - Stage identity: StageId is a telemetry id and MUST equal a NarrationStageKind.Name.
  - Finality: if IsFinal is true, the record MUST NOT change except by being replaced with an identical value.
  - Outcome consistency:
      - If IsFinal is true, Outcome MUST be one of Succeeded|Failed|Canceled.
      - If Outcome=Failed, FailureClass MUST be non-null.
      - If Outcome=Succeeded or Outcome=Canceled, FailureClass MUST be null.

failure_modes:
  - StageMismatch :: a persisted stage StageId is not in StageOrder :: drop the mismatched stage snapshot during replay; emit warning log; render that chip as Pending.
  - InvalidRecord :: required fields missing or invalid (e.g., empty Prompt, empty StageOrder) :: return structured error; do not attempt replay.
  - Cancellation :: cancellation requested during load/replay :: abort operation; do not produce partial replay output.

policies:
  - persistence_update_model:
      - A store MAY persist a non-final record during streaming.
      - A store MUST persist a final record on completion (success, failure, or cancellation) so restore can render stable transcript.
      - Final record outcome mapping:
          - success => Outcome=Succeeded, IsFinal=true, FailureClass=null
          - failure => Outcome=Failed, IsFinal=true, FailureClass!=null
          - cancellation => Outcome=Canceled, IsFinal=true, FailureClass=null
      - If non-final updates are persisted, UpdatedAt MUST monotonically increase.
  - concurrency:
      - Writes are last-write-wins per (SessionId, TurnId).
      - Replay MUST tolerate partially-updated non-final records by treating missing Stages as Pending and missing OutputSegments as empty.
  - cancellation:
      - Load and replay MUST honor CancellationToken.

never:
  - Store provider secrets, raw attachment bytes, or system prompt text in NarrationTurnRecord.
  - Re-run provider dispatch during restore/replay.
  - Infer stage order from stored Stages; StageOrder is authoritative.

non_goals:
  - Defining the storage API surface for turn persistence (belongs to session store spec).
  - Defining stage telemetry emission (belongs to middleware and stage-event-contract specs).
  - Defining how OutputSegments are chunked (token-by-token vs buffered); only ordering semantics are required.

performance:
  - Replay under 5ms for 1 turn with 1,000 output segments.
  - Replay under 50ms for 50 turns with 1,000 output segments each (batch).

observability:
  - logs:
      - trace_id, request_id, session_id, turn_id, event (turn_replay|turn_replay_drop_stage|turn_replay_invalid), status, error_class, elapsed_ms
  - metrics:
      - turn_replay_count (by status), turn_replay_latency_ms, turn_replay_invalid_count

output:
  - minimal implementation only (no commentary, no TODOs)
