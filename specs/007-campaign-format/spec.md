# Feature Specification: Campaign Format

**Feature Branch**: `007-campaign-format`
**Created**: 2026-02-02
**Status**: Draft
**Input**: User description: "Define the campaign directory structure and manifest schema for Narratoria story packages"

## Overview

The Campaign Format defines how story authors package their narratives for Narratoria. A campaign is a self-contained directory containing world-building, characters, plot structure, lore, and optional assets. The AI "hydrates" the campaign based on its completeness—filling gaps intelligently when content is sparse, or executing faithfully when content is detailed.

**Core Principle**: The more a campaign provides, the less the AI invents. This is a spectrum, not a toggle.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Load and Play a Campaign (Priority: P1)

A player downloads a campaign package and loads it into Narratoria. The system validates the package structure, indexes lore content for semantic search, and presents the story premise. The player can begin playing immediately with contextually relevant choices.

**Why this priority**: This is the fundamental use case—without loading campaigns, nothing else works. It delivers the core value proposition of AI-driven storytelling.

**Independent Test**: Can be fully tested by providing a minimal campaign (just `manifest.json` + `world/setting.md`) and verifying the AI generates appropriate narrative and choices.

**Acceptance Scenarios**:

1. **Given** a valid campaign directory with manifest, **When** the player loads the campaign, **Then** the system displays the story title, setting summary, and initial scene.
2. **Given** a campaign with lore files, **When** the player loads the campaign, **Then** lore content is indexed for semantic retrieval during gameplay.
3. **Given** a campaign missing required files, **When** the player attempts to load it, **Then** the system displays a clear error identifying missing components.

---

### User Story 2 - Author Creates a Minimal Campaign (Priority: P1)

A story author creates a campaign with just a premise and basic setting description. The AI fills all gaps (characters, plot beats, locations) through improvisation, creating an emergent narrative experience.

**Why this priority**: Low barrier to entry enables rapid prototyping and encourages experimentation. Authors can start with a single idea and let the AI expand it.

**Independent Test**: Create a campaign with only `manifest.json`, `world/setting.md`, and `plot/premise.md`. Verify the AI generates coherent NPCs, locations, and story progression without explicit definitions.

**Acceptance Scenarios**:

1. **Given** a campaign with only setting and premise, **When** gameplay begins, **Then** the AI invents appropriate NPCs based on setting tone.
2. **Given** no explicit rules system, **When** a mechanical challenge arises, **Then** the AI uses the default rules-light system (2d6 + modifiers).
3. **Given** no character constraints, **When** the player creates a character, **Then** the AI suggests options appropriate to the setting.

---

### User Story 3 - Author Creates a Detailed Campaign (Priority: P2)

A story author creates a comprehensive campaign with defined NPCs, plot beats, world constraints, and endings. The AI executes the campaign faithfully, hitting defined beats while maintaining player agency between them.

**Why this priority**: Enables professional-quality interactive fiction with intentional design. Authors who invest time get precise results.

**Independent Test**: Create a campaign with defined NPCs, plot beats, and endings. Verify the AI uses provided character voices, respects constraints, and guides toward defined endings.

**Acceptance Scenarios**:

1. **Given** defined NPC profiles, **When** that NPC speaks, **Then** dialogue matches their personality, motivations, and speech patterns.
2. **Given** defined plot beats, **When** story conditions align, **Then** the AI triggers the appropriate beat.
3. **Given** world constraints (e.g., "magic is rare"), **When** generating content, **Then** the AI respects those constraints.
4. **Given** defined endings, **When** player choices lead toward an ending, **Then** the AI subtly steers toward that conclusion.

---

### User Story 4 - Campaign with Pre-made Assets (Priority: P3)

A story author includes character portraits, scene backgrounds, and ambient music in their campaign. Narratoria displays these assets at appropriate moments during gameplay.

**Why this priority**: Rich media enhances immersion but is optional. Core narrative works without assets.

**Independent Test**: Create a campaign with `assets/` directory containing images and audio. Verify assets display when referenced by NPCs or scenes.

**Acceptance Scenarios**:

1. **Given** an NPC with a portrait file, **When** that NPC appears, **Then** their portrait is displayed.
2. **Given** a scene with a background image, **When** that scene loads, **Then** the background is displayed.
3. **Given** missing asset files referenced in content, **When** loading, **Then** the system gracefully degrades (no crash, optional warning).

---

### Edge Cases

- What happens when a campaign has circular NPC relationships (A → B → C → A)?
  - System should handle cycles gracefully and use them for narrative tension.
- How does the system handle conflicting constraints (e.g., "magic is common" in setting.md but "magic is forbidden" in constraints.md)?
  - Constraints take precedence; system warns author about conflicts.
- What happens when plot beats become unreachable due to player choices?
  - AI adapts by skipping unreachable beats and improvising toward available endings.
- How does the system handle very large campaigns (1000+ lore files)?
  - Lore is indexed incrementally; semantic search limits context window size.

---

## Requirements *(mandatory)*

### Functional Requirements

#### Campaign Structure

