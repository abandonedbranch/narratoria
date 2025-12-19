## spec: narration attachment ingestion service

mode:
  - stateful

behavior:
  - what: Middleware that summarizes an uploaded text-based file via LLM (using its own OpenAI client), persists the processed attachment, and purges the raw upload.
  - input:
      - SessionId: Identifier for the active session.
      - UploadedFile: Name, MimeType, SizeBytes, Content (byte stream or buffer).
      - TraceMetadata: Trace identifiers for observability.
      - AttachmentOptions: Optional limits (max_tokens, max_chars, dedupe_by_hash).
  - output:
      - ProcessedAttachment: AttachmentId, SessionId, FileName, MimeType, SourceHash, NormalizedText, TokenEstimate, Truncated (bool), CreatedAt.
  - caller_obligations:
      - Provide temporary session upload store to hold the raw file until processing completes.
      - Provide OpenAI-capable LLM service instance (injected), credentials, endpoint, and timeout policy for summarization.
      - Provide persistent IndexedDB attachment store scoped to the session.
      - Supply logging/metrics hooks and cancellation token.
  - side_effects_allowed:
      - Send raw file content to the LLM provider for parsing/summarization using a prompt that removes prose and optimizes for LLM consumption (not human-readable).
      - Persist processed attachment into IndexedDB keyed by SessionId.
      - Delete raw file from temporary session storage immediately after success or failure.
      - Emit structured logs and metrics.

state:
  - temp_session_uploads: in-memory/session-scoped raw file buffer (purged after processing or failure)
  - processed_attachments: per-session persistent IndexedDB records containing normalized text and metadata

preconditions:
  - UploadedFile mime type is accepted (text/plain, text/markdown, or configured allowlist).
  - UploadedFile size is <= configured max_bytes and is fully captured into temporary session storage.
  - OpenAI provider configuration and credentials are available.

postconditions:
  - On success, ProcessedAttachment is persisted to IndexedDB and associated with SessionId; raw upload is purged.
  - On failure, a structured error is returned; raw upload is purged; no attachment is persisted.

invariants:
  - Raw file contents never persist beyond the temporary session upload store and are deleted after each attempt.
  - ProcessedAttachment is immutable once persisted; updates require a new AttachmentId.
  - Only text-derived content is sent to OpenAI; non-text payloads are rejected (PDF is not supported).
  - No pipeline context (player prompt, prior narration, metadata) is forwarded to the OpenAI call.
  - LLM prompt explicitly instructs stripping prose and optimizing for downstream LLM consumption (not human readability) and excludes secrets.
  - Session scoping is enforced for all reads/writes; attachments do not leak across sessions.

failure_modes:
  - UnsupportedFileType :: mime type/extension not in allowlist or cannot be treated as text :: purge raw upload; emit user-facing structured validation error; short-circuit pipeline.
  - FileTooLarge :: size_bytes exceeds configured max_bytes :: purge raw upload; emit user-facing structured validation error; short-circuit pipeline.
  - ProviderTimeout :: LLM call exceeds configured timeout :: purge raw upload; emit user-facing structured timeout error; short-circuit pipeline.
  - ProviderError :: LLM provider rejects or returns error :: purge raw upload; emit user-facing structured provider error with provider status/details; short-circuit pipeline.
  - DecodeError :: provider response cannot be decoded :: purge raw upload; emit user-facing structured decode error; short-circuit pipeline.
  - PersistenceError :: IndexedDB write fails or is unavailable :: purge raw upload; emit user-facing structured persistence error; short-circuit pipeline.
  - Cancellation :: operation canceled by caller :: purge raw upload; emit user-facing structured cancellation notice; short-circuit pipeline.

policies:
  - Timeout: honor configured provider timeout; no implicit retries; caller may inject retry policy if desired.
  - Idempotency: when dedupe_by_hash is true and a matching SessionId+SourceHash exists, return existing ProcessedAttachment and still purge raw upload.
  - Concurrency: safe under concurrent uploads per session; isolate temp buffers per upload; IndexedDB writes are transactional per attachment.
  - Cancellation: propagate the pipeline’s CancellationToken through provider call and persistence; cancel promptly and purge temp data.
  - Control flow: on any failure, short-circuit the pipeline with a user-facing structured error explaining why ingestion failed; on success, continue.
  - Ordering: should run before prompt templating/transform middleware for best results; pipeline continues even if placed later.
  - Provider ownership: middleware uses its own injected OpenAI client/policy; does not reuse pipeline provider.

never:
  - Persist or log raw file contents after processing completes or fails.
  - Send raw uploads directly to the Narration Pipeline without LLM summarization.
  - Forward pipeline context to the OpenAI call.
  - Store provider credentials or tokens in attachment records.
  - Bypass mime/size validation or quota checks.
  - Generate human-oriented summaries; output must be optimized for LLM consumption.

non_goals:
  - Virus scanning, PII redaction, or content moderation.
  - Rich binary extraction beyond text/markdown.
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
