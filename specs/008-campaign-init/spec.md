# Feature Specification: Campaign Init TUI

**Feature Branch**: `008-campaign-init`
**Created**: 2026-02-03
**Status**: Draft
**Input**: User description: "Let's create a new spec for a TUI app, that will allow campaign creators to get started in making a campaign."

## Overview

The Campaign Init TUI is an interactive terminal application that guides campaign creators through the process of scaffolding a new campaign directory structure and manifest. It provides both quick-start (minimal 3-file setup) and detailed creation workflows, respecting the spectrum principle from Spec 007: "The more a campaign provides, the less the AI invents." The TUI reduces friction for non-technical authors while offering advanced customization for experienced creators. It validates campaign structure in real-time and optionally triggers sparse data enrichment for minimal campaigns.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Quick-Start Campaign Creation (Priority: P1)

A new campaign creator launches the TUI and wants to get a playable campaign running in under 5 minutes. They choose "Quick Start," answer 3-4 prompts (title, genre, tone, one-sentence premise), and receive a scaffolded campaign directory ready for play or enrichment.

**Why this priority**: This is the MVP—without a fast path to playable campaigns, new authors will abandon the tool. Quick Start proves the concept and delivers immediate value.

**Independent Test**: Can be fully tested by launching the TUI in quick-start mode, providing minimal input, and verifying the resulting directory contains a valid `manifest.json`, `world/setting.md`, and `plot/premise.md`.

**Acceptance Scenarios**:

1. **Given** the TUI is launched with no arguments, **When** the user selects "Quick Start", **Then** the TUI prompts for title, author, genre, tone, and one-sentence premise in sequence.
2. **Given** all quick-start prompts are answered, **When** the user confirms, **Then** the system creates a campaign directory and scaffolds the three required files with user input embedded.
3. **Given** a campaign is successfully created, **When** the user exits the TUI, **Then** the system displays the campaign path and an option to launch the Narratoria campaign player.
4. **Given** the user provides invalid input (empty title, for example), **When** they attempt to proceed, **Then** the TUI displays a clear error and re-prompts without losing previous valid input.

---

### User Story 2 - Detailed Campaign Setup (Priority: P1)

An experienced author wants fine-grained control over campaign structure. They choose "Detailed Setup" and are guided through optional sections: NPC profiles, plot beats, world constraints, and asset directories. The TUI scaffolds subdirectories and provides template JSON files for structured data entry.

**Why this priority**: P1 because experienced authors represent a significant user segment that drives campaign quality and encourages experimentation. Without this workflow, Narratoria loses advanced use cases.

**Independent Test**: Can be fully tested by launching the TUI in detailed mode, selecting subsets of optional features (e.g., "Create NPCs" and "Add plot beats"), and verifying the resulting directory contains correctly-structured subdirectories and template files.

**Acceptance Scenarios**:

1. **Given** the user selects "Detailed Setup", **When** they are prompted for campaign metadata (manifest fields), **Then** they can choose to include/skip each optional section (NPCs, plot beats, world constraints, assets).
2. **Given** the user selects "Create NPC profiles", **When** they specify the number of NPCs, **Then** the TUI creates `characters/npcs/{name}/` subdirectories and scaffolds `profile.json` templates.
3. **Given** the user selects "Add plot beats", **When** they specify the number of beats, **Then** the TUI scaffolds a `plot/beats.json` template with empty beat structures.
4. **Given** the user selects "Add creative assets", **When** they choose asset types (art/music), **Then** the TUI creates `art/` and/or `music/` directories with usage guidance files.
5. **Given** the user reviews the final campaign structure, **When** they confirm, **Then** the TUI displays a summary of created files and directories.

---

### User Story 3 - Campaign Validation and Error Reporting (Priority: P1)

As the TUI creates a campaign, it validates the manifest against the Campaign Format schema (Spec 007, FR-029). If validation fails, it displays clear errors identifying the problematic field and suggests fixes. Invalid campaigns are not saved until corrected.

**Why this priority**: P1 because validation prevents downstream errors during campaign load. Authors must see clear feedback immediately, not after attempting to play the campaign.

**Independent Test**: Can be fully tested by intentionally providing invalid manifest data (missing required fields, malformed JSON) and verifying the TUI rejects it with specific error messages and repair suggestions.

