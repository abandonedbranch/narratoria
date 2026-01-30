# Feature Specification: Plan Generation and Skill Discovery

**Feature Branch**: `002-plan-generation-skills`  
**Created**: 2026-01-26  
**Status**: Draft  
**Input**: User description: "Plan generation and skill discovery with Agent Skills Standard integration"

## Terminology (Authoritative Definitions)

**Note**: These definitions are authoritative for Spec 002 and harmonized with Spec 001 protocol terms. For protocol-level terminology (event types, exit codes, transport model), see Spec 001 §3 Glossary.

To ensure consistency across spec, plan, and tasks, this feature uses the following terms:

- **`disabledSkills` (Set[String])**: Set of skill names that planner MUST NOT select for the current plan attempt (populated by failed skill tracking during replan loop). Used in Plan JSON and executor feedback.
- **`errorState` (Enum)**: Health status of a skill at runtime: `healthy` (available), `degraded` (slow/unreliable), `temporaryFailure` (transient network issue, retry), `permanentFailure` (unrecoverable, disable). Used in SkillDiscovery and replan decision logic.
- **Available Skills**: Skills with `errorState != permanentFailure` AND not in `disabledSkills` set. Selectable by narrator AI for plan generation.
- **Skill Manifest** (`skill.json`): Metadata file per Agent Skills Standard; defines skill identity, version, author, behavioral prompt path, scripts available.
- **Behavioral Prompt** (`prompt.md`): Markdown file injected into narrator AI system context; guides narrator behavior for this skill (e.g., "Emphasize vivid sensory details").
- **Graceful Degradation**: System continues functioning and presents narration to user even when optional features fail (e.g., hosted API unavailable → fallback to local model).
- **Replan Loop**: Bounded retry system (max 5 plan generation attempts) that learns from failures and disables failed skills in subsequent plans.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Basic Interactive Storytelling (Priority: P1)

A player launches Narratoria for the first time and types a simple action like "I look around the room." The narrator (powered by a local LLM) generates a Plan JSON that selects appropriate skills to create an engaging response, then executes the plan to deliver rich narration back to the player.

**Why this priority**: This is the core value proposition of Narratoria. Without functional plan generation and execution, the application cannot deliver interactive storytelling experiences.

**Independent Test**: Can be fully tested by launching the app, typing a simple prompt, and verifying that the narrator responds with contextually appropriate narration. Delivers immediate value as a working storytelling system.

**Acceptance Scenarios**:

1. **Given** Narratoria is launched with default configuration, **When** player types "I examine the ancient door", **Then** narrator generates a plan that may invoke storyteller skill and returns vivid description
2. **Given** player has initiated a session, **When** player types "I roll to pick the lock" and a dice-roller skill is available, **Then** narrator generates a plan that invokes dice-roller script and narrates the outcome based on roll result
3. **Given** narrator AI is generating a plan, **When** plan generation fails (LLM unavailable or error), **Then** system falls back to simple pattern-based response and logs the error without crashing

---

### User Story 2 - Skill Configuration (Priority: P2)

A player wants to enhance their storytelling experience by configuring the storyteller skill to use a hosted AI provider (like Claude or GPT-4) instead of the default local model. They navigate to Settings → Skills, select the storyteller skill, enter their API key, and choose their preferred model.

**Why this priority**: Configuration is essential for skills that require external services (API keys, model selection) and user preferences (narrative style, detail level). Without this, users cannot unlock the full potential of advanced skills.

**Independent Test**: Can be tested by creating a skill with a config schema, opening the skills settings screen, modifying values, saving, and verifying that the skill uses the new configuration. Delivers value by enabling personalization.

**Acceptance Scenarios**:

