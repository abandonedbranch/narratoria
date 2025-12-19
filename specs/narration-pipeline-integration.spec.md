## spec: narration-pipeline-integration

mode:
  - compositional (coordinates DI-built pipeline, attachment ingestion, and log metadata without owning persistence)

behavior:
  - what: Route prompt submissions through a DI-resolved pipeline factory, prepend attachment ingestion when attachments are staged, and emit hover metadata keyed to UI turns and stage order.
  - input:
    - INarrationPipelineFactory : DI-resolved factory that composes a per-submission NarrationPipelineService
      - arguments:
        - NarrationPipelineBuildRequest : composition request provided to INarrationPipelineFactory.Create
          - IReadOnlyList<NarrationStageKind> StageOrder : canonical UI stage order
          - CancellationToken CancellationToken : caller-provided cancellation
          - Guid SessionId : active session identifier
    - CancellationToken : caller-provided cancellation
    - Guid SessionId : active session identifier
    - Guid TurnId : per-turn identifier used for UI log lookup
    - string Prompt : user-supplied prompt text
    - TraceMetadata Trace : trace identifiers
    - IStageMetadataProvider : hover aggregation keyed by turn id and stage kind
  - output:
      - NarrationPipelineTurnView : append-only turn with stage statuses, output stream, and hover metadata
  - caller_obligations:
    - supply a DI-resolved INarrationPipelineFactory; do not construct fallback pipelines
    - provide StageOrder and a deterministic mapping between pipeline telemetry stage ids and NarrationStageKind values
    - for each accepted attachment, write raw content into IAttachmentUploadStore before invoking the pipeline
      - propagate CancellationToken for submit and streaming
  - side_effects_allowed:
      - invoke attachment ingestion ahead of provider dispatch when attachments exist
      - persist narration context via persistence middleware
      - stream narration tokens to UI and update hover metadata

state:
  - turn : NarrationPipelineTurnView | per-submission in-memory state
  - hovers : IReadOnlyDictionary<(Guid, NarrationStageKind), NarrationStageHover> | recomputed snapshots

preconditions:
  - StageOrder is non-empty, unique, and mapped to pipeline telemetry stage ids via a deterministic mapping
  - attachments, if any, are accepted and written to IAttachmentUploadStore for SessionId prior to pipeline invocation
  - DI container is initialized with required middleware and services, including INarrationPipelineFactory

postconditions:
  - submissions invoke a DI-composed pipeline exactly once; no fallback provider is used
  - attachment ingestion middleware runs before provider dispatch when attachments are staged; failures short-circuit with surfaced error
  - stage telemetry and provider metrics populate hovers keyed by (TurnId, StageKind) matching UI lookup
  - stage status transitions reflect mapped pipeline telemetry; streaming tokens append to the latest turn only

invariants:
  - one active submission at a time per orchestrator instance; log is append-only
  - hover keys use TurnId (not SessionId) and NarrationStageKind.Name for lookup
  - attachment ingestion precedes provider dispatch when attachments exist; when none, ingestion is skipped
  - middleware order remains: persistence → system prompt → content guardian → attachment ingestion (per attachment) → provider dispatch
  - turn scoping: the UI integration MUST translate pipeline events keyed by SessionId into UI updates keyed by TurnId without requiring changes to NarrationStageTelemetry
  - stage mapping is deterministic and total for rendered stages: every telemetry stage id that should update the UI MUST map to exactly one NarrationStageKind in StageOrder

failure_modes:
  - ingestion_error :: attachment ingestion fails or rejects :: pipeline short-circuits; turn marks failed; hovers include error_class
  - stage_mismatch :: middleware stage name not found in StageOrder :: drop event; log warning; chip remains Pending
  - missing_pipeline :: DI NarrationPipelineService missing :: throw structured error before submission
  - cancellation :: caller token canceled :: stop streaming; mark running stage canceled/failed; do not mutate prior turns

policies:
  - cancellation: propagate caller token through ingestion and pipeline; stop streaming promptly
  - timeout: honor provider dispatch timeout from ProviderDispatchOptions
  - idempotency: submissions are gated so duplicate clicks while submitting are ignored
  - concurrency: serialized submissions; no parallel runs per orchestrator instance

never:
  - construct or use fake/fallback pipelines when DI service is available
  - bypass attachment ingestion when attachments are present
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
