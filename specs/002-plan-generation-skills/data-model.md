# Data Model: Plan Generation and Skill Discovery

> **Scope**: Language-agnostic data model definitions for Spec 002.
> **Implementation**: See [Spec 003 data-model.md](../003-dart-flutter-implementation/data-model.md) for Dart class implementations.

This document defines the conceptual data structures and algorithms for plan generation and skill discovery. Implementations may use any programming language while adhering to these structural contracts.

---

## 1. Skill Entity

A Skill represents a capability bundle following the Agent Skills Standard.

### Structure

```
Skill {
  // Identity
  name: String              // Unique identifier (e.g., "storyteller")
  displayName: String?      // Human-readable name
  version: String           // Semantic version (e.g., "1.0.0")
  description: String       // Brief description
  author: String?           // Author/organization

  // Paths
  directoryPath: Path       // Root directory (e.g., "skills/storyteller/")
  manifestPath: Path        // skill.json location

  // Components
  scripts: List<SkillScript>    // Executable scripts
  behavioralPrompt: String?     // Contents of prompt.md
  configSchema: Object?         // Parsed config-schema.json
  userConfig: Object?           // Parsed config.json

  // Runtime state
  enabled: Boolean              // User toggle
  errorState: ErrorState        // Current health status
}
```

### Error States

```
ErrorState = healthy | degraded | temporaryFailure | permanentFailure

healthy:          Skill available and functioning
degraded:         Skill slow or unreliable but usable
temporaryFailure: Transient error, retry may succeed
permanentFailure: Unrecoverable, skill disabled
```

### Skill Script

```
SkillScript {
  name: String          // Script identifier (e.g., "narrate")
  path: Path            // Absolute path to executable
  isExecutable: Boolean // Has execute permissions
}
```

---

## 2. Plan JSON Structure

Plan JSON is the structured output from the narrator AI.

### Structure

See `contracts/plan-json.schema.json` for the authoritative JSON Schema.

```
PlanJson {
  requestId: UUID                 // Unique plan identifier
  narrative: String?              // Optional narrator text
  tools: List<ToolInvocation>     // Tools to execute
  parallel: Boolean               // Global parallel execution flag
  disabledSkills: List<String>    // Skills to exclude
  metadata: PlanMetadata          // Generation tracking
}

ToolInvocation {
  toolId: String                  // Unique within plan
  toolPath: Path                  // Executable path
  input: Object                   // JSON passed via stdin
  dependencies: List<String>      // toolIds that must complete first
  required: Boolean               // Abort plan on failure?
  async: Boolean                  // Allow parallel execution?
  retryPolicy: RetryPolicy        // Retry configuration
}

RetryPolicy {
  maxRetries: Integer             // Max attempts (default: 3)
  backoffMs: Integer              // Base backoff delay (default: 100)
}

PlanMetadata {
  generationAttempt: Integer      // Attempt number (1, 2, 3...)
  parentPlanId: UUID?             // Previous plan if replan
}
```

### Retry Backoff Algorithm

```
function calculateBackoff(attempt: Integer, backoffMs: Integer): Integer
  return backoffMs * (2 ^ (attempt - 1))

// Example: backoffMs=100
// Attempt 1: 100ms
// Attempt 2: 200ms
// Attempt 3: 400ms
```

---

## 3. Execution Result Structure

ExecutionResult is returned by the plan executor after running a plan.

### Structure

See `contracts/execution-result.schema.json` for the authoritative JSON Schema.

```
ExecutionResult {
  planId: UUID                        // Matches plan.requestId
  success: Boolean                    // All required tools succeeded?
  narrative: String?                  // Final narrative text
  failedTools: List<String>           // toolIds that failed
  canReplan: Boolean                  // Failures recoverable?
  failureReason: FailureReason?       // Why plan failed
  executionTrace: List<ToolResult>    // Per-tool results
  finalState: Object                  // Aggregated session state
  totalExecutionTimeMs: Integer       // Total execution time
  generationMetadata: PlanMetadata?   // Passed through from plan
}

ToolResult {
  toolId: String
  toolPath: String?
  ok: Boolean                         // done.ok value
  state: ExecutionState
  output: Object?                     // Aggregated output
  events: List<ProtocolEvent>         // All emitted events
  executionTimeMs: Integer
  retryCount: Integer
  error: ToolError?
}

ExecutionState = pending | running | completed | failed | skipped | timeout

ToolError {
  type: String                        // Error classification
  message: String                     // Human-readable message
  exitCode: Integer?                  // Process exit code
}

FailureReason = circular_dependency | tool_failure | timeout | protocol_violation
```

---

## 4. Plan Execution Algorithm

### Topological Sort (Kahn's Algorithm)

```
function topologicalSort(tools: List<ToolInvocation>): List<ToolInvocation> | CycleError
  // Build adjacency list and in-degree map
  inDegree = Map<String, Integer>()
  adjacency = Map<String, List<String>>()

  for tool in tools:
    inDegree[tool.toolId] = 0
    adjacency[tool.toolId] = []

  for tool in tools:
    for dep in tool.dependencies:
      adjacency[dep].append(tool.toolId)
      inDegree[tool.toolId] += 1

  // Find nodes with no dependencies
  queue = Queue<String>()
  for toolId, degree in inDegree:
    if degree == 0:
      queue.enqueue(toolId)

  // Process in topological order
  result = []
  while not queue.isEmpty():
    current = queue.dequeue()
    result.append(findTool(tools, current))

    for neighbor in adjacency[current]:
      inDegree[neighbor] -= 1
      if inDegree[neighbor] == 0:
        queue.enqueue(neighbor)

  // Check for cycle
  if result.length != tools.length:
    return CycleError("Circular dependency detected")

  return result
```

