## spec: attachment-context-injection-element

mode:
  - compositional (inserts processed attachment summaries into the flowing narration context; no owned state)

behavior:
  - what: Inject staged processed attachment summaries into `WorkingContextSegments` before provider dispatch.
  - input:
      - NarrationContext Context : { SessionId, PlayerPrompt, PriorNarration, WorkingNarration, Metadata, Trace }
      - ImmutableArray<ContextSegment> WorkingContextSegments : ordered segments accumulated for provider dispatch
      - INarrationProcessedAttachmentStore Store : provides staged processed attachments for SessionId
      - CancellationToken CancellationToken
  - output:
      - MiddlewareResult Result : downstream result with NarrationContext updated so `WorkingContextSegments` includes attachment segments
  - caller_obligations:
      - register this element before provider_dispatch and after any elements that ensure WorkingContextSegments exists
      - ensure StageOrder includes `attachment_context_injection` when stage chips are rendered
      - propagate CancellationToken
  - side_effects_allowed:
      - read staged processed attachments from store
      - mutate flowing NarrationContext via immutable copy
      - emit structured logs and metrics

state:
  - none

preconditions:
  - Context.SessionId is a non-empty Guid
  - WorkingContextSegments exists (may be empty)

postconditions:
  - when there are staged processed attachments, WorkingContextSegments contains one attachment segment per staged attachment in CreatedAt ascending order
  - when there are no staged attachments, the element performs no segment insertion and MAY emit Skipped telemetry
  - downstream elements receive the updated WorkingContextSegments

invariants:
  - telemetry stage id emitted by this element is `attachment_context_injection`
  - raw attachment bytes are never loaded or included; only processed attachment NormalizedText is injected
  - attachment segment ordering is deterministic for a given store state
  - attachment injection is idempotent per pipeline run: segments are inserted at most once
  - ContextSegment structure: { Role: system | instruction | user | attachment | history, Content: string, Source: string }
  - inserted attachment segments use Role=attachment and Source includes AttachmentId and FileName in a stable, non-sensitive format

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