1. **Given** storyteller skill is installed, **When** user opens Skills settings, **Then** storyteller appears with configuration form showing API provider options, model selection, and style preferences
2. **Given** user is configuring storyteller skill, **When** user enters API key and selects "Claude" provider, **Then** configuration is validated and saved to `skills/storyteller/config.json`
3. **Given** storyteller skill is configured for Claude API, **When** narrator generates a plan that uses storyteller, **Then** storyteller script uses Claude API instead of local model
4. **Given** Claude API request fails (network error, invalid key), **When** storyteller script runs, **Then** script gracefully falls back to local model and logs the fallback

---

### User Story 3 - Skill Discovery and Installation (Priority: P2)

A player wants to add a new skill (e.g., a rules engine for D&D 5e) to their Narratoria installation. They download a skill package following Agent Skills Standard, place it in the `skills/` directory, and restart the application. Narratoria automatically discovers the new skill and makes it available to the narrator AI.

**Why this priority**: Extensibility is core to Narratoria's vision. Users should be able to add custom skills for different game systems, narrative styles, and creative tools without modifying application code.

**Independent Test**: Can be tested by creating a minimal skill with `skill.json` manifest, placing it in `skills/`, restarting the app, and verifying it appears in the skills list and is available for plan generation. Delivers value as a plugin system.

**Acceptance Scenarios**:

1. **Given** Narratoria is running, **When** user places a new skill directory in `skills/` and restarts, **Then** skill appears in Skills settings with metadata from `skill.json`
2. **Given** new skill has configuration requirements, **When** skill is discovered, **Then** configuration form is auto-generated from `config-schema.json`
3. **Given** new skill includes behavioral prompts, **When** plan generator runs, **Then** skill's `prompt.md` is injected into system context for narrator AI
4. **Given** new skill includes scripts, **When** narrator generates a plan using that skill, **Then** scripts are executable and follow NDJSON protocol (Spec 001)

---

### User Story 4 - Memory and Continuity (Priority: P3)

A player engages in a long storytelling session over multiple days. The memory skill tracks significant events, character interactions, and world changes. When the player returns and types "What happened last time?", the narrator uses the memory skill to recall key events without sending the entire conversation history to the LLM.

**Why this priority**: Memory enables long-form storytelling and campaigns. While not essential for MVP, it significantly enhances the user experience for extended play sessions and makes Narratoria viable for campaign-based storytelling.

**Independent Test**: Can be tested by playing a session, causing notable events, closing the app, reopening, and verifying that memory skill recalls relevant context. Delivers value for returning players.

**Acceptance Scenarios**:

1. **Given** memory skill is enabled, **When** player action causes significant event (e.g., "befriends the blacksmith"), **Then** memory skill stores event summary with semantic embedding
2. **Given** memory skill has stored events, **When** narrator generates new plan, **Then** memory-recall script is invoked to fetch relevant context from past events
3. **Given** player asks "What happened with the blacksmith?", **When** memory skill searches stored events, **Then** narrator incorporates relevant past context into response
4. **Given** memory database grows large (>1000 events), **When** memory skill performs search, **Then** search completes in under 500ms using vector similarity

---

### User Story 5 - Reputation and Consequence Tracking (Priority: P3)

A player's actions have consequences. When the player steals from a merchant in town, the reputation skill records decreased standing with the Merchants Guild. Later, when attempting to trade with another merchant, the narrator checks reputation and generates narration reflecting the player's negative reputation.

**Why this priority**: Reputation tracking creates a living world where actions have lasting consequences. This enhances immersion and makes player choices meaningful over time.

**Independent Test**: Can be tested by performing actions that affect reputation, then interacting with related factions and verifying that reputation influences narration and available options. Delivers value for cause-and-effect gameplay.

**Acceptance Scenarios**:

1. **Given** reputation skill is enabled and tracks "Merchants Guild", **When** player steals from merchant, **Then** reputation skill records -20 reputation with Merchants Guild
2. **Given** player has negative reputation with faction, **When** narrator generates plan for interaction with that faction, **Then** reputation skill is queried and result influences narration tone
3. **Given** reputation skill tracks multiple factions, **When** player action affects multiple factions simultaneously, **Then** all relevant faction reputations are updated in single transaction
4. **Given** time passes in-game, **When** reputation skill applies decay (configured decay rate), **Then** old reputation modifications fade gradually toward neutral