- **FR-001**: System MUST recognize a campaign as a directory containing a `manifest.json` file at root level.
- **FR-002**: System MUST support the following top-level directories: `world/`, `characters/`, `plot/`, `lore/`, `assets/`.
- **FR-003**: System MUST treat all directories and files except `manifest.json` as optional.
- **FR-004**: System MUST support Markdown (`.md`) files for prose content (setting, lore, constraints).
- **FR-005**: System MUST support JSON (`.json`) files for structured data (profiles, beats, manifest).
- **FR-006**: System MUST support common image formats (PNG, JPEG, WebP) in the `assets/` directory.

#### Manifest Schema

- **FR-007**: Manifest MUST include `title` (string) and `version` (semver string) fields.
- **FR-008**: Manifest SHOULD include `author`, `description`, `genre`, `tone`, and `content_rating` fields.
- **FR-009**: Manifest MAY include `rules_hint` to suggest a game mechanics style (e.g., "rules-light", "crunchy", "narrative").
- **FR-010**: Manifest MAY include `hydration_guidance` providing hints about how much the AI should invent.

#### World Definition

- **FR-011**: `world/setting.md` MUST describe the world, era, tone, and key environmental details.
- **FR-012**: `world/rules.md` MAY define custom game mechanics; if absent, default rules apply.
- **FR-013**: `world/constraints.md` MAY define absolute boundaries the AI must respect (e.g., "No resurrection", "Technology is medieval-era").

#### Character System

- **FR-014**: `characters/npcs/{name}/profile.json` MUST include `name`, `role`, and `personality` fields.
- **FR-015**: NPC profiles SHOULD include `motivations`, `relationships`, `speech_patterns`, and `secrets` fields.
- **FR-016**: `characters/npcs/{name}/portrait.png` MAY provide character artwork.
- **FR-017**: `characters/player/template.json` MAY define character creation constraints (allowed races, classes, backgrounds).

#### Plot Structure

- **FR-018**: `plot/premise.md` MUST describe the starting situation and initial hook.
- **FR-019**: `plot/beats.json` MAY define key story moments with conditions for triggering.
- **FR-020**: `plot/endings/` MAY contain multiple ending definitions as Markdown files.
- **FR-021**: Plot beats MUST include `id`, `description`, and MAY include `conditions` (trigger criteria) and `priority` fields.

#### Lore System

- **FR-022**: All files in `lore/` MUST be indexed for semantic search (RAG retrieval).
- **FR-023**: Lore files SHOULD be chunked into retrievable segments for context-window efficiency.
- **FR-024**: System MUST support nested directories within `lore/` for organizational flexibility.

#### Validation

- **FR-025**: System MUST validate campaign structure on load and report errors clearly.
- **FR-026**: System MUST validate JSON files against their schemas and report parsing errors with file paths and line numbers.
- **FR-027**: System SHOULD warn about orphaned asset references (files referenced but missing).

---

### Key Entities

- **Campaign**: A complete story package containing world, characters, plot, and lore. Identified by manifest.
- **Manifest**: Metadata describing the campaign (title, author, version, configuration hints).
- **NPC Profile**: Structured definition of a non-player character including personality, motivations, and relationships.
- **Plot Beat**: A defined story moment with optional trigger conditions and priority for the AI to work toward.
- **Constraint**: An absolute rule the AI must respect during generation (negative constraints like "no magic" or positive like "always medieval").
- **Lore Entry**: Background information indexed for semantic retrieval during play.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A minimal campaign (manifest + setting + premise) can be loaded and played within 5 seconds of selection.
- **SC-002**: Authors can create a playable campaign with fewer than 5 files.
- **SC-003**: Campaign validation errors identify the specific file and issue within 2 seconds of load attempt.
- **SC-004**: Lore retrieval returns relevant context for 90% of narrative queries (measured by author-defined test queries).
- **SC-005**: Defined NPC dialogue is recognizably consistent with profile personality in 95% of interactions (measured by author review).
- **SC-006**: Plot beats trigger within 2 scenes of their conditions being met (when conditions are satisfiable).
- **SC-007**: Campaigns up to 100MB load without memory issues on target hardware (8GB RAM device).

---

## Assumptions

- Authors have basic familiarity with file systems and can create directories and edit text files.
- Markdown is acceptable as a prose authoring format (widely supported, human-readable).
- JSON is acceptable for structured data (widely supported, tooling available).
- The AI memory system (Spec 008) will handle semantic indexing of lore content.
- The default rules-light system (2d6 + modifiers) is sufficient when no custom rules are provided.
- Campaign assets are pre-made by authors; no runtime AI image generation is performed.

---

## Dependencies

- **Spec 008: Narrative Engine** - Scene pipeline and memory system for executing campaigns.
- **Spec 006: Skill State Persistence** - For saving player progress within campaigns.
- **Spec 003: Skills Framework** - Skills that consume campaign data (narrator, choice generator, etc.).

---

## Out of Scope

- Runtime AI image generation (explicitly excluded per project research findings).
- Multiplayer campaigns (single-player focus for initial implementation).
- Campaign marketplace or distribution system.
- Campaign versioning or migration between format versions.
- DRM or copy protection for campaigns.
