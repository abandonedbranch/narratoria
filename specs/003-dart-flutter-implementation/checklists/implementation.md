# Implementation Verification Checklist

> Use this checklist to verify the Dart/Flutter implementation against parent specifications.

## Protocol Compliance (Spec 001)

### Event Handling
- [ ] Parse NDJSON from tool stdout (line-by-line)
- [ ] Handle all event types: `log`, `state_patch`, `asset`, `ui_event`, `error`, `done`
- [ ] Validate event envelope (`version: "0"`, `type` field)
- [ ] Stop processing after `done` event
- [ ] Treat unknown event types as protocol error
- [ ] Handle process exit codes (0 = protocol intact, != 0 = failure)

### State Management
- [ ] Deep merge for `state_patch` events
- [ ] Nested objects merged recursively
- [ ] Arrays replaced entirely
- [ ] Null values remove keys
- [ ] Unit test deep merge semantics

### Asset Registry
- [ ] Register assets from `asset` events
- [ ] Validate asset paths exist
- [ ] Display placeholder for unsupported MIME types
- [ ] Support image, audio, video asset kinds

### UI Events
- [ ] Handle `narrative_choice` events (display buttons)
- [ ] Graceful degradation for unknown ui_event types
- [ ] Player selection becomes next prompt

---

## Architecture Compliance (Spec 002)

### Plan Execution
- [ ] Parse Plan JSON per schema
- [ ] Detect circular dependencies before execution
- [ ] Topological sort for dependency order
- [ ] Execute tools in dependency-respecting order
- [ ] Support parallel execution when `parallel: true` and `async: true`
- [ ] Sequential fallback when `parallel: false`

### Retry Logic
- [ ] Retry up to `maxRetries` on tool failure
- [ ] Exponential backoff with `backoffMs` base
- [ ] Record retry count in execution trace
- [ ] Respect `required` flag for failure handling

### Replan Loop
- [ ] Track generation attempt count (max 5)
- [ ] Track disabled skills from failures
- [ ] Set `metadata.parentPlanId` on replan
- [ ] Template fallback after 5 attempts

### Execution Result
- [ ] Return full execution trace
- [ ] Include tool results with state, output, events, timing
- [ ] Populate `failedTools` array
- [ ] Set `canReplan` flag correctly
- [ ] Include `failureReason` when applicable

### Skill Discovery
- [ ] Scan `skills/` directory at startup
- [ ] Parse `skill.json` manifests
- [ ] Validate required fields (name, version, description)
- [ ] Load behavioral prompts from `prompt.md`
- [ ] Skip invalid skills with warning

### Skill Configuration
- [ ] Generate forms from `config-schema.json`
- [ ] Support string, number, boolean, enum types
- [ ] Obscure sensitive fields (password format)
- [ ] Validate against schema constraints
- [ ] Persist to `config.json`
- [ ] Support environment variable substitution

---

## UI Components (Material Design 3)

### Layout
- [ ] NavigationRail with 4 destinations (Narrative, Tools, Assets, State)
- [ ] Main content area
- [ ] Dark theme with deep purple seed color

### Tool Execution Panel
- [ ] Display tool name and status
- [ ] Stream log output in real-time
- [ ] Show progress indicators
- [ ] Display errors from `error` events
- [ ] Show completion status

### Asset Gallery
- [ ] Grid/list view of assets
- [ ] Image preview with metadata
- [ ] Audio/video player controls
- [ ] Placeholder for unsupported types

### Narrative State Panel
- [ ] Expandable tree view
- [ ] Highlight state changes
- [ ] JSON inspector for debugging

### Player Input
- [ ] Multiline text field
- [ ] Send button
- [ ] Visual feedback during processing

### Story View
- [ ] Display narrative text
- [ ] Render narrative_choice buttons
- [ ] Show inline assets

---

## Core Skills

### Storyteller
- [ ] `narrate.dart` script
- [ ] Config: provider, model, API key, style
- [ ] Fallback to local model

### Dice Roller
- [ ] `roll-dice.dart` script
- [ ] Parse dice formulas (NdM+X)
- [ ] Emit ui_event with results

### Memory (if implemented)
- [ ] `store-memory.dart` script
- [ ] `recall-memory.dart` script
- [ ] Vector search <500ms for 1000 events

### Reputation (if implemented)
- [ ] `update-reputation.dart` script
- [ ] `query-reputation.dart` script
- [ ] Decay rate configuration

---

## Testing

### Contract Tests
- [ ] Protocol event schema validation
- [ ] Plan JSON schema validation
- [ ] Execution result schema validation

### Unit Tests
- [ ] Deep merge semantics
- [ ] Topological sort correctness
- [ ] Cycle detection
- [ ] Retry backoff calculation

### Integration Tests
- [ ] Tool script execution
- [ ] Plan execution with dependencies
- [ ] Skill discovery
- [ ] Configuration persistence

---

## Performance

- [ ] Plan generation <5 seconds
- [ ] Per-tool timeout enforced (30s default)
- [ ] Plan-level timeout enforced (60s default)
- [ ] UI remains responsive during execution (60 fps)