---

### Edge Cases

- What happens when a skill's script fails to execute (file not found, permission denied, crashes)?
  - Plan executor logs error, emits `done.ok=false` per protocol, narrator falls back to simple narration
  
- How does system handle skills with missing or invalid manifests?
  - Skill discovery skips invalid skills and logs warning, application continues with valid skills only
  
- What happens when narrator LLM generates invalid Plan JSON?
  - JSON parser rejects invalid plan, fallback pattern-based planner generates safe default plan
  
- How does system handle circular dependencies in Plan JSON tools array?
  - Plan executor detects cycles during dependency resolution and rejects plan as invalid
  
- What happens when user configures skill with invalid API key?
  - Skill validation detects error on first use, displays warning in UI, falls back to local alternative
  
- How does narrator AI select between multiple skills that could handle the same action?
  - Plan generator uses LLM reasoning + skill metadata (priority, capabilities) to choose most appropriate
  
- What happens when multiple skills want to modify the same piece of application state?
  - Skills are independent; state patches are applied in dependency order, later patches may override earlier
  
- How does system handle skills that take very long to execute (>30 seconds)?
  - Plan executor enforces timeout per script, emits timeout error, continues with other plan steps
  
- What happens when user closes application while skill script is running?
  - Application waits for in-flight scripts with shutdown timeout (5s), then terminates remaining processes
  
- How does system handle skills that require data migration across versions?
  - Skill's responsibility; skill checks data schema version and migrates at load time or first use

## Requirements *(mandatory)*

### Functional Requirements

#### Plan Generation (Narrator AI)

- **FR-001**: System MUST include a local small language model (recommended: Gemma 2B, Llama 3.2 3B, Qwen 2.5 3B) for plan generation that runs entirely in-process
- **FR-002**: Plan generator MUST convert player text input into structured Plan JSON documents following Spec 001 extended schema
- **FR-003**: Plan generator MUST select relevant skills and their scripts based on player intent and available skills
- **FR-004**: Plan generator MUST inject active skills' behavioral prompts into system context when generating plans
- **FR-005**: Plan generator MUST fall back to simple pattern-based planning if LLM fails or is unavailable
- **FR-006**: Plan generator MUST complete plan generation within 5 seconds for typical player inputs (under 100 words)
- **FR-007**: Plan generator MUST NOT make network calls or access external APIs (Constitution Principle II exception for in-process AI)
- **FR-008**: Plan generator MUST consult `disabledSkills` in execution results and avoid selecting those skills for the next plan
- **FR-009**: Plan generator MUST avoid creating circular dependencies in the tools array (validated by executor before execution)
- **FR-010**: Plan generator MUST track generation attempt count and set `metadata.generationAttempt` and `metadata.parentPlanId` in Plan JSON

#### Plan Execution Engine (NEW)

- **FR-011**: Plan executor MUST perform topological sort on `dependencies` array before execution
- **FR-012**: Plan executor MUST detect circular dependencies and reject plans with cycles before execution; MUST request new plan from generator with error context
- **FR-013**: Plan executor MUST respect `required` flag: if true and tool fails, abort dependent tools; if false, dependent tools may proceed with null/empty input
- **FR-014**: Plan executor MUST respect `async` flag: if true, tool may run in parallel with unrelated tasks; if false, tool runs sequentially
- **FR-015**: Plan executor MUST implement retry logic per `retryPolicy`: up to `maxRetries` attempts with exponential backoff of `backoffMs`
- **FR-016**: Plan executor MUST track retry count and include in execution trace
- **FR-017**: Plan executor MUST enforce per-skill timeout (default 30 seconds in Spec 001, configurable)
- **FR-018**: Plan executor MUST enforce plan-level execution timeout (default 60 seconds, configurable)
- **FR-019**: Plan executor MUST continue executing non-dependent tasks even when a non-required tool fails
- **FR-020**: Plan executor MUST generate full execution trace with tool results, including state, output, events, execution time, retry count, and error details
- **FR-021**: Plan executor MUST return success/failure status, failed tool list, and `canReplan` flag to indicate whether narrator AI should attempt replan
- **FR-022**: Plan executor MUST handle graceful failure: if plan execution fails after retries, aggregate partial results and present to user without crashing

