# Narratoria Future Requirements

The following backlog items use Scrum-style acceptance criteria to clarify expected behavior for the MVP and near-term roadmap. See `CONTRIBUTORS.md` for contribution rules and workflow expectations.

---

## Narration request pipeline
  - **Status**: Done
  - **Assignee**: gpt-codex-5@api.openai.org
  - **As a** player, **I want** narrator replies to stream after I submit a message so I see progress and the conversation is persisted locally.
  - **Acceptance criteria:**
    - Player messages are appended to the active session log and a connection status change is broadcast before the narrator call begins.
    - Context sent to the narrator includes the current system prompt, a summary of the session/personas, and prior messages after filtering out system command tokens (e.g., unescaped `@command` tags).
    - Narrator responses stream via the OpenAI-compatible chat client, update UI status to Writing while text arrives, and default to a safe message if the provider returns no content.
    - Narrator responses (or diagnostics on failure) are stored in the chat history and status resets to Idle or Disconnected so the player can continue or retry.
  - **Technical summary:** Implemented in `NarratoriaClient/Services/NarrationService.cs` using `IOpenAiChatService` streaming, `IAppDataService` for session persistence and context building, and `ILogBuffer` for structured telemetry around status changes and error handling.


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
