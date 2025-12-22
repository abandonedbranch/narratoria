## spec: narration-session-title-element

mode:
  - compositional (generates and persists session title updates via collaborators)

behavior:
  - what: Set a relevant session title at least once using an inexpensive LLM call, without overwriting user-set titles.
  - input:
      - NarrationContext : includes SessionId and narration content sufficient to derive a title
      - IOpenAiApiService : collaborator used to generate a short title
      - INarrationSessionStore : collaborator used to persist the title
      - TitleOptions: record { string Model; int MaxChars; int MaxTokens }
      - CancellationToken
  - output:
      - MiddlewareResult : passes through downstream result; title update is a side effect
  - caller_obligations:
      - register this element after persistence has loaded session state and after provider dispatch has produced sufficient narration
      - ensure title updates are guarded by IsTitleUserSet
  - side_effects_allowed:
      - call IOpenAiApiService with a system prompt that requests a short title
      - persist title to the session store if and only if IsTitleUserSet is false and title has not been set previously

state:
  - none (uses collaborators)

preconditions:
  - SessionId exists in the session store
  - title generation is only attempted when there is sufficient content to name the session

postconditions:
  - at least one successful run sets a non-empty title for sessions that are not user-titled
  - once IsTitleUserSet is true, automatic title updates never occur

invariants:
  - title updates are idempotent for a given session once a non-default title is set
  - title generation never includes system prompt text or hidden configuration

failure_modes:
  - ProviderError :: OpenAI call fails :: log and continue without failing the narration pipeline
  - PersistenceError :: store rename fails :: log and continue without failing the narration pipeline
  - Cancellation :: token signaled :: abort title update; do not fail narration pipeline

policies:
  - retry: none implicit
  - timeout: honor configured timeouts via OpenAi context/options
  - cancellation: honor token

never:
  - overwrite user-set titles
  - block or short-circuit provider dispatch or persistence

non_goals:
  - user-facing title editing UI
  - multilingual title policies

performance:
  - title generation should complete within 500ms under normal conditions

observability:
  - logs:
      - trace_id, request_id, session_id, stage (session_title_update), elapsed_ms, status, error_class
  - metrics:
      - session_title_update_count (by status), session_title_update_latency_ms

output:
  - minimal implementation only (no commentary, no TODOs)
