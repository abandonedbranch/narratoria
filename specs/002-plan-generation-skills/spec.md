# Feature Specification: Plan Generation and Skill Discovery

**Feature Branch**: `002-plan-generation-skills`  
**Created**: 2026-01-26  
**Status**: Draft  
**Input**: User description: "Plan generation and skill discovery with Agent Skills Standard integration"

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
- **FR-002**: Plan generator MUST convert player text input into structured Plan JSON documents following Spec 001 schema
- **FR-003**: Plan generator MUST select relevant skills and their scripts based on player intent and available skills
- **FR-004**: Plan generator MUST inject active skills' behavioral prompts into system context when generating plans
- **FR-005**: Plan generator MUST fall back to simple pattern-based planning if LLM fails or is unavailable
- **FR-006**: Plan generator MUST complete plan generation within 5 seconds for typical player inputs (under 100 words)
- **FR-007**: Plan generator MUST NOT make network calls or access external APIs (Constitution Principle II exception for in-process AI)

#### Skill Discovery

- **FR-008**: System MUST scan `skills/` directory at startup and discover all valid skills following Agent Skills Standard
- **FR-009**: Skill discovery MUST parse `skill.json` manifest files and validate required fields (name, version, description)
- **FR-010**: Skill discovery MUST load optional `prompt.md` files and make behavioral prompts available to plan generator
- **FR-011**: Skill discovery MUST identify all executable scripts in `skills/*/scripts/` directories
- **FR-012**: Skill discovery MUST skip skills with invalid manifests and log warnings without crashing application
- **FR-013**: System MUST support hot-reloading of skills without application restart (future enhancement; can log "restart required" for MVP)

#### Skill Configuration

- **FR-014**: System MUST provide a Skills Settings UI screen accessible from main application settings
- **FR-015**: Skills Settings UI MUST display all discovered skills with name, description, and enabled/disabled toggle
- **FR-016**: Skills Settings UI MUST dynamically generate configuration forms from `config-schema.json` files
- **FR-017**: Configuration forms MUST support input types: string (text, freeform), number, boolean (toggle), and enum (dropdown)
- **FR-018**: Configuration forms MUST obscure sensitive fields (API keys, passwords) using password-style text input
- **FR-019**: System MUST save configuration changes to skill-specific `config.json` files in skill directories
- **FR-020**: System MUST substitute environment variables in config values using `${VAR_NAME}` syntax
- **FR-021**: System MUST validate configuration values against schema constraints (required fields, type checking) before saving
- **FR-022**: System MUST display validation errors inline in configuration forms with actionable error messages

#### Skill Script Execution

- **FR-023**: Plan executor MUST invoke skill scripts as independent OS processes per Constitution Principle II
- **FR-024**: Skill scripts MUST communicate via NDJSON protocol over stdin/stdout following Spec 001
- **FR-025**: Plan executor MUST pass script input as JSON via stdin (single object per script invocation)
- **FR-026**: Plan executor MUST parse all NDJSON events emitted by scripts: `log`, `state_patch`, `asset`, `ui_event`, `error`, `done`
- **FR-027**: Plan executor MUST respect script dependencies declared in Plan JSON and execute scripts in correct order
- **FR-028**: Plan executor MUST support parallel script execution when `parallel: true` in Plan JSON
- **FR-029**: Plan executor MUST enforce per-script timeout (default 30 seconds) and terminate unresponsive scripts
- **FR-030**: Plan executor MUST handle script failures gracefully (exit code != 0 or `done.ok=false`) without crashing application

#### Core Skills (MVP)

- **FR-031**: System MUST ship with a `storyteller` skill for rich narrative enhancement
  - Behavioral prompt for evocative narration
  - `narrate.dart` script that calls LLM (local or hosted) for detailed prose
  - Configuration: provider (ollama/claude/openai), model, API key, style (terse/vivid/poetic), fallback settings
  
- **FR-032**: System MUST ship with a `dice-roller` skill for randomness
  - `roll-dice.dart` script that parses dice formulas (e.g., "1d20+5", "3d6")
  - Emits `ui_event` with roll results for display to player
  - Configuration: show/hide individual die rolls, random source (crypto/pseudo)
  
- **FR-033**: System MUST ship with a `memory` skill for semantic memory and continuity
  - `store-memory.dart` script that embeds and stores event summaries in local database
  - `recall-memory.dart` script that performs vector search for relevant context
  - Configuration: storage backend (sqlite/files), embedding model, max context events
  
- **FR-034**: System MUST ship with a `reputation` skill for tracking player standing
  - `update-reputation.dart` script that records reputation changes by faction
  - `query-reputation.dart` script that returns current reputation values
  - Configuration: faction list, reputation scale, decay rate, storage backend

#### Data Management

- **FR-035**: Each skill MUST be allowed to maintain its own data storage in `skills/<skill-name>/data/` directory
- **FR-036**: Skill data storage MUST persist across application restarts
- **FR-037**: Skill data MUST remain private to that skill; other skills MUST NOT directly access another skill's data directory
- **FR-038**: Skills MAY use SQLite, JSON files, or other local storage formats for their data
- **FR-039**: System MUST create skill data directories on first use if they do not exist

#### Graceful Degradation (Constitution Principle IV)

- **FR-040**: System MUST continue functioning when optional skills are not installed or disabled
- **FR-041**: System MUST display user-friendly warnings when skills are misconfigured, not crash
- **FR-042**: Skill scripts that use hosted APIs MUST fall back to local models when network is unavailable
- **FR-043**: Narrator AI MUST provide simple template-based narration if plan generation fails completely
- **FR-044**: Plan executor MUST continue executing remaining plan steps when one script fails

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
