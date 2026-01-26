# Implementation Plan: Tool Protocol Spec 001

**Branch**: 001-tool-protocol-spec | **Date**: 2026-01-24 | **Spec**: [specs/001-tool-protocol-spec/spec.md](specs/001-tool-protocol-spec/spec.md)
**Input**: Feature specification from [specs/001-tool-protocol-spec/spec.md](specs/001-tool-protocol-spec/spec.md)

## Summary

Document Tool Protocol Spec 001 and define Narratoria client MVP requirements. The protocol enables external tools to communicate with Narratoria via NDJSON events (`log`, `state_patch`, `asset`, `ui_event`, `error`, `done`) over stdin/stdout. The client uses Material Design 3 with Provider state management to present a player input field (natural language prompts), tool execution panel, asset gallery, and narrative state view. Player prompts are converted to Plan JSON (via Narrator AI Stub) which drives sequential or parallel tool execution. Deliverable includes protocol documentation, Plan JSON schema, UI component specifications, and two example tools (torch-lighter, door-examiner) for MVP validation.

## Technical Context

**Language/Version**: Dart 3.x + Flutter (client implementation); tools can be any language  
**Primary Dependencies**: Flutter SDK, Material Design 3 widgets, `flutter_test`, `integration_test`, `provider` package (state management)  
**Storage**: Session state in memory (persisted JSON files optional)  
**Testing**: `flutter_test` for UI and unit tests; contract tests for tool protocol validation; integration tests for Plan JSON execution  
**Target Platform**: Narratoria runtime on macOS/Windows/Linux desktop; tools as platform-native executables  
**Project Type**: Single Flutter desktop app with external tool executables  
**State Management**: Provider with ChangeNotifier pattern (rationale: first-class Flutter support, excellent testability via flutter_test, mature ecosystem, suitable for MVP scope with 4 UI panels requiring real-time updates)  
**Performance Goals**: <100ms UI response to protocol events; stream NDJSON events incrementally; support multiple concurrent tools  
**Constraints**: UTF-8 NDJSON only; process isolation mandatory; exactly one `done` event per tool; graceful degradation for unknown asset types  
**Scale/Scope**: Protocol spec covering 6 event types; client MVP with 4 UI panels; 2 example tools; Plan JSON execution engine

## Constitution Check

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Dart+Flutter First | ✅ Pass | All client UI in idiomatic Flutter using Material Design 3 |
| II. Protocol-Boundary Isolation | ✅ Pass | Tools run as independent processes communicating via NDJSON stdin/stdout |
| III. Single-Responsibility Tools | ✅ Pass | Example tools (torch-lighter, door-examiner) each perform one task |
| IV. Graceful Degradation | ✅ Pass | Asset Gallery displays placeholders for unsupported mediaTypes |
| V. Testability and Composability | ✅ Pass | All UI components testable via flutter_test; contract tests validate protocol; Plan JSON execution engine testable with mocks |

## Project Structure

### Documentation (this feature)

```text
specs/001-tool-protocol-spec/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
└── contracts/
    └── tool-protocol.openapi.yaml
```

### Source Code (repository root)

```text
lib/
├── models/              # Data models
│   ├── protocol_events.dart      # EventEnvelope, LogEvent, etc.
│   ├── plan_json.dart             # PlanJson, ToolInvocation
│   ├── asset.dart                 # Asset model
│   └── session_state.dart         # SessionState model
├── services/            # Business logic
│   ├── tool_invoker.dart          # Process launch and NDJSON parsing
│   ├── plan_executor.dart         # Plan JSON execution engine
│   ├── narrator_ai_stub.dart      # Mock narrator AI (Plan JSON generator)
│   └── state_manager.dart         # Session state management
├── ui/                  # Material Design 3 UI components
│   ├── screens/
│   │   └── main_screen.dart       # NavigationRail + content area
│   ├── widgets/
│   │   ├── tool_execution_panel.dart
│   │   ├── asset_gallery.dart
│   │   ├── narrative_state_panel.dart
│   │   ├── player_input_field.dart
│   │   └── story_view.dart
│   └── theme.dart                 # Material Design 3 theme
├── utils/               # Helpers
│   └── json_helpers.dart
└── main.dart            # App entry point

test/
├── contract/            # Protocol contract tests
│   └── tool_protocol_test.dart
├── integration/         # Plan execution tests
│   └── plan_executor_test.dart
└── unit/                # Unit tests
    ├── models_test.dart
    └── services_test.dart

tools/                   # Example tool executables
├── torch-lighter/
│   └── main.dart (or any language)
└── door-examiner/
    └── main.dart (or any language)
```

**Structure Decision**: Single Flutter desktop application with example tools as separate executables.

## Complexity Tracking

None.