### Cycle Detection

```
function hasCycle(tools: List<ToolInvocation>): Boolean
  visited = Set<String>()
  recursionStack = Set<String>()

  function dfs(toolId: String): Boolean
    visited.add(toolId)
    recursionStack.add(toolId)

    for dep in findTool(tools, toolId).dependencies:
      if dep not in visited:
        if dfs(dep):
          return true
      else if dep in recursionStack:
        return true

    recursionStack.remove(toolId)
    return false

  for tool in tools:
    if tool.toolId not in visited:
      if dfs(tool.toolId):
        return true

  return false
```

---

## 5. Replan Loop State Machine

```
         ┌─────────────────────────────────────────────┐
         │                                             │
         ▼                                             │
    ┌─────────┐    plan generated    ┌──────────┐     │
    │ PENDING │───────────────────►│ EXECUTING │     │
    └─────────┘                      └──────────┘     │
         ▲                               │            │
         │                               │            │
         │                    ┌──────────┴──────────┐ │
         │                    │                     │ │
         │                    ▼                     ▼ │
         │              ┌─────────┐           ┌────────┐
         │              │ SUCCESS │           │ FAILED │
         │              └─────────┘           └────────┘
         │                                         │
         │         attempts < 5                    │
         └─────────────────────────────────────────┘
                           │
                           │ attempts >= 5
                           ▼
                    ┌────────────┐
                    │ EXHAUSTED  │
                    │ (fallback) │
                    └────────────┘
```

### Replan Loop Algorithm

```
function executeWithReplan(prompt: String): ExecutionResult
  disabledSkills = Set<String>()
  parentPlanId = null

  for attempt in 1..5:
    plan = generatePlan(prompt, disabledSkills, attempt, parentPlanId)

    if plan is Error:
      continue  // Try again with same disabledSkills

    result = executePlan(plan)

    if result.success:
      return result

    if not result.canReplan:
      return result  // Permanent failure, no point retrying

    // Learn from failures
    for toolId in result.failedTools:
      skill = getSkillForTool(toolId)
      if skill:
        disabledSkills.add(skill.name)

    parentPlanId = plan.requestId

  // All attempts exhausted
  return createFallbackResult(prompt)
```

---

## 6. Deep Merge Algorithm

Used for applying `state_patch` events to session state.

```
function deepMerge(target: Object, patch: Object): Object
  result = copy(target)

  for key, value in patch:
    if value is null:
      // Null removes keys
      delete result[key]
    else if value is Object and result[key] is Object:
      // Recursively merge nested objects
      result[key] = deepMerge(result[key], value)
    else:
      // Arrays and primitives replace entirely
      result[key] = value

  return result

// Example:
// target = {"a": {"b": 1, "c": 2}}
// patch  = {"a": {"c": 3, "d": 4}}
// result = {"a": {"b": 1, "c": 3, "d": 4}}
```

---

## 7. Skill Discovery Algorithm

```
function discoverSkills(skillsDirectory: Path): List<Skill>
  skills = []

  for entry in listDirectories(skillsDirectory):
    manifestPath = entry / "skill.json"

    if not exists(manifestPath):
      log.warn("Skipping ${entry}: no skill.json")
      continue

    manifest = parseJson(readFile(manifestPath))

    if not validateManifest(manifest):
      log.warn("Skipping ${entry}: invalid manifest")
      continue

    skill = Skill {
      name = manifest.name
      displayName = manifest.displayName
      version = manifest.version
      description = manifest.description
      author = manifest.author
      directoryPath = entry
      manifestPath = manifestPath
      scripts = discoverScripts(entry / "scripts")
      behavioralPrompt = readOptionalFile(entry / "prompt.md")
      configSchema = parseOptionalJson(entry / "config-schema.json")
      userConfig = parseOptionalJson(entry / "config.json")
      enabled = true
      errorState = healthy
    }

    skills.append(skill)

  return skills

function discoverScripts(scriptsDirectory: Path): List<SkillScript>
  scripts = []

  for file in listFiles(scriptsDirectory):
    scripts.append(SkillScript {
      name = removeExtension(file.name)
      path = file
      isExecutable = hasExecutePermission(file)
    })

  return scripts
```

---

## 8. Configuration Validation

```
function validateConfig(config: Object, schema: JsonSchema): ValidationResult
  errors = []

  // Check required fields
  for field in schema.required:
    if field not in config:
      errors.append(RequiredFieldError(field))

  // Validate each field
  for key, value in config:
    if key in schema.properties:
      fieldSchema = schema.properties[key]

      // Type check
      if not typeMatches(value, fieldSchema.type):
        errors.append(TypeError(key, expected=fieldSchema.type, actual=typeOf(value)))

      // Enum check
      if fieldSchema.enum and value not in fieldSchema.enum:
        errors.append(EnumError(key, allowed=fieldSchema.enum, actual=value))

      // Range checks
      if fieldSchema.minimum and value < fieldSchema.minimum:
        errors.append(RangeError(key, min=fieldSchema.minimum))

      if fieldSchema.maximum and value > fieldSchema.maximum:
        errors.append(RangeError(key, max=fieldSchema.maximum))

  if errors.isEmpty():
    return ValidationResult.success()
  else:
    return ValidationResult.failure(errors)
```

---

## Related Documents

| Document | Description |
|----------|-------------|
| [spec.md](spec.md) | Feature specification with requirements |
| [plan.md](plan.md) | Architecture and implementation plan |
| [contracts/](contracts/) | JSON Schemas for Plan JSON, Execution Result, Skill Manifest |
| [Spec 003 data-model.md](../003-dart-flutter-implementation/data-model.md) | Dart class implementations |