**Acceptance Scenarios**:

1. **Given** the user provides a campaign title that is empty or only whitespace, **When** they attempt to proceed, **Then** the TUI displays "Title is required and cannot be empty" and returns to the title prompt.
2. **Given** the user provides an invalid semver version (e.g., "1.x.x"), **When** they attempt to proceed, **Then** the TUI displays "Version must follow semantic versioning (e.g., 1.0.0)" and re-prompts.
3. **Given** the manifest is constructed from user input, **When** the TUI validates it against the schema, **Then** it reports any missing required fields with repair instructions.
4. **Given** invalid manifest JSON is detected, **When** validation runs, **Then** the TUI displays the specific field and error (e.g., "genre: 'fantasy' is not a valid enum value") with supported options.

---

### User Story 4 - Campaign Directory and Asset Guidance (Priority: P2)

The TUI provides in-context help and usage guidance for each campaign directory. When the user selects "Add creative assets," the TUI explains the `art/` and `music/` directory structures, shows naming conventions that enable semantic linking, and creates guidance files (`README.md` in each directory explaining best practices).

**Why this priority**: P2 because it improves the author experience but isn't strictly necessary for MVP. Authors can create directories manually if needed, but guided setup reduces errors.

**Independent Test**: Can be tested by selecting asset directory creation and verifying that guidance files are created in `art/` and `music/` with clear conventions for file naming and metadata.

**Acceptance Scenarios**:

1. **Given** the user selects "Add creative assets", **When** they choose to create asset directories, **Then** the TUI creates `art/` and `music/` with `README.md` guidance files.
2. **Given** asset directories are created, **When** the TUI displays the campaign structure, **Then** it shows helpful hints like "Save character portraits as `art/characters/npc_name.png` to enable automatic linking."
3. **Given** the user is creating NPC profiles in detailed mode, **When** they define an NPC, **Then** the TUI suggests they can add a portrait file `art/characters/{npc_name}.png` for automatic indexing.

---

### User Story 5 - Sparse Data Enrichment Prompt (Priority: P2)

When the TUI completes campaign creation with a minimal set of files (manifest + setting + premise), it prompts the user: "Your campaign is ready! Would you like Narratoria to auto-generate missing content (NPCs, lore, etc.) using AI?" If accepted, it integrates with the sparse data enrichment pipeline from Spec 007 (FR-033, User Story 2b).

**Why this priority**: P2 because it bridges the gap between quick-start campaigns and enriched playable campaigns. However, the enrichment itself is owned by the Narrative Engine (Spec 008), so this TUI only triggers the workflow.

**Independent Test**: Can be tested by creating a minimal campaign and verifying the enrichment prompt appears, and that selecting "yes" creates a marker or flag for downstream enrichment processing.

**Acceptance Scenarios**:

1. **Given** a minimal campaign is created (manifest + 2-3 files), **When** creation completes, **Then** the TUI displays the enrichment prompt with clear explanation.
2. **Given** the user accepts enrichment, **When** they confirm, **Then** the TUI creates a `.narratoria-enrich` marker file indicating enrichment is requested.
3. **Given** the user declines enrichment, **When** they exit, **Then** the campaign is marked as ready-to-play without modification.
4. **Given** enrichment is accepted, **When** the user launches the Narratoria player, **Then** the narrative engine detects the marker and runs sparse data enrichment before loading the campaign.

---

### User Story 6 - Campaign Template Selection (Priority: P3)

The TUI offers optional campaign templates to users: "Fantasy RPG," "Sci-Fi Adventure," "Mystery Thriller," "Slice of Life." Selecting a template pre-fills manifest fields (genre, tone, default constraints) and provides sample NPC and plot beat structures.

**Why this priority**: P3 because it accelerates setup for common genres but isn't required for MVP. Authors can always create from scratch.

**Independent Test**: Can be tested by selecting a template and verifying that manifest fields are pre-populated correctly and sample files are created.

**Acceptance Scenarios**:

1. **Given** the user starts the TUI, **When** they select "Detailed Setup" and are offered templates, **Then** they can choose from predefined templates or skip to blank setup.
2. **Given** a template is selected (e.g., "Fantasy RPG"), **When** it is applied, **Then** the TUI pre-fills genre="fantasy", tone="adventurous", and creates sample NPC and plot beat templates.
3. **Given** a template is applied, **When** the user reviews the campaign, **Then** they can edit or remove any pre-populated content.

