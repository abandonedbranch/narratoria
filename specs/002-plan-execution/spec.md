# Specification 002: Plan Execution

**Status**: Draft
**Version**: 0.1.0
**Created**: 2026-01-26
**Parent Specs**: [001-tool-protocol](../001-tool-protocol-spec/spec.md)

## RFC 2119 Keywords

The key words "MUST", "MUST NOT", "REQUIRED", "SHALL", "SHALL NOT", "SHOULD", "SHOULD NOT", "RECOMMENDED", "MAY", and "OPTIONAL" in this document are to be interpreted as described in [RFC 2119](https://datatracker.ietf.org/doc/html/rfc2119).

## 1. Purpose

This specification defines the plan execution system for Narratoria, including:

- Plan JSON schema and semantics
- Plan execution engine (topological sort, parallel execution, retry logic)
- Replan loop and robustness (Constitution Principle IV.A)
- Narrator AI interface requirements

**Scope excludes:**
- Skill discovery and configuration (see [Spec 003](../003-skills-framework/spec.md))
- Individual skill specifications (see [Spec 004](../004-narratoria-skills/spec.md))
- Dart/Flutter implementation details (see [Spec 005](../005-dart-implementation/spec.md))

---

## 2. Terminology

**Note**: These definitions are authoritative for plan execution. For protocol-level terminology, see [Spec 001](../001-tool-protocol-spec/spec.md). For skill-related terminology, see [Spec 003](../003-skills-framework/spec.md).

### Skill vs Tool Distinction

Per the [Agent Skills Standard](https://agentskills.io/what-are-skills):
- **Skill**: A capability bundle that the Narrator AI can invoke. Skills contain behavioral prompts, optional scripts, configuration, and data storage. The Narrator AI selects and orchestrates skills.
- **Skill Script**: An optional executable component within a skill that performs actions (e.g., `roll-dice.dart`, `narrate.dart`). Scripts communicate via the NDJSON protocol defined in Spec 001.
- **Tool**: At the protocol level (Spec 001), "tool" refers generically to any external process. In Plan JSON, the `tools` array contains **skill script invocations**—references to specific scripts within skills.

### Plan Execution Terms

- **`disabledSkills` (Set[String])**: Set of skill names that planner MUST NOT select for the current plan attempt (populated by failed skill tracking during replan loop). Used in Plan JSON and executor feedback.
- **Replan Loop**: Bounded retry system (max 5 plan generation attempts) that learns from failures and disables failed skills in subsequent plans.
- **Plan JSON**: Structured document produced by Narrator AI describing which skill scripts to invoke, their inputs, dependencies, and execution strategy.
- **Skill Invocation**: An entry in the Plan JSON `tools` array that references a specific skill script to execute.
- **Execution Trace**: Complete record of skill script execution including results, events, timing, and errors.
- **Narrator AI**: The plan generation component that converts player input into Plan JSON by selecting appropriate skills and their scripts. May be implemented as local LLM, hosted API, or stub.
- **Narrator AI Stub**: Simplified in-process implementation that converts player prompts to Plan JSON using hard-coded mappings (for MVP before LLM integration). See [Spec 005](../005-dart-implementation/spec.md) for Dart implementation.

---

## 3. User Scenarios

### User Story 1 - Basic Interactive Storytelling (Priority: P1)

A player launches Narratoria for the first time and types a simple action like "I look around the room." The narrator (powered by a local LLM) generates a Plan JSON that selects appropriate skills to create an engaging response, then executes the plan to deliver rich narration back to the player.

**Why this priority**: This is the core value proposition of Narratoria. Without functional plan generation and execution, the application cannot deliver interactive storytelling experiences.

**Independent Test**: Can be fully tested by launching the app, typing a simple prompt, and verifying that the narrator responds with contextually appropriate narration. Delivers immediate value as a working storytelling system.

**Acceptance Scenarios**:

1. **Given** Narratoria is launched with default configuration, **When** player types "I examine the ancient door", **Then** narrator generates a plan that may invoke storyteller skill and returns vivid description
2. **Given** player has initiated a session, **When** player types "I roll to pick the lock" and a dice-roller skill is available, **Then** narrator generates a plan that invokes dice-roller script and narrates the outcome based on roll result
3. **Given** narrator AI is generating a plan, **When** plan generation fails (LLM unavailable or error), **Then** system falls back to simple pattern-based response and logs the error without crashing

---

## 4. Functional Requirements

### 4.1 Plan Generation (Narrator AI)

- **FR-001**: System MUST include Phi-3.5 Mini (3.8B parameters) as the local narrator AI for plan generation. Model runs entirely in-process (2.5GB GGUF quantized, compatible with iPhone 17+). Model downloads automatically from HuggingFace Hub (`microsoft/Phi-3.5-mini-instruct` GGUF variant) on first app launch and caches locally for offline use
- **FR-002**: Plan generator MUST convert player text input into structured Plan JSON documents following the schema defined in `contracts/plan-json.schema.json`
- **FR-003**: Plan generator MUST select relevant skills based on player intent, then determine which skill scripts (if any) to invoke
- **FR-004**: Plan generator MUST inject active skills' behavioral prompts into system context when generating plans
- **FR-005**: Plan generator MUST fall back to simple pattern-based planning if LLM fails or is unavailable
- **FR-006**: Plan generator MUST complete plan generation within 5 seconds for typical player inputs (under 100 words)
- **FR-007**: Plan generator MUST NOT make network calls or access external APIs (Constitution Principle II exception for in-process AI)
- **FR-008**: Plan generator MUST consult `disabledSkills` in execution results and avoid selecting those skills for the next plan
- **FR-009**: Plan generator MUST avoid creating circular dependencies in the tools array (validated by executor before execution)
- **FR-010**: Plan generator MUST track generation attempt count and set `metadata.generationAttempt` and `metadata.parentPlanId` in Plan JSON

### 4.2 Plan Execution Engine

- **FR-011**: Plan executor MUST perform topological sort on `dependencies` array before execution
- **FR-012**: Plan executor MUST detect circular dependencies and reject plans with cycles before execution; MUST request new plan from generator with error context
- **FR-013**: Plan executor MUST respect `required` flag: if true and tool fails, abort dependent tools; if false, dependent tools may proceed with null/empty input
- **FR-014**: Plan executor MUST respect `async` flag: if true, tool may run in parallel with unrelated tasks; if false, tool runs sequentially
- **FR-015**: Plan executor MUST implement retry logic per `retryPolicy`: up to `maxRetries` attempts with exponential backoff of `backoffMs`
- **FR-016**: Plan executor MUST track retry count and include in execution trace
- **FR-017**: Plan executor MUST enforce per-skill timeout (default 30 seconds, configurable)
- **FR-018**: Plan executor MUST enforce plan-level execution timeout (default 60 seconds, configurable)
- **FR-019**: Plan executor MUST continue executing non-dependent tasks even when a non-required tool fails
- **FR-020**: Plan executor MUST generate full execution trace with tool results, including state, output, events, execution time, retry count, and error details
- **FR-021**: Plan executor MUST return success/failure status, failed tool list, and `canReplan` flag to indicate whether narrator AI should attempt replan
- **FR-022**: Plan executor MUST handle graceful failure: if plan execution fails after retries, aggregate partial results and present to user without crashing

### 4.3 Plan Generation Robustness

- **FR-023**: Narrator AI system MUST implement bounded retry loop: max 5 plan generation attempts before escalating to user
- **FR-024**: Narrator AI system MUST track which skills have failed and disable them in subsequent replans
- **FR-025**: Narrator AI system MUST provide simple template-based narration if plan generation fails after max attempts (graceful fallback)
- **FR-026**: Narrator AI system MUST log detailed error context for each failed plan (attempted plan, failed tools, retry counts)
- **FR-027**: Plan executor MUST report specific failure reason (tool failure, circular dependency, timeout, invalid JSON) to enable accurate replan strategy
- **FR-028**: System MUST NOT loop infinitely; if planner cannot generate viable plan after 5 attempts, display error to user and allow manual session recovery
- **FR-029**: System MUST log all plan generation and execution attempts with timestamps, plan IDs, skill selections, and outcomes for debugging and analytics

---

## 5. Plan Execution Semantics

### 5.1 Player Interaction Flow

Players interact with Narratoria by submitting natural language prompts (e.g., "I light the torch" or "I examine the mysterious door"). The narrator AI converts these prompts into executable plans that invoke tools via the protocol defined in Spec 001.

```
┌──────────────┐
│ Player types │
│   prompt     │
└──────┬───────┘
       │
       ▼
┌──────────────┐
│ Narrator AI  │ (local LLM or stub)
│ analyzes     │
│   prompt     │
└──────┬───────┘
       │
       ▼
┌──────────────┐
│  Plan JSON   │ {tools: [...], parallel: bool}
└──────┬───────┘
       │
       ▼
┌──────────────┐
│ Plan         │ executes tools per plan
│ Executor     │ collects events via protocol
└──────┬───────┘
       │
       ▼
┌──────────────┐
│ UI updates   │ display results, assets, state
└──────────────┘
```

### 5.2 Plan JSON Schema

The Narrator AI MUST produce a **Plan JSON** document with this structure. Note that the `tools` array contains **skill script invocations**—the field name is `tools` for protocol compatibility, but each entry references a script within a skill:

```json
{
  "requestId": "<uuid>",
  "narrative": "<string, optional narrator response>",
  "tools": [
    {
      "toolId": "<string>",
      "toolPath": "<filesystem-path-to-executable>",
      "input": { "...arbitrary JSON..." },
      "dependencies": ["<toolId>", "..."],
      "required": true,
      "async": false,
      "retryPolicy": {
        "maxRetries": 3,
        "backoffMs": 100
      }
    }
  ],
  "parallel": false,
  "disabledSkills": ["<skillName>", "..."],
  "metadata": {
    "generationAttempt": 1,
    "parentPlanId": null
  }
}
```

**Fields**:
- `requestId`: Unique identifier for this plan execution
- `narrative`: Optional narrative text to display before or during script execution
- `tools`: Array of skill script invocation descriptors (field named `tools` for protocol compatibility)
  - `toolId`: Unique ID for this invocation within the plan (for dependency tracking)
  - `toolPath`: Path to the skill script executable (e.g., `skills/dice-roller/roll-dice.dart`)
  - `input`: JSON object passed to the script via stdin (as described in Spec 001 §6)
  - `dependencies`: Array of `toolId` values that must complete before this script runs
  - `required`: If true, script failure aborts dependent scripts; if false, failure is non-blocking
  - `async`: If true, script may run in parallel with unrelated scripts (respecting `dependencies`)
  - `retryPolicy`: Configures retry behavior for this specific script invocation
- `parallel`: If true and dependencies allow, scripts run concurrently
- `disabledSkills`: Skills that failed in previous attempts (planner MUST NOT select)
- `metadata`: Plan metadata for debugging and replan tracking

See `contracts/plan-json.schema.json` for the authoritative JSON Schema.

### 5.3 Plan Execution Rules

The runtime MUST execute plans according to these behavioral requirements:

1. **Circular Dependency Detection**: Before execution, the runtime MUST detect any circular dependencies among tools (direct or transitive). If detected, the runtime MUST reject the plan and request a new plan from the narrator AI.

2. **Topological Execution Order**: Tools MUST execute in dependency-respecting order. A tool MUST NOT begin execution until all tools listed in its `dependencies` array have completed successfully.

3. **Parallel Execution**:
   - If `parallel: true` in the plan AND `async: true` for a tool, tools with satisfied dependencies MAY run concurrently
   - Concurrent execution MUST NOT exceed the number of available CPU cores (implementation-specific limit)
   - Tools with no dependencies MAY run in parallel if both plan and tool have `parallel`/`async: true`

4. **Sequential Fallback**: If `parallel: false`, tools MUST run in topological order, waiting for each to complete before starting the next.

5. **Retry Logic**:
   - If a tool fails (emits `done.ok: false` or exits non-zero), the runtime MUST retry up to `retryPolicy.maxRetries` times
   - The runtime MUST apply exponential backoff between retries: `delay = backoffMs × 2^(attempt-1)`
   - After exhausting retries, the runtime MUST mark the tool as failed and proceed per `required` flag
   - The runtime MUST record retry count in the execution trace

6. **Failure Handling (by `required` flag)**:
   - **If `required: true` and tool fails**: Dependent tools MUST NOT execute; plan execution fails
   - **If `required: false` and tool fails**: Dependent tools MAY execute with null/empty input; plan continues
   - Independent tools (no dependency on failed tool) continue execution automatically

7. **Event Aggregation**: The runtime MUST collect all events from all tools and merge:
   - `log` events → displayed in Tool Execution Panel
   - `state_patch` events → merged into session state
   - `asset` events → registered in Asset Gallery
   - `ui_event` events → dispatched to UI handlers
   - `error` events → displayed with context

8. **Execution Trace**: The runtime MUST maintain a full execution trace with results for each tool

### 5.4 Plan Executor Output

After executing a plan, the runtime MUST return an execution result with full trace. See `contracts/execution-result.schema.json` for the authoritative schema.

**Purpose**: This trace allows the narrator AI to:
- Understand which tools failed and why
- Disable failed skills for the next plan via `disabledSkills` field
- Determine if replanning is possible
- Debug execution issues

### 5.5 Narrator AI Interface

Per Constitution Principle IV.A, the narrator AI MUST implement:

**Required Behavior**:
- MUST return Plan JSON in the format specified above
- MUST implement bounded replan loop: maximum 5 plan generation attempts before graceful fallback
- MUST consult `disabledSkills` in execution results to avoid selecting failed skills
- MUST track generation attempt count in `metadata.generationAttempt`
- MUST set `metadata.parentPlanId` to the previous plan's UUID when replanning
- After 5 failed attempts, MUST provide template-based fallback narration

**Implementation Flexibility**:
- The narrator AI MAY be a separate process, remote service, or in-process module
- Tool capability discovery mechanisms are implementation-specific
- Plan generation strategy (prompt engineering, model selection) is implementation-specific

---

## 6. Edge Cases

### Plan Execution Edge Cases

- **What happens when narrator LLM generates invalid Plan JSON?**
  - JSON parser rejects invalid plan, fallback pattern-based planner generates safe default plan

- **How does system handle circular dependencies in Plan JSON tools array?**
  - Plan executor detects cycles during dependency resolution and rejects plan as invalid

- **What happens when multiple skills want to modify the same piece of application state?**
  - Skills are independent; state patches are applied in dependency order, later patches may override earlier

- **How does system handle skills that take very long to execute (>30 seconds)?**
  - Plan executor enforces timeout per script, emits timeout error, continues with other plan steps

- **What happens when user closes application while skill script is running?**
  - Application waits for in-flight scripts with shutdown timeout (5s), then terminates remaining processes

---

## 7. Success Criteria

- **SC-001**: Plan generator produces valid Plan JSON for 95% of player inputs within 5 seconds using local LLM
- **SC-002**: Plan generator correctly selects relevant skills for player actions (evaluated via acceptance tests) in 90% of cases
- **SC-005**: Skill scripts execute successfully via NDJSON protocol and return results within configured timeout (30s default) in 99% of invocations
- **SC-006**: System gracefully degrades when skills fail: application continues without crash, UI displays helpful error message, narrative continues with fallback content
- **SC-012**: Plan executor handles script failures without crashing: logs error, marks step as failed, continues with remaining plan steps

---

## 8. Related Specifications

| Specification | Relationship |
|---------------|--------------|
| [001: Tool Protocol](../001-tool-protocol-spec/spec.md) | Defines event types, transport model, NDJSON protocol |
| [003: Skills Framework](../003-skills-framework/spec.md) | Defines skill discovery, configuration, and script execution |
| [004: Narratoria Skills](../004-narratoria-skills/spec.md) | Defines individual skill specifications |
| [005: Dart Implementation](../005-dart-implementation/spec.md) | Dart+Flutter reference implementation |

---

## 9. Contracts

This specification defines the following machine-readable contracts in `contracts/`:

- **plan-json.schema.json**: JSON Schema for Plan JSON documents
- **execution-result.schema.json**: JSON Schema for plan execution results
- **example-plan.json**: Example Plan JSON (first attempt)
- **example-plan-replan.json**: Example Plan JSON (replan after failure)
