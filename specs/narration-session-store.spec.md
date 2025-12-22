## spec: narration-session-store

mode:
  - stateful (persists session records and narration contexts in IndexedDB-backed storage)

behavior:
  - what: Persist and query narration sessions, including session metadata (title, timestamps), and the persisted NarrationContext used by narration middleware.
  - input:
      - CreateSessionRequest: record { TraceMetadata Trace; string? InitialTitle }
      - Guid SessionId
      - string Title
      - NarrationTurnRecord : persisted turn record (see narration-turn-log-record)
      - Guid TurnId
      - CancellationToken
  - output:
      - SessionRecord: record { Guid SessionId; string Title; bool IsTitleUserSet; DateTimeOffset CreatedAt; DateTimeOffset UpdatedAt }
      - NarrationContext : stored session context used by narration pipeline persistence middleware
      - IReadOnlyList<SessionRecord> : ordered list for the Open... UI
      - IReadOnlyList<NarrationTurnRecord> : ordered turn transcript for a session
  - caller_obligations:
      - treat the store as the source of truth for whether a session exists
      - call CreateSession before the first narration pipeline run for that session
      - propagate CancellationToken
  - side_effects_allowed:
      - read/write/delete IndexedDB records for sessions, contexts, and (optionally) turn summaries

state:
  - sessions : persistent | SessionRecord keyed by SessionId
  - contexts : persistent | NarrationContext keyed by SessionId

preconditions:
  - browser supports IndexedDB and schema is initialized

postconditions:
  - CreateSession creates SessionRecord + initial NarrationContext such that narration persistence middleware will not raise MissingSession
  - DeleteSession deletes all session-scoped records (SessionRecord, NarrationContext, persisted turns, and processed attachments owned by the session)

turn_persistence:
  - operations:
      - ListTurns(SessionId, CancellationToken) => IReadOnlyList<NarrationTurnRecord>
          - returns turns ordered by CreatedAt ascending (oldest to newest)
      - UpsertTurn(NarrationTurnRecord, CancellationToken) => ValueTask
          - stores the record for (SessionId, TurnId) using last-write-wins semantics
      - DeleteTurn(SessionId, TurnId, CancellationToken) => ValueTask
          - removes the persisted record for the turn
  - finalization:
      - if an existing stored record has IsFinal=true, UpsertTurn MUST reject any non-identical update for that (SessionId, TurnId) by returning a structured persistence error

invariants:
  - SessionId uniquely identifies all session-scoped records
  - TurnId uniquely identifies a turn within a SessionId
  - UpdatedAt monotonically increases per SessionId on successful writes
  - IsTitleUserSet, when true, prevents automatic title updates

failure_modes:
  - NotSupported :: IndexedDB unavailable :: return structured capability error; no partial records
  - PersistenceError :: write/delete fails :: return structured persistence error; no partial records
  - MissingSession :: requested SessionId does not exist :: return structured missing-session error
  - Cancellation :: token signaled :: abort; no partial writes

policies:
  - ordering:
      - ListSessions returns most-recently-updated-first
      - ListTurns returns oldest-to-newest (CreatedAt ascending)
  - idempotency:
      - DeleteSession is idempotent per SessionId (repeated deletes succeed with no-op semantics)
  - concurrency:
      - safe under concurrent callers; transactions are scoped to SessionId

never:
  - log persisted narration contents or raw attachment payloads
  - create sessions implicitly in read/load APIs

non_goals:
  - cross-device sync
  - encryption at rest

performance:
  - CreateSession under 50ms under normal IndexedDB conditions
  - ListSessions under 50ms for 200 sessions

observability:
  - logs:
      - trace_id, request_id, session_id, operation (create|load|save|list|delete|rename), elapsed_ms, status, error_class
  - metrics:
      - session_store_latency_ms (by operation), session_store_error_count (by error_class)

output:
  - minimal implementation only (no commentary, no TODOs)