---

### Edge Cases

- What happens if the user exits the TUI mid-creation (Ctrl+C)?
  - System should prompt "Save progress?" and either save a partial campaign or discard. Partial campaigns should not be corrupted.

- What happens if the target directory already contains a campaign with the same name?
  - System should warn "Campaign already exists at [path]. Overwrite? (yes/no)" and prevent accidental overwrites.

- What happens if the user's filesystem has no write permissions to the target directory?
  - System should display "Permission denied: Cannot write to [path]. Choose a different location." and prompt for a new path.

- What happens if the user specifies a campaign name with invalid filesystem characters (e.g., `/`, `\`, `*`)?
  - System should display "Campaign name cannot contain: / \ * ? : < > |" and re-prompt.

- What happens if the user provides very long campaign descriptions (1000+ characters)?
  - System should accept them but warn "This description is very long. Consider shortening for better UX." Validation should not reject.

- What happens if the user selects all optional features in detailed mode (many NPCs, plot beats, assets)?
  - System should handle gracefully, creating all requested subdirectories and templates without timeout or memory issues.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST recognize user input via terminal prompts and keyboard navigation (arrow keys, Enter, Esc).
- **FR-002**: System MUST support two primary workflows: "Quick Start" (minimal prompts) and "Detailed Setup" (optional features).
- **FR-003**: System MUST validate campaign titles: required, non-empty, no invalid filesystem characters (/, \, *, ?, :, <, >, |).
- **FR-004**: System MUST validate semantic version strings (e.g., "1.0.0") and reject invalid formats with specific error messages.
- **FR-005**: System MUST validate manifest JSON against the Campaign Format schema (Spec 007 contracts/manifest.schema.json) before saving.
- **FR-006**: System MUST create a campaign root directory with the user-specified name.
- **FR-007**: System MUST scaffold a `manifest.json` file with all provided user input, following Spec 007 manifest structure.
- **FR-008**: System MUST scaffold `world/setting.md` with user-provided setting description.
- **FR-009**: System MUST scaffold `plot/premise.md` with user-provided premise.
- **FR-010**: System MUST, in detailed mode, create optional subdirectories: `characters/npcs/{name}/`, `plot/`, `lore/`, `art/`, `music/` based on user selections.
- **FR-011**: System MUST, for each selected NPC in detailed mode, scaffold a `characters/npcs/{name}/profile.json` template following Spec 007 NPC schema.
- **FR-012**: System MUST, if plot beats are selected, scaffold a `plot/beats.json` template with empty beat structures following Spec 007 plot schema.
- **FR-013**: System MUST create guidance files (README.md) in asset directories (`art/`, `music/`) explaining directory structure and file naming conventions.
- **FR-014**: System MUST support optional template selection (e.g., "Fantasy RPG") that pre-populates manifest fields and creates sample NPC/beat structures.
- **FR-015**: System MUST preserve user input across navigation (do not lose data if user scrolls back to previous prompts).
- **FR-016**: System MUST display clear, actionable error messages with suggestions for fixing invalid input.
- **FR-017**: System MUST warn users if a campaign directory already exists and prevent accidental overwrites without explicit confirmation.
- **FR-018**: System MUST validate filesystem permissions before creating directories and display clear errors if write permissions are denied.
- **FR-019**: System MUST, upon successful campaign creation, display the full campaign path and a prompt to launch the Narratoria campaign player.
- **FR-020**: System MUST, for minimal campaigns (fewer than 4 files), display an optional enrichment prompt: "Would you like Narratoria to auto-generate missing content?"
- **FR-021**: System MUST, if enrichment is accepted, create a `.narratoria-enrich` marker file in the campaign root to signal downstream enrichment.
- **FR-022**: System MUST provide inline help and context hints for each section (accessible via Ctrl+H or similar keybinding).
- **FR-023**: System MUST support configurable output directory (default: current working directory, or accept `--dir /path/to/campaigns` CLI argument).
- **FR-024**: System MUST display a summary of all created files and directories before final confirmation.
- **FR-025**: System MUST handle graceful exit on Ctrl+C with prompt to save partial progress.

---

### Key Entities

- **Campaign Root**: A directory containing all campaign content, identified by the presence of `manifest.json`.
- **Manifest**: A JSON file containing campaign metadata (title, author, version, genre, tone, content_rating, optional rules_hint and hydration_guidance).
- **Quick-Start Campaign**: A minimal campaign with 3 files: `manifest.json`, `world/setting.md`, `plot/premise.md`.
- **Detailed Campaign**: A comprehensive campaign with optional subdirectories and structured files (NPCs, plot beats, constraints, assets).
- **Campaign Template**: A pre-configured set of manifest defaults and sample structures for common genres (e.g., "Fantasy RPG").
- **Enrichment Marker**: A `.narratoria-enrich` file indicating that sparse data enrichment should be applied on next load.
- **Validation Schema**: The JSON schema (Spec 007 contracts/manifest.schema.json) used to validate manifest correctness.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A new campaign creator can complete Quick Start and create a valid, playable campaign in under 5 minutes (median time across 10 users).
- **SC-002**: 95% of user-provided manifest data is correctly saved to `manifest.json` without data loss or corruption.
- **SC-003**: Invalid input is caught and reported with specific error messages (not generic "error" messages) in under 500ms.
- **SC-004**: All generated files conform to their respective schemas (manifest.json, NPC profiles, plot beats) with 100% validation pass rate.
- **SC-005**: Users successfully create campaigns with all optional features (NPCs, plot beats, assets) without timeout or system slowdown.
- **SC-006**: The TUI UI is responsive to user input with sub-100ms latency for navigation and text input.
- **SC-007**: 90% of first-time campaign creators can complete Quick Start without consulting documentation.
- **SC-008**: Directory structure creation succeeds in 100% of attempts on target platforms (macOS, Linux, Windows via WSL) with proper permission handling.
- **SC-009**: Campaigns created by the TUI are immediately loadable by the Narratoria campaign player (Spec 008) without errors.
- **SC-010**: The enrichment prompt appears for all minimal campaigns and correctly sets the `.narratoria-enrich` marker when accepted.

---

## Assumptions

- Campaign creators are comfortable using terminal applications (TUI assumes basic CLI literacy).
- The host system has write permissions to the target campaign directory (or the TUI can prompt for a different location).
- Campaign names follow filesystem naming conventions and can be used as directory names.
- The Campaign Format schema (Spec 007) is stable and available at `contracts/manifest.schema.json`.
- Authors can edit scaffolded template files (manifest.json, NPC profiles, plot beats) using standard text editors after TUI creation.
- Semantic versioning is the appropriate versioning scheme for campaigns (not date-based or custom schemes).
- Templates are optional and can be skipped; blank setup is always available.

---

## Dependencies

- **Spec 007: Campaign Format** - Defines manifest schema, file structure, and validation requirements that the TUI must enforce.
- **Spec 008: Narrative Engine** - Consumes campaigns created by this TUI; must handle `.narratoria-enrich` marker and sparse data enrichment.
- **Spec 003: Skills Framework** - Optional: Skills may be used to validate or enrich campaign data (future integration).

---

## Out of Scope

- Editing existing campaigns (this TUI only creates new ones; editing is a separate feature).
- Campaign publishing or distribution.
- Campaign versioning or migration between format versions.
- Runtime AI generation (enrichment is triggered by marker, not performed by TUI).
- Campaign marketplace or template download from remote sources (templates are bundled with TUI).
- Multi-user or collaborative campaign creation.
- Campaign import from external formats (e.g., converting D&D modules to Narratoria campaigns).

---

## Documentation Artifacts

This specification includes the following documentation artifacts for implementers:

### Contracts (JSON Schemas)
- **`contracts/manifest.schema.json`** - Manifest structure (inherited from Spec 007)
- **`contracts/cli-args.schema.json`** - CLI argument schema for TUI (--dir, --template, --quick-start, etc.)

### Examples
- **`examples/quickstart-flow.md`** - Walkthrough of a complete quick-start session
- **`examples/detailed-flow.md`** - Walkthrough of a detailed setup session with all optional features
- **`examples/sample-manifest.json`** - Completed manifest from quick-start flow

### Guides
- **`TUI_USER_GUIDE.md`** - User-facing guide for campaign creators using the TUI
- **`TUI_DEVELOPER_GUIDE.md`** - Implementation guide for developers building the TUI
