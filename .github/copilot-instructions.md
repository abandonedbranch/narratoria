# Narratoria - GitHub Copilot Instructions

## Project Overview

Narratoria is a cross-platform Dart+Flutter application for interactive, agent-driven storytelling. The architecture isolates the Dart runtime from external tools through a defined NDJSON protocol (Spec 001).

## Constitutional Principles (MANDATORY)

All code suggestions **MUST** comply with these principles from `.specify/memory/constitution.md`:

### I. Dart+Flutter First
- **ALL** Narratoria client logic MUST be idiomatic Dart using Flutter
- UI, state management, networking, agent orchestration → Dart only
- Clear separation of concerns and unit-testing best practices required
- No application logic may bypass Dart runtime except via tool protocol

### II. Protocol-Boundary Isolation  
- External tools run as **independent OS processes**
- Communication via **NDJSON on stdout/stdin** (see `specs/001-tool-protocol-spec/`)
- Tools can be any language (Rust, Go, Python, etc.) but MUST comply with Spec 001
- NEVER suggest loading untrusted code into the Dart process

### III. Single-Responsibility Tools
- Each tool performs **ONE** well-defined task
- Tools emit: `log`, `state_patch`, `asset`, `ui_event`, `error`, and exactly one `done` event
- No bundling of unrelated capabilities

### IV. Graceful Degradation
- Unknown media types, UI events, or capabilities MUST degrade gracefully
- Display placeholders for unsupported assets
- Log unsupported events without crashing
- Users MUST maintain narrative continuity even when optional features unavailable

### V. Testability and Composability
- All Dart modules MUST support unit testing in isolation
- Integration tests verify tool protocol via mock processes
- Acceptance tests validate end-to-end journeys without live services
- Suggest test code alongside implementation

## Technology Stack

| Component | Technology | Notes |
|-----------|------------|-------|
| Client Runtime | Dart 3.x + Flutter | Cross-platform (macOS, Windows, Linux) |
| Tool Protocol | NDJSON over stdin/stdout | See `specs/001-tool-protocol-spec/` |
| Tool Languages | Any | Must comply with Spec 001 |
| State Management | TBD (Provider, Riverpod, Bloc) | Must support unit testing |
| Testing | `flutter_test`, `integration_test` | Contract + integration + unit layers |

## Code Generation Guidelines

### When Generating Dart Code

1. **Follow Flutter/Dart conventions**:
   - Use `lowerCamelCase` for variables, methods, parameters
   - Use `UpperCamelCase` for classes, enums, typedefs, type parameters
   - Use `snake_case` only for library/file names
   - Prefer `final` over `var` when value won't change
   - Use `const` for compile-time constants

2. **Separation of concerns**:
   - Models in `lib/models/`
   - Services/business logic in `lib/services/`
   - UI widgets in `lib/ui/` or `lib/screens/`
   - Keep widgets small and focused

3. **Testability first**:
   - Inject dependencies (avoid static/global state)
   - Keep methods pure when possible
   - Suggest test file alongside implementation
   - Use interfaces/abstract classes for mockability

4. **Error handling**:
   - Use structured exceptions (extend `Exception` or `Error`)
   - Never silently catch; always log or rethrow
   - Provide meaningful error messages

### When Generating Tool Protocol Code

1. **Event emission** (stdout):
   ```dart
   // Example Dart tool emitting protocol events
   void emitLog(String level, String message) {
     final event = jsonEncode({
       'version': '0',
       'type': 'log',
       'level': level,
       'message': message,
     });
     stdout.writeln(event); // One JSON object per line
   }
   ```

2. **Always include**:
   - `version: "0"` in every event
   - Exactly ONE `done` event per invocation
   - Valid `type` field: `log`, `state_patch`, `asset`, `ui_event`, `error`, `done`

3. **Process exit codes**:
   - Exit 0: protocol intact (check `done.ok` for logical success)
   - Non-zero: protocol failure

### When Generating Tests