#### Plan Generation Robustness (NEW)

- **FR-023**: Narrator AI system MUST implement bounded retry loop: max 5 plan generation attempts before escalating to user
- **FR-024**: Narrator AI system MUST track which skills have failed and disable them in subsequent replans
- **FR-025**: Narrator AI system MUST provide simple template-based narration if plan generation fails after max attempts (graceful fallback)
- **FR-026**: Narrator AI system MUST log detailed error context for each failed plan (attempted plan, failed tools, retry counts)
- **FR-027**: Plan executor MUST report specific failure reason (tool failure, circular dependency, timeout, invalid JSON) to enable accurate replan strategy
- **FR-028**: System MUST NOT loop infinitely; if planner cannot generate viable plan after 5 attempts, display error to user and allow manual session recovery

#### Skill Discovery

- **FR-030**: System MUST scan `skills/` directory at startup and discover all valid skills following Agent Skills Standard
- **FR-031**: Skill discovery MUST parse `skill.json` manifest files and validate required fields (name, version, description)
- **FR-032**: Skill discovery MUST load optional `prompt.md` files and make behavioral prompts available to plan generator
- **FR-033**: Skill discovery MUST identify all executable scripts in `skills/*/scripts/` directories
- **FR-034**: Skill discovery MUST skip skills with invalid manifests and log warnings without crashing application
- **FR-035**: System MUST support hot-reloading of skills without application restart (future enhancement; can log "restart required" for MVP)

#### Skill Configuration

- **FR-036**: System MUST provide a Skills Settings UI screen accessible from main application settings
- **FR-037**: Skills Settings UI MUST display all discovered skills with name, description, and enabled/disabled toggle
- **FR-038**: Skills Settings UI MUST dynamically generate configuration forms from `config-schema.json` files
- **FR-039**: Configuration forms MUST support input types: string (text, freeform), number, boolean (toggle), and enum (dropdown)
- **FR-040**: Configuration forms MUST obscure sensitive fields (API keys, passwords) using password-style text input
- **FR-041**: System MUST save configuration changes to skill-specific `config.json` files in skill directories
- **FR-042**: System MUST substitute environment variables in config values using `${VAR_NAME}` syntax
- **FR-043**: System MUST validate configuration values against schema constraints (required fields, type checking) before saving
- **FR-044**: System MUST display validation errors inline in configuration forms with actionable error messages

#### Skill Script Execution

- **FR-045**: Plan executor MUST invoke skill scripts as independent OS processes per Constitution Principle II
- **FR-046**: Skill scripts MUST communicate via NDJSON protocol over stdin/stdout following Spec 001
- **FR-047**: Plan executor MUST pass script input as JSON via stdin (single object per script invocation)
- **FR-048**: Plan executor MUST parse all NDJSON events emitted by scripts: `log`, `state_patch`, `asset`, `ui_event`, `error`, `done`
- **FR-049**: Plan executor MUST respect script dependencies declared in Plan JSON and execute scripts in topological order
- **FR-050**: Plan executor MUST support both parallel and sequential script execution per Plan JSON `parallel` flag and tool `async` property
- **FR-051**: Plan executor MUST enforce per-script timeout (default 30 seconds) and terminate unresponsive scripts
- **FR-052**: Plan executor MUST handle script failures gracefully (exit code != 0 or `done.ok=false`) per `required` flag in Plan JSON
- **FR-053**: Plan executor MUST collect all events from scripts, including intermediate state_patch events, for full execution trace

