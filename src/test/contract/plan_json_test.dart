// Contract Tests for Plan JSON
// T011: Validates Plan JSON schema per Spec 001 §13.3

import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:narratoria/models/plan_json.dart';

void main() {
  group('PlanJson Contract (§13.3)', () {
    test('MUST have requestId field', () {
      const json = '''
        {"requestId": "req-001", "tools": []}
      ''';
      final plan = PlanJson.fromJson(jsonDecode(json));
      expect(plan.requestId, equals('req-001'));
    });

    test('MUST have tools array', () {
      const json = '''
        {"requestId": "req-002", "tools": [
          {"toolId": "tool-1", "toolPath": "/path/to/tool", "input": {}}
        ]}
      ''';
      final plan = PlanJson.fromJson(jsonDecode(json));
      expect(plan.tools, hasLength(1));
    });

    test('MAY include optional narrative field', () {
      const json = '''
        {"requestId": "req-003", "narrative": "A torch flickers to life...", "tools": []}
      ''';
      final plan = PlanJson.fromJson(jsonDecode(json));
      expect(plan.narrative, equals('A torch flickers to life...'));
    });

    test('MAY include optional parallel field', () {
      const json = '''
        {"requestId": "req-004", "tools": [], "parallel": true}
      ''';
      final plan = PlanJson.fromJson(jsonDecode(json));
      expect(plan.parallel, isTrue);
    });

    test('parallel defaults to false', () {
      const json = '''
        {"requestId": "req-005", "tools": []}
      ''';
      final plan = PlanJson.fromJson(jsonDecode(json));
      expect(plan.parallel, isFalse);
    });

    test('serializes to correct JSON format', () {
      final plan = PlanJson(
        requestId: 'req-006',
        narrative: 'The door creaks open...',
        tools: [
          const ToolInvocation(
            toolId: 'door-1',
            toolPath: '/tools/door-examiner',
            input: {'door_id': 'main_gate'},
          ),
        ],
        parallel: false,
      );
      
      final json = plan.toJson();
      expect(json['requestId'], equals('req-006'));
      expect(json['narrative'], equals('The door creaks open...'));
      expect(json['tools'], hasLength(1));
      expect(json['parallel'], isFalse);
    });

    test('copyWith creates modified copy', () {
      const original = PlanJson(
        requestId: 'req-007',
        tools: [],
        parallel: false,
      );
      
      final modified = original.copyWith(
        narrative: 'Added narrative',
        parallel: true,
      );
      
      expect(modified.requestId, equals('req-007'));
      expect(modified.narrative, equals('Added narrative'));
      expect(modified.parallel, isTrue);
      // Original unchanged
      expect(original.narrative, isNull);
      expect(original.parallel, isFalse);
    });
  });

  group('ToolInvocation Contract', () {
    test('MUST have toolId, toolPath, and input fields', () {
      const json = '''
        {"toolId": "torch-lighter-1", "toolPath": "/tools/torch-lighter/main", 
         "input": {"action": "light"}}
      ''';
      final tool = ToolInvocation.fromJson(jsonDecode(json));
      
      expect(tool.toolId, equals('torch-lighter-1'));
      expect(tool.toolPath, equals('/tools/torch-lighter/main'));
      expect(tool.input['action'], equals('light'));
    });

    test('MAY include dependencies array', () {
      const json = '''
        {"toolId": "tool-2", "toolPath": "/path", "input": {}, 
         "dependencies": ["tool-1"]}
      ''';
      final tool = ToolInvocation.fromJson(jsonDecode(json));
      
      expect(tool.dependencies, contains('tool-1'));
    });

    test('dependencies defaults to empty array', () {
      const json = '''
        {"toolId": "tool-3", "toolPath": "/path", "input": {}}
      ''';
      final tool = ToolInvocation.fromJson(jsonDecode(json));
      
      expect(tool.dependencies, isEmpty);
    });

    test('serializes to correct JSON format', () {
      const tool = ToolInvocation(
        toolId: 'examine-1',
        toolPath: '/tools/door-examiner',
        input: {'target': 'wooden_door', 'detail_level': 'high'},
        dependencies: ['unlock-1'],
      );
      
      final json = tool.toJson();
      expect(json['toolId'], equals('examine-1'));
      expect(json['toolPath'], equals('/tools/door-examiner'));
      expect(json['input']['target'], equals('wooden_door'));
      expect(json['dependencies'], contains('unlock-1'));
    });

    test('copyWith creates modified copy', () {
      const original = ToolInvocation(
        toolId: 'test-1',
        toolPath: '/original/path',
        input: {'key': 'value'},
      );
      
      final modified = original.copyWith(
        toolPath: '/new/path',
        dependencies: ['dep-1'],
      );
      
      expect(modified.toolId, equals('test-1'));
      expect(modified.toolPath, equals('/new/path'));
      expect(modified.dependencies, contains('dep-1'));
      // Original unchanged
      expect(original.toolPath, equals('/original/path'));
      expect(original.dependencies, isEmpty);
    });
  });

  group('Full Plan JSON Examples', () {
    test('torch-lighter plan parses correctly', () {
      const json = '''
        {
          "requestId": "plan-torch-001",
          "narrative": "You strike a match and hold it to the torch. The oil-soaked cloth catches fire, casting dancing shadows on the dungeon walls.",
          "tools": [
            {
              "toolId": "torch-1",
              "toolPath": "tools/torch-lighter/main",
              "input": {
                "torch_id": "dungeon_entrance_torch",
                "action": "light"
              }
            }
          ],
          "parallel": false
        }
      ''';
      
      final plan = PlanJson.fromJson(jsonDecode(json));
      
      expect(plan.requestId, equals('plan-torch-001'));
      expect(plan.narrative, contains('dancing shadows'));
      expect(plan.tools, hasLength(1));
      expect(plan.tools.first.toolId, equals('torch-1'));
      expect(plan.tools.first.input['action'], equals('light'));
    });

    test('multi-tool plan with dependencies parses correctly', () {
      const json = '''
        {
          "requestId": "plan-door-001",
          "narrative": "You approach the ancient door...",
          "tools": [
            {
              "toolId": "examine-1",
              "toolPath": "tools/door-examiner/main",
              "input": {"door_id": "ancient_gate"}
            },
            {
              "toolId": "unlock-1",
              "toolPath": "tools/lock-picker/main",
              "input": {"door_id": "ancient_gate"},
              "dependencies": ["examine-1"]
            }
          ],
          "parallel": false
        }
      ''';
      
      final plan = PlanJson.fromJson(jsonDecode(json));
      
      expect(plan.tools, hasLength(2));
      expect(plan.tools[1].dependencies, contains('examine-1'));
    });

    test('parallel tools plan parses correctly', () {
      const json = '''
        {
          "requestId": "plan-parallel-001",
          "tools": [
            {"toolId": "gen-image", "toolPath": "/gen/image", "input": {}},
            {"toolId": "gen-audio", "toolPath": "/gen/audio", "input": {}}
          ],
          "parallel": true
        }
      ''';
      
      final plan = PlanJson.fromJson(jsonDecode(json));
      
      expect(plan.parallel, isTrue);
      expect(plan.tools, hasLength(2));
      // Both tools have no dependencies, can run in parallel
      expect(plan.tools[0].dependencies, isEmpty);
      expect(plan.tools[1].dependencies, isEmpty);
    });
  });

  group('Edge Cases', () {
    test('handles empty tools array', () {
      const json = '{"requestId": "empty-plan", "tools": []}';
      final plan = PlanJson.fromJson(jsonDecode(json));
      expect(plan.tools, isEmpty);
    });

    test('handles complex nested input', () {
      const json = '''
        {
          "requestId": "complex-001",
          "tools": [{
            "toolId": "gen-1",
            "toolPath": "/generator",
            "input": {
              "config": {
                "model": "stable-diffusion",
                "params": {
                  "width": 512,
                  "height": 512,
                  "seeds": [1, 2, 3]
                }
              }
            }
          }]
        }
      ''';
      
      final plan = PlanJson.fromJson(jsonDecode(json));
      final input = plan.tools.first.input;
      
      expect(input['config']['model'], equals('stable-diffusion'));
      expect(input['config']['params']['width'], equals(512));
      expect(input['config']['params']['seeds'], equals([1, 2, 3]));
    });

    test('handles missing optional fields gracefully', () {
      const json = '{"tools": [{"input": {}}]}';
      final plan = PlanJson.fromJson(jsonDecode(json));
      
      // Defaults applied
      expect(plan.requestId, isEmpty);
      expect(plan.narrative, isNull);
      expect(plan.parallel, isFalse);
      expect(plan.tools.first.toolId, isEmpty);
      expect(plan.tools.first.toolPath, isEmpty);
    });
  });
}
