# Research Findings: Narrator Agent & Skills System (004)

## Decisions

- Decision: Support multi-turn planning with clarification step
  - Rationale: Reduces plan errors for ambiguous inputs; allows agent to request missing details before finalizing the plan.
  - Alternatives considered: Single-shot planning (faster but higher error rate); full interactive loop (too heavy for P1 scope).

- Decision: Append-only state with compensating actions (guided undo)
  - Rationale: Preserves provenance and auditability; atomic per skill; avoids complex distributed transactions.
  - Alternatives considered: Hard rollback across skills; pure append-only without undo.

- Decision: Reactive by default; proactive suggestions behind preference flag
  - Rationale: Keeps agent predictable; enables opt-in autonomy aligning with player preferences.

- Decision: Reputation = numeric score with derived qualitative status
  - Rationale: Numeric enables thresholds; qualitative statuses derived (Hostile → Allied).

- Decision: Quest dependencies modeled as a DAG with prerequisites
  - Rationale: Supports linear, branching, networked quests; enables cycle validation.

- Decision: External tools (CLI) contract via JSON stdin/stdout
  - Rationale: Simple, language-agnostic; easy testing; clear exit code semantics.

- Decision: Timeouts: 30s per plan; 10s per skill/tool; 3 retries (expo backoff)
  - Rationale: Matches FR-028/FR-029 and EH-010.

## Best Practices

- Deterministic tests: inject time/IO/random; mock UnifiedInference; snapshot JSON.
- Cancellation: propagate tokens; external tools: graceful signal then kill on timeout.
- Atomic updates: apply `stateChanges` in one transaction per skill; validate invariants.
- Validation: JSON schema + registry resolution + dependency ordering.
- Provenance: record skill/tool name, actionId, timestamp, reason.

## Patterns

- External Tool Adapter: marshal JSON to/from process; enforce timeouts; map to `ISkill`.
- Plan Executor: topo sort `dependencies`; parallel independent actions.
- Error Handling: transient vs permanent; retry transient; proceed with partial when safe.
