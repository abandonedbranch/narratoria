## spec: <name>

mode:
  - isolated | compositional | stateful
  # isolated: pure function, no shared state
  # compositional: cooperates with collaborators, no owned state
  # stateful: reads/writes owned state (describe scope/persistence)

behavior:
  - what: <one-line behavior summary>
  - input:
      - <type>: <semantic role>
      - <type>: <semantic role>
  - output:
      - <type>: <semantic role>
  - caller_obligations:
      - <auth/session/state loading requirements>
  - side_effects_allowed:
      - <permitted IO or state changes>

state:
  - <state_key>: <type> | <persistence_scope>

preconditions:
  - <logical predicate>
  - <logical predicate>

postconditions:
  - <logical predicate>
  - <logical predicate>

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
  - <explicit exclusion>
  - <explicit exclusion>

performance:
  - <upper bound>
  - <latency SLO>

observability:
  - logs:
      - <required fields e.g., trace_id, request_id, stage, elapsed_ms, status, error_class>
  - metrics:
      - <required metrics>

output:
  - minimal implementation only (no commentary, no TODOs)
