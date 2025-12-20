## spec: workspace-shell-ui

mode:
  - stateful (owns in-memory tab state for the running app instance; persists sessions via collaborators)

behavior:
  - what: Provide a workspace-first Blazor UI shell with tabs, a collapsible sidebar, a fixed compose bar, and a main scrollback area that renders either a starter workspace or a session transcript.
  - input:
      - INarrationSessionStore : collaborator for session create/open/list/delete/rename
      - INarrationPipelineFactory : collaborator to compose per-prompt narration pipelines
      - INarrationAttachmentIngestionService : collaborator to ingest dropped files immediately into staged attachment summaries
      - IAttachmentUploadStore : collaborator to temporarily hold raw upload bytes during ingestion
      - IReadOnlyList<NarrationStageKind> NarrationStageOrder : stage order for narration turns rendered in the scrollback
        - recommended_default (NarrationStageKind.Name values):
            - session_load
            - system_prompt_injection
            - content_guardian_injection
            - attachment_context_injection
            - provider_dispatch
            - persist_context
      - string IngestionStageId : stage id used for attachment-chip telemetry
        - required_value: attachment_ingestion
      - CancellationToken : caller-provided cancellation (application lifetime token)
  - output:
      - RenderFragment : the shell UI
  - caller_obligations:
      - register collaborators in DI; shell MUST NOT construct fallback implementations
      - provide NarrationStageOrder consistent with middleware telemetry stage ids used by the narration pipeline
  - side_effects_allowed:
      - create sessions on first meaningful user action in a workspace tab
      - ingest dropped attachments immediately via ingestion service
      - open/close/focus tabs
      - persist session state via store and middleware

state:
  - tabs : IReadOnlyList<WorkspaceTabView> | in-memory for the running app instance
  - active_tab_id : Guid | in-memory
  - is_sidebar_open : bool | in-memory
  - WorkspaceTabView: record { Guid TabId; WorkspaceTabKind Kind; Guid? SessionId; string Title; DateTimeOffset CreatedAt }
  - WorkspaceTabKind: enum { Workspace, Session }

preconditions:
  - DI container provides INarrationSessionStore and INarrationAttachmentIngestionService
  - NarrationStageOrder is non-empty and unique

postconditions:
  - app startup shows an active Workspace tab with starter links in the main pane
  - compose bar is always available and targets the active tab
  - first meaningful action in a Workspace tab creates a session and converts the tab to a Session tab
  - when a prompt is sent in a Session tab, the scrollback renders a new turn entry that includes stage chips in NarrationStageOrder and streams narration output

invariants:
  - tabs are in-memory UI constructs; closing a tab never deletes a session
  - sessions are persisted automatically; there is no draft session concept
  - a Session tab always has a non-null SessionId
  - `attachment_ingestion` is used only for attachment chip state/telemetry and MUST NOT be included in NarrationStageOrder

failure_modes:
  - store_error :: session create/open/list/delete/rename fails :: show banner; keep shell usable
  - ingestion_error :: ingestion fails for one or more dropped files :: mark failed chips; keep shell usable; do not block navigation

policies:
  - tab_creation:
      - on app startup, create exactly one Workspace tab
      - on double-click in empty tab strip area, create a new Workspace tab
  - workspace_to_session_transition:
      - on first dropped file or first prompt send in a Workspace tab, create a new session in the store and convert the tab to a Session tab
  - sidebar:
      - toggled by double-clicking a shell icon; the action is idempotent

never:
  - delete sessions implicitly on tab close
  - run narration provider dispatch during attachment ingestion
  - show system prompt text or hidden configuration

non_goals:
  - multi-user presence or shared sessions
  - per-prompt toggles for including staged attachments

performance:
  - initial render under 150ms on target hardware
  - tab switch under 50ms for sessions up to 50 turns

observability:
  - logs:
      - trace_id, event (tab_create|tab_focus|tab_close|sidebar_toggle|workspace_transition), tab_id, session_id, status, error_class
  - metrics:
      - tab_created_count, tab_closed_count, tab_switch_latency_ms, workspace_transition_count

output:
  - minimal implementation only (no commentary, no TODOs)
