## spec: indexeddb storage service

mode:
  - stateful

behavior:
  - what: Provide IndexedDB-backed storage for application state, settings, and chat sessions with quota awareness and generic CRUD operations.
  - input:
      - StoreName: Logical store for the data segment (e.g., settings, sessions, state).
      - Key: Identifier within the store.
      - Value: Arbitrary serializable payload for the store.
  - output:
      - Result: Confirmation payload or retrieved Value/collection, plus quota metadata when requested.
  - caller_obligations:
      - Supply storage schema definition (stores, key paths, indexes) and versioning strategy.
      - Provide serialization/deserialization policy for stored payloads.
      - Provide cancellation token for async operations when supported by host runtime.
  - side_effects_allowed:
      - Reads/writes/deletes IndexedDB records; retrieves storage estimates via StorageManager.
      - Emits structured logs/metrics via injected hooks.

state:
  - indexeddb_database: persisted per-origin database containing namespaced object stores
  - schema_version: persisted per-database version for migrations

preconditions:
  - Browser supports IndexedDB and (for quota APIs) StorageManager.
  - Caller provides database name, version, and store definitions before first access.

postconditions:
  - On success, CRUD operations are committed atomically per transaction.
  - On quota/unsupported failure, a structured error is returned and no partial writes remain.

invariants:
  - All operations run within transactions scoped to the targeted store(s).
  - Data is namespaced by store; cross-store leakage does not occur.
  - Quota/usage reporting never triggers writes.
  - Idempotent reads; deterministic writes per StoreName/Key.

failure_modes:
  - NotSupported :: IndexedDB or StorageManager unavailable :: emit structured capability error
  - QuotaExceeded :: write exceeds available storage :: emit structured quota error and rollback transaction
  - TransactionFailure :: transaction abort or constraint violation :: emit structured transaction error
  - SerializationError :: payload cannot be serialized/deserialized :: emit structured serialization error

policies:
  - Retry: none by default; caller supplies retry policy if desired.
  - Timeout: honor caller-provided timeout/cancellation if supported by host runtime.
  - Concurrency: safe under concurrent callers via separate transactions.
  - Idempotency: writes keyed by StoreName/Key are idempotent when Value unchanged.

never:
  - Block the main thread for storage operations.
  - Emit stored payload contents in logs/metrics.
  - Mutate schema without explicit version upgrade path.

non_goals:
  - Cross-origin storage or sync.
  - Data encryption or compression at rest.
  - Conflict resolution beyond last-write-wins per key.

performance:
  - Single-store CRUD operation completes within host IndexedDB transaction budget (<50ms target under normal load).
  - Quota/usage retrieval completes without IndexedDB writes and respects host API limits.

observability:
  - logs:
      - trace_id, request_id, store_name, operation, elapsed_ms, status, error_class
  - metrics:
      - storage_read_latency_ms, storage_write_latency_ms, storage_delete_latency_ms, storage_quota_lookup_latency_ms
      - storage_quota_remaining_bytes, storage_used_bytes
      - storage_error_count (by error_class)

output:
  - minimal implementation only (no commentary, no TODOs)
