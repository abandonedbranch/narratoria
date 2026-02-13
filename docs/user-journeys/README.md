# User Journeys

This directory documents the user journeys within Narratoria - the specific sequences of actions and interactions that users perform when using the application.

Unlike the [architecture documentation](../architecture.md) which describes the system's technical design, user journeys capture the **WHAT** - what users actually do, see, and experience.

## Journey Structure

Each journey has its own directory with a semantic name that describes the core user action or goal:

- `game-startup-and-main-menu/` - Starting the game and navigating the main menu
- *Additional journeys to be added*

Each journey directory contains:
- `README.md` - Overview and flow description of the journey
- `steps.md` - Detailed step-by-step walkthrough
- `states.md` - Key UI/application states involved
- `interactions.md` - Notable interactions and decision points
- `metadata.txt` - Architecture alignment, contracts, and consistency requirements (used by SpecKit)
- Supporting files as needed (diagrams, screenshots, etc.)

## Creating a New User Journey

Follow this process to create a new journey with consistent structure:

### Step 1: Plan the Journey Scope

Before writing documentation, define:
- **User Goal**: What is the user trying to accomplish? (e.g., "Start a new game")
- **Entry Point**: What action initiates this journey? (e.g., "User taps app icon")
- **Exit Point**: What marks journey completion? (e.g., "First scene displays")
- **Sub-journeys**: What decision branches exist? (e.g., "Campaign selection", "Character selection")
- **Related Architecture Sections**: Which sections of architecture.md does this involve? (e.g., Layer 5, Section 6.2)

Document these in a planning note (internal, not published).

### Step 2: Create the Journey Directory

Create a new directory in `docs/user-journeys/` with a kebab-case semantic name:

```
docs/user-journeys/[journey-name]/
  README.md
  steps.md
  states.md
  interactions.md
  metadata.txt
```

Example: `docs/user-journeys/gameplay-scene-rendering/`

### Step 3: Write README.md

Structure:
```markdown
# [Journey Name] Journey

## Overview
[2-3 sentence description of what player does]

## Entry Point
[What action starts this journey]

## Exit Point
[What marks journey completion]

## Goals
- Specific goal 1
- Specific goal 2
- ...

## Sub-journeys
1. [Sub-journey 1] - Brief description
2. [Sub-journey 2] - Brief description

## Related Files
- [steps.md](steps.md) - Detailed step-by-step breakdown
- [states.md](states.md) - UI and application states
- [interactions.md](interactions.md) - Key interactions and decision points
```

**Reference Example**: [game-startup-and-main-menu/README.md](game-startup-and-main-menu/README.md)

### Step 4: Write steps.md

Document the journey in phases and steps:

```markdown
# [Journey Name] - Steps

## Phases

### Phase 1: [Phase Name]

**Step 1.1: [User action]**
- Action description
- System response
- Next step

**Step 1.2: [User action]**
- ...

### Phase 2: [Phase Name]
- ...

### Alternative Flow: [Flow name]
**Step A.1: ...**
```

**Guidelines**:
- Organize into logical phases (usually 3-5)
- Each step includes user action, system response, transition
- Include duration estimates where relevant
- Reference [architecture.md](../architecture.md) sections when describing system details (e.g., "See Section 6.3 for campaign discovery")
- Document alternative flows (Load, Settings, Error cases) similarly
- Include timing expectations in parentheses (e.g., "~2 seconds", "2-30 seconds")

**Reference Example**: [game-startup-and-main-menu/steps.md](game-startup-and-main-menu/steps.md)

### Step 5: Write states.md

Define each UI state the player encounters:

```markdown
# [Journey Name] - States

## State Hierarchy

[ASCII diagram showing state transitions, similar to reference]

---

## State Definitions

### STATE_NAME
**Duration**: [time estimate]
**UI Elements**:
- [Element 1]
- [Element 2]

**Data Loaded**:
- [Data item 1]
- [Data item 2]

**Transitions**:
- → `NEXT_STATE` (condition)
- ← `PREVIOUS_STATE` (condition)

**Notes**:
- [Implementation notes]
```

**Guidelines**:
- Use UPPER_SNAKE_CASE for state names (example: `GAMEPLAY`, `CHARACTER_SELECTION`)
- Include all 11 required fields for each state: name, duration, UI elements, data loaded, transitions, notes
- Transitions must be bidirectional (forward and back)
- Include error states (e.g., if data loading fails, where does player go?)
- Duration estimates help with performance planning

**Reference Example**: [game-startup-and-main-menu/states.md](game-startup-and-main-menu/states.md)

