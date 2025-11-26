# Narratoria Future Requirements

The following backlog items use Scrum-style acceptance criteria to clarify expected behavior for the MVP and near-term roadmap. See `CONTRIBUTORS.md` for contribution rules and workflow expectations.

## Stage hook chaining
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** developer, **I want** each stage to run multiple hook listeners safely so features can be composed without rewriting the pipeline.
- **Acceptance criteria:**
  - Stages resolve hooks via `IEnumerable<IStageHook>` and execute them deterministically (document default ordering + guidance for parallel execution when safe).
  - Hooks can emit sub-events (`input.tags.detected`, `image.generated.chunk`, etc.) that appear in the lifecycle stream before the stage reports completion.
  - Provide diagnostics/logging that record which hook ran and what it mutated; failures in one hook short-circuit the remaining ones with clear metadata.
  - Tests include fake hooks to assert sequencing, cancellation, and error propagation across multiple listeners.

## InputPreprocessor stage
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** developer, **I want** the pipeline’s first stage to normalize player input and detect command hints through composable hooks.
- **Acceptance criteria:**
  - Define `IInputPreprocessorHook` with access to the shared context and emitted telemetry.
  - Built-in hooks cover whitespace/emoji normalization, `@command` detection, and workflow hint extraction (e.g., `/system` prefixes).
  - Stage emits `input.preprocessed` events summarizing detected tags/modifiers; downstream context stores normalized text + tags.
  - Tests verify hook ordering, mutation safety, and emitted lifecycle events for sample inputs.

## SafetyPolicyChecker stage
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** developer, **I want** pipeline-pluggable safety checks that can block or rewrite requests before they reach the model.
- **Acceptance criteria:**
  - Create `ISafetyCheckHook` interface and default hooks for mode compliance, explicit-mode gating, and tone restrictions/world rules.
  - Stage emits `safety.checked` events with pass/fail verdicts; failures produce actionable messages returned to the UI.
  - Pipeline short-circuits when a hook rejects input while still writing a narrator response explaining the rejection.
  - Tests cover allowed vs. blocked input, event payloads, and interaction with `INarrationService`.

## PromptAssembler stage
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** developer, **I want** the pipeline to build narrator-ready prompts from global rules, memory, and personas without duplicating logic elsewhere.
- **Acceptance criteria:**
  - Implement `PromptAssemblerStage` that reads API settings, personas, memory summaries, and normalized input to produce a `NarrationPromptBundle`.
  - Ensure stage injects workflow-specific system prompts plus rolling summaries (when available) and emits `prompt.assembled` events with counts/titles (no raw content).
  - Provide hooks for future prompt mutators (e.g., persona overrides) with deterministic ordering.
  - Tests verify message construction, persona inclusion, and event metadata.

## ModelRouter stage
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** developer, **I want** model selection to be centralized so workflow-specific keys/models/flags are honored before dispatching the request.
- **Acceptance criteria:**
  - Implement `ModelRouterStage` that inspects the selected workflow, per-workflow settings, and any hook-provided overrides to choose the model + endpoint + headers.
  - Stage emits `model.selected` events with the chosen workflow, model name, and rationale (no secrets); these events drive UI notifications.
  - Hooks can swap models (e.g., escalate to reasoning models) while logging why; invalid configurations surface descriptive errors before hitting the LLM.
  - Tests cover selection fallback, override precedence, error handling, and credential usage.

## LLMClient stage
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** developer, **I want** the pipeline to stream narrator output chunks while emitting lifecycle events so the UI and logging stay in sync.
- **Acceptance criteria:**
  - Wrap `IOpenAiChatService` in a stage that listens for streaming updates, emits `llm.response.chunk` and final `llm.response.received` events, and forwards text deltas to the shared context.
  - Handle cancellation and HTTP errors by emitting `StageFailed` plus diagnostic narrator messages, keeping compatibility with existing log buffer behavior.
  - Ensure per-workflow headers/keys selected by ModelRouter are applied to outbound requests.
  - Tests simulate streaming responses, cancellation, and failure payloads, asserting event ordering and buffered text output.

## PostProcessor stage
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** developer, **I want** a hook-based post-processing stage so narrator replies can be cleaned, validated, and annotated before persistence.
- **Acceptance criteria:**
  - Provide `IPostProcessorHook` implementations for meta-text stripping, lore consistency checks, narrator-style enforcement, and structured event extraction.
  - Stage emits `output.postprocessed` events summarizing adjustments plus any warnings for the UI/log panel.
  - Hooks can append metadata (e.g., detected NPC updates) for consumption by the MemoryManager stage.
  - Tests cover text normalization, lore violation detection, metadata emission, and failure propagation.

