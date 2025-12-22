## spec: narration system prompt element

mode:
  - compositional

behavior:
  - what: Insert a configured system prompt and instruction block into the working narration context before provider dispatch so system guidance always precedes player/attachment content.
  - input:
      - NarrationContext: Session-scoped pipeline context carrying PlayerPrompt, PriorNarration, WorkingNarration, Metadata, Trace.
      - SystemPromptProfile: profile_id, prompt_text, instructions (ordered strings), version/etag.
      - WorkingContextSegments: ordered context segments accumulated for the provider request (player prompt, attachments, narration history).
  - output:
      - MiddlewareResult: Downstream result with NarrationContext updated to include system prompt and instructions as highest-priority segments plus metadata annotations.
  - caller_obligations:
      - Register element ahead of provider_dispatch and after any context-building elements that populate WorkingContextSegments.
      - Supply a non-empty SystemPromptProfile per session/request (static config or injected resolver).
      - Propagate the pipeline CancellationToken.
  - side_effects_allowed:
      - Read system prompt profile from injected provider/resolver.
      - Mutate the flowing NarrationContext (immutable copy) to prepend system prompt/instruction segments and update Metadata.
      - Emit structured logs and metrics only.

state:
  - none: stateless element with no persistence or cross-session cache

context:
  - WorkingContextSegments:
      - Ordered ImmutableArray<ContextSegment> representing the prompt passed to provider_dispatch.
      - ContextSegment: { Role: system | instruction | user | attachment | history, Content: string, Source: string }.
  - mutation:
      - Prepend one ContextSegment for prompt_text (Role=system) and one per instruction (Role=instruction) ahead of existing segments; preserve relative order of existing segments.

preconditions:
  - SystemPromptProfile resolves and prompt_text is non-empty.
  - WorkingContextSegments exists (may be empty) on NarrationContext or in Metadata under a reserved key.
  - CancellationToken is not already canceled.

postconditions:
  - On success, WorkingContextSegments begins with the system prompt segment followed by instruction segments; remaining segments retain their original order and content.
  - Metadata includes system_prompt_profile_id and system_prompt_version entries for downstream observability.
  - MiddlewareResult.StreamedNarration is passed through unchanged; UpdatedContext carries the modified WorkingContextSegments.
  - On failure, emit a structured NarrationPipelineError (stage=system_prompt_injection) and do not invoke downstream elements.

invariants:
  - System prompt and instruction segments appear exactly once per pipeline run and precede attachments and player/user prompts.
  - PlayerPrompt, PriorNarration, WorkingNarration, and existing context segments are not mutated or dropped.
  - Deterministic ordering: insertion order is stable given the same inputs.
  - Thread-safe and side-effect free beyond context mutation and observability hooks.

failure_modes:
  - PromptUnavailable :: System prompt profile missing or prompt_text empty :: emit NarrationPipelineError (stage=system_prompt_injection) and short-circuit.
  - ContextMissing :: WorkingContextSegments unavailable or null :: emit NarrationPipelineError (stage=system_prompt_injection) and short-circuit.
  - Cancellation :: CancellationToken signaled :: propagate cancellation without invoking downstream elements.

policies:
  - Ordering: must execute before provider_dispatch; should follow any elements that build WorkingContextSegments (attachments, history, templating).
  - Idempotency: if Metadata indicates the same profile_id/version already injected, skip reinsertion to avoid duplicates.
  - Retry: none; failures are terminal for the pipeline run.
  - Concurrency: safe under concurrent sessions; no shared mutable state.
  - Cancellation: check token before and after profile resolution and before mutating context.

never:
  - Log or emit raw system prompt or instruction text.
  - Reorder or modify existing non-system segments beyond shifting them after the inserted system prompt/instructions.
  - Persist system prompt content to session storage.
  - Invoke the narration provider or perform network/file IO.

non_goals:
  - Selecting or authoring system prompts; the element only applies the provided profile.
  - Prompt templating, summarization, or attachment ingestion.
  - Provider selection or timeout policy changes.

performance:
  - Insertion is in-memory and O(n) over segment count; target <5ms per invocation with minimal allocations.

observability:
  - logs:
      - trace_id, request_id, session_id, stage (system_prompt_injection), status, error_class, prompt_profile_id, prompt_version, elapsed_ms
  - metrics:
      - system_prompt_injection_count (by status/error_class), system_prompt_injection_latency_ms

output:
  - minimal implementation only (no commentary, no TODOs)