#### Core Skills (MVP)

- **FR-054**: System MUST ship with a `storyteller` skill for rich narrative enhancement
  - Behavioral prompt for evocative narration
  - `narrate.dart` script that calls LLM (local or hosted) for detailed prose
  - Configuration: provider (ollama/claude/openai), model, API key, style (terse/vivid/poetic), fallback settings
  
- **FR-055**: System MUST ship with a `dice-roller` skill for randomness
  - `roll-dice.dart` script that parses dice formulas (e.g., "1d20+5", "3d6")
  - Emits `ui_event` with roll results for display to player
  - Configuration: show/hide individual die rolls, random source (crypto/pseudo)
  
- **FR-056**: System MUST ship with a `memory` skill for semantic memory and continuity
  - `store-memory.dart` script that embeds and stores event summaries in local database
  - `recall-memory.dart` script that performs vector search for relevant context
  - Configuration: storage backend (sqlite/files), embedding model, max context events
  
- **FR-057**: System MUST ship with a `reputation` skill for tracking player standing
  - `update-reputation.dart` script that records reputation changes by faction
  - `query-reputation.dart` script that returns current reputation values
  - Configuration: faction list, reputation scale, decay rate, storage backend

#### Data Management

- **FR-058**: Each skill MUST be allowed to maintain its own data storage in `skills/<skill-name>/data/` directory
- **FR-059**: Skill data storage MUST persist across application restarts
- **FR-060**: Skill data MUST remain private to that skill; other skills MUST NOT directly access another skill's data directory
- **FR-061**: Skills MAY use SQLite, JSON files, or other local storage formats for their data
- **FR-062**: System MUST create skill data directories on first use if they do not exist

#### Graceful Degradation (Constitution Principle IV)

- **FR-063**: System MUST continue functioning when optional skills are not installed or disabled
- **FR-064**: System MUST display user-friendly warnings when skills are misconfigured, not crash
- **FR-065**: Skill scripts that use hosted APIs MUST fall back to local models when network is unavailable
- **FR-066**: Narrator AI MUST provide simple template-based narration if plan generation fails completely
- **FR-067**: Plan executor MUST continue executing remaining plan steps when one script fails (if independent from failure)

### Key Entities *(include if feature involves data)*

- **Skill**: A capability bundle following Agent Skills Standard
  - Attributes: name, version, description, enabled status, skill directory path
  - Contains: behavioral prompt, scripts, configuration schema, user configuration, data storage
  
- **Skill Manifest** (`skill.json`): Metadata file per Agent Skills Standard
  - Attributes: name, displayName, description, version, author, license
  
- **Skill Script**: Executable program in `skills/*/scripts/` directory
  - Attributes: name, file path, executable permissions
  - Behavior: Follows Spec 001 NDJSON protocol
  
- **Skill Configuration**: User-editable settings for a skill
  - Attributes: key-value pairs matching config schema
  - Persistence: Saved to `skills/*/config.json`
  
- **Configuration Schema** (`config-schema.json`): Defines available settings for a skill
  - Attributes: field name, type, label, hint, default value, validation rules
  
- **Plan JSON**: Structured plan generated by narrator AI
  - Attributes: requestId, narrative (fallback text), tools array, parallel flag
  - Relationships: References skill scripts via toolPath
  
