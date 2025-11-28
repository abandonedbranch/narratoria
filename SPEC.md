## Scenario file attachments component

You are implementing one isolated behavior.

Behavior:
- WHAT: Attach text files to a scenario
- INPUT: .txt or .md files ≤ 5 MB from the active session
- OUTPUT: Attachment metadata bound to ScenarioId
- FAILS: Non-text files or files > 5 MB are rejected
- NEVER: File contents sent to LLM by default

Invariants:
- No cross-scenario leakage
- No file body in LLM context by default

Write the minimal implementation.
No commentary. No extras.

## Scenario import & restoration

You are implementing one isolated behavior.

Behavior:
- WHAT: Import a saved scenario snapshot
- INPUT: Exported scenario file with schema version
- OUTPUT: Sessions, personas, settings, and summaries restored
- FAILS: Schema mismatch or corrupted snapshot
- NEVER: Require app restart after import

Invariants:
- Restored data immediately drives the pipeline
- Import never partially mutates active state on failure

Write the minimal implementation.
No commentary. No extras.

## Message deletion

You are implementing one isolated behavior.

Behavior:
- WHAT: Delete a single chat message
- INPUT: MessageId within the active session
- OUTPUT: Message removed from UI, storage, and prompt context
- FAILS: Invalid or nonexistent MessageId
- NEVER: Deleted messages appear in prompts or summaries

Invariants:
- Deletion updates persistence and memory together
- UI reflects deletion immediately

Write the minimal implementation.
No commentary. No extras.

## Transient command rendering

You are implementing one isolated behavior.

Behavior:
- WHAT: Render slash commands as transient UI entries
- INPUT: Parsed slash command invocation
- OUTPUT: Temporary UI entry near the editor
- FAILS: Unknown command
- NEVER: Persist transient commands to storage or LLM context

Invariants:
- Transient entries clear on session change or reload
- Persisted chat history is never polluted

Write the minimal implementation.
No commentary. No extras.

## Player message rewriting stage

You are implementing one isolated behavior.

Behavior:
- WHAT: Rewrite player input into narration-friendly prose
- INPUT: Raw player text
- OUTPUT: Rewritten text stored and sent to narrator
- FAILS: Rewriting model unavailable or rewrite error
- NEVER: Lose the original player input

Invariants:
- Rewritten text is what the narrator sees
- Original text is always retained for audit/export

Write the minimal implementation.
No commentary. No extras.

## Player message rewriting stage

You are implementing one isolated behavior.

Behavior:
- WHAT: Rewrite player input into narration-friendly prose
- INPUT: Raw player text
- OUTPUT: Rewritten text stored and sent to narrator
- FAILS: Rewriting model unavailable or rewrite error
- NEVER: Lose the original player input

Invariants:
- Rewritten text is what the narrator sees
- Original text is always retained for audit/export

Write the minimal implementation.
No commentary. No extras.

## OutputFormatter & UI streaming integration

You are implementing one isolated behavior.

Behavior:
- WHAT: Stream pipeline stage progress and final output to the UI
- INPUT: Post-processed pipeline output + lifecycle events
- OUTPUT: UI-ready narration payload and live status updates
- FAILS: Any stage emits an error
- NEVER: Suppress stage progress from the UI

Invariants:
- UI reflects real pipeline state
- Final output is emitted exactly once per run

Write the minimal implementation.
No commentary. No extras.

## Image sketch workflow branch

You are implementing one isolated behavior.

Behavior:
- WHAT: Generate rough sketch images during narration
- INPUT: Prompt output from PromptAssembler
- OUTPUT: Image metadata linked to narrator output
- FAILS: Image model call fails or is disabled
- NEVER: Block narrator output when image generation fails

Invariants:
- Image workflow is fully optional
- Narration path always completes independently

Write the minimal implementation.
No commentary. No extras.

## Workflow multi-model chaining

You are implementing one isolated behavior.

Behavior:
- WHAT: Chain multiple models in sequence within a workflow
- INPUT: Ordered list of enabled model slots
- OUTPUT: Final result from the last enabled model
- FAILS: Any enabled slot fails
- NEVER: Execute disabled model slots

Invariants:
- Models execute strictly in configured order
- Intermediate outputs propagate forward only

Write the minimal implementation.
No commentary. No extras.

## System workflow state summarization

You are implementing one isolated behavior.

Behavior:
- WHAT: Maintain a rolling summary of scenario state
- INPUT: Ongoing session message stream
- OUTPUT: Continuously refreshed summary for the narrator
- FAILS: Summarization model failure
- NEVER: Block narration on summary generation

Invariants:
- Older content is summarized more aggressively than recent
- Latest summary is always used when available

Write the minimal implementation.
No commentary. No extras.

## System workflow context enrichment

You are implementing one isolated behavior.

Behavior:
- WHAT: Enrich system workflow prompts with config and session context
- INPUT: System-targeted prompt request
- OUTPUT: Bounded, redacted context summary
- FAILS: Missing config or session state
- NEVER: Leak secrets or API keys

Invariants:
- Context length is strictly bounded
- Secrets are always redacted

Write the minimal implementation.
No commentary. No extras.

## System workflow command awareness

You are implementing one isolated behavior.

Behavior:
- WHAT: Allow the system workflow to list and invoke chat commands
- INPUT: System workflow prompt containing a command
- OUTPUT: Routed command execution
- FAILS: Unknown or malformed command
- NEVER: Allow recursive system command loops

