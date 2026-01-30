# Research: Dart/Flutter Implementation

**Spec**: [003-dart-flutter-implementation](spec.md)
**Status**: Reference

## Overview

This document consolidates research relevant to the Dart/Flutter implementation. For foundational research, see the parent specification research documents.

---

## Parent Specification Research

### Protocol Research (Spec 001)
- [001-tool-protocol-spec/research.md](../001-tool-protocol-spec/research.md)
- Topics: NDJSON protocol, event types, transport model

### Architecture Research (Spec 002)
- [002-plan-generation-skills/research.md](../002-plan-generation-skills/research.md)
- Topics: Agent Skills Standard, LLM integration, plan execution

---

## Dart/Flutter Specific Research

### Flutter AI Toolkit

**Package**: `flutter_ai_toolkit` on pub.dev

Key features:
- Local LLM integration via Ollama
- Streaming response handling
- Cross-platform support (desktop)

Integration pattern:
```dart
// Example: Local LLM with Ollama
final ai = FlutterAI.ollama(model: 'llama3.2:3b');
final response = await ai.generate(prompt);
```

### Material Design 3

**Documentation**: https://m3.material.io/

Key components used:
- NavigationRail for desktop navigation
- ColorScheme.fromSeed for theming
- Typography.material2021 for text styles

Theme configuration:
```dart
ThemeData(
  useMaterial3: true,
  colorScheme: ColorScheme.fromSeed(
    seedColor: Colors.deepPurple,
    brightness: Brightness.dark,
  ),
)
```

### State Management

**Package**: `provider`

Pattern: ChangeNotifier with Provider

Used for:
- SessionState management
- ToolExecutionStatus tracking
- Asset registry

### Process Execution

**API**: `dart:io` Process class

Pattern: Launch, write stdin, read stdout/stderr streams

```dart
final process = await Process.start('dart', ['script.dart']);
process.stdin.writeln(jsonEncode(input));
await process.stdin.close();

await for (final line in process.stdout.transform(utf8.decoder).transform(LineSplitter())) {
  // Parse NDJSON line
}
```

### SQLite Storage

**Package**: `sqflite` for cross-platform SQLite

Used by skills for:
- Memory embeddings (vector search)
- Reputation persistence
- Session data

---

## Algorithm References

### Topological Sort (Kahn's Algorithm)

Reference: Introduction to Algorithms, CLRS Chapter 22

Implementation in [data-model.md](data-model.md) §3.

### Deep Merge

Reference: JSON Merge Patch RFC 7396

Implementation in [data-model.md](data-model.md) §11.

### Exponential Backoff

Formula: `delay = baseDelay × 2^(attempt-1)`

Industry standard for retry logic (AWS, Google Cloud, etc.)

---

## External Standards

### Agent Skills Standard
- Specification: https://agentskills.io/specification
- Defines skill manifest structure (`skill.json`)
- Defines configuration schema format

### NDJSON
- Specification: http://ndjson.org/
- One JSON object per line
- Used for streaming tool output

### JSON Schema Draft-07
- Specification: https://json-schema.org/draft-07/
- Used for config-schema.json validation
- Used for Plan JSON and Execution Result schemas

---

## Performance Considerations

### LLM Latency
- Local models (3B params): ~2-5 seconds for plan generation
- Target: <5 seconds for typical input

### Process Spawn Overhead
- Dart process start: ~50-100ms
- Acceptable for tool invocation frequency

### UI Responsiveness
- Target: 60 fps during execution
- Use isolates/async for heavy computation
- Stream events for real-time UI updates
