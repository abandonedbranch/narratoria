# Narratoria Future Requirements

The following backlog items use Scrum-style acceptance criteria to clarify expected behavior for the MVP and near-term roadmap. See `CONTRIBUTORS.md` for contribution rules and workflow expectations.

## Playwright test runner availability
- **Status**: Blocked
- **Assignee**: Unassigned
- **As a** developer, **I want** the Playwright test suite to run in the repo so we can catch regressions automatically.
- **Acceptance criteria:**
  - Install or document the required .NET SDK so `dotnet test tests/NarratoriaClient.PlaywrightTests` executes (current runs fail because `dotnet` is unavailable in the environment).
  - Ensure Playwright browser dependencies are installed as part of the test workflow and can run headless in CI/dev containers.
  - Provide a repeatable command (including any `playwright install` steps) that passes locally and in automation without manual setup.

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
- **As a** player, **I want** a reusable tab component so I can swap between multiple pieces of content inline.
- **Acceptance criteria:**
  - Tab component renders a list of child components passed as children in Razor markup; each child corresponds to a tab with a label/title.
  - Tapping/clicking a tab makes its child the only visible panel; non-active children are hidden but remain mounted to preserve state.
  - Provides a simple API for callers to define tab order, labels, and initial selected tab; supports programmatic selection changes.
  - Keyboard accessibility: arrow keys move focus/selection between tabs, selection is indicated, and panels are navigable when active.
  - Visual active state differentiates the selected tab; layout centers the tab list within its parent.
  - Tests cover rendering multiple children, switching active tab via click/keyboard, maintaining child state, and initial selection behavior.

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

## Carousel page control component
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** to page through horizontally stacked content with a dot control so I can switch cards/panels without scrolling vertically.
- **Acceptance criteria:**
  - Provide a reusable horizontal pager that renders its children as pages laid out side-by-side with only the active page visible (using translate/snap rather than stacking vertically).
  - Dot control renders one dot per page (supporting small counts and 8+ cases) with a highlighted state for the active page; clicking/tapping a dot animates to that page and updates state.
  - Keyboard and touch/trackpad interactions allow paging (e.g., left/right arrows or swipe) and keep dot selection in sync; components wrapped in the pager are tabbable/focusable when active.
  - Dots expose accessible button semantics/labels (e.g., “Page 2 of 5”) and maintain contrast in light/dark themes.
  - Tests cover rendering variable page counts, dot navigation updates, and event callbacks fired when the active page changes.

## Progress indicator component
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** a progress indicator with text so I can see what’s happening and how far along it is.
- **Acceptance criteria:**
  - Supports both indeterminate (spinner + label) and determinate (progress bar + label) modes with a single API; label is optional but displayed when provided.
  - Determinate mode accepts a numeric progress value (0–100 or 0–1) and clamps to bounds; bar visually fills according to the value and animates smoothly on change.
  - Indeterminate mode shows a non-blocking spinner that does not reserve unnecessary width and does not mislead with arbitrary percentages.
  - Accessible semantics: label is associated with the control, determinate mode exposes `aria-valuenow/min/max`, indeterminate mode uses `aria-busy`/status roles; supports light/dark themes with sufficient contrast.
  - Tests cover rendering with/without label, switching between indeterminate/determinate modes, value clamping/updates, and accessibility attributes.

## Property sheet component
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** a slide-up property sheet so I can edit settings without leaving the current view.
- **Acceptance criteria:**
  - Sheet anchors to the bottom of the viewport and slides up over content with smooth enter/exit animation; supports modal scrim and dismissal via close button, drag-down, or escape key.
  - Provides a header with title, optional primary action, and close; content area stacks child components vertically with scrolling when content exceeds available height.
  - Respects safe areas/margins on mobile sizes; width adapts (full-width on mobile, constrained on desktop) with responsive max height and rounded top corners.
  - Focus management: trap focus within the sheet when open, restore focus on close, and ensure scroll locking on the body to prevent background interaction.
  - Accessibility: ARIA roles/labels for dialog pattern, keyboard navigation for close/primary actions, and high-contrast support across themes.
  - Tests cover open/close triggers, focus trap/restore, scroll locking, and responsive layout behavior.

## Workflow settings sheet
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** a property sheet that lets me configure workflow credentials/prompts without leaving the current view.
- **Acceptance criteria:**
  - Reuses the property sheet component to present workflow settings (per-workflow endpoint/model/API key/prompt) for Narrator/System/Image.
  - Lists each workflow with inputs for endpoint, model, API key, and system prompt; keys are masked, validated, and stored securely; prompts are saved per workflow.
  - Sheet loads existing values on open and persists changes on save/apply; changes surface confirmation/error states without closing the sheet.
  - Accessibility: dialog semantics, labeled inputs, keyboard navigable sections, and clear error messaging; honors light/dark themes.
  - Tests cover loading existing settings, saving updates, masked key handling, prompt persistence per workflow, and error/validation states.

## Persona management sheet
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** a dedicated property sheet to create, edit, and delete personas without leaving the current view.
- **Acceptance criteria:**
  - Reuses the property sheet component with a personas list and editor; supports add/edit/delete with name/avatar/prompt fields.
  - Selecting a persona from the sheet applies it to the active session; changes persist on save/apply with clear confirmation/error states.
  - Supports keyboard navigation, labeled inputs, and accessible dialog semantics; honors light/dark themes.
  - Tests cover loading existing personas, create/edit/delete flows, applying a persona to the active session, and validation/error handling.

## Per-workflow API keys
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** to configure separate API keys/endpoints for the narrator, system, and image workflows so I can mix providers per task.
- **Acceptance criteria:**
  - Settings UI allows distinct endpoint/model/API key inputs for Narrator, System, and Image workflows with validation and secure local storage.
  - Update request plumbing to use the corresponding credentials per workflow; ensure keys are not logged or leaked.
  - Export/import includes per-workflow credentials (redacted in UI logs) and preserves schemas for future migrations.
  - Playwright/automated tests cover saving, loading, and using the per-workflow settings.

## Workflow picker component
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** a reusable workflow picker so I can choose Narrator, System, or Image from a dropdown wherever it’s needed.
- **Acceptance criteria:**
  - Renders as a labeled dropdown/select that lists exactly the three workflows (Narrator/System/Image) and highlights the current selection.
  - Exposes a simple API for default/controlled selection and emits change events so callers can update request plumbing and UI state.
  - Keyboard and screen reader accessible (combobox/select semantics, focusable, ESC/Enter/arrow navigation); honors dark/light themes.
  - Supports disabled/read-only state for contexts where workflow changes aren’t allowed; component is reusable across pages/dialogs without layout hacks.
  - Tests cover default selection, change handling, disabled state, and accessibility attributes/keyboard navigation.

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
