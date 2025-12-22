## spec: narration provider dispatch element

mode:
  - compositional

behavior:
  - what: Invoke the configured narration provider to stream narration tokens, capture WorkingNarration, and pass the streaming result downstream.
  - input:
      - NarrationContext: SessionId, PlayerPrompt, PriorNarration, WorkingNarration, Metadata, Trace.
      - ProviderDispatchOptions: timeout budget for the provider call.
  - output:
      - MiddlewareResult: Streamed narration tokens plus updated NarrationContext with WorkingNarration set to streamed tokens.
  - caller_obligations:
      - Register element after context-building elements (system prompt, attachments, templating) and inside the persistence wrapper (so persistence loads context before provider calls and persists only after streaming completes successfully).
      - Provide an INarrationProvider implementation, ProviderDispatchOptions, and CancellationToken.
      - Propagate observability hooks (observer/metrics).
  - side_effects_allowed:
      - Call external provider APIs to obtain narration tokens.
      - Emit structured logs/metrics.

state:
  - none: stateless aside from per-run timers/counters.

preconditions:
  - Provider is configured and reachable.
  - CancellationToken is not canceled at invocation.

postconditions:
  - On success, narration tokens are streamed to downstream elements/consumers; WorkingNarration reflects streamed tokens.
  - On failure, a structured NarrationPipelineError is emitted and the stream terminates faulted; downstream stops receiving tokens.

streaming_contract:
  - This element is a streaming source (provider) that returns immediately with:
      - StreamedNarration: an async stream of tokens.
      - UpdatedContext: a task that completes when streaming completes (success/fault/cancellation).
  - Downstream elements may be invoked during composition so they can wrap/observe the stream; this does not imply tokens were produced.
  - If the returned stream is canceled or disposed early by the caller, the provider call must be canceled promptly.

invariants:
  - Provider timeout is enforced per run.
  - Tokens are forwarded in the order received; no reordering or mutation.
  - Deterministic ordering: executes exactly where registered; thread-safe under concurrent sessions.
  - Cancellation is honored between tokens.

failure_modes:
  - ProviderError :: provider call fails or returns an error :: emit NarrationPipelineError(stage=provider_dispatch) and short-circuit.
  - ProviderTimeout :: call exceeds ProviderDispatchOptions.Timeout :: emit NarrationPipelineError(stage=provider_dispatch) and short-circuit.
  - DecodeError :: provider response cannot be decoded :: emit NarrationPipelineError(stage=provider_dispatch) and short-circuit.
  - Cancellation :: CancellationToken is signaled :: propagate cancellation and stop streaming.

policies:
  - Timeout: enforce ProviderDispatchOptions.Timeout (or infinite when configured).
  - Retry: none; caller may compose a retrying provider if desired.
  - Ordering: should be terminal inside the persistence wrapper so persistence can load context before provider calls and persist only after the stream completes successfully.
  - Cancellation: check token before/after provider calls and between token writes.
  - Idempotency: none; each invocation triggers a provider call.

never:
  - Persist session context or modify PriorNarration.
  - Log raw token contents beyond necessary debugging/metrics.
  - Swallow provider errors without emitting a structured error.

non_goals:
  - Provider selection or prompt templating.
  - Persistence or attachment handling.

performance:
  - Streaming latency bounded by provider response time; middleware overhead limited to buffering and token fan-out.

observability:
  - logs:
      - trace_id, request_id, session_id, stage (provider_dispatch), elapsed_ms, status, error_class
  - metrics:
      - provider_latency_ms, provider_error_count (by error_class), tokens_streamed

output:
  - minimal implementation only (no commentary, no TODOs)
