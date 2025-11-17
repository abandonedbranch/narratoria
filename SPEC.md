# Narratoria Future Requirements

The following backlog items use Scrum-style acceptance criteria to clarify expected behavior for the MVP and near-term roadmap. See `CONTRIBUTORS.md` for contribution rules and workflow expectations.

## Chat session as tabs
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** to be able to manage my sessions through a tab-based interface. Tabs should display the session title, and a close button. On the far right of the tabs I want a plus button which will add a new session and switch to its tab.
- **Acceptance criteria:**
  - Use existing layout components to achieve this; modify them if needed.
  - Keep the existing `@sessions` command available so sessions can still be managed via chat commands in addition to tabs.
  - Closing a tab deletes that session and removes it from the session list.
  - Pressing the plus button creates a new tab and starts a new session in it.
- **Discussion:**
  - Prefer reusing existing layout components (e.g., the grid component) to arrange the session tabs instead of introducing new layout primitives.

## Session tab strip component
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** to see a tab strip for sessions so I can quickly switch stories and open or close them inline.
- **Acceptance criteria:**
  - Implement a tab strip using existing layout primitives (Grid/Dock) with labels, close buttons, and a plus button aligned to the right.
  - Active tab is visually distinct and matches the currently active session from `IAppDataService`.
  - Tab close invokes session deletion and refreshes the visible list without a page reload.
  - Plus button calls session creation and selects the new session tab.

## Session tab state sync
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** session tabs to stay in sync with session changes so UI state matches the underlying data.
  - **Acceptance criteria:**
    - Tab strip listens to `SessionsChanged` and `ChatSessionChanged` events to keep tab labels, active state, and counts current.
    - Switching tabs triggers `SwitchSessionAsync` and refreshes scrollback in the chat view.
    - Closing a tab calls `DeleteSessionAsync` and updates the active session to the next available tab (or creates a default when none remain).
    - `@sessions` chat command output reflects tab changes (new, deleted, switched) without manual refresh.

## Component test coverage baseline
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** developer, **I want** automated tests for key UI components that currently have no coverage so regressions in rendering and interactions are caught early.
- **Acceptance criteria:**
  - Add component/integration tests for `Components/SessionsManager.razor` (renders sessions list, switch/delete/start actions call `IAppDataService` and update UI).
  - Add coverage for the home page scrollback (`Components/Pages/Home.razor`) ensuring active session heading/subheading and chat history update on `SessionsChanged` and `ChatSessionChanged`.
  - Add coverage for `Components/ReplyEditor.razor` or input flow to verify message send invokes `INarrationService` and handles empty/disabled states.
  - Tests run via `dotnet test` (or Playwright where applicable) and are required before marking related backlog items Done.

## Service test coverage baseline
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** developer, **I want** unit tests for core services lacking coverage so session/data behaviors remain stable.
- **Acceptance criteria:**
  - Add tests for `AppDataService` covering session create/switch/delete, persona upsert/delete, and export/import paths with storage mocked.
  - Add tests for `NarrationService` ensuring empty input is ignored, system commands are filtered from history, status notifications fire in success/cancel/error flows, and narration fallback text is applied when responses are empty.
  - Tests run via `dotnet test` and gate completion of dependent backlog items.

## Client storage resilience
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** clear feedback and graceful fallback when browser storage is unavailable or full, so I understand whether my sessions are persisted.
- **Acceptance criteria:**
  - `BrowserClientStorageService` surfaces quota and unavailability errors with actionable messages; `AppDataService` catches these and updates UI/state with a user-friendly notification.
  - Falling back to in-memory/session storage preserves current session state when persistent storage fails, and informs the player of limited persistence.
  - Logging captures storage error context (area/key/operation) without leaking sensitive data.
  - Playwright/automated tests cover quota/unavailable scenarios.

## Reply editor robustness
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** the reply editor to behave predictably so I don’t lose input or trigger duplicate sends.
- **Acceptance criteria:**
  - “Send” is disabled while a send is in progress and when the input is empty; a visual cue shows busy state.
  - Keyboard handling supports Enter to send and Shift+Enter to insert a newline without double submission.
  - Errors from `OnSend` path surface a non-blocking notification and keep the unsent text intact.
  - Tests validate empty-state behavior, busy-state guarding, and keyboard interactions.

