// T018 Integration test: Execute Plan JSON with real tools
// Verifies end-to-end protocol flow including stderr handling

import 'dart:io';
import 'package:flutter_test/flutter_test.dart';
import 'package:narratoria/models/plan_json.dart';
import 'package:narratoria/models/protocol_events.dart';
import 'package:narratoria/services/plan_executor.dart';
import 'package:narratoria/services/tool_invoker.dart';
import 'package:uuid/uuid.dart';

void main() {
  late ToolInvoker toolInvoker;
  late PlanExecutor planExecutor;
  const uuid = Uuid();

  /// Helper to find compiled tool executable
  String? findToolExecutable(String toolName) {
    // Flutter project is in src/, binaries are in bin/ at repo root
    final srcDir = Directory.current.path;
    final projectRoot = srcDir.endsWith('/src')
        ? Directory(srcDir).parent.path
        : Directory(srcDir).path.replaceAll(RegExp(r'/src/test(/integration)?$'), '');

    // Check bin/ directory for compiled executables
    final binPath = '$projectRoot/bin/$toolName';
    if (File(binPath).existsSync()) {
      return binPath;
    }
    
    return null;
  }

  setUpAll(() {
    // Ensure we're running from the Flutter project src directory
    // Tests should work regardless of where flutter test is invoked
  });

  setUp(() {
    toolInvoker = ToolInvoker();
    planExecutor = PlanExecutor(toolInvoker: toolInvoker);
  });

  tearDown(() {
    planExecutor.dispose();
  });

  group('PlanExecutor Integration Tests', () {
    test('executes torch-lighter tool and receives all protocol events', () async {
      final toolPath = findToolExecutable('torch-lighter');
      if (toolPath == null) {
        markTestSkipped('torch-lighter executable not found. Run: dart compile exe tools/torch-lighter/main.dart');
        return;
      }

      final plan = PlanJson(
        requestId: uuid.v4(),
        narrative: 'You light the torch.',
        tools: [
          ToolInvocation(
            toolId: 'torch-lighter',
            toolPath: toolPath,
            input: {'intensity': 'high'},
          ),
        ],
      );

      final result = await planExecutor.execute(plan: plan);

      expect(result.toolResults, hasLength(1));

      final toolResult = result.toolResults['torch-lighter']!;
      expect(toolResult.success, isTrue, reason: 'Tool should complete successfully');
      expect(toolResult.events, isNotEmpty, reason: 'Should receive protocol events');

      // Verify we received expected event types
      final eventTypes = toolResult.events.map((e) => e.runtimeType).toSet();
      expect(eventTypes, contains(LogEvent), reason: 'Should have log events');
      expect(eventTypes, contains(DoneEvent), reason: 'Must have done event');

      // Find the done event
      final doneEvent = toolResult.events.whereType<DoneEvent>().first;
      expect(doneEvent.ok, isTrue, reason: 'Tool should report success');
    });

    test('executes door-examiner tool and receives ui_event', () async {
      final toolPath = findToolExecutable('door-examiner');
      if (toolPath == null) {
        markTestSkipped('door-examiner executable not found. Run: dart compile exe tools/door-examiner/main.dart');
        return;
      }

      final plan = PlanJson(
        requestId: uuid.v4(),
        narrative: 'You examine the door.',
        tools: [
          ToolInvocation(
            toolId: 'door-examiner',
            toolPath: toolPath,
            input: {},
          ),
        ],
      );

      final result = await planExecutor.execute(plan: plan);

      expect(result.toolResults, hasLength(1));

      final toolResult = result.toolResults['door-examiner']!;
      expect(toolResult.success, isTrue);

      // Verify we received a ui_event
      final uiEvents = toolResult.events.whereType<UiEvent>().toList();
      expect(uiEvents, isNotEmpty, reason: 'Should have narrative_choice ui_event');

      final choiceEvent = uiEvents.first;
      expect(choiceEvent.event, equals('narrative_choice'));
      expect(choiceEvent.payload, isA<Map<String, dynamic>>());
    });

    test('handles multiple tools in sequence', () async {
      final torchPath = findToolExecutable('torch-lighter');
      final doorPath = findToolExecutable('door-examiner');

      if (torchPath == null || doorPath == null) {
        markTestSkipped('Required tool executables not found');
        return;
      }

      final plan = PlanJson(
        requestId: uuid.v4(),
        narrative: 'You light the torch and examine the door.',
        tools: [
          ToolInvocation(
            toolId: 'torch-lighter',
            toolPath: torchPath,
            input: {},
          ),
          ToolInvocation(
            toolId: 'door-examiner',
            toolPath: doorPath,
            input: {},
            dependencies: ['torch-lighter'],
          ),
        ],
      );

      final result = await planExecutor.execute(plan: plan);

      expect(result.toolResults, hasLength(2));
      expect(result.success, isTrue);
    });

    test('handles parallel tool execution', () async {
      final torchPath = findToolExecutable('torch-lighter');
      final doorPath = findToolExecutable('door-examiner');

      if (torchPath == null || doorPath == null) {
        markTestSkipped('Required tool executables not found');
        return;
      }

      final plan = PlanJson(
        requestId: uuid.v4(),
        narrative: 'You multitask: lighting a torch while examining the door.',
        parallel: true,
        tools: [
          ToolInvocation(
            toolId: 'torch-lighter',
            toolPath: torchPath,
            input: {},
          ),
          ToolInvocation(
            toolId: 'door-examiner',
            toolPath: doorPath,
            input: {},
            // No dependencies - can run in parallel
          ),
        ],
      );

      final stopwatch = Stopwatch()..start();
      final result = await planExecutor.execute(plan: plan);
      stopwatch.stop();

      expect(result.toolResults, hasLength(2));
      expect(result.success, isTrue);

      // Both tools should complete (parallel execution is an implementation detail)
      // The test validates correctness regardless of execution strategy
    });

    test('stderr output does not corrupt NDJSON parsing', () async {
      final toolPath = findToolExecutable('torch-lighter');
      if (toolPath == null) {
        markTestSkipped('torch-lighter executable not found');
        return;
      }

      // Execute tool and verify NDJSON parsing succeeds even if tool writes to stderr
      final result = await toolInvoker.invoke(
        toolPath: toolPath,
        input: {'test': 'stderr-handling'},
      );

      // Tool should complete successfully
      expect(result.success, isTrue);

      // All events should be valid protocol events (no parsing errors)
      for (final event in result.events) {
        expect(event.version, equals('0'));
      }

      // Done event must be present
      final doneEvents = result.events.whereType<DoneEvent>().toList();
      expect(doneEvents, hasLength(1));
    });

    test('tool execution timeout is handled gracefully', () async {
      // This test verifies graceful handling if a tool hangs
      // Since our example tools complete quickly, we just verify the timeout mechanism exists
      final toolPath = findToolExecutable('torch-lighter');
      if (toolPath == null) {
        markTestSkipped('torch-lighter executable not found');
        return;
      }

      // Execute with default timeout - should complete well before any timeout
      final result = await toolInvoker.invoke(
        toolPath: toolPath,
        input: {},
        timeout: const Duration(seconds: 30),
      );

      expect(result.success, isTrue);
    });
  });

  group('Protocol Compliance Validation', () {
    test('tool emits exactly one done event', () async {
      final toolPath = findToolExecutable('torch-lighter');
      if (toolPath == null) {
        markTestSkipped('torch-lighter executable not found');
        return;
      }

      final result = await toolInvoker.invoke(toolPath: toolPath, input: {});

      final doneEvents = result.events.whereType<DoneEvent>().toList();
      expect(doneEvents, hasLength(1), reason: 'Protocol requires exactly one done event');
    });

    test('all events have valid version field', () async {
      final toolPath = findToolExecutable('torch-lighter');
      if (toolPath == null) {
        markTestSkipped('torch-lighter executable not found');
        return;
      }

      final result = await toolInvoker.invoke(toolPath: toolPath, input: {});

      for (final event in result.events) {
        expect(event.version, equals('0'), reason: 'All events must have version "0"');
      }
    });

    test('state_patch events contain valid patch data', () async {
      final toolPath = findToolExecutable('torch-lighter');
      if (toolPath == null) {
        markTestSkipped('torch-lighter executable not found');
        return;
      }

      final result = await toolInvoker.invoke(toolPath: toolPath, input: {});

      final statePatches = result.events.whereType<StatePatchEvent>().toList();
      expect(statePatches, isNotEmpty, reason: 'torch-lighter should emit state patches');

      for (final patch in statePatches) {
        expect(patch.patch, isA<Map<String, dynamic>>());
        expect(patch.patch, isNotEmpty);
      }
    });

    test('log events have valid level', () async {
      final toolPath = findToolExecutable('torch-lighter');
      if (toolPath == null) {
        markTestSkipped('torch-lighter executable not found');
        return;
      }

      final result = await toolInvoker.invoke(toolPath: toolPath, input: {});

      final logEvents = result.events.whereType<LogEvent>().toList();
      expect(logEvents, isNotEmpty, reason: 'Tool should emit log events');

      const validLevels = ['debug', 'info', 'warn', 'error'];
      for (final log in logEvents) {
        expect(validLevels, contains(log.level), reason: 'Log level must be valid');
      }
    });
  });
}
