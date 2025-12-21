## spec: attachment-context-injection-middleware

mode:
  - compositional

behavior:
  - what: Inject staged processed attachment summaries into `WorkingContextSegments` before provider dispatch.
  - input:
      - NarrationContext: SessionId, PlayerPrompt, PriorNarration, WorkingNarration, Metadata, Trace.
      - WorkingContextSegments: ordered context segments accumulated for provider dispatch.
      - INarrationProcessedAttachmentStore: provides staged processed attachments for SessionId.
      - CancellationToken
  - output:
      - MiddlewareResult: Downstream result with NarrationContext updated so `WorkingContextSegments` includes attachment segments.
  - caller_obligations:
      - register this middleware before provider_dispatch and after any middleware that ensures WorkingContextSegments exists
      - ensure StageOrder includes `attachment_context_injection` when stage chips are rendered
      - propagate CancellationToken
  - side_effects_allowed:
      - read staged processed attachments from store
      - mutate flowing NarrationContext via immutable copy
      - emit structured logs/metrics; no network IO

state:
  - none

context:
  - StageId:
      - this middleware MUST emit telemetry stage id `attachment_context_injection`
  - WorkingContextSegments:
      - Ordered ImmutableArray<ContextSegment> representing the prompt passed to provider_dispatch.
      - ContextSegment: { Role: system | instruction | user | attachment | history, Content: string, Source: string }.
  - insertion:
      - append one ContextSegment per staged processed attachment with Role=attachment
      - Source MUST include AttachmentId and FileName in a stable, non-sensitive format

preconditions:
  - NarrationContext has a valid SessionId
  - WorkingContextSegments exists (may be empty)

postconditions:
  - when there are staged processed attachments, WorkingContextSegments contains an attachment segment for each staged attachment in CreatedAt ascending order
  - when there are no staged attachments, the middleware performs no segment insertion and MAY emit Skipped telemetry
  - downstream middleware receives the updated WorkingContextSegments

invariants:
  - raw attachment bytes are never loaded or included; only processed attachment NormalizedText is injected
  - attachment segment ordering is deterministic for a given store state
  - attachment injection is idempotent per pipeline run: segments are inserted at most once

failure_modes:
  - ContextMissing :: WorkingContextSegments unavailable :: emit NarrationPipelineError(stage=attachment_context_injection) and short-circuit
  - StoreError :: processed attachment store fails :: emit NarrationPipelineError(stage=attachment_context_injection) and short-circuit
  - Cancellation :: token signaled :: propagate cancellation

policies:
  - ordering:
      - must execute before provider_dispatch
      - should execute after system_prompt_injection and content_guardian_injection
  - idempotency:
      - if Metadata indicates attachment context already injected for this run, skip reinsertion
  - cancellation:
      - check token before and after store load

never:
  - include processed attachment text in logs
  - mutate or persist WorkingContextSegments to session storage

non_goals:
  - attachment ingestion
  - attachment deletion UX

performance:
  - injection under 10ms for up to 20 attachments (excluding store latency)

observability:
  - logs:
      - trace_id, request_id, session_id, stage (attachment_context_injection), status, error_class, elapsed_ms, attachments_count
  - metrics:
      - attachment_context_injection_count (by status), attachment_context_injection_latency_ms

output:
  - minimal implementation only (no commentary, no TODOs)