## Per-workflow API keys
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** to configure separate API keys/endpoints for the narrator, system, and image workflows so I can mix providers per task.
- **Acceptance criteria:**
  - Settings UI allows distinct endpoint/model/API key inputs for Narrator, System, and Image workflows with validation and secure local storage.
  - Update request plumbing to use the corresponding credentials per workflow; ensure keys are not logged or leaked.
  - Export/import includes per-workflow credentials (redacted in UI logs) and preserves schemas for future migrations.
  - Playwright/automated tests cover saving, loading, and using the per-workflow settings.

## Targeted workflow sending
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** to explicitly route a message to the narrator, system, or image workflows so I control which engine responds.
- **Acceptance criteria:**
  - Reply editor (or adjacent controls) offers an explicit selector/switch to choose Narrator/System/Image before sending.
  - The selection is reflected in prompts/request payloads and surfaced in the chat log metadata.
  - Defaults to Narrator; preserves last selection per session; prevents sending without a selected workflow.
  - Tests verify routing changes request construction and the correct provider receives the message.

## Workflow-specific system prompts
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** to edit the system prompt for each workflow so I can tune behavior independently.
- **Acceptance criteria:**
  - System prompt editor supports separate prompt content/title per workflow (Narrator/System/Image) with persistence.
  - The System workflow prompt is prefilled with a sensible default guidance message.
  - Prompt changes are applied to outbound requests for the selected workflow and reflected in exported settings.
  - Tests validate defaults, editing, persistence, and correct prompt inclusion per workflow.

## System prompt editor safety note
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** a reminder when editing system prompts to follow the terms of service of my LLM provider so I avoid prohibited content or misuse.
- **Acceptance criteria:**
  - System prompt editor displays a concise note reminding users to comply with their provider’s TOS/policies when customizing prompts.
  - Note appears near the prompt input for all workflows (Narrator/System/Image) and links to provider policy docs if available.
  - Tests confirm the note renders in the editor UI across workflows.

## System workflow state summarization
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** narrator system, **I want** to maintain a rolling working memory of the scenario so the narrator model receives concise, relevant context.
- **Acceptance criteria:**
  - System workflow maintains a rolling summary of past turns, applying heavier summarization to older messages while retaining recent details.
  - Summaries are generated via the System model and stored with timestamps/ordering so they can be refreshed as the session evolves.
  - Narrator requests include the latest system-generated summary in their context payload.
  - Tests verify summaries are produced, refresh over time, and are attached to narrator calls.

## System workflow cadence and persistence
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** the system workflow to refresh its working memory without slowing down narration so the story keeps flowing smoothly.
- **Acceptance criteria:**
  - System summarization runs asynchronously/off the critical path of sending a narrator request; narrator calls are not blocked on summaries when not ready.
  - Summaries are persisted per session (alongside chat history) and restored on reload; they update when new messages arrive or after a defined interval/count.
  - Summaries respect storage limits and fall back gracefully if storage is unavailable; logging captures refresh events and errors without leaking content.
  - Tests cover persistence/restore, non-blocking behavior, and storage-failure handling.

## Theme management command
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** to type `@themes` to list available themes so I can quickly switch between dark and light modes.
- **Acceptance criteria:**
  - Implement `@themes` chat command that lists available themes (at least Dark and Light) and indicates the current selection.
  - Selecting a theme via the command switches the app’s theme immediately and persists the choice locally.
  - Themes apply across all major surfaces (scrollback, editor, controls) with accessible contrast.
  - Tests cover listing themes, switching, and persistence.

## Theme switching UI
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** an in-app control to toggle dark/light themes so I don’t have to remember commands.
- **Acceptance criteria:**
  - Add a visible toggle or selector in the UI to switch themes between Dark and Light (and future themes).
  - Theme choice is persisted and restored on reload; UI updates immediately on change.
  - Styling updates cover common components (layout, scrollback, editor, buttons) and meet accessibility contrast guidelines.
  - Tests verify the UI toggle, persistence, and correct theme application.
