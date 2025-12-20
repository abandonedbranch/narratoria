## spec: pipeline-observer-view-adapter

mode:
  - compositional (translates observer callbacks into view-model updates; no owned persistence)

behavior:
  - what: Map either `INarrationPipelineObserver` events or shared `StageEvent` telemetry into `NarrationPipelineTurnView` updates: stage status transitions, metadata population, and streaming output concatenation.
  - input:
      - INarrationPipelineObserver : source of pipeline events
      - StageEvent : shared stage telemetry events (see stage-event-contract)
      - IReadOnlyList<NarrationStageKind> StageOrder : canonical stage order
  - output:
      - Immutable updates to `NarrationPipelineTurnView` instances
  - caller_obligations:
      - supply StageOrder whose NarrationStageKind.Name values match the telemetry stage ids emitted by the configured middleware
      - ensure a single turn is active for streaming
  - side_effects_allowed:
      - none (pure mapping)

state:
  - none (stateless translator functions)

preconditions:
  - events reference a valid active turn

postconditions:
  - stage chips progress deterministically: Pending → Running → Completed | Failed | Skipped
  - streaming segments append in order; ellipsis visible while Running

invariants:
  - one running stage per turn
  - upstream failure halts downstream progression for the turn
  - append-only output composition

failure_modes:
  - event_mismatch :: unknown stage or turn :: drop event and log warning

policies:
  - no retries or reordering; events are applied as received
  - cancellation handled upstream; adapter does not synthesize events

never:
  - mutate historical turns beyond allowed status transitions
  - expose raw system prompt

non_goals:
  - persistence or recovery of lost events

performance:
  - apply event under 5ms per update

observability:
  - logs:
      - trace_id, session_id, turn_id, stage, event_type, applied=true|false, reason
  - metrics:
      - observer_event_applied_count, observer_event_dropped_count

output:
  - minimal implementation only (no commentary, no TODOs)
