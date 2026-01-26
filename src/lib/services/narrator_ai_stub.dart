// Narrator AI Stub Service
// T019, T048: Hard-coded prompt→Plan JSON mappings for MVP testing

import 'package:uuid/uuid.dart';

import '../models/plan_json.dart';
import '../models/player_prompt.dart';

/// Stub narrator AI that maps player prompts to Plan JSON.
/// 
/// Per Spec 001 §13: The narrator AI (or a stub) produces a Plan JSON
/// describing which tools to invoke. This stub provides hardcoded mappings
/// for MVP testing.
class NarratorAIStub {
  /// Tool base path (prepended to relative tool paths)
  final String toolBasePath;
  
  /// UUID generator for request IDs
  final _uuid = const Uuid();

  NarratorAIStub({
    this.toolBasePath = 'tools',
  });

  /// Generate a Plan JSON from a player prompt.
  /// 
  /// Returns null if no matching action is found.
  PlanJson? generatePlan(PlayerPrompt prompt) {
    final normalized = prompt.normalized;
    
    // Try to match known prompt patterns
    for (final pattern in _promptPatterns) {
      if (pattern.matches(normalized)) {
        return pattern.generatePlan(
          requestId: _uuid.v4(),
          toolBasePath: toolBasePath,
          prompt: prompt,
        );
      }
    }
    
    // No match found - return a fallback narrative-only plan
    return PlanJson(
      requestId: _uuid.v4(),
      narrative: "I'm not sure what you mean by '${prompt.text}'. "
          'Try actions like "light torch", "examine door", or "look around".',
      tools: [],
    );
  }

  /// Check if a prompt would generate a plan with tools
  bool hasToolsForPrompt(PlayerPrompt prompt) {
    final normalized = prompt.normalized;
    return _promptPatterns.any((p) => p.matches(normalized));
  }
}

/// Pattern for matching player prompts to plan generation.
abstract class _PromptPattern {
  bool matches(String normalizedPrompt);
  PlanJson generatePlan({
    required String requestId,
    required String toolBasePath,
    required PlayerPrompt prompt,
  });
}

/// Pattern for torch-related actions
class _TorchPattern implements _PromptPattern {
  @override
  bool matches(String normalizedPrompt) {
    return normalizedPrompt.contains('light') && normalizedPrompt.contains('torch') ||
           normalizedPrompt.contains('ignite') ||
           normalizedPrompt.contains('use torch') ||
           normalizedPrompt == 'torch';
  }

  @override
  PlanJson generatePlan({
    required String requestId,
    required String toolBasePath,
    required PlayerPrompt prompt,
  }) {
    return PlanJson(
      requestId: requestId,
      narrative: 'You strike a match and hold it to the torch. The oil-soaked '
          'cloth catches fire, casting dancing shadows on the stone walls.',
      tools: [
        ToolInvocation(
          toolId: 'torch-1',
          toolPath: '$toolBasePath/torch-lighter',
          input: const {
            'torch_id': 'player_torch',
            'action': 'light',
          },
        ),
      ],
    );
  }
}

/// Pattern for door examination
class _DoorPattern implements _PromptPattern {
  @override
  bool matches(String normalizedPrompt) {
    return normalizedPrompt.contains('examine') && normalizedPrompt.contains('door') ||
           normalizedPrompt.contains('look at') && normalizedPrompt.contains('door') ||
           normalizedPrompt.contains('check') && normalizedPrompt.contains('door') ||
           normalizedPrompt.contains('inspect') && normalizedPrompt.contains('door') ||
           normalizedPrompt == 'door';
  }

  @override
  PlanJson generatePlan({
    required String requestId,
    required String toolBasePath,
    required PlayerPrompt prompt,
  }) {
    // Extract door type from prompt
    String doorId = 'wooden_door';
    if (prompt.normalized.contains('ancient') || prompt.normalized.contains('gate')) {
      doorId = 'ancient_gate';
    } else if (prompt.normalized.contains('iron') || prompt.normalized.contains('metal')) {
      doorId = 'iron_door';
    }

    return PlanJson(
      requestId: requestId,
      narrative: 'You approach the door and examine it carefully...',
      tools: [
        ToolInvocation(
          toolId: 'examine-1',
          toolPath: '$toolBasePath/door-examiner',
          input: {
            'door_id': doorId,
            'detail_level': 'high',
          },
        ),
      ],
    );
  }
}

/// Pattern for looking around
class _LookPattern implements _PromptPattern {
  @override
  bool matches(String normalizedPrompt) {
    return normalizedPrompt == 'look' ||
           normalizedPrompt == 'look around' ||
           normalizedPrompt.contains('survey') ||
           normalizedPrompt == 'examine room' ||
           normalizedPrompt == 'describe';
  }

  @override
  PlanJson generatePlan({
    required String requestId,
    required String toolBasePath,
    required PlayerPrompt prompt,
  }) {
    return PlanJson(
      requestId: requestId,
      narrative: 'You find yourself in a dimly lit stone chamber. The air is '
          'cool and damp, carrying the faint scent of mildew. Shadows dance at '
          'the edges of your vision.\n\n'
          'To the north, an ancient stone gate blocks the passage, its surface '
          'covered in mysterious runes. An unlit torch rests in an iron sconce '
          'on the wall beside you.\n\n'
          'What would you like to do?',
      tools: [], // Narrative-only response
    );
  }
}

/// Pattern for extinguishing torch
class _ExtinguishPattern implements _PromptPattern {
  @override
  bool matches(String normalizedPrompt) {
    return normalizedPrompt.contains('extinguish') ||
           normalizedPrompt.contains('put out') && normalizedPrompt.contains('torch') ||
           normalizedPrompt.contains('douse');
  }

  @override
  PlanJson generatePlan({
    required String requestId,
    required String toolBasePath,
    required PlayerPrompt prompt,
  }) {
    return PlanJson(
      requestId: requestId,
      narrative: 'You carefully extinguish the torch, plunging the area into '
          'near darkness. Only faint ambient light remains.',
      tools: [
        ToolInvocation(
          toolId: 'torch-1',
          toolPath: '$toolBasePath/torch-lighter',
          input: const {
            'torch_id': 'player_torch',
            'action': 'extinguish',
          },
        ),
      ],
    );
  }
}

/// All registered prompt patterns
final List<_PromptPattern> _promptPatterns = [
  _TorchPattern(),
  _DoorPattern(),
  _LookPattern(),
  _ExtinguishPattern(),
];