Invariants:
- Only known commands may execute
- Safeguards prevent runaway recursion

Write the minimal implementation.
No commentary. No extras.

## System workflow cadence and persistence

You are implementing one isolated behavior.

Behavior:
- WHAT: Refresh and persist system summaries asynchronously
- INPUT: New session messages or refresh trigger
- OUTPUT: Updated persisted summaries
- FAILS: Storage unavailable
- NEVER: Block narration on summary I/O

Invariants:
- Summaries persist across reloads
- Failures degrade gracefully

Write the minimal implementation.
No commentary. No extras.

## Per-workflow API keys

You are implementing one isolated behavior.

Behavior:
- WHAT: Store and apply distinct API credentials per workflow
- INPUT: User-provided endpoint, model, and API key
- OUTPUT: Correct credentials used per request
- FAILS: Missing or invalid credentials
- NEVER: Log or leak raw API keys

Invariants:
- Credentials are scoped per workflow
- Secrets never appear in logs

Write the minimal implementation.
No commentary. No extras.

## Workflow-specific system prompts

You are implementing one isolated behavior.

Behavior:
- WHAT: Edit and persist a system prompt per workflow
- INPUT: User-edited prompt text
- OUTPUT: Updated prompt applied to outbound requests
- FAILS: Empty or invalid prompt
- NEVER: Apply the wrong prompt to a workflow

Invariants:
- Each workflow has exactly one active system prompt
- Prompt changes apply immediately

Write the minimal implementation.
No commentary. No extras.

## System prompt editor safety note

You are implementing one isolated behavior.

Behavior:
- WHAT: Display a provider TOS safety reminder in the prompt editor
- INPUT: Prompt editor rendered
- OUTPUT: Visible compliance notice
- FAILS: Missing provider policy URL
- NEVER: Allow the note to be hidden silently

Invariants:
- The note is always visible near the editor
- The note appears for all workflows

Write the minimal implementation.
No commentary. No extras.

## Workflow picker component

You are implementing one isolated behavior.

Behavior:
- WHAT: Select the active workflow from a reusable UI control
- INPUT: User workflow selection
- OUTPUT: Updated active workflow state
- FAILS: Invalid workflow value
- NEVER: Allow an undefined workflow to be selected

Invariants:
- Exactly one workflow is always selected
- Selection is accessible and theme-safe

Write the minimal implementation.
No commentary. No extras.

## Targeted workflow sending

You are implementing one isolated behavior.

Behavior:
- WHAT: Route messages explicitly to a chosen workflow
- INPUT: User message + selected workflow
- OUTPUT: Correct workflow receives the request
- FAILS: No workflow selected
- NEVER: Send without an explicit workflow context

Invariants:
- Last selected workflow persists per session
- Default workflow is Narrator

Write the minimal implementation.
No commentary. No extras.

## Reply editor robustness

You are implementing one isolated behavior.

Behavior:
- WHAT: Prevent duplicate sends and lost input
- INPUT: User keystrokes and send actions
- OUTPUT: Exactly one message per send
- FAILS: Send error from narration service
- NEVER: Drop unsent text on failure

Invariants:
- Send is disabled while busy
- Empty messages cannot be sent

Write the minimal implementation.
No commentary. No extras.

## Client storage resilience

You are implementing one isolated behavior.

Behavior:
- WHAT: Gracefully handle browser storage failures
- INPUT: Storage read/write operation
- OUTPUT: User-visible persistence status
- FAILS: Quota exceeded or storage unavailable
- NEVER: Crash or silently lose session data

Invariants:
- In-memory fallback always preserves the active session
- Users are always informed of persistence limits

Write the minimal implementation.
No commentary. No extras.

## Service test coverage baseline

You are implementing one isolated behavior.

Behavior:
- WHAT: Add baseline unit test coverage for core services
- INPUT: Existing service implementations
- OUTPUT: Passing automated tests under dotnet test
- FAILS: Any uncovered critical session or narration path
- NEVER: Merge without test coverage

Invariants:
- Core services always have regression coverage
- Test failures block completion

Write the minimal implementation.
No commentary. No extras.

## Component test coverage baseline

You are implementing one isolated behavior.

Behavior:
- WHAT: Add automated tests for key UI components
- INPUT: User interaction with sessions and reply flow
- OUTPUT: Verified rendering and interaction behavior
- FAILS: Any untested critical UI path
- NEVER: Ship UI changes without regression tests

Invariants:
- Tests reflect real user flows
- All tests run in CI

Write the minimal implementation.
No commentary. No extras.

## Theme management command

You are implementing one isolated behavior.

Behavior:
- WHAT: List and switch themes via chat command
- INPUT: @themes command
- OUTPUT: Displayed theme options and updated selection
- FAILS: Invalid theme selection
- NEVER: Lose the persisted theme choice

Invariants:
- Selected theme applies immediately
- Theme persists across reloads

Write the minimal implementation.
No commentary. No extras.

## Theme switching UI

You are implementing one isolated behavior.

Behavior:
- WHAT: Toggle application theme via UI control
- INPUT: User toggle action
- OUTPUT: Updated active theme and persisted value
- FAILS: Theme persistence error
- NEVER: Apply a partially updated theme

Invariants:
- UI updates immediately on change
- Theme persists across reloads

Write the minimal implementation.
No commentary. No extras.
