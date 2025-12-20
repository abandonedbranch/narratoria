## spec: narration-processed-attachment-store

mode:
  - stateful (persists processed attachment summaries per session)

behavior:
  - what: Store and retrieve processed attachment summaries that are automatically applied to future narration runs.
  - input:
      - Guid SessionId
      - ProcessedAttachment: record { string AttachmentId; Guid SessionId; string FileName; string MimeType; string SourceHash; string NormalizedText; int? TokenEstimate; bool Truncated; DateTimeOffset CreatedAt }
      - string AttachmentId
      - CancellationToken
  - output:
      - IReadOnlyList<ProcessedAttachment> : staged attachments for a session
  - caller_obligations:
      - delete processed attachments only via explicit user remove actions
      - propagate CancellationToken
  - side_effects_allowed:
      - write/delete IndexedDB records

state:
  - processed_attachments : persistent | ProcessedAttachment keyed by (SessionId, AttachmentId)

preconditions:
  - SessionId exists

postconditions:
  - ListBySession returns all staged attachments for SessionId
  - Delete removes the attachment and it no longer influences narration prompt construction

invariants:
  - processed attachments contain normalized text only; raw file bytes are never stored
  - attachments are session-scoped and do not leak across sessions

failure_modes:
  - PersistenceError :: write/delete fails :: return structured persistence error
  - MissingSession :: SessionId does not exist :: return structured missing-session error
  - Cancellation :: token signaled :: abort

policies:
  - ordering:
      - ListBySession returns attachments ordered by CreatedAt ascending
  - idempotency:
      - Delete is idempotent

never:
  - log NormalizedText contents
  - store provider credentials

non_goals:
  - full-text search
  - binary extraction beyond text/markdown normalization

performance:
  - ListBySession under 50ms for 50 attachments

observability:
  - logs:
      - trace_id, request_id, session_id, attachment_id, operation (list|put|delete), elapsed_ms, status, error_class
  - metrics:
      - processed_attachment_store_latency_ms (by operation), processed_attachment_store_error_count

output:
  - minimal implementation only (no commentary, no TODOs)
