## spec: system-prompt-profile-resolver

mode:
  - compositional (cooperates with configuration or storage; no owned persistent state required)

behavior:
  - what: Resolve the active `SystemPromptProfile` for a narration session to drive system prompt injection middleware.
  - input:
      - Guid : session_id for which to resolve a profile
      - CancellationToken : cancellation
  - output:
      - SystemPromptProfile? : nullable resolved profile (null when unavailable)
  - caller_obligations:
      - provide a valid session_id correlated to current narration run
      - honor cancellation propagation (do not ignore provided token)
  - side_effects_allowed:
      - read-only access to configuration or storage

state:
  - none (implementation MAY cache profiles in memory with deterministic eviction policy; caching MUST not change returned values for the same inputs within a single process lifetime)

preconditions:
  - session_id is non-default (not Guid.Empty)
  - resolver has access to its configured profile source (config or store)

postconditions:
  - returns a `SystemPromptProfile` when available; returns null when not found or disabled
  - no mutation of external state occurs

invariants:
  - pure resolution: same inputs yield same output within a process lifetime, unless underlying source changes deterministically and is versioned
  - thread-safe: concurrent calls for different session_ids MUST be safe
  - deterministic selection: if multiple profiles exist, selection MUST be deterministic based on configured policy

failure_modes:
  - configuration_error :: missing/invalid profile source :: emit log with error_class=config_error; return null
  - storage_error :: transient read failure :: emit log with error_class=io_error; return null
  - cancelled :: cancellation_token requested :: do not resolve; throw OperationCanceledException

policies:
  - timeout: caller controls via cancellation; resolver MUST not implement its own timeouts
  - idempotency: repeated calls for same session_id MAY return cached value; returned value MUST match source policy
  - concurrency: implementation MUST avoid shared mutable state; use immutable snapshots or concurrent-safe caches

never:
  - write to storage or configuration
  - block indefinitely without honoring cancellation
  - generate non-deterministic profile content (e.g., random text)

non_goals:
  - authoring or editing of profile content
  - multi-tenant access control enforcement (assumed upstream)

performance:
  - resolve within 5ms for in-memory/config sources; within 50ms for local storage sources on target hardware

observability:
  - logs:
      - trace_id, request_id, session_id, status (success|not_found|error|cancelled), error_class, elapsed_ms
  - metrics:
      - system_prompt_resolve_ms (histogram), system_prompt_resolve_count (counter by status)

output:
  - minimal implementation only (no commentary, no TODOs)