## MemoryManager stage
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** developer, **I want** the pipeline to persist chat entries, world state, and rolling summaries so future prompts have high-quality context.
- **Acceptance criteria:**
  - Integrate `AppDataService` operations into a `MemoryManagerStage` that appends player/narrator messages, updates NPC/inventory structures, and logs automation decisions.
  - Stage emits `state.memory.updated` with counts/timestamps plus optional summary references for long-running sessions.
  - Provide scaffolding for future rolling summaries (trigger thresholds, placeholder fields) even if summarization hooks are implemented later.
  - Tests verify persistence calls, event payloads, and resilience when storage is unavailable.

## Scenario export hooks
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** to save my adventure (sessions, personas, summaries, workflow settings) to local storage so I can resume on another device or after reinstalling.
- **Acceptance criteria:**
  - Add pipeline hooks (invoked via UI command or `@save`) that package the current `AppDataService` state, workflow configuration, and latest memory summaries into a portable JSON payload.
  - Emitting `scenario.export.requested` / `scenario.exported` lifecycle events lets the UI show progress/success or errors.
  - Downloads use browser-friendly mechanisms (e.g., File System Access API or generated file download) without exposing secrets in logs.
  - Tests cover serialization contents, event emission, and graceful failure when storage APIs are unavailable.

## Scenario import & restoration
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** to load a previously saved scenario so Narratoria rebuilds sessions and summaries automatically.
- **Acceptance criteria:**
  - Provide an import hook that validates snapshot schema versions, hydrates `AppDataService` sessions/personas/settings, and restores rolling summaries before the next pipeline run.
  - Emit `scenario.import.requested` / `scenario.imported` events so the UI can confirm the load or surface errors (with actionable messages when schema mismatch occurs).
  - Ensure restored data drives PromptAssembler/ModelRouter immediately (no restart required) and that export/import integrates with existing export/import backlog expectations.
  - Tests include successful import, schema mismatch handling, and partial failure (e.g., missing image workflow config) with recovery guidance.

## OutputFormatter & UI streaming integration
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** the UI to display stage-by-stage progress and final narrator output so I know exactly what the system is doing.
- **Acceptance criteria:**
  - Implement `OutputFormatterStage` that converts post-processed text + metadata into UI-ready structures, emits `output.ready`, and hands final replies back to `INarrationService`.
  - Update components (e.g., `NarrationStatusIndicator`, future progress indicator) to subscribe to the pipeline event stream and show “Checking safety… / Selecting model…” etc.
  - Provide a lightweight API (`INarrationPipelineEvents`) for other components (logging panel, future toasts) to observe the same stream without duplicating logic.
  - Tests cover UI bindings to lifecycle events, ensuring statuses change as stages progress and revert to idle on completion/failure.

## Image sketch workflow branch
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** rough sketch images generated at key story beats so the adventure includes lightweight visuals.
- **Acceptance criteria:**
  - Add optional pipeline hooks (`ImagePromptAssembler`, `ImageModelRouter`, `ImageClientStage`) that branch off after `PromptAssembler`, emit `image.prompt.assembled`, `image.model.selected`, `image.generated` events, and rejoin before `OutputFormatter`.
  - Reuse per-workflow settings for the Image workflow (endpoint/model/key/enable flag); disabling the workflow skips image hooks entirely.
  - Output formatter attaches generated image metadata/URIs to the final payload so the UI displays sketches alongside narrator text; lifecycle events make progress visible (“Sketching scene…”).
  - Tests cover enabled/disabled cases, event ordering, error handling, and integration with the existing narration stages.

## Workflow multi-model chaining
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** power user, **I want** to chain multiple models within a workflow (e.g., Model A → Model B) so I can refine outputs before presenting them.
- **Acceptance criteria:**
  - Extend `ModelRouterStage` and downstream stages to accept ordered lists of model slots per workflow; each slot has endpoint/model/key/enable flags stored in settings.
  - Pipeline lifecycle events include slot indices when emitting `model.selected` / `llm.response.received` so UI/logs show multi-step progress.
  - Provide config UI affordances (add/remove/reorder slots) and validation plus warnings about extra latency/cost.
  - Tests ensure chained invocations run in order, propagate intermediate results, handle per-slot failures gracefully, and skip disabled slots.

