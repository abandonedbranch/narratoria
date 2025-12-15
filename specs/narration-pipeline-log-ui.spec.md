## spec: narration-pipeline-log-ui

mode:
  - stateful (maintains append-only turns and in-flight input; collaborates with caller-supplied stage order)

behavior:
  - what: Render a vertical narration log with stage chips per turn, stream narration output, and accept a new prompt submission.
  - input:
      - IReadOnlyList<NarrationPipelineTurnView> : ordered turns (oldest to newest) with immutable content
      - IReadOnlyList<NarrationStageKind> : canonical pipeline stage ordering used to render chips
      - EventCallback<string> OnSubmitPrompt : invoked when user submits the next prompt
      - bool IsSubmitting : gate that disables input while true
  - output:
      - RenderFragment : UI tree containing turn blocks and prompt input row
  - caller_obligations:
      - supply stable identifiers for each turn and stage; do not mutate prior turn payloads once rendered
      - propagate streaming tokens into the most recent turn via stage status updates and output segments
      - ensure OnSubmitPrompt is idempotent or guarded against double submission server-side
  - side_effects_allowed:
      - emit OnSubmitPrompt exactly once per user action
      - auto-scroll to latest turn when a new turn or stream segment arrives
      - trigger hover popover describing chip metadata

state:
  - turns : IReadOnlyList<NarrationPipelineTurnView> | ephemeral UI memory
  - input_text : string | ephemeral UI memory
  - active_stream : NarrationStreamState? | ephemeral UI memory
  - hover_metadata : NarrationStageHover? | ephemeral UI memory
  - NarrationPipelineTurnView: record { Guid TurnId; string UserPrompt; DateTimeOffset? PromptAt; ImmutableArray<NarrationStageView> Stages; NarrationOutputView Output } | provided by caller
  - NarrationStageView: record { NarrationStageKind Kind; NarrationStageStatus Status; TimeSpan? Duration; int? PromptTokens; int? CompletionTokens; string? Model; string? ErrorClass; string? ErrorMessage } | provided by caller
  - NarrationOutputView: record { bool IsStreaming; string? FinalText; ImmutableArray<string> StreamedSegments } | provided by caller
  - NarrationStageKind: enum { Sanitize, Context, Lore, Llm, Image, Custom(string Name) } | render label text
  - NarrationStageStatus: enum { Pending, Running, Completed, Skipped, Failed } | visual state mapping
  - NarrationStreamState: record { Guid TurnId; NarrationStageKind Stage; ImmutableArray<string> Segments } | tracks current streaming turn
  - NarrationStageHover: record { Guid TurnId; NarrationStageKind Stage; TimeSpan? Duration; int? PromptTokens; int? CompletionTokens; string? Model } | hover payload

preconditions:
  - stage order is non-empty and contains unique NarrationStageKind values
  - every turn Stages array aligns with stage order (one entry per stage) and is immutable once rendered
  - active_stream, if present, references the newest turn and a stage whose status is Running

postconditions:
  - turns render in chronological order; prior turns are read-only
  - stage chips render in caller-supplied order; downstream chips remain Pending until upstream completes or fails
  - OnSubmitPrompt invocation clears input_text and re-disables submit UI until IsSubmitting becomes false
  - streaming segments append in-order to the active turn Output; when status flips to Completed or Failed, streaming stops

invariants:
  - log is append-only; existing turn content is never edited by the component
  - only one stage per turn may be Running at a time
  - Failed status halts rendering of Running/Completed for downstream chips until caller replays the turn
  - visuals remain deterministic for a given input model (no randomness in ordering or styling choices)

failure_modes:
  - validation_error :: missing stage in stage order or duplicate kinds :: render error state and suppress input submission
  - submission_error :: OnSubmitPrompt throws or returns faulted task :: show error banner; keep input text for retry; do not append turn
  - stream_mismatch :: active_stream references non-latest turn :: drop segments and log warning

policies:
  - no implicit retries; caller must resubmit failed prompt explicitly
  - input submission is serialized; multiple submissions are queued or rejected while IsSubmitting is true
  - cancellation: caller may set IsSubmitting=false to re-enable input after upstream cancellation; streaming stops when status leaves Running

never:
  - expose system or hidden prompts in the UI
  - mutate or reorder historical turns
  - mark downstream chips Completed when any upstream chip Failed or Skipped
  - drop hover metadata fields when provided

non_goals:
  - presenting per-stage raw payloads or token-level timelines
  - multi-user collaboration or presence indicators
  - theming or visual design tokens beyond state colors and chip shapes

performance:
  - render incremental updates within 50ms per new segment on target hardware
  - keep initial render under 100ms for 50 turns and 5 stages each

observability:
  - logs:
      - trace_id, request_id, turn_id, stage, status, elapsed_ms, error_class, event (submit|stream_append|render_error)
  - metrics:
      - narration_log_render_ms (histogram), narration_prompt_submitted (counter), narration_stage_hover_shown (counter), narration_stream_segments_appended (counter)

output:
  - minimal implementation only (no commentary, no TODOs)
