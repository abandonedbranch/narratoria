## spec: storage quota awareness service

mode:
  - compositional

behavior:
  - what: Provide quota and usage awareness via an injected quota provider so callers can gate writes and surface capacity information.
  - input:
      - StorageScope: Logical scope identifier (e.g., database/store/namespace) for contextual logging and metrics.
      - RequestedBytes: Optional predicted write size to evaluate against available capacity.
      - EstimationHints: Optional store names or payload descriptors to annotate the request.
  - output:
      - QuotaReport: usage_bytes, quota_bytes, available_bytes, can_accommodate (when RequestedBytes provided), source, provider_id.
  - caller_obligations:
      - Provide a quota provider implementing a minimal quota interface and the target StorageScope.
      - Handle capability errors and define fallback behavior when estimates are unavailable.
      - Provide cancellation token for async operations when supported by host runtime.
      - Supply logging/metrics hooks.
  - side_effects_allowed:
      - Only through injected collaborators (quota provider, logging, metrics).

state:
  - service_state: none (stateless)
  - indexeddb_capability_cache: ephemeral in-memory flag indicating whether StorageManager is supported (IndexedDB provider only)

preconditions:
  - A quota provider is supplied for the targeted StorageScope.
  - When using the IndexedDB provider, calls execute in secure contexts and the host allows StorageManager access.

postconditions:
  - On success, returns quota/usage report; no data is written or deleted.
  - On provider capability failure, a structured error is returned and no storage is modified.

invariants:
  - Quota lookups never trigger storage writes, evictions, or schema changes.
  - Reporting reflects provider-supplied estimates (typically origin-level); it does not claim per-store precision.
  - Repeated lookups are side-effect free aside from logs/metrics.
  - Service remains backend-agnostic; provider selection is injected per call or instance.

failure_modes:
  - CalculationError :: RequestedBytes is not finite or negative :: emit structured validation error
  - NotSupported :: provider reports quota unsupported for the scope :: emit structured capability error
  - EstimateUnavailable :: provider cannot return quota/usage or returns undefined fields :: emit structured estimate error
  - ProviderFailure :: injected provider throws or rejects :: emit structured provider error with inner details
  - IndexedDbNotSupported :: StorageManager unavailable, disallowed, or insecure context when using IndexedDB provider :: emit structured capability error

policies:
  - Retry: none by default; caller supplies if transient errors are expected.
  - Timeout: honor caller-provided timeout/cancellation if supported by host runtime.
  - Concurrency: safe under concurrent callers; operations are read-only.
  - Idempotency: repeat calls with the same host state yield equivalent reports.

never:
  - Block the main thread for quota lookups.
  - Perform deletions/evictions to reclaim space.
  - Open write transactions or mutate backing storage.
  - Infer quotas by sampling writes.
  - Bind the service to a single storage backend; selection is via injected provider.

non_goals:
  - Enforcing quotas; this service only informs callers.
  - Cross-origin estimation or sync.
  - Estimating network transfer or bandwidth constraints.
  - Owning provider selection or wiring beyond the injected interface.

performance:
  - Quota/usage retrieval completes without storage writes and within host API limits (<50ms target under normal load).

observability:
  - logs:
      - trace_id, request_id, storage_scope, provider_id, operation, elapsed_ms, status, error_class, estimation_source
  - metrics:
      - quota_lookup_latency_ms, quota_used_bytes, quota_remaining_bytes, quota_error_count (by error_class, provider_id)
      - quota_can_accommodate_count (by boolean when RequestedBytes present, provider_id)

output:
  - minimal implementation only (generic quota interface plus IndexedDB-backed provider; no commentary, no TODOs)
