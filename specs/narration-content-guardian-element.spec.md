## spec: narration content guardian element

mode:
  - compositional

behavior:
  - what: Inject a safety/system prompt that requires the downstream provider to act as a “Narratoria Content Guardian” that inspects and cleans mature/NSFW context before narration.
  - input:
      - NarrationContext: SessionId, PlayerPrompt, PriorNarration, WorkingNarration, Metadata, Trace.
      - WorkingContextSegments: ordered context segments accumulated for the provider request (player prompt, attachments, narration history).
  - output:
      - MiddlewareResult: Downstream result with NarrationContext updated to include the content-guardian system prompt segment prepended to WorkingContextSegments.
  - caller_obligations:
      - Register this element before provider_dispatch and after any elements that build WorkingContextSegments.
      - Supply a CancellationToken.
  - side_effects_allowed:
      - Mutate the flowing NarrationContext (immutable copy) to prepend the system prompt segment and update Metadata.
      - Emit structured logs/metrics only; no external IO.

state:
  - none: stateless element

context:
  - WorkingContextSegments:
      - Ordered ImmutableArray<ContextSegment> representing the prompt passed to provider_dispatch.
      - ContextSegment: { Role: system | instruction | user | attachment | history, Content: string, Source: string }.
  - mutation:
      - Prepend a single system ContextSegment containing the content-guardian prompt; preserve all existing segments and their order after the insertion.

preconditions:
  - WorkingContextSegments exists (may be empty) on NarrationContext or in Metadata under a reserved key.
  - CancellationToken is not already canceled.

postconditions:
  - On success, WorkingContextSegments begins with the content-guardian system prompt segment; remaining segments retain their original order and content.
  - Metadata marks the content-guardian prompt as applied (e.g., metadata key `content_guardian_applied=true`).
  - MiddlewareResult.StreamedNarration is passed through unchanged; UpdatedContext carries the modified WorkingContextSegments.
  - On failure, emit a structured NarrationPipelineError (stage=content_guardian_injection) and do not invoke downstream elements.

invariants:
  - Content-guardian prompt is inserted exactly once per pipeline run.
  - PlayerPrompt, PriorNarration, WorkingNarration, and existing context segments are not mutated or dropped.
  - Deterministic ordering: insertion is stable given the same inputs.
  - Thread-safe and side-effect free beyond context mutation and observability hooks.

failure_modes:
  - ContextMissing :: WorkingContextSegments unavailable or null :: emit NarrationPipelineError (stage=content_guardian_injection) and short-circuit.
  - Cancellation :: CancellationToken signaled :: propagate cancellation without invoking downstream elements.

policies:
  - Ordering: must execute before provider_dispatch; should follow any elements that build WorkingContextSegments (attachments, history, templating, system prompt).
  - Idempotency: if Metadata indicates content-guardian prompt already applied, skip reinsertion.
  - Retry: none; failures are terminal for the pipeline run.
  - Concurrency: safe under concurrent sessions; no shared mutable state.
  - Cancellation: check token before and after context mutation.

never:
  - Log or emit raw player content or the system prompt text.
  - Persist the system prompt content to session storage.
  - Invoke the narration provider or perform network/file IO.

non_goals:
  - Selecting or authoring other prompts; this element only inserts the fixed content-guardian prompt.
  - Provider selection, persistence, or attachment ingestion.

performance:
  - Insertion is in-memory and O(n) over segment count; target <5ms per invocation with minimal allocations.

observability:
  - logs:
      - trace_id, request_id, session_id, stage (content_guardian_injection), status, error_class, elapsed_ms
  - metrics:
      - content_guardian_injection_count (by status/error_class), content_guardian_injection_latency_ms

output:
  - minimal implementation only (no commentary, no TODOs)
