## spec: openai api service

mode:
  - isolated

behavior:
  - what: Convert SerializedPrompt into StreamedTokens by invoking the configured LLM provider via injected collaborators.
  - input:
      - SerializedPrompt: Serialized prompt payload.
  - output:
      - StreamedTokens: Streamed LLM response tokens.
  - caller_obligations:
      - Provide configured provider client, credentials, endpoint, and retry/timeout policy.
      - Provide logging/metrics hooks and cancellation token for request lifetime.
  - side_effects_allowed:
      - Only through injected collaborators (HTTP client, logging, metrics); service stores no state.

state:
  - none: stateless

preconditions:
  - Provider client is initialized with valid configuration and credentials.
  - Cancellation token is supplied.

postconditions:
  - On success, all response tokens are streamed to the caller.
  - On failure, a structured error is emitted.

invariants:
  - API credentials are never logged or emitted in lifecycle events.
  - All failures emit a deterministic, structured error object.
  - No state is stored or mutated across calls.
  - Idempotent with respect to SerializedPrompt and injected policy.

failure_modes:
  - NetworkTimeout :: provider response exceeds timeout :: emit structured timeout error and stop streaming
  - HttpError :: non-success status code :: emit structured provider error with status
  - DecodeError :: response stream cannot be decoded :: emit structured decode error and stop streaming

policies:
  - Honor injected retry/timeout/idempotency policy; do not invent retries.
  - Concurrency: safe under concurrent callers using injected client.
  - Cancellation: honor CancellationToken on all async operations.

never:
  - Emit credentials or secrets in logs or events.
  - Maintain or mutate internal state across calls.
  - Omit a surfaced, structured error report on failure.

non_goals:
  - Prompt assembly or templating.
  - Response post-processing beyond streaming raw tokens.
  - Owning HTTP client configuration, retry policy, or metrics policy.

performance:
  - Respect configured timeout per request (from injected policy).
  - Latency SLO driven by caller-provided policy.

observability:
  - logs:
      - trace_id, request_id, stage, elapsed_ms, status, error_class
  - metrics:
      - request_count (by status/error_class), request_latency_ms, bytes_sent, bytes_received

output:
  - minimal implementation only (no commentary, no TODOs)
