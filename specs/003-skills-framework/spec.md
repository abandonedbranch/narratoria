# Specification 003: Skills Framework

**Status**: Draft
**Version**: 0.1.0
**Created**: 2026-01-26
**Parent Specs**: [001-tool-protocol](../001-tool-protocol-spec/spec.md), [002-plan-execution](../002-plan-execution/spec.md)

## Prerequisites

**Read first**: [Spec 001 - Tool Protocol](../001-tool-protocol-spec/spec.md)

**Then read together with**: [Spec 002 - Plan Execution](../002-plan-execution/spec.md)

Specs 003 and 002 are **co-dependent** (see Spec 002's Prerequisites for explanation). Spec 003 defines the component model (skills); Spec 002 defines how they're orchestrated in plans.

**How they connect**: 
- Spec 002 produces Plan JSON describing which skill scripts to invoke
- Spec 003 explains how skills are discovered, configured, and executed—skills respond to script invocations from Spec 002
- The Narrator AI (in Spec 002) injects behavioral prompts from Spec 003 into system context when generating plans

**After these**: Specs 004-008 build further. Spec 004 defines specific individual skills; Specs 005-008 show how to implement and execute skills.

---

## RFC 2119 Keywords

The key words "MUST", "MUST NOT", "REQUIRED", "SHALL", "SHALL NOT", "SHOULD", "SHOULD NOT", "RECOMMENDED", "MAY", and "OPTIONAL" in this document are to be interpreted as described in [RFC 2119](https://datatracker.ietf.org/doc/html/rfc2119).

## 1. Purpose

This specification defines the skills framework for Narratoria, implementing the [Agent Skills Standard](https://agentskills.io/specification). This includes:

- Skill discovery and loading
- Skill configuration management
- Skill script execution
- Data management for skills
- Graceful degradation patterns

**Scope excludes:**
- Plan generation and execution semantics (see [Spec 002](../002-plan-execution/spec.md))
- Individual skill specifications (see [Spec 004](../004-narratoria-skills/spec.md))
- Dart/Flutter implementation details (see [Spec 005](../005-dart-implementation/spec.md))

---

## 2. Terminology

- **Skill**: A capability bundle following Agent Skills Standard containing behavioral prompts, scripts, configuration, and data storage
- **Skill Manifest** (`skill.json`): Metadata file per Agent Skills Standard; defines skill identity, version, author, behavioral prompt path, scripts available
- **Behavioral Prompt** (`prompt.md`): Markdown file injected into narrator AI system context; guides narrator behavior for this skill
- **`errorState` (Enum)**: Health status of a skill at runtime:
  - `healthy`: Available for planning
  - `degraded`: Slow/unreliable but available
  - `temporaryFailure`: Transient network issue, retry with backoff
  - `permanentFailure`: Unrecoverable, disable for session
- **Available Skills**: Skills with `errorState != permanentFailure` AND not in `disabledSkills` set
- **Graceful Degradation**: System continues functioning and presents narration to user even when optional features fail

---

## 3. User Scenarios

### User Story 2 - Skill Configuration (Priority: P2)

A player wants to enhance their storytelling experience by configuring the storyteller skill to use a hosted AI provider (like Claude or GPT-4) instead of the default local model. They navigate to Settings → Skills, select the storyteller skill, enter their API key, and choose their preferred model.

**Why this priority**: Configuration is essential for skills that require external services (API keys, model selection) and user preferences (narrative style, detail level). Without this, users cannot unlock the full potential of advanced skills.

**Independent Test**: Can be tested by creating a skill with a config schema, opening the skills settings screen, modifying values, saving, and verifying that the skill uses the new configuration.

**Acceptance Scenarios**:

1. **Given** storyteller skill is installed, **When** user opens Skills settings, **Then** storyteller appears with configuration form showing API provider options, model selection, and style preferences
2. **Given** user is configuring storyteller skill, **When** user enters API key and selects "Claude" provider, **Then** configuration is validated and saved to `skills/storyteller/config.json`
3. **Given** storyteller skill is configured for Claude API, **When** narrator generates a plan that uses storyteller, **Then** storyteller script uses Claude API instead of local model
4. **Given** Claude API request fails (network error, invalid key), **When** storyteller script runs, **Then** script gracefully falls back to local model and logs the fallback

---

### User Story 3 - Skill Discovery and Installation (Priority: P2)

A player wants to add a new skill (e.g., a rules engine for D&D 5e) to their Narratoria installation. They download a skill package following Agent Skills Standard, place it in the `skills/` directory, and restart the application. Narratoria automatically discovers the new skill and makes it available to the narrator AI.

**Why this priority**: Extensibility is core to Narratoria's vision. Users should be able to add custom skills for different game systems, narrative styles, and creative tools without modifying application code.

**Independent Test**: Can be tested by creating a minimal skill with `skill.json` manifest, placing it in `skills/`, restarting the app, and verifying it appears in the skills list and is available for plan generation.

**Acceptance Scenarios**:

1. **Given** Narratoria is running, **When** user places a new skill directory in `skills/` and restarts, **Then** skill appears in Skills settings with metadata from `skill.json`
2. **Given** new skill has configuration requirements, **When** skill is discovered, **Then** configuration form is auto-generated from `config-schema.json`
3. **Given** new skill includes behavioral prompts, **When** plan generator runs, **Then** skill's `prompt.md` is injected into system context for narrator AI
4. **Given** new skill includes scripts, **When** narrator generates a plan using that skill, **Then** scripts are executable and follow NDJSON protocol (Spec 001)

---

## 4. Functional Requirements

### 4.1 Skill Discovery

- **FR-030**: System MUST scan `skills/` directory at startup and discover all valid skills following Agent Skills Standard
- **FR-031**: Skill discovery MUST parse `skill.json` manifest files and validate required fields (name, version, description)
- **FR-032**: Skill discovery MUST load optional `prompt.md` files and make behavioral prompts available to plan generator
- **FR-033**: Skill discovery MUST identify all executable scripts in `skills/*/scripts/` directories
- **FR-034**: Skill discovery MUST skip skills with invalid manifests and log warnings without crashing application
- **FR-035**: System SHOULD support hot-reloading of skills without application restart; MVP implementation MUST log "restart required" message when skill changes detected, with full hot-reload as post-MVP enhancement

### 4.2 Skill Configuration

- **FR-036**: System MUST provide a Skills Settings UI screen accessible from main application settings
- **FR-037**: Skills Settings UI MUST display all discovered skills with name, description, and enabled/disabled toggle
- **FR-038**: Skills Settings UI MUST dynamically generate configuration forms from `config-schema.json` files
- **FR-039**: Configuration forms MUST support input types: string (text, freeform), number, boolean (toggle), and enum (dropdown)
- **FR-040**: Configuration forms MUST obscure sensitive fields (API keys, passwords) using password-style text input
- **FR-041**: System MUST save configuration changes to skill-specific `config.json` files in skill directories
- **FR-042**: System MUST substitute environment variables in config values using `${VAR_NAME}` syntax
- **FR-043**: System MUST validate configuration values against schema constraints (required fields, type checking) before saving
- **FR-044**: System MUST display validation errors inline in configuration forms with actionable error messages

### 4.3 Skill Script Execution

- **FR-045**: Plan executor MUST invoke skill scripts as independent OS processes per Constitution Principle II
- **FR-046**: Skill scripts MUST communicate via NDJSON protocol over stdin/stdout following Spec 001
- **FR-047**: Plan executor MUST pass script input as JSON via stdin (single object per script invocation)
- **FR-048**: Plan executor MUST parse all NDJSON events emitted by scripts: `log`, `state_patch`, `asset`, `ui_event`, `error`, `done`
- **FR-049**: Plan executor MUST respect script dependencies declared in Plan JSON and execute scripts in topological order
- **FR-050**: Plan executor MUST support both parallel and sequential script execution per Plan JSON `parallel` flag and tool `async` property
- **FR-051**: Plan executor MUST enforce per-script timeout (default 30 seconds) and terminate unresponsive scripts
- **FR-052**: Plan executor MUST handle script failures gracefully (exit code != 0 or `done.ok=false`) per `required` flag in Plan JSON
- **FR-053**: Plan executor MUST collect all events from scripts, including intermediate state_patch events, for full execution trace

### 4.4 Data Management (Framework)

- **FR-103**: Each skill MUST be allowed to maintain its own data storage in `skills/<skill-name>/data/` directory for temporary working files, caches, and skill-private runtime state
- **FR-104**: Skill data storage MUST persist across application restarts
- **FR-105**: Skill data MUST remain private to that skill; other skills MUST NOT directly access another skill's data directory
- **FR-106**: Skills MAY use SQLite, JSON files, or other local storage formats for their data
- **FR-107**: System MUST create skill data directories on first use if they do not exist

> **Note on Cross-Skill Data**: `skills/<skill>/data/` is for skill-private working files. For persistent narrative data that is shared across skills (memory events, reputation, NPC perception, character portraits), see [Spec 006 - Skill State Persistence](../006-skill-state-persistence/spec.md). The Plan Generator in [Spec 002](../002-plan-execution/spec.md) orchestrates when skills access the shared persistence layer by including storage/retrieval operations in plans.

### 4.5 Graceful Degradation (Constitution Principle IV)

- **FR-113**: System MUST continue functioning when optional skills are not installed or disabled
- **FR-114**: System MUST display user-friendly warnings when skills are misconfigured, not crash
- **FR-115**: Skill scripts that use hosted APIs MUST fall back to local models when network is unavailable
- **FR-116**: Narrator AI MUST provide simple template-based narration if plan generation fails completely
- **FR-117**: Plan executor MUST continue executing remaining plan steps when one script fails (if independent from failure)
- **FR-118**: All skills MUST log failures and continue functioning for remaining capabilities

> **Note on FR numbering**: FR-119 through FR-121 are reserved for skill-specific graceful degradation requirements in [Spec 004](../004-narratoria-skills/spec.md).

---

## 5. Key Entities

### Skill

A capability bundle following Agent Skills Standard.

**Attributes**: name, version, description, enabled status, skill directory path
**Contains**: behavioral prompt, scripts, configuration schema, user configuration, data storage

### Skill Manifest (`skill.json`)

Metadata file per Agent Skills Standard.

**Attributes**: name, displayName, description, version, author, license

### Skill Script

Executable program in `skills/*/scripts/` directory.

**Attributes**: name, file path, executable permissions
**Behavior**: Follows Spec 001 NDJSON protocol

### Skill Configuration

User-editable settings for a skill.

**Attributes**: key-value pairs matching config schema
**Persistence**: Saved to `skills/*/config.json`

### Configuration Schema (`config-schema.json`)

Defines available settings for a skill.

**Attributes**: field name, type, label, hint, default value, validation rules

### Behavioral Prompt (`prompt.md`)

Markdown file with narrator guidance.

**Attributes**: skill name, prompt text
**Usage**: Injected into plan generator system context

---

## 6. Edge Cases

### Framework Edge Cases

- **What happens when a skill's script fails to execute (file not found, permission denied, crashes)?**
  - Plan executor logs error, emits `done.ok=false` per protocol, narrator falls back to simple narration

- **How does system handle skills with missing or invalid manifests?**
  - Skill discovery skips invalid skills and logs warning, application continues with valid skills only

- **What happens when user configures skill with invalid API key?**
  - Skill validation detects error on first use, displays warning in UI, falls back to local alternative

- **How does narrator AI select between multiple skills that could handle the same action?**
  - Plan generator uses LLM reasoning + skill metadata (priority, capabilities) to choose most appropriate

- **How does system handle skills that require data migration across versions?**
  - Skill's responsibility; skill checks data schema version and migrates at load time or first use

---

## 7. Success Criteria

- **SC-003**: Skills Settings UI allows users to configure any core skill (storyteller, memory, reputation, dice-roller) in under 2 minutes
- **SC-004**: Skill discovery successfully loads all valid skills from `skills/` directory on application startup without errors
- **SC-010**: Users can install new skills by placing skill directories in `skills/` folder and restarting, with no code changes required
- **SC-011**: Configuration changes persist across application restarts and are correctly loaded by skills on next invocation

---

## 8. Related Specifications

| Specification | Relationship |
|---------------|--------------|
| [001: Tool Protocol](../001-tool-protocol-spec/spec.md) | Defines NDJSON protocol for skill script communication |
| [002: Plan Execution](../002-plan-execution/spec.md) | Consumes skills via Plan JSON |
| [004: Narratoria Skills](../004-narratoria-skills/spec.md) | Individual skill specifications |
| [005: Dart Implementation](../005-dart-implementation/spec.md) | Dart+Flutter reference implementation |

---

## 9. Contracts

This specification defines the following machine-readable contracts in `contracts/`:

- **skill-manifest.schema.json**: JSON Schema for skill.json manifests
- **config-schema-meta.schema.json**: Meta-schema for skill config-schema.json files
