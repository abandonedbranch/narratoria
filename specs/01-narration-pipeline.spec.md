## spec: Narration pipeline

mode:
  - compositional

behavior:
  - what: Data flows source → transforms → sink.
  - how:
    1. Read from narration_source.
    2. Transform through the chain.
    3. Write to narration_sink.
  - input:
      - narration_source: generator of source data for narration
      - narration_sink: data consumer with no output
  - output:
    - errors: raised from source, transforms, or sink
  - caller_obligations:
    - Construct pipeline with narration_source, transforms, narration_sink; set initial state (running | paused | stopped).
    - Provide narration_source `<TSrc>` and narration_sink `<TSink>` instances at invocation.
    - Poll/read output to completion or cancel so persistence/telemetry finish.
    - Do not reapply system/safety prompts if already applied; propagate exceptions.
  - side_effects_allowed:
      - none

state:
  - pipeline_state: { running | paused | stopped } | ephemeral/session

preconditions:
  - narration_source != null && compatible <TSrc>
  - narration_sink != null && compatible <TSink>
  - transforms length > 0 && types compose <TSrc> -> ... -> <TSink>
  - pipeline_state ∈ {running|paused|stopped}; start stopped for a new run

postconditions:
  - Transformed narration is delivered to narration_sink in produced order.
  - If success, errors == ∅; else errors records source/transform/sink failures.

invariants:
  - <always-true predicate>
  - <determinism/thread-safety constraints>

failure_modes:
  - <error_class> :: <trigger_condition> :: <mandatory_side_effect>
  - <error_class> :: <trigger_condition> :: <mandatory_side_effect>

policies:
  - <retry | timeout | idempotency | rate_limit>
  - <concurrency model | cancellation>

never:
  - <forbidden behavior>
  - <forbidden side effect>

non_goals:
  - narration_source or narration_sink implementations
  - specific transformation elements

performance:
  - <upper bound>
  - <latency SLO>

observability:
  - logs:
      - <required fields>
  - metrics:
      - <required metrics>

output:
  - minimal implementation only (no commentary, no TODOs)