1. **Unit tests** (`test/` directory):
   - Test individual classes/functions in isolation
   - Mock external dependencies
   - Use `flutter_test` package
   - Name: `<filename>_test.dart`

2. **Integration tests** (`integration_test/` directory):
   - Test tool protocol interactions with mock processes
   - Verify event sequences
   - Test error handling and degradation

3. **Contract tests**:
   - Validate external tool outputs against Spec 001
   - Verify NDJSON format and event schemas
   - Use `contracts/` from spec directories

## Development Workflow

1. **Feature branches**: `###-feature-name` (e.g., `001-tool-protocol-spec`)
2. **Spec-first**: Feature starts with `specs/###-feature-name/spec.md`
3. **Plan-then-implement**: `specs/###-feature-name/plan.md` before coding
4. **Constitution check**: All code must pass constitutional compliance
5. **Test coverage**: Unit + contract + integration tests

## File Structure Conventions

```
lib/
├── models/           # Data models, entities
├── services/         # Business logic, state management
├── ui/              # Widgets, screens
│   ├── screens/
│   └── widgets/
├── utils/           # Utilities, helpers
└── main.dart        # App entry point

test/
├── unit/            # Unit tests
└── integration/     # Integration tests

integration_test/    # Flutter integration tests

specs/
└── ###-feature/     # Feature specifications
    ├── spec.md
    ├── plan.md
    ├── research.md
    ├── data-model.md
    └── contracts/
```

## Common Patterns to Suggest

### 1. Tool Invocation Service

```dart
class ToolInvoker {
  Future<ToolResult> invokeTool(String toolPath, Map<String, dynamic> input) async {
    final process = await Process.start(toolPath, []);
    
    // Send input via stdin
    process.stdin.writeln(jsonEncode(input));
    await process.stdin.close();
    
    // Parse NDJSON events from stdout
    final events = <Map<String, dynamic>>[];
    await for (final line in process.stdout.transform(utf8.decoder).transform(LineSplitter())) {
      events.add(jsonDecode(line));
    }
    
    final exitCode = await process.exitCode;
    return ToolResult(events: events, exitCode: exitCode);
  }
}
```

### 2. Event Parser

```dart
sealed class ToolEvent {
  factory ToolEvent.fromJson(Map<String, dynamic> json) {
    final type = json['type'] as String;
    return switch (type) {
      'log' => LogEvent.fromJson(json),
      'state_patch' => StatePatchEvent.fromJson(json),
      'asset' => AssetEvent.fromJson(json),
      'ui_event' => UiEvent.fromJson(json),
      'error' => ErrorEvent.fromJson(json),
      'done' => DoneEvent.fromJson(json),
      _ => throw UnknownEventTypeError(type),
    };
  }
}
```

### 3. Graceful Degradation

```dart
Widget buildAssetPreview(Asset asset) {
  return switch (asset.kind) {
    'image' => Image.file(File(asset.path)),
    'audio' => AudioPlayerWidget(asset.path),
    'video' => VideoPlayerWidget(asset.path),
    _ => PlaceholderWidget(
        message: 'Unsupported asset type: ${asset.kind}',
        asset: asset,
      ),
  };
}
```

## What NOT to Suggest

❌ **DO NOT**:
- Generate code that loads external libraries into Dart process directly
- Suggest breaking tool protocol boundaries
- Create multi-responsibility tools
- Hard-fail on missing optional features
- Generate code without tests
- Violate Dart naming conventions
- Use global mutable state
- Suggest `dynamic` when types are knowable
- Skip error handling

## Quick Reference

- **Constitution**: `.specify/memory/constitution.md`
- **Tool Protocol Spec**: `specs/001-tool-protocol-spec/spec.md`
- **Current Feature**: Check branch name or `specs/` directory
- **Templates**: `.specify/templates/`

## When Uncertain

If constitutional compliance is unclear:
1. Check `.specify/memory/constitution.md` for principles
2. Check `specs/001-tool-protocol-spec/` for protocol details
3. Favor testability, isolation, and graceful degradation
4. Ask for clarification rather than guess
