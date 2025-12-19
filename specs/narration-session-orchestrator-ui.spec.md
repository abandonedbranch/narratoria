## spec: narration-session-orchestrator-ui

mode:
  - stateful (coordinates child components and session state; no external persistence beyond collaborators)

behavior:
  - what: Compose attachments dropzone, prompt input bar, and pipeline log; restore turns from storage; submit prompts; and update the log via observer.
  - input:
      - INarrationSessionStore : collaborator to load/save session turns
      - INarrationPipelineFactory : collaborator to compose per-submission pipelines
      - IAttachmentUploadStore : collaborator to store raw attachment bytes before ingestion
      - IReadOnlyList<NarrationStageKind> StageOrder : canonical stage order
  - output:
      - RenderFragment : composed UI containing dropzone, prompt bar, and log
  - caller_obligations:
      - provide a session identifier and load initial turns from store
      - supply StageOrder consistent with pipeline configuration
  - side_effects_allowed:
      - load and save turns via INarrationSessionStore
      - write accepted attachments to IAttachmentUploadStore
      - invoke pipeline factory with prompt and attachments

state:
  - turns : IReadOnlyList<NarrationPipelineTurnView> | append-only
  - is_submitting : bool | gating for prompt bar
  - staged_attachments : IReadOnlyList<AttachmentUploadCandidate> | ephemeral (from attachments-dropzone-ui OnAccepted)

preconditions:
  - StageOrder non-empty and unique
  - session store accessible

postconditions:
  - restored turns render in log; new submissions append turns in chronological order
  - observer events update the latest turn’s stage statuses and output segments deterministically

invariants:
  - log remains append-only; prior turns are not mutated
  - only one stage per turn may be Running at a time
  - upstream failures halt downstream chips for that turn

failure_modes:
  - store_error :: load/save failure :: show banner; keep UI usable; do not drop current state
  - submission_error :: pipeline returns fault :: show banner; do not append successful turn
  - cancelled :: cancellation requested :: stop submission; keep prompt text

policies:
  - serialized submissions: one prompt in-flight at a time
  - cancellation: propagate cancellation to upload-store writes and pipeline execution
  - idempotency: avoid duplicate submissions via gating

never:
  - expose system prompt text or hidden configuration
  - reorder historical turns

non_goals:
  - complex session management (multi-user presence)
  - theming beyond basic state styles

performance:
  - restore and first render under 150ms for 50 turns

observability:
  - logs:
      - trace_id, session_id, event (restore|submit|observer_update|save), status, error_class, elapsed_ms
  - metrics:
      - session_restore_ms, prompt_submit_count, observer_update_count

output:
  - minimal implementation only (no commentary, no TODOs)