### Step 6: Write interactions.md

Document key decision points and user choices:

```markdown
# [Journey Name] - Interactions

## Primary Decision Points

### Decision 1: [Decision Name]
**User Choice**: [What is the user deciding?]

**Options**:
1. `Option 1` → [Outcome]
2. `Option 2` → [Outcome]

**System Response**:
- [Response to option 1]
- [Response to option 2]

**Accessibility Notes**:
- [Touch target size, keyboard nav, color contrast, etc.]

### Decision 2: [Decision Name]
- ...

## Secondary Interactions

### Scrolling/Navigation
- ...

### Analytics Points
- [What should be tracked for user behavior analysis]
```

**Guidelines**:
- Identify 3-5 primary decision points (major player choices)
- Each decision has options, system response, accessibility requirements
- Include secondary interactions (scrolling, long-press, settings access)
- Document analytics tracking points (useful for learning about player behavior)
- Reference accessibility standards (WCAG 2.1, platform-specific)

**Reference Example**: [game-startup-and-main-menu/interactions.md](game-startup-and-main-menu/interactions.md)

### Step 7: Write metadata.txt

Generate metadata needed for SpecKit to create implementation specifications:

```
Journey: [Journey Name]
====================

Architecture References
- Layer X: [Layer Purpose]
- Section Y.Z: [Section Topic]

Involved Layers
- Layer X

Key Contracts from Architecture
- [Contract 1]

Consistency Requirements
- [Naming convention]
- [Pattern to follow]

Error Handling Patterns
- [Error type]: [Recovery behavior]

Performance Targets
- [Metric]: [Target]

Data Shapes
- [Type]: [Structure]

Failure Modes
- [Failure]: [Fallback]

UI Pattern References
- [Pattern name]: [Description]

Testing Checkpoints
- [Checkpoint]

Cross-References for Implementation
- [Related specification]
```

**Guidelines**:
- Identify which architecture layers this journey touches (ref: architecture.md Section 1.4)
- List specific architecture sections to reference in generated specs
- Document any new UI patterns introduced
- Include error cases and how system recovers
- Specify performance targets from architecture (or propose if new)
- This file is **input to SpecKit** for generating implementation specifications

**Reference Example**: [game-startup-and-main-menu/metadata.txt](game-startup-and-main-menu/metadata.txt)

---

## Journey Quality Checklist

Before publishing a new journey, verify:

- [ ] **Completeness**
  - [ ] Entry and exit points clearly defined (README)
  - [ ] All phases have 2+ steps with user actions and system responses (steps.md)
  - [ ] All UI states have transitions defined in both directions (states.md)
  - [ ] All major player decisions documented (interactions.md)

- [ ] **Consistency**
  - [ ] States use UPPER_SNAKE_CASE naming
  - [ ] Back button behavior documented for all states
  - [ ] Alternative flows (error paths, save/exit) documented
  - [ ] Duration estimates for all states and long-running operations

- [ ] **Architecture Alignment**
  - [ ] metadata.txt identifies relevant architecture sections
  - [ ] All system descriptions reference architecture (e.g., "per Section 6.3")
  - [ ] Data shapes match architecture contracts (e.g., campaign manifest structure)
  - [ ] Error handling aligns with graceful degradation patterns

- [ ] **Traceability**
  - [ ] Each step in steps.md maps to one or more states in states.md
  - [ ] Each decision in interactions.md leads to documented state transition
  - [ ] Every architecture reference includes section number
  - [ ] Cross-links between files work correctly

- [ ] **Accessibility & Testing**
  - [ ] interactions.md includes accessibility notes for all decision points
  - [ ] metadata.txt lists testing checkpoints
  - [ ] Performance targets specified
  - [ ] Error cases documented (incomplete data, network failures, etc.)

---

## Journey Categories

As we expand, journeys will be organized by category:
- **Game Initialization** - Starting the game, loading saves, initial setup
- **Gameplay** - In-game interactions and progression
- **Campaign Management** - Creating and managing campaigns
- **Character Interactions** - Character creation, dialogue, relationships
- **World/Story Navigation** - Exploring the world and narrative
- **Settings & Configuration** - Game settings and preferences

## Next Steps

After creating a journey, use SpecKit to generate an implementation specification:

```bash
speckit generate \
  --journey docs/user-journeys/[journey-name]/ \
  --architecture docs/architecture.md \
  --output docs/specifications/specification-NNN-[feature-name].md
```

See [AGENT.md](../../AGENT.md) for specification generation guidelines.

