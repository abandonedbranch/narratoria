# Data Model: Plan Execution Algorithms

> **Spec**: 002-plan-execution
> **Status**: Draft
> **Purpose**: Language-agnostic algorithm specifications for plan execution

This document defines the behavioral algorithms that implementations MUST follow. For Dart-specific class implementations, see [Spec 005 data-model.md](../005-dart-implementation/data-model.md).

---

## §1 Overview

The plan generation and execution system operates in three phases:

1. **Plan Generation**: Narrator AI converts player input to Plan JSON
2. **Plan Execution**: Runtime executes tools per Plan JSON, collecting events
3. **Replan Loop**: On failure, system retries with disabled skills

All algorithms in this document are language-agnostic and MUST be implemented consistently across runtimes.

---

## §2 Plan JSON Structure

See [contracts/plan-json.schema.json](contracts/plan-json.schema.json) for the formal JSON Schema.

### §2.1 RetryPolicy Backoff Formula

When a tool fails, the executor retries with exponential backoff:

```
delay = backoffMs × 2^(attempt - 1)
```

Where:
- `backoffMs` is the base delay from RetryPolicy (default: 100ms)
- `attempt` is the current retry attempt (1-indexed)

**Example** (backoffMs = 100):
- Attempt 1: 100ms delay
- Attempt 2: 200ms delay
- Attempt 3: 400ms delay

### §2.2 Tool Dependency Graph

Tools form a directed acyclic graph (DAG) based on `dependencies` arrays:

```
tools: [
  {toolId: "A", dependencies: []},
  {toolId: "B", dependencies: ["A"]},
  {toolId: "C", dependencies: ["A"]},
  {toolId: "D", dependencies: ["B", "C"]}
]

Graph:
    A
   / \
  B   C
   \ /
    D
```

---

## §3 Skill Error States

Skills track runtime health using these states:

| State | Description | Planner Behavior |
|-------|-------------|------------------|
| `healthy` | Available for planning | Select normally |
| `degraded` | Slow or unreliable | Select with caution; may increase timeout |
| `temporaryFailure` | Transient issue (network, timeout) | Retry with backoff; may recover |
| `permanentFailure` | Unrecoverable in this session | Add to `disabledSkills`; do not select |

### §3.1 State Transitions

```
healthy ──[timeout/network error]──> temporaryFailure
healthy ──[3 consecutive failures]──> degraded
degraded ──[success]──> healthy
degraded ──[3 more failures]──> permanentFailure
temporaryFailure ──[retry success]──> healthy
temporaryFailure ──[max retries exceeded]──> permanentFailure
permanentFailure ──[session restart]──> healthy
```

---

## §4 Plan Execution Algorithm

### §4.1 Topological Sort (Kahn's Algorithm)

Before execution, tools MUST be sorted in dependency order:

```
function topologicalSort(tools):
    // Build adjacency list and in-degree map
    graph = {}
    inDegree = {}
    for tool in tools:
        graph[tool.toolId] = []
        inDegree[tool.toolId] = 0

    for tool in tools:
        for dep in tool.dependencies:
            graph[dep].append(tool.toolId)
            inDegree[tool.toolId] += 1

    // Initialize queue with zero in-degree nodes
    queue = [id for id in inDegree if inDegree[id] == 0]
    result = []

    while queue is not empty:
        current = queue.dequeue()
        result.append(current)

        for neighbor in graph[current]:
            inDegree[neighbor] -= 1
            if inDegree[neighbor] == 0:
                queue.enqueue(neighbor)

    // Detect cycle
    if length(result) != length(tools):
        throw CyclicDependencyError

    return result
```

### §4.2 Cycle Detection

Cycles MUST be detected before execution using DFS:

```
function hasCycle(tools):
    visited = {}
    recStack = {}

    function dfs(toolId):
        visited[toolId] = true
        recStack[toolId] = true

        for dep in getToolById(toolId).dependencies:
            if not visited[dep]:
                if dfs(dep):
                    return true
            else if recStack[dep]:
                return true  // Back edge found = cycle

        recStack[toolId] = false
        return false

    for tool in tools:
        if not visited[tool.toolId]:
            if dfs(tool.toolId):
                return true

    return false
```

### §4.3 Parallel Execution Logic

When `parallel: true` in Plan JSON and `async: true` for tools:

```
function executeParallel(tools, maxConcurrency):
    sorted = topologicalSort(tools)
    completed = {}
    running = {}
    results = {}

    while not all tools completed:
        // Find ready tools (all dependencies satisfied)
        ready = [t for t in sorted
                 if t.toolId not in completed
                 and t.toolId not in running
                 and all(dep in completed for dep in t.dependencies)]

        // Respect concurrency limit
        available_slots = maxConcurrency - length(running)
        to_start = ready[:available_slots]

        for tool in to_start:
            if tool.async:
                running[tool.toolId] = startAsync(tool)
            else:
                // Sequential tools block
                results[tool.toolId] = executeSync(tool)
                completed[tool.toolId] = true

        // Wait for any running tool to complete
        if running:
            (toolId, result) = waitAny(running)
            results[toolId] = result
            completed[toolId] = true
            delete running[toolId]

    return results
```

### §4.4 Failure Handling