## System workflow state summarization
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** narrator system, **I want** to maintain a rolling working memory of the scenario so the narrator model receives concise, relevant context.
- **Acceptance criteria:**
  - System workflow maintains a rolling summary of past turns, applying heavier summarization to older messages while retaining recent details.
  - Summaries are generated via the System model and stored with timestamps/ordering so they can be refreshed as the session evolves.
  - Summaries plug into the Narration Pipeline: the MemoryManager stage updates them, and the PromptAssembler stage includes the latest summary when building narrator requests.
  - Tests verify summaries are produced, refresh over time, and are attached to narrator calls.

## System workflow cadence and persistence
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** the system workflow to refresh its working memory without slowing down narration so the story keeps flowing smoothly.
- **Acceptance criteria:**
  - System summarization runs asynchronously/off the critical path of sending a narrator request; narrator calls are not blocked on summaries when not ready.
  - Summaries are persisted per session (alongside chat history) and restored on reload; updates happen when new messages arrive or after a defined interval/count, and refreshed data flows through the Narration Pipeline’s MemoryManager + PromptAssembler stages automatically.
  - Summaries respect storage limits and fall back gracefully if storage is unavailable; logging captures refresh events and errors without leaking content.
  - Tests cover persistence/restore, non-blocking behavior, and storage-failure handling.

## Per-workflow API keys
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** to configure separate API keys/endpoints for the narrator, system, and image workflows so I can mix providers per task.
- **Acceptance criteria:**
  - Settings UI allows distinct endpoint/model/API key inputs for Narrator, System, and Image workflows with validation and secure local storage.
  - Update the Narration Pipeline's ModelRouter stage to use the corresponding credentials per workflow when building requests; ensure keys are not logged or leaked.
  - Export/import includes per-workflow credentials (redacted in UI logs) and preserves schemas for future migrations.
  - Automated tests cover saving, loading, and using the per-workflow settings.

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

## Workflow picker component
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** a reusable workflow picker so I can choose Narrator, System, or Image from a dropdown wherever it’s needed.
- **Acceptance criteria:**
  - Renders as a labeled dropdown/select that lists exactly the three workflows (Narrator/System/Image) and highlights the current selection.
  - Exposes a simple API for default/controlled selection and emits change events so callers can update the Narration Pipeline context/ModelRouter selection plus UI state.
  - Keyboard and screen reader accessible (combobox/select semantics, focusable, ESC/Enter/arrow navigation); honors dark/light themes.
  - Supports disabled/read-only state for contexts where workflow changes aren’t allowed; component is reusable across pages/dialogs without layout hacks.
  - Tests cover default selection, change handling, disabled state, and accessibility attributes/keyboard navigation.

## Targeted workflow sending
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** to explicitly route a message to the narrator, system, or image workflows so I control which engine responds.
- **Acceptance criteria:**
  - Reply editor (or adjacent controls) offers an explicit selector/switch to choose Narrator/System/Image before sending.
  - The selection feeds into the Narration Pipeline (PromptAssembler + ModelRouter stages) so prompts/request payloads use the chosen workflow and the metadata reflects it in chat logs.
  - Defaults to Narrator; preserves last selection per session; prevents sending without a selected workflow.
  - Tests verify routing changes request construction and the correct provider receives the message.

## Reply editor robustness
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** the reply editor to behave predictably so I don’t lose input or trigger duplicate sends.
- **Acceptance criteria:**
  - “Send” is disabled while a send is in progress and when the input is empty; a visual cue shows busy state.
  - Keyboard handling supports Enter to send and Shift+Enter to insert a newline without double submission.
  - Errors from `OnSend` path surface a non-blocking notification and keep the unsent text intact.
  - Tests validate empty-state behavior, busy-state guarding, and keyboard interactions.

## Progress indicator component
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** a progress indicator with text so I can see what’s happening and how far along it is.
- **Acceptance criteria:**
  - Supports both indeterminate (spinner + label) and determinate (progress bar + label) modes with a single API; label is optional but displayed when provided.
  - Determinate mode accepts a numeric progress value (0–100 or 0–1) and clamps to bounds; bar visually fills according to the value and animates smoothly on change.
  - Indeterminate mode shows a non-blocking spinner that does not reserve unnecessary width and does not mislead with arbitrary percentages.
  - Component can bind to Narration Pipeline lifecycle events so status text/progress reflect real stage updates without extra adapters.
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

