## spec: narration-pipeline-integration

mode:
  - compositional (coordinates DI-built pipeline, attachment ingestion, and log metadata without owning persistence)

behavior:
  - what: Route prompt submissions through a DI-resolved pipeline factory, inject already-staged attachment summaries as narration context, and emit hover metadata keyed to UI turns and stage order.
  - input:
      - INarrationPipelineFactory : DI-resolved factory that composes a per-submission NarrationPipelineService
        - Create(buildRequest: NarrationPipelineBuildRequest) : NarrationPipelineService
      - INarrationProcessedAttachmentStore : store providing staged attachment summaries for the active session
      - IStageMetadataProvider : hover aggregation keyed by turn id and stage kind
      - IReadOnlyList<NarrationStageKind> StageOrder : canonical pipeline stage order rendered by the UI; MUST match telemetry stage ids exactly
        - recommended_default (NarrationStageKind.Name values):
            - session_load
            - system_prompt_injection
            - content_guardian_injection
            - attachment_context_injection
            - provider_dispatch
            - persist_context
        - notes:
            - persist_context is expected to complete after downstream streaming ends (it is rendered last)
            - attachment_context_injection represents inclusion of all staged attachments for the session
      - Guid SessionId : active session identifier
      - Guid TurnId : per-turn identifier used for UI log lookup
      - string Prompt : user-supplied prompt text
      - TraceMetadata Trace : trace identifiers
      - CancellationToken : caller-provided cancellation
      - NarrationPipelineBuildRequest : factory argument constructed by this integration step (see narration-pipeline-factory)
        - Guid SessionId
        - Guid TurnId
        - TraceMetadata Trace
        - IReadOnlyList<NarrationStageKind> StageOrder
        - IReadOnlyList<string> AttachmentIds
        - INarrationPipelineObserver Observer (turn-scoped)
        - IStageMetadataProvider? StageMetadata
  - output:
      - NarrationPipelineTurnView : append-only turn with stage statuses, output stream, and hover metadata
  - caller_obligations:
      - supply a DI-resolved INarrationPipelineFactory; do not construct fallback pipelines
      - provide StageOrder whose NarrationStageKind.Name values exactly match the telemetry stage ids emitted by the configured middleware
      - propagate CancellationToken for submit and streaming
  - side_effects_allowed:
      - read staged processed attachments for SessionId and provide their identifiers to the pipeline factory
      - persist narration context via persistence middleware
      - stream narration tokens to UI and update hover metadata

state:
  - turn : NarrationPipelineTurnView | per-submission in-memory state
  - hovers : IReadOnlyDictionary<(Guid, NarrationStageKind), NarrationStageHover> | recomputed snapshots

preconditions:
  - StageOrder is non-empty and unique
  - every middleware stage id that should be visualized is present in StageOrder exactly once
  - attachments, if any, are accepted and provide a readable stream via AttachmentUploadCandidate.OpenRead
  - DI container is initialized with required middleware and services, including INarrationPipelineFactory

postconditions:
  - submissions invoke a DI-composed pipeline exactly once; no fallback provider is used
  - when staged attachments exist for SessionId, the integration supplies their processed attachment identifiers (ProcessedAttachment.AttachmentId values from INarrationProcessedAttachmentStore) to the pipeline so they are included as context
  - stage telemetry and provider metrics populate hovers keyed by (TurnId, StageKind) matching UI lookup
  - stage status transitions reflect mapped pipeline telemetry; streaming tokens append to the latest turn only

invariants:
  - one active submission at a time per orchestrator instance; log is append-only
  - hover keys use TurnId (not SessionId) and NarrationStageKind.Name for lookup
  - attachment context injection precedes provider dispatch when staged attachments exist; when none, it is skipped
  - middleware order remains: persistence → system prompt → content guardian → attachment context injection → provider dispatch
  - turn scoping: the UI integration MUST translate pipeline events keyed by SessionId into UI updates keyed by TurnId without requiring changes to NarrationStageTelemetry
  - stage identity is total for rendered stages: every telemetry stage id that should update the UI MUST equal exactly one NarrationStageKind.Name in StageOrder

failure_modes:
  - attachments_load_error :: staged attachments cannot be loaded :: proceed with no attachments; log warning; chip remains Skipped
  - stage_mismatch :: middleware stage name not found in StageOrder :: drop event; log warning; chip remains Pending
  - missing_pipeline :: DI NarrationPipelineService missing :: throw structured error before submission
  - cancellation :: caller token canceled :: stop streaming; mark running stage Canceled; do not mutate prior turns

policies:
  - cancellation: propagate caller token through attachment load and pipeline; stop streaming promptly
  - timeout: honor provider dispatch timeout from ProviderDispatchOptions
  - idempotency: submissions are gated so duplicate clicks while submitting are ignored
  - concurrency: serialized submissions; no parallel runs per orchestrator instance

never:
  - construct or use fake/fallback pipelines when DI service is available
  - bypass staged attachments when they are available (except when attachments cannot be loaded)
  - key hover metadata by SessionId
  - mutate historical turns after append

non_goals:
  - multi-session orchestration
  - attachment upload UX or storage quota policy definitions
  - retry strategies beyond provider timeout handling

performance:
  - hover aggregation per event under 5ms
  - submission setup (creating turn, wiring observer/metadata) under 10ms

observability:
  - logs:
      - trace_id, request_id, session_id, turn_id, stage, status, error_class, elapsed_ms, attachments_count
  - metrics:
      - prompt_submit_count (by status), attachment_ingestion_count (by status/error_class), stage_hover_emitted_count, provider_latency_ms

output:
  - minimal implementation only (no commentary, no TODOs)
