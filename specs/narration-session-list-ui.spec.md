## spec: narration-session-list-ui

mode:
  - stateful (renders modal/drawer state; delegates persistence to store)

behavior:
  - what: Present an Open... UI that lists sessions and allows open, delete, and rename actions.
  - input:
      - INarrationSessionStore : collaborator for list/delete/rename
      - Func<Guid, CancellationToken, ValueTask> OpenSession : callback invoked when a session is selected
      - CancellationToken
  - output:
      - RenderFragment : modal or drawer containing session list and actions
  - caller_obligations:
      - provide OpenSession that focuses an already-open tab or opens a new Session tab
      - propagate CancellationToken
  - side_effects_allowed:
      - list sessions
      - delete sessions (confirmed)
      - rename sessions
      - invoke OpenSession

state:
  - is_open : bool | in-memory
  - sessions : IReadOnlyList<SessionRecord> | in-memory snapshot
  - filter_text : string | in-memory
  - pending_delete_session_id : Guid? | in-memory

preconditions:
  - store is accessible

postconditions:
  - opening the UI refreshes the session list snapshot
  - selecting a session invokes OpenSession exactly once

invariants:
  - delete requires explicit user confirmation
  - rename sets IsTitleUserSet=true for the session

failure_modes:
  - store_error :: list/delete/rename fails :: show inline error; keep UI usable
  - cancelled :: token signaled :: close modal/drawer without side effects

policies:
  - ordering:
      - default list is most-recently-updated-first
  - concurrency:
      - actions are serialized within the component

never:
  - create sessions
  - expose system prompt text

non_goals:
  - bulk delete
  - advanced search beyond simple substring filtering

performance:
  - open and render under 100ms for 200 sessions

observability:
  - logs:
      - trace_id, event (open|close|list|open_session|delete|rename), session_id, status, error_class
  - metrics:
      - session_list_open_count, session_list_action_count (by action/status)

output:
  - minimal implementation only (no commentary, no TODOs)
