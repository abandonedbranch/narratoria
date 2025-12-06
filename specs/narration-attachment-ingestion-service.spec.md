## spec: narration attachment ingestion service

mode:
  - stateful

behavior:
  - what: Summarize an uploaded text-based file via LLM and persist the processed attachment for reuse in narration context.
  - input:
      - SessionId: Identifier for the active session.
      - UploadedFile: Name, MimeType, SizeBytes, Content (byte stream or buffer).
      - TraceMetadata: Trace identifiers for observability.
      - AttachmentOptions: Optional limits (max_tokens, max_chars, dedupe_by_hash).
  - output:
      - ProcessedAttachment: AttachmentId, SessionId, FileName, MimeType, SourceHash, NormalizedText, TokenEstimate, Truncated (bool), CreatedAt.
  - caller_obligations:
      - Provide temporary session upload store to hold the raw file until processing completes.
      - Provide OpenAI-capable LLM service, credentials, endpoint, and timeout policy for summarization.
      - Provide persistent IndexedDB attachment store scoped to the session.
      - Supply logging/metrics hooks and cancellation token.
  - side_effects_allowed:
      - Send raw file content to the LLM provider for parsing/summarization.
      - Persist processed attachment into IndexedDB keyed by SessionId.
      - Delete raw file from temporary session storage after success or failure.
      - Emit structured logs and metrics.

state:
  - temp_session_uploads: in-memory/session-scoped raw file buffer (purged after processing or failure)
  - processed_attachments: per-session persistent IndexedDB records containing normalized text and metadata

preconditions:
  - UploadedFile mime type is accepted (text/plain, text/markdown, application/pdf, or configured allowlist).
  - UploadedFile size is <= configured max_bytes and is fully captured into temporary session storage.
  - OpenAI provider configuration and credentials are available.

postconditions:
  - On success, ProcessedAttachment is persisted to IndexedDB and associated with SessionId; raw upload is purged.
  - On failure, a structured error is returned; raw upload is purged; no attachment is persisted.

invariants:
  - Raw file contents never persist beyond the temporary session upload store and are deleted after each attempt.
  - ProcessedAttachment is immutable once persisted; updates require a new AttachmentId.
  - Only text-derived content is sent down the Narration Pipeline; binary payloads are rejected.
  - LLM prompt explicitly instructs parsing/summarizing/optimizing for downstream non-reasoning LLM consumption and excludes secrets.
  - Session scoping is enforced for all reads/writes; attachments do not leak across sessions.

failure_modes:
  - UnsupportedFileType :: mime type/extension not in allowlist or cannot be treated as text :: purge raw upload; emit structured validation error.
  - FileTooLarge :: size_bytes exceeds configured max_bytes :: purge raw upload; emit structured validation error.
  - ProviderTimeout :: LLM call exceeds configured timeout :: purge raw upload; emit structured timeout error.
  - ProviderError :: LLM provider rejects or returns error :: purge raw upload; emit structured provider error with provider status/details.
  - DecodeError :: provider response cannot be decoded :: purge raw upload; emit structured decode error.
  - PersistenceError :: IndexedDB write fails or is unavailable :: purge raw upload; emit structured persistence error; do not partially persist.
  - Cancellation :: operation canceled by caller :: purge raw upload; emit structured cancellation notice.

policies:
  - Timeout: honor configured provider timeout; no implicit retries; caller may inject retry policy if desired.
  - Idempotency: when dedupe_by_hash is true and a matching SessionId+SourceHash exists, return existing ProcessedAttachment and still purge raw upload.
  - Concurrency: safe under concurrent uploads per session; isolate temp buffers per upload; IndexedDB writes are transactional per attachment.
  - Cancellation: propagate CancellationToken through provider call and persistence; cancel promptly and purge temp data.
  - Inclusion: processed attachments are flagged for inclusion in the next Narration Pipeline execution; pipeline selects them by SessionId and optional caller selection.

never:
  - Persist or log raw file contents after processing completes or fails.
  - Send raw uploads directly to the Narration Pipeline without LLM summarization.
  - Store provider credentials or tokens in attachment records.
  - Bypass mime/size validation or quota checks.

non_goals:
  - Virus scanning, PII redaction, or content moderation.
  - Rich binary extraction beyond text/PDF/markdown.
  - Long-term archival or cross-session sharing of attachments.
  - Provider selection logic beyond the configured OpenAI-capable service.

performance:
  - Enforce configured max_bytes per upload and max_tokens/max_chars for processed output; truncate with explicit markers when limits are exceeded.
  - Release temporary session storage immediately after processing or failure; target negligible retention time.

observability:
  - logs:
      - trace_id, request_id, session_id, attachment_id, file_name, mime_type, size_bytes, stage, elapsed_ms, status, error_class, token_estimate, truncated
  - metrics:
      - attachments_processed_count (by status/error_class), attachment_bytes_ingested, attachment_persist_latency_ms, provider_latency_ms, provider_error_count

output:
  - minimal implementation only (no commentary, no TODOs)
