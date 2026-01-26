# Quickstart: Implementing Spec 001

## For Tool Authors: Implementing Protocol-Compliant Tools

### 1) Emit NDJSON to stdout
- Write one JSON object per line (UTF-8, Unix newlines).
- Include `version: "0"` and a valid `type` in every event.
- Do not buffer the whole payload; flush after each line for streaming responsiveness.

## 2) Minimal tool skeleton (pseudocode)

```
print({"version": "0", "type": "log", "level": "info", "message": "Starting"})
# ...do work, emit state_patch/asset/ui_event as needed...
print({"version": "0", "type": "done", "ok": true, "summary": "Completed."})
exit(0)
```

## 3) Event types to use
- `log`: progress or diagnostics; never treated as failure.
- `state_patch`: JSON object merged into Narratoria session state.
- `asset`: register a generated file with `assetId`, `kind`, `mediaType`, `path`.
- `ui_event`: request UI actions by `event` name with optional `payload`.
- `error`: structured error, followed by a `done` event with `ok: false`.
- `done`: exactly one per invocation; set `ok` true/false, include optional `summary`.

## 4) Process exit rules
- Exit code 0 means protocol intact; `done.ok` reports logical success/failure.
- Non-zero exit code means protocol failure even if `done` was emitted.

## 5) Validation tips
- Use a JSON/NDJSON linter to ensure each line is valid and complete.
- Ensure `state_patch.patch` is an object, not an array or primitive.
- Keep `assetId` unique per invocation and provide valid MIME strings.

## 6) Forward compatibility
- Accept and ignore unknown fields from Narratoria input.
- Emit only defined `type` values; keep `version` at "0" until Spec 002.

---

## For Client Developers: Building the Narratoria Flutter App

### 1) Setup Flutter project

```bash
flutter create narratoria
cd narratoria
```

Update `pubspec.yaml`:

```yaml
dependencies:
  flutter:
    sdk: flutter

dev_dependencies:
  flutter_test:
    sdk: flutter
  integration_test:
    sdk: flutter
```

### 2) Implement ToolInvoker service

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
      final event = jsonDecode(line);
      events.add(event);
      // Optionally: dispatch event immediately to UI via stream
    }
    
    final exitCode = await process.exitCode;
    return ToolResult(events: events, exitCode: exitCode);
  }
}
```

### 3) Implement PlanExecutor service

```dart
class PlanExecutor {
  final ToolInvoker toolInvoker;
  
  Future<void> executePlan(PlanJson plan) async {
    final completed = <String>{};
    
    if (plan.parallel) {
      // Execute tools with no dependencies in parallel
      await _executeParallel(plan.tools, completed);
    } else {
      // Execute sequentially
      for (final tool in plan.tools) {
        await _executeTool(tool, completed);
      }
    }
  }
  
  Future<void> _executeTool(ToolInvocation tool, Set<String> completed) async {
    // Check dependencies
    for (final dep in tool.dependencies) {
      if (!completed.contains(dep)) {
        throw Exception('Dependency $dep not completed for ${tool.toolId}');
      }
    }
    
    final result = await toolInvoker.invokeTool(tool.toolPath, tool.input);
    
    // Check for success
    final doneEvent = result.events.lastWhere((e) => e['type'] == 'done');
    if (doneEvent['ok'] == true && result.exitCode == 0) {
      completed.add(tool.toolId);
    } else {
      throw Exception('Tool ${tool.toolId} failed');
    }
  }
}
```

### 4) Build Material Design 3 UI

```dart
MaterialApp(
  theme: ThemeData(
    useMaterial3: true,
    colorScheme: ColorScheme.fromSeed(
      seedColor: Colors.deepPurple,
      brightness: Brightness.dark,
    ),
  ),
  home: Scaffold(
    body: Row(
      children: [
        NavigationRail(
          destinations: [
            NavigationRailDestination(icon: Icon(Icons.book), label: Text('Narrative')),
            NavigationRailDestination(icon: Icon(Icons.build), label: Text('Tools')),
            NavigationRailDestination(icon: Icon(Icons.image), label: Text('Assets')),
            NavigationRailDestination(icon: Icon(Icons.data_object), label: Text('State')),
          ],
          selectedIndex: 0,
        ),
        Expanded(
          child: MainContentArea(), // Your story view, player input, tool panel
        ),
      ],
    ),
  ),
)
```

### 5) Implement graceful degradation

```dart
Widget buildAssetPreview(Asset asset) {
  return switch (asset.kind) {
    'image' => Image.file(File(asset.path)),
    'audio' => AudioPlayerWidget(asset.path),
    'video' => VideoPlayerWidget(asset.path),
    _ => Card(
        child: Column(
          children: [
            Icon(Icons.help_outline, size: 48),
            Text('Unsupported: ${asset.kind}'),
            Text('MIME: ${asset.mediaType}'),
            TextButton(
              onPressed: () => showAssetDetails(asset),
              child: Text('View Details'),
            ),
          ],
        ),
      ),
  };
}
```

### 6) Create example tools

Place in `tools/torch-lighter/main.dart`:

```dart
import 'dart:convert';
import 'dart:io';

void main() {
  final event1 = jsonEncode({'version': '0', 'type': 'log', 'level': 'info', 'message': 'Lighting torch...'});
  stdout.writeln(event1);
  
  final event2 = jsonEncode({
    'version': '0',
    'type': 'state_patch',
    'patch': {'inventory': {'torch': {'lit': true}}}
  });
  stdout.writeln(event2);
  
  final event3 = jsonEncode({
    'version': '0',
    'type': 'asset',
    'assetId': 'torch-01',
    'kind': 'image',
    'mediaType': 'image/png',
    'path': '/tmp/torch_lit.png',
    'metadata': {'width': 512, 'height': 512}
  });
  stdout.writeln(event3);
  
  final done = jsonEncode({'version': '0', 'type': 'done', 'ok': true, 'summary': 'Torch lit.'});
  stdout.writeln(done);
  
  exit(0);
}
```

Compile: `dart compile exe tools/torch-lighter/main.dart -o tools/torch-lighter`

### 7) Test the integration

Write integration test in `integration_test/plan_executor_test.dart`:

```dart
testWidgets('Execute simple plan with torch-lighter', (tester) async {
  final plan = PlanJson(
    requestId: 'test-001',
    narrative: 'You light the torch.',
    tools: [
      ToolInvocation(
        toolId: 'light1',
        toolPath: './tools/torch-lighter',
        input: {'action': 'light_torch'},
        dependencies: [],
      ),
    ],
    parallel: false,
  );
  
  final executor = PlanExecutor(ToolInvoker());
  await executor.executePlan(plan);
  
  // Verify: asset registered, state updated
  expect(assetRegistry.contains('torch-01'), true);
  expect(sessionState['inventory']['torch']['lit'], true);
});
```
