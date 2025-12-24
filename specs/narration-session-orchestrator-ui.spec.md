## spec: narration-session-orchestrator-ui

mode:
  - stateful (coordinates child components and session state; no external persistence beyond collaborators)

behavior:
  - what: Compose attachments dropzone, prompt input bar, and pipeline log; restore turns from storage; ingest dropped attachments immediately; submit prompts; and update the log via observer.
  - input:
      - INarrationSessionStore : collaborator to list and upsert persisted session turns (NarrationTurnRecord)
      - INarrationPipelineFactory : collaborator to compose per-submission pipelines
      - INarrationAttachmentIngestionService : collaborator to ingest dropped attachments immediately
      - IReadOnlyList<NarrationStageKind> StageOrder : canonical stage order
        - recommended_default (NarrationStageKind.Name values):
            - session_load
            - system_prompt_injection
            - content_guardian_injection
            - attachment_context_injection
            - provider_dispatch
            - persist_context
  - output:
      - RenderFragment : composed UI containing dropzone, prompt bar, and log
  - caller_obligations:
      - provide a session identifier and load initial turns from store
      - supply StageOrder consistent with the pipeline element stage ids (telemetry stage names)
  - side_effects_allowed:
      - load and save turns via INarrationSessionStore
      - ingest accepted attachments immediately and persist processed summaries
      - invoke pipeline factory with prompt and attachments

state:
  - turns : IReadOnlyList<NarrationPipelineTurnView> | append-only
  - is_submitting : bool | gating for prompt bar
  - is_ingesting_attachments : bool | gating for prompt bar
  - new_attachments : IReadOnlyList<AttachmentUploadCandidate> | ephemeral (dropped, pending ingestion)

preconditions:
  - StageOrder non-empty and unique
  - session store accessible

postconditions:
  - restored turns render in log; new submissions append turns in chronological order
  - observer events update the latest turn’s stage statuses and output segments deterministically
  - each prompt submission appends a new turn whose stage chips are rendered in StageOrder during streaming and completion

invariants:
  - log remains append-only; prior turns are not mutated
  - only one stage per turn may be Running at a time
  - upstream failures halt downstream chips for that turn
  - prompt submission is disabled while is_ingesting_attachments is true

failure_modes:
  - store_error :: load/save failure :: show banner; keep UI usable; do not drop current state
  - submission_error :: pipeline returns fault :: show banner; append a failed turn; persist a final NarrationTurnRecord for replay
  - cancelled :: cancellation requested during an in-flight submission :: stop streaming; append a canceled turn; persist a final NarrationTurnRecord for replay; keep prompt text

policies:
  - serialized submissions: one prompt in-flight at a time
  - cancellation: propagate cancellation to ingestion and pipeline execution
  - idempotency: avoid duplicate submissions via gating

never:
  - expose system prompt text or hidden configuration
  - reorder historical turns

non_goals:
  - complex session management (multi-user presence)
  - theming beyond basic state styles

performance:
  - restore and first render under 150ms for 50 turns

non_functional_requirements:
  - accessibility (WCAG 2.2 AA):
    - focus management: after session creation and prompt submission, focus returns to actionable controls deterministically
    - landmarks: `main` wraps the log; banners announce with `aria-live="assertive"`
    - states: `aria-busy` during in-flight submission/ingestion; controls disabled with `aria-disabled`
  - responsive_ux:
    - layout: compose bar and log stack appropriately on mobile; attachment chips wrap; no overlap with banners
    - target_sizes: controls ≥44x44 px; primary actions remain reachable at all breakpoints
  - performance_budgets:
    - orchestrator route LCP ≤2.5s; TTI ≤2s
    - streaming updates: append ≤50ms; restore ≤150ms for 50 turns
  - testing_hooks:
    - axe-core on orchestrator route; fail CI on violations
    - keyboard traversal across dropzone, input bar, log
    - reduced motion scenario; assert animations disabled
    - viewport matrix assertions

observability:
  - logs:
      - trace_id, session_id, event (restore|submit|observer_update|save), status, error_class, elapsed_ms
  - metrics:
      - session_restore_ms, prompt_submit_count, observer_update_count

output:
  - minimal implementation only (no commentary, no TODOs)
