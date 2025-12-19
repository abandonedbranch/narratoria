## spec: narration-pipeline-factory

mode:
  - compositional (constructs per-request pipeline instances from DI-provided middleware; owns no persistence)

behavior:
  - what: Compose a per-submission NarrationPipelineService from DI-provided middleware, inserting attachment ingestion steps when requested and wiring the provided observer.
  - input:
      - NarrationPipelineBuildRequest : composition request
        - NarrationPipelineBuildRequest: record {
            Guid SessionId;
            Guid TurnId;
            TraceMetadata Trace;
            IReadOnlyList<NarrationStageKind> StageOrder;
            IReadOnlyList<string> AttachmentIds;
            INarrationPipelineObserver Observer;
            IStageMetadataProvider? StageMetadata;
          }
  - output:
      - NarrationPipelineService : pipeline instance to be used exactly once for the submission
  - caller_obligations:
      - provide a non-null Observer that is already turn-scoped (events applied to the active turn) or otherwise safe for concurrent turns
      - ensure any AttachmentIds have corresponding raw bytes written to IAttachmentUploadStore prior to running the returned pipeline
      - provide StageOrder whose NarrationStageKind.Name values match the middleware telemetry stage ids (identity; no mapping)
  - side_effects_allowed:
      - none (composition only; returned pipeline performs side effects when executed)

state:
  - none

preconditions:
  - request.SessionId is non-empty
  - request.TurnId is non-empty
  - request.StageOrder is non-empty and unique
  - request.Observer is non-null

postconditions:
  - returned pipeline contains the DI base chain in order: persistence → system prompt → content guardian → provider dispatch
  - when request.AttachmentIds is non-empty, one attachment ingestion middleware is inserted per attachment id between content guardian and provider dispatch
  - returned pipeline uses DI-provided provider dispatch and does not substitute providers

invariants:
  - the factory has exactly one public member:
      - NarrationPipelineService Create(NarrationPipelineBuildRequest request)
  - composition is deterministic: the same request yields the same middleware order
  - attachment middleware insertion preserves relative ordering of attachments as provided

failure_modes:
  - invalid_request :: missing required fields :: throw ArgumentException/ArgumentNullException; no pipeline returned
  - missing_dependency :: DI missing required middleware/service :: throw InvalidOperationException; no pipeline returned

policies:
  - concurrency: factory is thread-safe and may be used concurrently
  - cancellation:
      - composition does not observe CancellationToken and never performs IO
      - NarrationPipelineService.RunAsync accepts a CancellationToken; this token is propagated through all middleware, including attachment ingestion and provider dispatch

never:
  - perform IO, persistence, or provider calls during composition
  - create fake providers or fallback pipelines
  - reorder base middleware stages

non_goals:
  - stage-id to NarrationStageKind mapping
  - attachment upload UI or writing to upload store
  - persistence schema configuration

performance:
  - Create completes under 2ms on target hardware for up to 10 attachments

observability:
  - logs:
      - trace_id, request_id, session_id, turn_id, attachments_count, event (pipeline_composed), status
  - metrics:
      - pipeline_composed_count

output:
  - minimal implementation only (no commentary, no TODOs)
