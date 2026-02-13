# AGENT.md: Narratoria Development Guidelines

**Purpose**: This document establishes consistent rules for creating user journeys and related documentation for the Narratoria project. It ensures all documentation follows the same structure, naming conventions, and quality standards.

**Audience**: AI agents (Copilot) and developers contributing to Narratoria documentation.

**Status**: Living Document | Updated: February 2026

---

## 1. Naming Conventions

### 7.1 Files and Directories

```
docs/specifications/
  specification-001-ui-layer-implementation.md
  specification-002-campaign-selection.md
  specification-003-character-filtering.md
```

Naming pattern: `specification-NNN-[kebab-case-feature-name].md`

### 7.2 Code/State Names

**States** (from journey states.md):
- UPPER_SNAKE_CASE
- Example: `APP_LAUNCHING`, `CAMPAIGN_DETAIL`, `CHARACTER_SELECTION`

**Functions** (implementing steps):
- camelCase
- Verb-first: `loadCampaign()`, `selectCharacter()`, `acknowledgeWarning()`
- Example: `displayMainMenu()`, `filterCharactersByClass()`, `validateCampaignManifest()`

**Data Fields** (from campaign manifest / session state):
- snake_case
- Example: `allowed_classes`, `content_warnings`, `starting_stats`

**Error States** (from architecture):
- UPPER_CASE with underscore prefix: `ERROR_NETWORK_TIMEOUT`, `ERROR_MISSING_CAMPAIGN`

---

## 2. Current Journeys

The following journeys have been documented:

| Name | Directory | Status |
|------|-----------|--------|
| Game Startup and Main Menu | `user-journeys/game-startup-and-main-menu/` | Complete |

---

## 3. Future Journeys to Document

As the project evolves, document journeys for:

- Game Startup - Character Selection
- Game Startup - Content Warnings
- Gameplay - Scene Rendering and Choices
- Gameplay - Memory Retrieval (vector search)
- Gameplay - Choice Generation (plan execution)
- Save/Load Game
- Settings and Accessibility
- Campaign Management (creation, distribution)
- iOS/Android Platform-Specific (iCloud sync, scoped storage)

---

## References

- [User Journeys](../user-journeys/README.md) - Journey documentation standards
- [Architecture.md](../architecture.md) - System design and contracts
- [Agent Skills Standard](https://agentskills.io/specification) - Extended by Narratoria skills
