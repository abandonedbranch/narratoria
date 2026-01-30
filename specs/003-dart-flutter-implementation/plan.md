# Implementation Plan: Dart/Flutter Reference Implementation

**Spec**: [003-dart-flutter-implementation](spec.md)
**Parent Specs**: [001-tool-protocol-spec](../001-tool-protocol-spec/spec.md), [002-plan-generation-skills](../002-plan-generation-skills/spec.md)
**Status**: Draft

## Summary

This plan describes the implementation phases for Narratoria's Dart/Flutter reference implementation. The implementation is organized in two tracks:

1. **Protocol Track**: Implements Spec 001 (tool execution, event handling, UI components)
2. **Skills Track**: Implements Spec 002 (plan generation, skill discovery, replan loop)

See [tasks.md](tasks.md) for the complete task list with dependencies.

---

## Technology Stack

| Component | Technology | Notes |
|-----------|------------|-------|
| Language | Dart 3.x | Null safety, pattern matching |
| Framework | Flutter SDK | Latest stable |
| UI | Material Design 3 | Dark theme |
| State | Provider | ChangeNotifier pattern |
| AI | Flutter AI Toolkit + Ollama | Local LLM |
| Storage | SQLite (sqflite) | Skill data |
| Testing | flutter_test, integration_test | Contract + integration |

---

## Implementation Phases

### Protocol Track (Spec 001)

#### Phase P1: Setup
- Flutter project initialization
- Material Design 3 theme configuration
- Directory structure creation

#### Phase P2: Foundational
- Protocol event models
- Plan JSON models
- Asset and SessionState models
- Contract tests

#### Phase P3-P7: User Stories
- P3: Tool Execution Engine (MVP)
- P4: Player Input & Plan Generation
- P5: Asset Display
- P6: State Management
- P7: UI Events

#### Phase P8: Example Tools
- torch-lighter tool
- door-examiner tool

#### Phase P9: Polish
- Error display
- Loading indicators
- Documentation

### Skills Track (Spec 002)

#### Phase S1: Setup
- Dependencies update
- Skills directory structure

#### Phase S2: Foundational
- Extended Plan JSON models
- SkillDiscovery service
- SkillConfig loader
- NarratorAI interface
- PlanExecutor skeleton

#### Phase S3-S7: User Stories
- S3: Basic Storytelling (MVP)
- S4: Skill Configuration
- S5: Skill Discovery
- S6: Memory and Continuity
- S7: Reputation Tracking

#### Phase S8-S10: Resilience & Verification
- Data persistence
- Performance benchmarks
- Integration tests

---

## Dependencies

```
Protocol Track (P1-P9)
    │
    └── Skills Track (S1-S10)
         └── Complete Implementation
```

- Complete Protocol Phases P1-P4 before starting Skills Track
- Skills Track builds on Protocol Track foundation

---

## Milestones

| Milestone | Description | Phases |
|-----------|-------------|--------|
| **MVP 1** | Tool execution works | P1-P4, P8 |
| **MVP 2** | Skills work with narrator | S1-S3 |
| **Full** | All features complete | All phases |

---

## Verification

After each phase:
1. Run contract tests
2. Run integration tests
3. Manual verification against [checklists/implementation.md](checklists/implementation.md)

---

## Related Documents

- [spec.md](spec.md) - Feature specification
- [tasks.md](tasks.md) - Implementation tasks
- [data-model.md](data-model.md) - Dart class definitions
- [quickstart.md](quickstart.md) - Developer quickstart
- [checklists/implementation.md](checklists/implementation.md) - Verification checklist
