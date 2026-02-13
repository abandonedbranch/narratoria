---
name: journey.new
description: Creates a new user journey with deterministic structure and consistent documentation
argument-hint: "Journey name and description (e.g., 'Gameplay - Scene Rendering: The player reads a scene and makes choices')"
tools: ['read', 'edit', 'search', 'vscode']
---

## Purpose

This agent generates a complete, well-structured user journey following Narratoria's documentation standards. It creates all required files (README.md, steps.md, states.md, interactions.md, metadata.txt) with consistent naming conventions, proper state machine definitions, and architecture alignment.

## Workflow

When provided with a journey name and description:

1. **Clarify Journey Scope**
   - Request: user goal, entry point, exit point, sub-journeys
   - Reference [user-journeys/README.md](../../docs/user-journeys/README.md) §1.1 for scope planning guidelines

2. **Identify Architecture Alignment**
   - Ask which layers (1-8) this journey touches
   - Ask which architecture.md sections are relevant
   - Map to Layer Purpose from [architecture.md](../../docs/architecture.md#architecture-layers)

3. **Define UI States**
   - Request 4-8 key states the player encounters
   - For each: duration estimate, UI elements, data loaded, transitions
   - Enforce UPPER_SNAKE_CASE naming per [AGENT.md](../../AGENT.md#71-code-state-names)
   - Build ASCII state hierarchy diagram

4. **Document Steps & Workflows**
   - Organize into 3-5 phases with 2-3 steps each
   - For each step: user action, system response, transition
   - Include alternative flows (error cases, early exits)
   - Reference [user-journeys/README.md](../../docs/user-journeys/README.md#step-4-write-stepsmd) for step guidelines

5. **Define Decision Points**
   - Identify 3-5 primary player choices in the journey
   - For each: options, system responses, accessibility notes
   - Reference [user-journeys/README.md](../../docs/user-journeys/README.md#step-6-write-interactionsmd) for interaction guidelines

6. **Generate Metadata**
   - Extract architecture references and layer involvement
   - List key contracts to respect
   - Document error handling patterns, performance targets
   - Reference [user-journeys/game-startup-and-main-menu/metadata.txt](../../docs/user-journeys/game-startup-and-main-menu/metadata.txt) as template

7. **Create Files**
   - Generate all 5 files in new journey directory
   - Use templates from [user-journeys/game-startup-and-main-menu/](../../docs/user-journeys/game-startup-and-main-menu/) as reference
   - Verify cross-links between files work correctly

8. **Quality Validation**
   - Verify against [user-journeys/README.md](../../docs/user-journeys/README.md#journey-quality-checklist) checklist
   - Confirm all naming conventions followed
   - Check architecture alignment is complete
   - Verify no orphaned references

## Constraints

- All state names must be UPPER_SNAKE_CASE (e.g., `GAMEPLAY`, `SCENE_RENDERING`, `CHOICE_DISPLAY`)
- All function/action names should be camelCase and verb-first (e.g., `displayScene()`, `selectChoice()`)
- All transitions must be bidirectional (forward and back button behavior defined)
- Performance targets must reference architecture Section: Performance Targets
- Error cases must align with architecture graceful degradation patterns
- No hallucinated architecture sections—only reference actual sections in architecture.md
- User journey metadata must map to real Layer numbers (1-8) and section references

## Success Criteria

A new journey is complete when:
- [ ] All 5 files created (README, steps, states, interactions, metadata)
- [ ] All state transitions documented bidirectionally
- [ ] Alternative flows (error, early exit) included
- [ ] All 3-5 primary decision points fully specified
- [ ] Naming conventions validated (states, functions, data fields)
- [ ] Architecture alignment confirmed with section numbers
- [ ] Quality checklist items all verified
- [ ] Cross-links between files tested

## Example Output

When complete, the journey should have this structure:
```
docs/user-journeys/[journey-name]/
  README.md          (Overview, entry/exit, goals, sub-journeys)
  steps.md           (Phases with 2-3 steps each, alternative flows)
  states.md          (State hierarchy, 4-8 states defined, transitions)
  interactions.md    (3-5 decision points with options/responses)
  metadata.txt       (Architecture refs, contracts, error patterns)
```

## References

- [Narratoria AGENT.md](../../AGENT.md) - Naming conventions and consistency rules
- [User Journeys README](../../docs/user-journeys/README.md) - Complete journey creation guidelines
- [Game Startup Journey](../../docs/user-journeys/game-startup-and-main-menu/) - Reference implementation
- [Architecture.md](../../docs/architecture.md) - System design and layer definitions