## spec: compose-bar-ui

mode:
  - stateful (tab-scoped ephemeral input; delegates persistence/ingestion/narration to collaborators)

behavior:
  - what: Provide a fixed bottom compose bar that unifies prompt input and attachment drag-and-drop, immediately creates a session on first meaningful action, ingests dropped attachments immediately, and submits prompts to the narration pipeline.
  - input:
      - INarrationSessionStore : creates sessions and resolves current session metadata
      - INarrationAttachmentIngestionService : ingests dropped attachments (stage id `attachment_ingestion`)
      - INarrationProcessedAttachmentStore : lists staged processed attachments and deletes them on user request
      - Func<Guid, string, CancellationToken, ValueTask> SubmitPrompt : submits a prompt for SessionId
      - Guid TabId : active tab identity (workspace or session)
      - Guid? SessionId : active session identity (null when workspace tab has not transitioned)
      - IReadOnlyList<string> AllowedContentTypes : file allowlist (MUST align with ingestion supported types; recommended_default: text/plain, text/markdown)
      - long MaxBytesPerFile
      - long MaxBytesTotal
      - CancellationToken
  - output:
      - RenderFragment : unified compose UI
  - caller_obligations:
      - provide SubmitPrompt that runs the narration pipeline for the provided SessionId
      - propagate CancellationToken
      - provide store/service collaborators via DI
  - side_effects_allowed:
      - create a new session on first drop or first send when SessionId is null
      - ingest newly dropped files immediately (per file)
      - delete staged processed attachments on explicit remove action
      - invoke SubmitPrompt exactly once per send action

state:
  - input_text : string | tab-scoped ephemeral
  - new_uploads : IReadOnlyList<AttachmentUploadCandidate> | tab-scoped ephemeral (pending ingestion)
  - staged_attachments : IReadOnlyList<ProcessedAttachment> | session-scoped snapshot
  - is_ingesting : bool | tab-scoped ephemeral
  - is_submitting : bool | tab-scoped ephemeral
  - attachment_errors : IReadOnlyDictionary<string, string> | tab-scoped ephemeral keyed by AttachmentId

preconditions:
  - AllowedContentTypes is non-empty
  - limits are non-negative

postconditions:
  - drop behavior:
      - dropping valid files adds "new" chips immediately
      - if SessionId is null, the compose bar creates a new session and transitions to that SessionId before ingestion begins
      - ingestion runs immediately for each new chip and on success converts it to a staged processed attachment (persisted)
  - send behavior:
      - if SessionId is null, sending creates a session before submitting
      - sending is disabled while is_ingesting is true
      - sending appends a narration turn in scrollback via the caller (outside this component)

invariants:
  - no drafts: a session is created and persisted on first meaningful action (drop or send)
  - staged attachments always apply to subsequent narration runs; there is no per-prompt toggle
  - removing a staged attachment purges it from the processed attachment store

failure_modes:
  - validation_error :: dropped files violate allowlist/limits :: do not ingest; show per-file error
  - ingestion_error :: ingestion fails for a file :: keep chip with error state and allow remove
  - store_error :: session create fails :: show banner; keep user input intact
  - submission_error :: SubmitPrompt fails :: show banner; keep input intact
  - cancelled :: token signaled :: abort ingestion/submission; keep input and chips

policies:
  - gating:
      - Send button is disabled while is_ingesting or is_submitting
      - drop actions are allowed while not ingesting; if ingesting, new drops queue behind current ingestion
  - idempotency:
      - repeated send clicks while disabled have no effect
      - repeated remove clicks are idempotent
  - cancellation:
      - propagate CancellationToken to ingestion and prompt submission

never:
  - store raw attachment bytes in persistent storage
  - submit prompts while ingestion is in-flight
  - expose system prompts or hidden configuration

non_goals:
  - per-prompt inclusion toggles
  - rich markdown editor

performance:
  - per-keystroke UI updates under 16ms

non_functional_requirements:
  - accessibility (WCAG 2.2 AA):
    - unified controls: submit and attachment actions have accessible names; disabled states expose `aria-disabled`
    - errors: ingestion/submission errors announced via `aria-live="assertive"`
    - dropzone: keyboard and clickable file-picker fallback; constraints described via `aria-describedby`
  - responsive_ux:
    - fixed positioning: compose bar remains reachable without overlapping content at all breakpoints
    - target_sizes: all controls ≥44x44 px; chips wrap; long filenames truncate with accessible tooltip
  - performance_budgets:
    - per-keystroke ≤16ms; submission UI updates ≤50ms
  - testing_hooks:
    - axe-core on pages with compose bar; fail CI on violations
    - keyboard-only send and upload fallback
    - viewport matrix assertions

observability:
  - logs:
      - trace_id, event (drop|ingest_start|ingest_done|remove|send), tab_id, session_id, attachment_id, status, error_class, elapsed_ms
  - metrics:
      - compose_send_count (by status), compose_ingest_count (by status), compose_send_blocked_count

output:
  - minimal implementation only (no commentary, no TODOs)
