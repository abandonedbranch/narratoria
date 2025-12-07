## spec: narration persistence middleware

mode:
  - compositional

behavior:
  - what: Load the session narration context, invoke downstream middleware, then persist the merged context after narration streaming completes.
  - input:
      - NarrationContext: SessionId, PlayerPrompt, PriorNarration, WorkingNarration, Metadata, Trace.
  - output:
      - MiddlewareResult: Downstream stream wrapped to persist on completion plus the persisted NarrationContext.
  - caller_obligations:
      - Provide INarrationSessionStore and register this middleware ahead of provider dispatch so loading occurs before provider calls and persistence wraps the result.
      - Supply a CancellationToken and initial NarrationContext containing SessionId, PlayerPrompt, and Trace.
      - Propagate pipeline observability hooks (observer/metrics) if desired.
  - side_effects_allowed:
      - Read session context from the store; write updated context back after downstream completion.
      - Emit structured logs/metrics via the pipeline observer.

state:
  - none: uses the injected session store; owns no internal cache.

preconditions:
  - SessionId resolves to an existing stored context.
  - CancellationToken is not canceled at invocation.

postconditions:
  - On success, the stored context is updated with PlayerPrompt, merged PriorNarration + WorkingNarration, WorkingNarration cleared, and Trace/Metadata refreshed.
  - On MissingSession or persistence failure, a structured NarrationPipelineError is emitted and the pipeline short-circuits with an exception.

invariants:
  - Session scoping is preserved; no cross-session reads/writes.
  - PriorNarration is only appended with WorkingNarration; existing narration is not reordered or removed.
  - Context is loaded once per run and persisted once after downstream completion.
  - Deterministic ordering: executes exactly where registered; thread-safe under concurrent sessions.

failure_modes:
  - MissingSession :: no stored context for SessionId :: emit NarrationPipelineError(stage=session_load) and short-circuit.
  - PersistenceError :: store write fails :: emit NarrationPipelineError(stage=persist_context) and short-circuit.
  - Cancellation :: CancellationToken is signaled during load or save :: propagate cancellation without persisting.

policies:
  - Ordering: should wrap provider dispatch and any context-mutating middleware so persistence captures final state.
  - Idempotency: one load and one persist per run; caller must re-run to retry.
  - Retry: none implicit; caller may retry externally.
  - Concurrency: safe for concurrent sessions; underlying store must enforce its own consistency guarantees.
  - Cancellation: honor CancellationToken around load, downstream invocation, and persistence.

never:
  - Create new sessions when missing.
  - Persist partial WorkingNarration before downstream streaming completes.
  - Mutate SessionId or drop narration history.

non_goals:
  - Session creation, compaction, or archival.
  - Provider calls or prompt templating.

performance:
  - Persistence latency bounded by store performance; avoid blocking streaming except for post-stream save.

observability:
  - logs:
      - trace_id, request_id, session_id, stage (session_load|persist_context), elapsed_ms, status, error_class
  - metrics:
      - persistence_latency_ms, persistence_error_count (by error_class)

output:
  - minimal implementation only (no commentary, no TODOs)
