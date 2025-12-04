## spec: narrator pipeline service

mode:
  - compositional

behavior:
  - what: Transform a player prompt into a streamed narration response using the configured provider.
  - input:
      - SessionId: Identifier for the active session.
      - PlayerPrompt: Raw player prompt for the active session.
  - output:
      - StreamedNarration: Streamed narration response for the active session.
  - caller_obligations:
      - Provide valid session identifier and ensure session state is persisted/retrievable.
      - Supply cancellation token for the pipeline execution.
  - side_effects_allowed:
      - Reads prior session state; writes updated session context.
      - Structured logging/metrics emission.

state:
  - session_state: persisted per-session context

context:
  - NarrationContext:
      - SessionId
      - PlayerPrompt
      - PriorNarration: ordered narration already emitted for the session
      - WorkingNarration: narration collected during the current run
      - Metadata: trace identifiers and provider configuration
  - mutation:
      - Middleware receives a NarrationContext and may mutate it; the mutated context flows to the next stage.
      - Only the final context from the completed chain is eligible for persistence.

preconditions:
  - Session exists or can be created for the SessionId.
  - Provider configuration is available.

postconditions:
  - On success, narration tokens are streamed for the session and session context is updated.
  - On failure, a structured pipeline lifecycle error event is emitted.

invariants:
  - Every step of the pipeline is semantically logged.
  - LLM context is always derived from persisted session state.
  - LLM context contains only player input and prior narration output.
  - Middleware ordering is deterministic and preserves registration order.
  - Pipeline is thread-safe under concurrent callers.

failure_modes:
  - ProviderError :: provider call fails :: emit structured pipeline error event and stop streaming
  - MissingSession :: session state cannot be retrieved :: emit structured session error and stop
  - DecodeError :: provider response cannot be decoded :: emit structured decode error and stop streaming

policies:
  - Pipeline is constructed as middleware chain; each middleware takes NarrationContext, a next delegate, and CancellationToken.
  - MiddlewareResult contains StreamedNarration and updated NarrationContext.
  - Middleware must call next unless intentionally short-circuiting with a failure result.
  - Middleware execution order follows registration; built-in required middleware is appended in fixed order.
  - Required middleware stages:
      - provider_dispatch: transforms context into provider request and streams narration tokens.
      - persist_context: writes the completed transformed NarrationContext (including new narration) to storage.
  - Lifecycle listeners may attach at any time.
  - Pipeline stages may be added dynamically.
  - Concurrency: thread-safe execution under concurrent callers.
  - Cancellation: honor CancellationToken on all async operations.

never:
  - Emit narration without session scoping.
  - Omit a pipeline lifecycle error event on failure.

non_goals:
  - Provider selection logic beyond the configured provider.
  - Prompt authoring or templating.

performance:
  - Respect configured timeout per provider call.
  - Latency SLO defined by provider timeout and pipeline budget.

observability:
  - logs:
      - trace_id, request_id, session_id, stage, elapsed_ms, status, error_class
  - metrics:
      - pipeline_stage_latency_ms, pipeline_error_count (by error_class), tokens_streamed

output:
  - minimal implementation only (no commentary, no TODOs)
