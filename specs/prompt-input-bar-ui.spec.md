## spec: prompt-input-bar-ui

mode:
  - stateful (maintains transient input buffer; no owned persistent state)

behavior:
  - what: Capture user prompt text and invoke a submission delegate with gating and deterministic retry semantics.
  - input:
      - Func<string, CancellationToken, ValueTask> SubmitPrompt : submission delegate invoked on user action
      - bool IsSubmitting : disables input and submission while true
      - bool IsBlocked : additional caller-controlled gating (e.g., attachment ingestion in-flight)
      - string? InitialText : optional seed text
  - output:
      - RenderFragment : input field and submit button UI
  - caller_obligations:
      - provide a SubmitPrompt delegate that is idempotent or server-side guarded
      - propagate CancellationToken from the page/component
  - side_effects_allowed:
      - invoke SubmitPrompt exactly once per user action

state:
  - input_text : string | ephemeral UI memory
  - submission_error : string? | ephemeral UI memory

preconditions:
  - SubmitPrompt is non-null

postconditions:
  - successful submission clears input_text and hides submission_error
  - failure preserves input_text and shows submission_error; IsSubmitting resets per caller state

invariants:
  - serialized submissions: while IsSubmitting is true, submission actions are disabled
  - while IsBlocked is true, submission actions are disabled
  - deterministic behavior: same inputs produce same UI states

failure_modes:
  - submission_error :: SubmitPrompt throws or returns faulted task :: show error banner; keep input for retry
  - cancelled :: cancellation_token requested :: no submission; hide error; keep input

policies:
  - no implicit retries; user must re-submit
  - cancellation: honor provided token

never:
  - mutate external state beyond invoking SubmitPrompt
  - emit system prompts or internal configuration

non_goals:
  - multiline editing or rich formatting
  - history navigation or suggestions

performance:
  - submission UI updates under 50ms per action on target hardware

non_functional_requirements:
  - accessibility (WCAG 2.2 AA):
    - labels: input has a programmatic `label` and helper description; button has accessible name
    - states: disabled via `aria-disabled`; error banner announced via `aria-live="assertive"`
    - keyboard: Enter submits when enabled; Space toggles; ESC clears errors only when safe
  - responsive_ux:
    - layout: input and button stack on mobile; controls remain fully visible; targets ≥44x44 px
    - overflow: long input lines handle gracefully without horizontal scroll in main content
  - performance_budgets:
    - interaction updates ≤50ms; debounce policies explicit if introduced
  - testing_hooks:
    - axe-core on pages containing the bar; fail CI on violations
    - keyboard-only submit path
    - viewport matrix assertions

observability:
  - logs:
      - trace_id, request_id, event (submit|submit_error|submit_cancelled), elapsed_ms
  - metrics:
      - prompt_submit_count (by status), prompt_submit_latency_ms

output:
  - minimal implementation only (no commentary, no TODOs)
