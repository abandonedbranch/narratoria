## spec: attachments-dropzone-ui

mode:
  - stateful (tracks transient dropped files and validation results; no persistence)

behavior:
  - what: Accept drag-and-drop or file-picker attachments, validate allowlisted MIME/types and size limits, and emit accepted files (with a stream provider) to the caller for downstream upload/ingestion.
  - input:
      - IReadOnlyList<string> AllowedContentTypes : allowlist (recommended_default: text/plain, text/markdown)
      - long MaxBytesPerFile : size limit per file
      - long MaxBytesTotal : cumulative size limit
      - Func<IReadOnlyList<AttachmentUploadCandidate>, CancellationToken, ValueTask> OnAccepted : callback invoked with validated candidates
      - AttachmentUploadCandidate : AttachmentCandidate + stream provider
        - AttachmentId : string
        - FileName : string
        - MimeType : string
        - SizeBytes : long
        - OpenRead : Func<CancellationToken, ValueTask<Stream>>
  - output:
      - RenderFragment : dropzone UI with list of staged files and errors
  - caller_obligations:
      - supply accurate allowlist and limits aligned with ingestion service
      - propagate cancellation
  - side_effects_allowed:
      - invoke OnAccepted exactly once per user acceptance action

state:
  - staged_files : IReadOnlyList<AttachmentUploadCandidate> | ephemeral UI memory
  - validation_errors : IReadOnlyList<string> | ephemeral UI memory

preconditions:
  - AllowedContentTypes non-empty; size limits are non-negative

postconditions:
  - valid files appear in staged_files; invalid files produce entries in validation_errors
  - invoking acceptance emits OnAccepted with current staged_files and clears staged_files on success

invariants:
  - deterministic validation: same files yield same results given the same policy
  - no mutation of file contents; dropzone provides read-only access via a stream provider

failure_modes:
  - validation_error :: disallowed MIME or exceeded limits :: show error and do not emit
  - cancelled :: cancellation_token requested :: do not emit; keep staged_files

policies:
  - no implicit upload: the dropzone MUST NOT write to storage; upload/ingestion happens downstream upon OnAccepted
  - downstream may begin ingestion immediately after OnAccepted returns (in the unified compose bar flow)
  - cancellation: honor token on acceptance

never:
  - write files to storage
  - re-encode or transform attachments

non_goals:
  - live preview rendering beyond minimal metadata
  - server-side scanning or antivirus

performance:
  - validate and render lists under 50ms for up to 20 files

non_functional_requirements:
  - accessibility (WCAG 2.2 AA):
    - fallback: provide keyboard and clickable file-picker fallback to drag-and-drop
    - labels: announce allowed types and size limits via `aria-describedby`; accepted/rejected files are announced
    - states: validation errors surfaced via assertive live region; controls expose `aria-disabled` when applicable
  - responsive_ux:
    - layout: chip list wraps; long filenames truncate with accessible tooltip; no horizontal overflow
    - target_sizes: interactive areas ≥44x44 px
  - performance_budgets:
    - validation/render ≤50ms for ≤20 files; batch validation without blocking UI
  - testing_hooks:
    - axe-core on routes with dropzone; fail CI on violations
    - keyboard-only upload via file-picker
    - viewport matrix assertions

observability:
  - logs:
      - trace_id, request_id, event (drop|pick|accept|reject), file_count, error_class
  - metrics:
      - attachments_staged_count, attachments_accepted_count, attachments_rejected_count

output:
  - minimal implementation only (no commentary, no TODOs)