## Persona management sheet
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** a dedicated property sheet to create, edit, and delete personas without leaving the current view.
- **Acceptance criteria:**
  - Reuses the property sheet component with a personas list and editor; supports add/edit/delete with name/avatar/prompt fields.
  - Selecting a persona from the sheet applies it to the active session; changes persist on save/apply with clear confirmation/error states.
  - Supports keyboard navigation, labeled inputs, and accessible dialog semantics; honors light/dark themes.
  - Tests cover loading existing personas, create/edit/delete flows, applying a persona to the active session, and validation/error handling.

## Client storage resilience
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** clear feedback and graceful fallback when browser storage is unavailable or full, so I understand whether my sessions are persisted.
- **Acceptance criteria:**
  - `BrowserClientStorageService` surfaces quota and unavailability errors with actionable messages; `AppDataService` catches these and updates UI/state with a user-friendly notification.
  - Falling back to in-memory/session storage preserves current session state when persistent storage fails, and informs the player of limited persistence.
  - Logging captures storage error context (area/key/operation) without leaking sensitive data.
  - Automated tests cover quota/unavailable scenarios.

## Service test coverage baseline
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** developer, **I want** unit tests for core services lacking coverage so session/data behaviors remain stable.
- **Acceptance criteria:**
  - Add tests for `AppDataService` covering session create/switch/delete, persona upsert/delete, and export/import paths with storage mocked.
  - Add tests for `NarrationService` ensuring empty input is ignored, system commands are filtered from history, status notifications fire in success/cancel/error flows, and narration fallback text is applied when responses are empty.
  - Expand coverage to include the narration pipeline orchestration service once introduced (context cloning, lifecycle event sequencing, cancellation propagation, and failure handling).
  - Tests run via `dotnet test` and gate completion of dependent backlog items.

## Component test coverage baseline
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** developer, **I want** automated tests for key UI components that currently have no coverage so regressions in rendering and interactions are caught early.
- **Acceptance criteria:**
  - Add component/integration tests for `Components/SessionsManager.razor` (renders sessions list, switch/delete/start actions call `IAppDataService` and update UI).
  - Add coverage for the home page scrollback (`Components/Pages/Home.razor`) ensuring active session heading/subheading and chat history update on `SessionsChanged` and `ChatSessionChanged`.
  - Add coverage for `Components/ReplyEditor.razor` or input flow to verify message send invokes `INarrationService` and handles empty/disabled states.
  - Tests run via `dotnet test` (component tests) and are required before marking related backlog items Done.

## Chat session as tabs
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** to be able to manage my sessions through a tab-based interface. Tabs should display the session title, and a close button. On the far right of the tabs I want a plus button which will add a new session and switch to its tab.
- **Acceptance criteria:**
  - Uses the session tab component to render all sessions; tab labels reflect session titles and the active tab matches the active session.
  - Tab click switches the active session via `IAppDataService` and refreshes chat/metadata for that session.
  - Close button deletes the session via `IAppDataService` and removes it from the list; tab strip updates without reload.
  - Plus button creates a new session via `IAppDataService` and selects its tab.
  - Keep the existing `@sessions` command available so sessions can still be managed via chat commands in addition to tabs.

## Session tab strip component
- **Status**: In Review
- **Assignee**: Codex
- **As a** player, **I want** a reusable tab component so I can swap between multiple pieces of content inline.
- **Acceptance criteria:**
  - Tab component renders a list of child components passed as children in Razor markup; each child corresponds to a tab with a label/title.
  - Tapping/clicking a tab makes its child the only visible panel; non-active children are hidden but remain mounted to preserve state.
  - Provides a simple API for callers to define tab order, labels, and initial selected tab; supports programmatic selection changes.
  - Keyboard accessibility: arrow keys move focus/selection between tabs, selection is indicated, and panels are navigable when active.
  - Visual active state differentiates the selected tab; layout centers the tab list within its parent.
  - Tests cover rendering multiple children, switching active tab via click/keyboard, maintaining child state, and initial selection behavior (component tests).
- **Technical summary**: Added a reusable `TabStrip`/`TabStripTab` component with iPadOS-inspired pill styling, preserved panels, and keyboard navigation; created a `/tabs-demo` page to exercise programmatic selection and child state retention.
- **Tests**: `dotnet test tests/NarratoriaClient.ComponentTests/NarratoriaClient.ComponentTests.csproj`

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
