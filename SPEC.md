# Narratoria Future Requirements

The following backlog items use Scrum-style acceptance criteria to clarify expected behavior for the MVP and near-term roadmap. See `CONTRIBUTORS.md` for contribution rules and workflow expectations.

## SafetyPolicyChecker stage
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** developer, **I want** pipeline-pluggable safety checks that can block or rewrite requests before they reach the model.
- **Acceptance criteria:**
  - Create `ISafetyCheckHook` interface and default hooks for mode compliance, explicit-mode gating, and tone restrictions/world rules.
  - Stage emits `safety.checked` events with pass/fail verdicts; failures produce actionable messages returned to the UI.
  - Pipeline short-circuits when a hook rejects input while still writing a narrator response explaining the rejection.
  - Tests cover allowed vs. blocked input, event payloads, and interaction with `INarrationService`.

## Safety policy toggle
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** to enable or disable safety checks so I can use explicit-friendly models when appropriate.
- **Acceptance criteria:**
  - Add a pipeline-aware setting/command that toggles the SafetyPolicyChecker stage on/off per session; disabled mode bypasses safety hooks but logs that protection is off.
  - Provide a dedicated component for the toggle so its state is cleanly bound to persisted settings and can be reused across UI surfaces.
  - UI surfaces the current safety state (on/off) and warns when disabled; toggling emits lifecycle events (`safety.toggle`) for transparency.
  - Pipeline respects the toggle without affecting other stages; defaults remain safe (enabled).
  - Tests cover toggling on/off, persistence per session, event emission, and ensuring narration proceeds correctly when safety is disabled.

## Scenario export hooks
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** to save my adventure (sessions, personas, summaries, workflow settings) to local storage so I can resume on another device or after reinstalling.
- **Acceptance criteria:**
  - Add pipeline hooks (invoked via UI command or `@save`) that package the current `AppDataService` state, workflow configuration, and latest memory summaries into a portable JSON payload.
  - Emitting `scenario.export.requested` / `scenario.exported` lifecycle events lets the UI show progress/success or errors.
  - Downloads use browser-friendly mechanisms (e.g., File System Access API or generated file download) without exposing secrets in logs.
  - Tests cover serialization contents, event emission, and graceful failure when storage APIs are unavailable.

## Scenario file attachments component
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** to attach one or more files (e.g., background story, reference docs) to the current scenario so the narrator can leverage context-specific materials.
- **Acceptance criteria:**
  - Provide a UI component to add/remove attachments tied to the active session; preserves original filenames and MIME types; only accepts plain-text formats (e.g., .txt, .md) with a 5 MB per-file limit.
  - Attachments are stored with scenario-specific data (aligned with export/import) and do not leak across sessions.
  - PromptAssembler (or a dedicated hook) can surface attachment metadata to workflows without transmitting file contents unless explicitly enabled.
  - Tests cover uploading/removing attachments, MIME type preservation, session association, and export/import of attachment metadata.

## Scenario import & restoration
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** to load a previously saved scenario so Narratoria rebuilds sessions and summaries automatically.
- **Acceptance criteria:**
  - Provide an import hook that validates snapshot schema versions, hydrates `AppDataService` sessions/personas/settings, and restores rolling summaries before the next pipeline run.
  - Emit `scenario.import.requested` / `scenario.imported` events so the UI can confirm the load or surface errors (with actionable messages when schema mismatch occurs).
  - Ensure restored data drives PromptAssembler/ModelRouter immediately (no restart required) and that export/import integrates with existing export/import backlog expectations.
  - Tests include successful import, schema mismatch handling, and partial failure (e.g., missing image workflow config) with recovery guidance.

## Message deletion
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** to delete a chat message so it is removed from stored data, memory, and the context sent to the narrator.
- **Acceptance criteria:**
  - Provide a UI affordance to delete individual messages in the active session; confirm before removal and update the scrollback immediately.
  - Deleting a message removes it from `AppDataService` persistence and excludes it from future prompt assembly/context summaries.
  - MemoryManager and any rolling summaries are updated to reflect the deletion; exports/imports honor the updated message history.
  - Tests cover deleting narrator/player messages, persistence changes, prompt/context exclusion, and UI refresh behavior.

## Transient command rendering
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** slash commands to surface a temporary UI near the reply editor (not in scrollback) so I can act on them without polluting history or LLM context.
- **Acceptance criteria:**
  - Add a transient command log (non-persistent) populated when the command handler runs; entries include token, args, author, and timestamp.
  - Chat scrollback merges persisted messages with transient command entries for rendering; transient entries clear on session switch/reload and are bounded to prevent growth.
  - Unknown command errors surface as transient entries/notifications instead of persisted chat.
  - Tests cover rendering transient commands, clearing on session changes, and ensuring commands are excluded from persisted storage and LLM prompts.

## Player message rewriting stage
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** my input rewritten into narration-friendly prose before it’s stored so the story feels cohesive without extra effort.
- **Acceptance criteria:**
  - Add a pre-append pipeline stage (before `PlayerMessageRecorderStage`) that rewrites the player’s input (LLM or deterministic) into narrator-friendly text.
  - Persist both original and rewritten content; only the rewritten version is used for prompts/context and UI, while the original is retained for audit/export.
  - Tests cover persistence of rewritten/original fields and ensuring PromptAssembler uses the rewritten content.

## Player message rewriting stage
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** my input rewritten into narration-friendly prose before it’s stored so the story feels cohesive without extra effort.
- **Acceptance criteria:**
  - Add a pre-append pipeline stage (before `PlayerMessageRecorderStage`) that rewrites the player’s input (LLM or deterministic) into narrator-friendly text.
  - The rewritten text is what gets persisted and sent to the narrator; retain the original for audit/export.
  - Provide a toggle to enable/disable rewriting per session; when disabled, the original input is stored as-is.
  - Tests cover enabled/disabled behavior, persistence of rewritten text, and ensuring PromptAssembler uses the rewritten content.

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

## System workflow context enrichment
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** developer, **I want** the system workflow to receive a concise summary of configuration and session state so it can answer meta-questions and manage automation.
- **Acceptance criteria:**
  - PromptAssembler enriches system-targeted requests with a brief summary of current configuration (enabled workflows, selected models, safety toggle state) and a short per-session summary (name, created date, last activity, message count).
  - The enrichment stays lightweight (bounded length) and redacts secrets (API keys).
  - Lifecycle events or logs indicate when system context enrichment is attached, so we can debug what the system sees.
  - Tests cover inclusion/exclusion of config/session summaries, length bounds, and redaction behavior.

## System workflow command awareness
- **Status**: Proposed
- **Assignee**: Unassigned
- **As a** player, **I want** the system workflow to list and invoke built-in commands when asked (e.g., “@system what commands can I use?”).
- **Acceptance criteria:**
  - PromptAssembler includes a structured list of available chat commands (token + display name) when the target workflow is `system`, so the system agent knows which commands exist.
  - System workflow responses are parsed for commands and, when a recognized command is present, the pipeline routes it through the command handler instead of the narrator path; commands issued by the system are rendered in the UI just like player-issued commands.
  - Guardrails: system prompts instruct the agent to emit commands only when relevant (e.g., when asked for help) and to avoid recursive @system calls; unknown commands render a safe fallback.
  - Tests cover prompt enrichment for system workflow, system-issued command execution, and safeguards against runaway or recursive command emissions.

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
