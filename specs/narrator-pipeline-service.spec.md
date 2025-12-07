## spec: narrator pipeline service

mode:
  - compositional

behavior:
  - what: Execute the registered narration middleware chain over a NarrationContext and return the downstream result.
  - input:
      - NarrationContext: session-scoped context carrying player prompt, prior narration, working narration, metadata, and trace.
  - output:
      - MiddlewareResult: streamed narration (if any) plus updated NarrationContext from the terminal middleware.
  - caller_obligations:
      - Provide the middleware chain in the intended order (including persistence/provider middleware as needed).
      - Supply an initial NarrationContext and a CancellationToken.
  - side_effects_allowed:
      - None by the pipeline itself; all effects are owned by middleware.

state:
  - none: pipeline owns no state beyond the flowing NarrationContext.

preconditions:
  - Middleware chain is registered (may be empty).
  - CancellationToken is provided.
  - NarrationContext is non-null.

postconditions:
  - Middleware executes in registration order until completion or short-circuit; the final MiddlewareResult is returned to the caller.
  - If middleware throws or short-circuits, the pipeline surfaces that result/exception without additional mutation.

invariants:
  - Middleware ordering is deterministic and preserves registration order.
  - Pipeline does not perform provider calls, persistence, or context mutation outside middleware.
  - Cancellation is propagated to every middleware invocation.
  - Pipeline execution is thread-safe under concurrent callers.

failure_modes:
  - MiddlewareFailure :: a middleware throws or returns a failure result :: propagate the exception/result; no additional pipeline-side side effects.
  - Cancellation :: CancellationToken is signaled before or during execution :: stop invoking further middleware and propagate cancellation.

policies:
  - Pipeline is constructed as a middleware chain; each middleware takes NarrationContext, MiddlewareResult, a next delegate, and CancellationToken.
  - MiddlewareResult contains StreamedNarration and updated NarrationContext.
  - Middleware should call next unless intentionally short-circuiting.
  - Concurrency: thread-safe execution under concurrent callers.
  - Cancellation: honor CancellationToken on all async operations.

never:
  - Bake provider dispatch or persistence into the pipeline service.
  - Reorder middleware or inject hidden stages.
  - Emit narration without the middleware explicitly doing so.

non_goals:
  - Provider selection, context persistence, or prompt authoring—those belong to dedicated middleware.

performance:
  - Pipeline overhead is in-memory delegate invocation only; target negligible added latency relative to middleware work.

observability:
  - logs:
      - trace_id, request_id, session_id, stage, elapsed_ms, status, error_class (emitted by middleware or pipeline wrapper if present)
  - metrics:
      - pipeline_stage_latency_ms, pipeline_error_count (by error_class) emitted by middleware; pipeline does not add provider tokens metrics.

output:
  - minimal implementation only (no commentary, no TODOs)
