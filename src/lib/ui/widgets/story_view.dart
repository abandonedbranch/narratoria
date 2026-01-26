// Story View Widget
// T022: Display narrative text and handle UI events

import 'package:flutter/material.dart';

import '../../models/protocol_events.dart';

/// Represents a single entry in the story log.
class StoryEntry {
  /// Entry type for styling
  final StoryEntryType type;
  
  /// The text content
  final String text;
  
  /// When this entry was added
  final DateTime timestamp;
  
  /// Optional associated event
  final ProtocolEvent? event;

  StoryEntry({
    required this.type,
    required this.text,
    DateTime? timestamp,
    this.event,
  }) : timestamp = timestamp ?? DateTime.now();
}

/// Types of story entries for different styling
enum StoryEntryType {
  /// Narrative text from narrator
  narrative,
  /// Player input/action
  playerAction,
  /// System message (errors, status)
  system,
  /// Tool output summary
  toolOutput,
}

/// Widget displaying the narrative story flow.
/// 
/// Per Spec 001 §12.3: Story View displays narrative text,
/// player prompts, and tool execution summaries.
class StoryView extends StatefulWidget {
  /// The story entries to display
  final List<StoryEntry> entries;
  
  /// Currently pending narrative choice (if any)
  final UiEvent? pendingChoice;
  
  /// Callback when a narrative choice is selected
  final void Function(String choiceId, String choiceLabel)? onChoiceSelected;

  const StoryView({
    super.key,
    required this.entries,
    this.pendingChoice,
    this.onChoiceSelected,
  });

  @override
  State<StoryView> createState() => _StoryViewState();
}

class _StoryViewState extends State<StoryView> {
  final _scrollController = ScrollController();

  @override
  void didUpdateWidget(StoryView oldWidget) {
    super.didUpdateWidget(oldWidget);
    // Auto-scroll to bottom when new entries added
    if (widget.entries.length > oldWidget.entries.length) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (_scrollController.hasClients) {
          _scrollController.animateTo(
            _scrollController.position.maxScrollExtent,
            duration: const Duration(milliseconds: 300),
            curve: Curves.easeOut,
          );
        }
      });
    }
  }

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    if (widget.entries.isEmpty && widget.pendingChoice == null) {
      return const _EmptyStoryView();
    }

    return ListView.builder(
      controller: _scrollController,
      padding: const EdgeInsets.all(16),
      itemCount: widget.entries.length + (widget.pendingChoice != null ? 1 : 0),
      itemBuilder: (context, index) {
        if (index < widget.entries.length) {
          return _StoryEntryWidget(entry: widget.entries[index]);
        } else {
          return _NarrativeChoiceWidget(
            event: widget.pendingChoice!,
            onSelected: widget.onChoiceSelected,
          );
        }
      },
    );
  }
}

class _EmptyStoryView extends StatelessWidget {
  const _EmptyStoryView();

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(
            Icons.auto_stories,
            size: 64,
            color: Theme.of(context).colorScheme.primary,
          ),
          const SizedBox(height: 16),
          Text(
            'Your Adventure Awaits',
            style: Theme.of(context).textTheme.headlineMedium,
          ),
          const SizedBox(height: 8),
          Text(
            'Type a command below to begin your journey...',
            style: Theme.of(context).textTheme.bodyMedium?.copyWith(
              color: Theme.of(context).colorScheme.outline,
            ),
          ),
          const SizedBox(height: 24),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Try these commands:',
                    style: Theme.of(context).textTheme.titleSmall,
                  ),
                  const SizedBox(height: 8),
                  _suggestionChip(context, 'look around'),
                  _suggestionChip(context, 'light torch'),
                  _suggestionChip(context, 'examine door'),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _suggestionChip(BuildContext context, String text) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          const Icon(Icons.chevron_right, size: 16),
          const SizedBox(width: 8),
          Text(
            text,
            style: Theme.of(context).textTheme.bodyMedium?.copyWith(
              fontFamily: 'monospace',
            ),
          ),
        ],
      ),
    );
  }
}

class _StoryEntryWidget extends StatelessWidget {
  final StoryEntry entry;

  const _StoryEntryWidget({required this.entry});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: switch (entry.type) {
        StoryEntryType.narrative => SelectableText(
            entry.text,
            style: theme.textTheme.bodyLarge?.copyWith(
              height: 1.6,
            ),
          ),
        StoryEntryType.playerAction => Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Icon(
                Icons.person,
                size: 20,
                color: theme.colorScheme.primary,
              ),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  '> ${entry.text}',
                  style: theme.textTheme.bodyMedium?.copyWith(
                    color: theme.colorScheme.primary,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ),
            ],
          ),
        StoryEntryType.system => Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: theme.colorScheme.surfaceContainerHighest.withValues(alpha: 0.5),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Text(
              entry.text,
              style: theme.textTheme.bodySmall?.copyWith(
                fontStyle: FontStyle.italic,
                color: theme.colorScheme.outline,
              ),
            ),
          ),
        StoryEntryType.toolOutput => Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: theme.colorScheme.secondaryContainer.withValues(alpha: 0.3),
              borderRadius: BorderRadius.circular(8),
              border: Border.all(
                color: theme.colorScheme.secondary.withValues(alpha: 0.3),
              ),
            ),
            child: Row(
              children: [
                Icon(
                  Icons.build,
                  size: 16,
                  color: theme.colorScheme.secondary,
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    entry.text,
                    style: theme.textTheme.bodySmall?.copyWith(
                      fontFamily: 'monospace',
                    ),
                  ),
                ),
              ],
            ),
          ),
      },
    );
  }
}

class _NarrativeChoiceWidget extends StatelessWidget {
  final UiEvent event;
  final void Function(String choiceId, String choiceLabel)? onSelected;

  const _NarrativeChoiceWidget({
    required this.event,
    this.onSelected,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final payload = event.payload ?? {};
    final prompt = payload['prompt'] as String?;
    final choices = (payload['choices'] as List<dynamic>?)
        ?.cast<Map<String, dynamic>>() ?? [];

    return Card(
      margin: const EdgeInsets.symmetric(vertical: 8),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (prompt != null) ...[
              Text(
                prompt,
                style: theme.textTheme.bodyLarge?.copyWith(
                  height: 1.6,
                ),
              ),
              const SizedBox(height: 16),
            ],
            Text(
              'What do you do?',
              style: theme.textTheme.titleSmall?.copyWith(
                color: theme.colorScheme.primary,
              ),
            ),
            const SizedBox(height: 12),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: choices.map((choice) {
                final id = choice['id'] as String? ?? '';
                final label = choice['label'] as String? ?? 'Choose';
                final hint = choice['hint'] as String?;
                
                return Tooltip(
                  message: hint ?? '',
                  child: FilledButton.tonal(
                    onPressed: onSelected != null
                        ? () => onSelected!(id, label)
                        : null,
                    child: Text(label),
                  ),
                );
              }).toList(),
            ),
          ],
        ),
      ),
    );
  }
}