- **Behavioral Prompt** (`prompt.md`): Markdown file with narrator guidance
  - Attributes: skill name, prompt text
  - Usage: Injected into plan generator system context

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Plan generator produces valid Plan JSON for 95% of player inputs within 5 seconds using local LLM
- **SC-002**: Plan generator correctly selects relevant skills for player actions (evaluated via acceptance tests) in 90% of cases
- **SC-003**: Skills Settings UI allows users to configure any core skill (storyteller, memory, reputation, dice-roller) in under 2 minutes
- **SC-004**: Skill discovery successfully loads all valid skills from `skills/` directory on application startup without errors
- **SC-005**: Skill scripts execute successfully via NDJSON protocol and return results within configured timeout (30s default) in 99% of invocations
- **SC-006**: System gracefully degrades when skills fail: application continues without crash, UI displays helpful error message, narrative continues with fallback content
- **SC-007**: Memory skill stores and recalls story events with semantic search completing in under 500ms for databases with up to 1000 events
- **SC-008**: Reputation skill tracks and persists multiple faction standings, allowing queries to return current reputation within 100ms
- **SC-009**: Storyteller skill falls back to local LLM when hosted API is unavailable, with fallback completing within 10 seconds
- **SC-010**: Users can install new skills by placing skill directories in `skills/` folder and restarting, with no code changes required
- **SC-011**: Configuration changes persist across application restarts and are correctly loaded by skills on next invocation
- **SC-012**: Plan executor handles script failures without crashing: logs error, marks step as failed, continues with remaining plan steps

---

## Plan Execution Semantics

> Defines the behavioral contracts for plan execution. Implementations must satisfy these requirements.
> For Dart/Flutter reference implementation, see [Spec 003](../003-dart-flutter-implementation/spec.md).

### Player Interaction Flow

Players interact with Narratoria by submitting natural language prompts (e.g., "I light the torch" or "I examine the mysterious door"). The narrator AI converts these prompts into executable plans that invoke tools via the protocol defined in Spec 001.

```
┌──────────────┐
│ Player types │
│   prompt     │
└──────┬───────┘
       │
       ▼
┌──────────────┐
│ Narrator AI  │ (external LLM/agent service)
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
│ Narratoria   │ executes tools per plan
│  Runtime     │ collects events via protocol
└──────┬───────┘
       │
       ▼
┌──────────────┐
│ UI updates   │ display results, assets, state
│              │
└──────────────┘
```

### Plan JSON Schema

The narrator AI MUST produce a **Plan JSON** document with this structure:

```json
{
  "requestId": "<uuid>",
  "narrative": "<string, optional narrator response>",
  "tools": [
    {
      "toolId": "<string>",
      "toolPath": "<filesystem-path-to-executable>",
      "input": { ...arbitrary JSON... },
      "dependencies": ["<toolId>", ...],
      "required": <boolean, default true>,
      "async": <boolean, default false>,
      "retryPolicy": {
        "maxRetries": <integer, default 3>,
        "backoffMs": <integer, default 100>
      }
    }
  ],
  "parallel": <boolean, default false>,
  "disabledSkills": ["<skillName>", ...],
  "metadata": {
    "generationAttempt": <integer>,
    "parentPlanId": "<uuid, or null>"
  }
}
```

**Fields**:
- `requestId`: Unique identifier for this plan execution
- `narrative`: Optional narrative text to display before or during tool execution
- `tools`: Array of tool invocation descriptors
  - `toolId`: Unique ID for this tool within the plan (for dependency tracking)
  - `toolPath`: Absolute or relative path to the tool executable
  - `input`: JSON object passed to the tool via stdin (as described in Spec 001 §6)
  - `dependencies`: Array of `toolId` values that must complete before this tool runs
  - `required`: If true, tool failure aborts dependent tools and plan execution fails; if false, tool failure is non-blocking and dependent tools may still execute
  - `async`: If true, tool may run in parallel with unrelated tools and siblings (respecting `dependencies`); if false, tool runs sequentially
  - `retryPolicy`: Configures retry behavior for this specific tool
    - `maxRetries`: Maximum retry attempts before marking tool as failed (default 3)
    - `backoffMs`: Milliseconds between retries with exponential backoff
