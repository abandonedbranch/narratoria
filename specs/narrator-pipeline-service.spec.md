## spec: narrator pipeline service

mode:
  - compositional

behavior:
  - what: Execute the registered narration pipeline element chain over a NarrationContext and return the downstream result.
  - input:
      - NarrationContext: session-scoped context carrying player prompt, prior narration, working narration, metadata, and trace.
  - output:
      - MiddlewareResult: streamed narration (if any) plus updated NarrationContext from the terminal element.
  - caller_obligations:
      - Provide the element chain in the intended order (including persistence/provider elements as needed).
      - Supply an initial NarrationContext and a CancellationToken.
      - Compose the chain at application startup via DI (e.g., Program.cs) and invoke it from the UI/Blazor interaction flow (no HTTP endpoint required).
  - side_effects_allowed:
      - None by the pipeline itself; all effects are owned by elements.

state:
  - none: pipeline owns no state beyond the flowing NarrationContext.

preconditions:
  - Element chain is registered (may be empty).
  - CancellationToken is provided.
  - NarrationContext is non-null.

postconditions:
  - Elements execute in registration order until completion or short-circuit; the final MiddlewareResult is returned to the caller.
  - If an element throws or short-circuits, the pipeline surfaces that result/exception without additional mutation.

streaming_contract:
  - model: streaming async-enumerable pipeline (two-phase: composition + execution).
  - note: “GStreamer-like” is an intuition aid only; the rules below are normative.
  - phases:
      - composition: elements may be invoked to construct a StreamedNarration pipeline (IAsyncEnumerable) and an UpdatedContext task.
      - execution: tokens flow only while the returned stream is being consumed.
  - normative_rules:
    - Composition MAY invoke downstream elements to obtain StreamedNarration/UpdatedContext; this does not imply tokens were produced.
    - Execution begins when the caller enumerates StreamedNarration; tokens MUST be yielded in the order produced by the active source stage.
    - Errors during execution MUST surface by terminating StreamedNarration faulted (enumeration throws) and by faulting UpdatedContext.
    - A stage failure MUST stop further tokens from being emitted downstream.
    - If the caller cancels the pipeline CancellationToken, all in-flight work MUST stop promptly and no further tokens are emitted.
    - If the caller disposes/stops enumerating StreamedNarration early, upstream streaming work MUST be canceled promptly (via cancellation and/or backpressure).
    - Partial tokens MAY be emitted before a fault; after the first fault, no additional tokens may be emitted.
  - error_propagation:
      - if any stage faults during execution, the stream terminates faulted and downstream stages stop receiving tokens.
      - fault or cancellation should propagate upstream promptly (via cancellation/backpressure) so sources stop producing.
  - cancellation_propagation:
      - canceling the caller token, or disposing the stream enumerator early, should cause in-flight streaming work to stop promptly.

invariants:
  - Element ordering is deterministic and preserves registration order.
  - Pipeline does not perform provider calls, persistence, or context mutation outside elements.
  - Cancellation is propagated to every element invocation.
  - Pipeline execution is thread-safe under concurrent callers.

failure_modes:
  - ElementFailure :: an element throws or returns a failure result :: propagate the exception/result; no additional pipeline-side side effects.
  - Cancellation :: CancellationToken is signaled before or during execution :: stop invoking further elements and propagate cancellation.

policies:
  - Pipeline is constructed as an element chain using a middleware-style composition pattern; each element takes NarrationContext, MiddlewareResult, a next delegate, and CancellationToken.
  - MiddlewareResult contains StreamedNarration and updated NarrationContext.
  - Elements should call next unless intentionally short-circuiting.
  - Concurrency: thread-safe execution under concurrent callers.
  - Cancellation: honor CancellationToken on all async operations.

never:
  - Bake provider dispatch or persistence into the pipeline service.
  - Reorder elements or inject hidden stages.
  - Emit narration without elements explicitly doing so.

non_goals:
  - Provider selection, context persistence, or prompt authoring—those belong to dedicated pipeline elements.

performance:
  - Pipeline overhead is in-memory delegate invocation only; target negligible added latency relative to element work.

observability:
  - logs:
      - trace_id, request_id, session_id, stage, elapsed_ms, status, error_class (emitted by elements or pipeline wrapper if present)
  - metrics:
      - pipeline_stage_latency_ms, pipeline_error_count (by error_class) emitted by elements; pipeline does not add provider tokens metrics.

output:
  - minimal implementation only (no commentary, no TODOs)
