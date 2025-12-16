## spec: indexeddb storage service

mode:
  - stateful

behavior:
  - what: Provide generic IndexedDB-backed CRUD storage for arbitrary namespaced data segments.
  - input:
      - StoreName: Logical store for the data segment defined by the caller.
      - Key: Identifier within the store.
      - Value: Arbitrary serializable payload for the store.
      - QueryOptions: Optional index/query parameters for list or search operations.
  - output:
      - Result: Confirmation payload or retrieved Value/collection.
  - caller_obligations:
      - Supply storage schema definition (stores, key paths, indexes) and versioning strategy.
      - Provide serialization/deserialization policy for stored payloads.
      - Provide cancellation token for async operations when supported by host runtime.
      - Coordinate quota checks with the quota awareness service before writes when capacity matters.
      - Enforce domain-specific validation, retention, and access policies prior to invoking storage operations.
  - side_effects_allowed:
      - Reads/writes/deletes IndexedDB records.
      - Emits structured logs/metrics via injected hooks.

state:
  - indexeddb_database: persisted per-origin database containing namespaced object stores
  - schema_version: persisted per-database version for migrations

preconditions:
  - Browser supports IndexedDB.
  - Caller provides database name, version, and store definitions before first access.

postconditions:
  - On success, CRUD operations are committed atomically per transaction.
  - On unsupported failure or migration rejection, a structured error is returned and no partial writes remain.

invariants:
  - All operations run within transactions scoped to the targeted store(s).
  - Data is namespaced by store; cross-store leakage does not occur.
  - Storage operations do not perform quota estimation; capacity checks are delegated to a quota service.
  - Idempotent reads; deterministic writes per StoreName/Key.

failure_modes:
  - NotSupported :: IndexedDB unavailable :: emit structured capability error
  - TransactionFailure :: transaction abort or constraint violation :: emit structured transaction error
  - SerializationError :: payload cannot be serialized/deserialized :: emit structured serialization error
  - MigrationFailure :: schema upgrade fails or version mismatch :: emit structured migration error and rollback transaction

policies:
  - Retry: none by default; caller supplies retry policy if desired.
  - Timeout: honor caller-provided timeout/cancellation if supported by host runtime.
  - Concurrency: safe under concurrent callers via separate transactions.
  - Idempotency: writes keyed by StoreName/Key are idempotent when Value unchanged.

never:
  - Block the main thread for storage operations.
  - Emit stored payload contents in logs/metrics.
  - Mutate schema without explicit version upgrade path.
  - Attempt to estimate or enforce storage quota internally.

non_goals:
  - Quota awareness or enforcement (handled by a dedicated quota service).
  - Cross-origin storage or sync.
  - Data encryption or compression at rest.
  - Conflict resolution beyond last-write-wins per key.

performance:
  - Single-store CRUD operation completes within host IndexedDB transaction budget (<50ms target under normal load).

observability:
  - logs:
      - trace_id, request_id, store_name, operation, elapsed_ms, status, error_class
  - metrics:
      - storage_read_latency_ms, storage_write_latency_ms, storage_delete_latency_ms, storage_list_latency_ms
      - storage_error_count (by error_class)

output:
  - minimal implementation only (no commentary, no TODOs)