- `parallel`: If true and dependencies allow, tools run concurrently; if false, tools run sequentially
- `disabledSkills`: Array of skill names that failed in previous execution attempts (plan generator MUST NOT select these skills)
- `metadata`: Plan metadata for debugging and replan tracking
  - `generationAttempt`: Which attempt this plan represents (1, 2, 3...)
  - `parentPlanId`: If this is a replan, the UUID of the previous plan that failed

### Plan Execution Rules

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
   - The runtime MUST apply exponential backoff between retries with minimum delay `retryPolicy.backoffMs` milliseconds
   - After exhausting retries, the runtime MUST mark the tool as failed and proceed according to the tool's `required` flag
   - The runtime MUST record retry count in the execution trace
   - Backoff formula: `delay = backoffMs × 2^(attempt-1)`

6. **Failure Handling (by `required` flag)**:
   - **If `required: true` and tool fails**:
     - Dependent tools (listing failed tool in `dependencies`) MUST NOT execute
     - Plan execution stops; all remaining non-blocking tasks MAY continue
     - Tool failure counts against plan execution attempt limit (max 3 attempts)
   - **If `required: false` and tool fails**:
     - Dependent tools MAY execute (they receive null/empty for failed tool inputs)
     - Plan continues executing remaining tasks
     - Tool failure is logged but does not abort plan
   - Independent tools (no dependency on failed tool) continue execution automatically in all cases

7. **Event Aggregation**: Narratoria MUST collect all events from all tools in the plan and merge:
   - `log` events → displayed in Tool Execution Panel
   - `state_patch` events → merged into session state
   - `asset` events → registered and displayed in Asset Gallery
   - `ui_event` events → dispatched to UI handlers
   - `error` events → displayed with context

8. **Execution Trace**: Narratoria MUST maintain a full execution trace with results for each tool

### Plan Executor Output

After executing a plan, Narratoria MUST return an execution result with full trace. See `contracts/execution-result.schema.json` for the authoritative schema.

**Purpose**: This trace allows the narrator AI to:
- Understand which tools failed and why
- Disable failed skills for the next plan via `disabledSkills` field
- Determine if replanning is possible
- Debug execution issues

### Narrator AI Interface

Per Constitution Principle IV.A, the narrator AI MUST implement:

**Required Behavior**:
- The narrator AI MUST return Plan JSON in the format specified above
- The narrator AI MUST implement bounded replan loop: maximum 5 plan generation attempts before graceful fallback
- The narrator AI MUST consult `disabledSkills` in execution results to avoid selecting failed skills in subsequent plans
- The narrator AI MUST track generation attempt count in `metadata.generationAttempt`
- The narrator AI MUST set `metadata.parentPlanId` to the previous plan's UUID when replanning
- After 5 failed plan generation attempts, the narrator AI MUST provide template-based fallback narration

**Implementation Flexibility**:
- The narrator AI MAY be a separate process, remote service, or in-process module
- Tool capability discovery mechanisms are implementation-specific
- Plan generation strategy (prompt engineering, model selection) is implementation-specific

---

## Related Specifications

| Specification | Relationship |
|---------------|--------------|
| [001: Tool Protocol](../001-tool-protocol-spec/spec.md) | Defines event types, transport model, NDJSON protocol |
| [003: Dart/Flutter Implementation](../003-dart-flutter-implementation/spec.md) | Dart+Flutter reference implementation including algorithms and data models |

---

## Contracts

This specification defines the following machine-readable contracts in `contracts/`:

- **plan-json.schema.json**: JSON Schema for Plan JSON documents
- **execution-result.schema.json**: JSON Schema for plan execution results
- **skill-manifest.schema.json**: JSON Schema for skill.json manifests
- **config-schema-meta.schema.json**: Meta-schema for skill config-schema.json files
- **example-plan.json**: Example Plan JSON (first attempt)
- **example-plan-replan.json**: Example Plan JSON (replan after failure)
