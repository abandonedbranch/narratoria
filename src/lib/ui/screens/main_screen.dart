// Main Screen
// Spec 001 §12.2: Core navigation structure with NavigationRail

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'dart:convert';
import 'dart:io';

import '../../models/player_prompt.dart';
import '../../models/protocol_events.dart';
import '../../services/state_manager.dart';
import '../../services/plan_executor.dart';
import '../../services/narrator_ai_stub.dart';
import '../widgets/tool_execution_panel.dart';
import '../widgets/story_view.dart';
import '../widgets/player_input_field.dart';

/// Main screen with NavigationRail for MVP views.
/// 
/// Per Spec 001 §12.2, NavigationRail provides:
/// - Story View (primary narrative display)
/// - Tool Execution Panel (log streams, asset previews)
/// - State Inspector (debug view of session state)
class MainScreen extends StatefulWidget {
  const MainScreen({super.key});

  @override
  State<MainScreen> createState() => _MainScreenState();
}

class _MainScreenState extends State<MainScreen> {
  int _selectedIndex = 0;
  
  // Story state
  final List<StoryEntry> _storyEntries = [];
  UiEvent? _pendingChoice;
  bool _isProcessing = false;
  
  // Narrator AI stub
  late final NarratorAIStub _narrator;

 @override
 void initState() {
  super.initState();
  final toolsPath = _findToolsPath();
  _narrator = NarratorAIStub(
    toolBasePath: toolsPath,
  );
 }

 String _findToolsPath() {
  // Try to find tools in app bundle first (for release builds)
  final executable = Platform.resolvedExecutable;
  final execDir = File(executable).parent.path;
  
  // macOS app bundle: Contents/MacOS/narratoria -> Contents/Resources/tools
  final bundleTools = '${File(execDir).parent.path}/Resources/tools';
  if (Directory(bundleTools).existsSync()) {
    return bundleTools;
  }
  
  // Fall back to development path (when running via flutter run)
  // From src/build/macos/Build/Products/Debug/narratoria.app -> ../../../../../../bin
  var dir = Directory(execDir);
  while (dir.path != dir.parent.path) {
    final binPath = '${dir.path}/bin';
    if (Directory(binPath).existsSync() && 
        File('$binPath/torch-lighter').existsSync()) {
      return binPath;
    }
    dir = dir.parent;
  }
  
  // Last resort: absolute development path
  return '/Users/djlawhead/Developer/forkedagain/projects/narratoria/bin';
 }