```
function handleToolFailure(tool, error, results):
    if tool.required:
        // Mark all dependents as skipped
        for t in getAllDependents(tool.toolId):
            results[t] = {state: "skipped", reason: "dependency_failed"}

        // Plan execution fails
        return {success: false, canReplan: true}
    else:
        // Non-required failure: log and continue
        results[tool.toolId] = {state: "failed", error: error}
        // Dependents receive null input from this tool
        return {success: null, continue: true}
```

---

## §5 Replan Loop State Machine

The narrator AI system implements a bounded replan loop:

```
                    ┌─────────────┐
                    │   START     │
                    └──────┬──────┘
                           │
                           ▼
              ┌────────────────────────┐
              │  GENERATING (attempt N) │◄────────┐
              └───────────┬────────────┘          │
                          │                       │
                          ▼                       │
                   ┌─────────────┐                │
                   │  EXECUTING  │                │
                   └──────┬──────┘                │
                          │                       │
              ┌───────────┴───────────┐           │
              │                       │           │
              ▼                       ▼           │
       ┌──────────┐           ┌────────────┐     │
       │ SUCCESS  │           │  REPLANNING │─────┘
       └──────────┘           └──────┬─────┘  (N < 5)
                                     │
                                     │ (N >= 5)
                                     ▼
                              ┌────────────┐
                              │  FALLBACK  │
                              └────────────┘
```

### §5.1 State Definitions

| State | Description |
|-------|-------------|
| `GENERATING` | Narrator AI generating Plan JSON |
| `EXECUTING` | Runtime executing plan tools |
| `SUCCESS` | All required tools completed |
| `REPLANNING` | Plan failed; generating new plan with disabled skills |
| `FALLBACK` | Max attempts exceeded; using template narration |

### §5.2 Replan Algorithm

```
function replanLoop(playerInput, maxAttempts = 5):
    disabledSkills = []
    parentPlanId = null

    for attempt in 1..maxAttempts:
        plan = narratorAI.generate(
            input: playerInput,
            disabledSkills: disabledSkills,
            metadata: {
                generationAttempt: attempt,
                parentPlanId: parentPlanId
            }
        )

        result = executor.execute(plan)

        if result.success:
            return result

        // Update for next attempt
        parentPlanId = plan.requestId
        disabledSkills = disabledSkills.union(result.disabledSkills)

        log("Plan attempt {attempt} failed, disabling: {result.failedTools}")

    // Exhausted attempts
    log("Max replan attempts exceeded")
    return fallbackNarration(playerInput)
```

### §5.3 Fallback Narration Templates

When replanning exhausts all attempts, use simple templates:

```
templates = [
    "The narrator pauses, considering your words: '{input}'",
    "Your action '{input}' echoes in the stillness...",
    "The story continues, though the path is unclear...",
]

function fallbackNarration(input):
    template = random.choice(templates)
    return {
        narrative: template.format(input: input),
        tools: [],
        success: false,
        fallback: true
    }
```

---

## §6 Deep Merge Algorithm

Session state is updated using deep merge semantics:

```
function deepMerge(target, patch):
    for key, value in patch:
        if value is null:
            // Null removes key
            delete target[key]
        else if value is object and target[key] is object:
            // Recursively merge objects
            deepMerge(target[key], value)
        else if value is array:
            // Arrays are replaced entirely
            target[key] = value
        else:
            // Primitives replace
            target[key] = value

    return target
```

### §6.1 Merge Examples

```
// Example 1: Nested object merge
target: {"a": {"b": 1, "c": 2}}
patch:  {"a": {"c": 3, "d": 4}}
result: {"a": {"b": 1, "c": 3, "d": 4}}

// Example 2: Array replacement
target: {"items": [1, 2, 3]}
patch:  {"items": [4, 5]}
result: {"items": [4, 5]}

// Example 3: Key deletion
target: {"a": 1, "b": 2}
patch:  {"b": null}
result: {"a": 1}

// Example 4: Add new key
target: {"a": 1}
patch:  {"b": 2}
result: {"a": 1, "b": 2}
```

---

## §7 Timeout and Resource Bounds

Per Constitution IV.A:

| Resource | Default | Configurable | Notes |
|----------|---------|--------------|-------|
| Per-skill timeout | 30 seconds | Yes | Via skill manifest or plan |
| Per-plan execution | 60 seconds | Yes | Total time for all tools |
| Per-plan generation | 5 seconds | No | Strict; no LLM hangs |
| Max concurrent tools | CPU cores | Yes | Implementation-specific |

### §7.1 Timeout Handling

```
function executeWithTimeout(tool, timeoutMs):
    try:
        return await withTimeout(timeoutMs, execute(tool))
    catch TimeoutError:
        return {
            state: "timeout",
            error: {
                code: "TOOL_TIMEOUT",
                message: "Tool exceeded {timeoutMs}ms timeout",
                category: "timeout"
            }
        }
```

---

## Related Documents

- [Plan JSON Schema](contracts/plan-json.schema.json)
- [Execution Result Schema](contracts/execution-result.schema.json)
- [Skill Manifest Schema](../003-skills-framework/contracts/skill-manifest.schema.json)
- [Spec 005 Dart Classes](../005-dart-implementation/data-model.md)