  static const List<NavigationRailDestination> _destinations = [
    NavigationRailDestination(
      icon: Icon(Icons.book_outlined),
      selectedIcon: Icon(Icons.book),
      label: Text('Story'),
    ),
    NavigationRailDestination(
      icon: Icon(Icons.build_outlined),
      selectedIcon: Icon(Icons.build),
      label: Text('Tools'),
    ),
    NavigationRailDestination(
      icon: Icon(Icons.data_object_outlined),
      selectedIcon: Icon(Icons.data_object),
      label: Text('State'),
    ),
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Row(
        children: [
          NavigationRail(
            selectedIndex: _selectedIndex,
            onDestinationSelected: (int index) {
              setState(() {
                _selectedIndex = index;
              });
            },
            labelType: NavigationRailLabelType.all,
            leading: const SizedBox(height: 16),
            destinations: _destinations,
          ),
          const VerticalDivider(thickness: 1, width: 1),
          Expanded(
            child: _buildSelectedView(),
          ),
        ],
      ),
    );
  }

  Widget _buildSelectedView() {
    return switch (_selectedIndex) {
      0 => _buildStoryScreen(),
      1 => const ToolExecutionPanel(),
      2 => const _StateInspector(),
      _ => _buildStoryScreen(),
    };
  }

  Widget _buildStoryScreen() {
    return Column(
      children: [
        Expanded(
          child: StoryView(
            entries: _storyEntries,
            pendingChoice: _pendingChoice,
            onChoiceSelected: _handleChoiceSelected,
          ),
        ),
        PlayerInputField(
          onSubmit: _handlePlayerInput,
          disabled: _isProcessing,
        ),
      ],
    );
  }

  Future<void> _handlePlayerInput(String text) async {
    if (_isProcessing) return;
    
    final prompt = PlayerPrompt(text: text);
    
    // Add player action to story
    setState(() {
      _storyEntries.add(StoryEntry(
        type: StoryEntryType.playerAction,
        text: text,
      ));
      _pendingChoice = null; // Clear any pending choice
      _isProcessing = true;
    });

    await _processPrompt(prompt);
  }

  void _handleChoiceSelected(String choiceId, String choiceLabel) {
    final prompt = PlayerPrompt.fromChoice(
      choiceId: choiceId,
      choiceLabel: choiceLabel,
    );
    
    // Add player action to story
    setState(() {
      _storyEntries.add(StoryEntry(
        type: StoryEntryType.playerAction,
        text: choiceLabel,
      ));
      _pendingChoice = null;
      _isProcessing = true;
    });

    _processPrompt(prompt);
  }

  Future<void> _processPrompt(PlayerPrompt prompt) async {
    try {
      // Generate plan from narrator stub
      final plan = _narrator.generatePlan(prompt);
      
      if (plan == null) {
        setState(() {
          _storyEntries.add(StoryEntry(
            type: StoryEntryType.system,
            text: 'I couldn\'t understand that command.',
          ));
          _isProcessing = false;
        });
        return;
      }

      // Display narrative before tool execution
      if (plan.narrative != null && plan.narrative!.isNotEmpty) {
        setState(() {
          _storyEntries.add(StoryEntry(
            type: StoryEntryType.narrative,
            text: plan.narrative!,
          ));
        });
      }

      // Execute tools if any
      if (plan.tools.isNotEmpty) {
        final executor = context.read<PlanExecutor>();
        final stateManager = context.read<StateManager>();
        
        final result = await executor.execute(
          plan: plan,
          onEvent: (toolId, event) {
            // Handle state patches
            if (event is StatePatchEvent) {
              stateManager.applyPatch(event);
            }
            // Handle UI events (narrative choices)
            if (event is UiEvent && event.event == 'narrative_choice') {
              setState(() {
                _pendingChoice = event;
              });
            }
          },
        );

        // Add tool execution summary
        if (result.success) {
          // Don't add a message for success, the narrative already told the story
        } else {
          setState(() {
            _storyEntries.add(StoryEntry(
              type: StoryEntryType.system,
              text: 'Something went wrong: ${result.errorMessage}',
            ));
          });
        }
      }
    } catch (e) {
      setState(() {
        _storyEntries.add(StoryEntry(
          type: StoryEntryType.system,
          text: 'Error: $e',
        ));
      });
    } finally {
      setState(() {
        _isProcessing = false;
      });
    }
  }
}

/// State Inspector - displays session state JSON
class _StateInspector extends StatelessWidget {
  const _StateInspector();

  @override
  Widget build(BuildContext context) {
    return Consumer<StateManager>(
      builder: (context, stateManager, _) {
        final state = stateManager.sessionState.state;
        
        if (state.isEmpty) {
          return Center(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Icon(
                  Icons.data_object,
                  size: 64,
                  color: Theme.of(context).colorScheme.outline,
                ),
                const SizedBox(height: 16),
                Text(
                  'No Session State',
                  style: Theme.of(context).textTheme.titleLarge,
                ),
                const SizedBox(height: 8),
                Text(
                  'State patches will appear here as tools execute',
                  style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                    color: Theme.of(context).colorScheme.outline,
                  ),
                ),
              ],
            ),
          );
        }
        
        final encoder = const JsonEncoder.withIndent('  ');
        final prettyJson = encoder.convert(state);
        
        return Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  const Icon(Icons.data_object),
                  const SizedBox(width: 8),
                  Text(
                    'Session State',
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                  const Spacer(),
                  IconButton(
                    icon: const Icon(Icons.refresh),
                    onPressed: () => stateManager.reset(),
                    tooltip: 'Clear state',
                  ),
                ],
              ),
              const Divider(),
              Expanded(
                child: SingleChildScrollView(
                  child: SelectableText(
                    prettyJson,
                    style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                      fontFamily: 'monospace',
                    ),
                  ),
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}
