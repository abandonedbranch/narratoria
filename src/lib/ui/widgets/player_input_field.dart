// Player Input Field Widget
// T021: Multiline text input with send button

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

/// Text input field for player prompts.
/// 
/// Per Spec 001 §12.3: Player enters natural language prompts
/// which are sent to the narrator AI for plan generation.
class PlayerInputField extends StatefulWidget {
  /// Callback when player submits a prompt
  final void Function(String text) onSubmit;
  
  /// Whether input is currently disabled (e.g., during tool execution)
  final bool disabled;
  
  /// Hint text to display when empty
  final String hintText;

  const PlayerInputField({
    super.key,
    required this.onSubmit,
    this.disabled = false,
    this.hintText = 'What do you do?',
  });

  @override
  State<PlayerInputField> createState() => _PlayerInputFieldState();
}

class _PlayerInputFieldState extends State<PlayerInputField> {
  final _controller = TextEditingController();
  final _focusNode = FocusNode();

  @override
  void dispose() {
    _controller.dispose();
    _focusNode.dispose();
    super.dispose();
  }

  void _handleSubmit() {
    final text = _controller.text.trim();
    if (text.isNotEmpty && !widget.disabled) {
      widget.onSubmit(text);
      _controller.clear();
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: theme.colorScheme.surfaceContainerHighest.withValues(alpha: 0.3),
        border: Border(
          top: BorderSide(color: theme.dividerColor),
        ),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          Expanded(
            child: KeyboardListener(
              focusNode: _focusNode,
              onKeyEvent: (event) {
                // Submit on Enter (without Shift)
                if (event is KeyDownEvent &&
                    event.logicalKey == LogicalKeyboardKey.enter &&
                    !HardwareKeyboard.instance.isShiftPressed) {
                  _handleSubmit();
                }
              },
              child: TextField(
                controller: _controller,
                enabled: !widget.disabled,
                maxLines: 3,
                minLines: 1,
                decoration: InputDecoration(
                  hintText: widget.disabled ? 'Please wait...' : widget.hintText,
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                  filled: true,
                  contentPadding: const EdgeInsets.symmetric(
                    horizontal: 16,
                    vertical: 12,
                  ),
                ),
                textInputAction: TextInputAction.send,
                onSubmitted: (_) => _handleSubmit(),
              ),
            ),
          ),
          const SizedBox(width: 12),
          IconButton.filled(
            onPressed: widget.disabled ? null : _handleSubmit,
            icon: widget.disabled
                ? const SizedBox(
                    width: 20,
                    height: 20,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : const Icon(Icons.send),
            tooltip: 'Send',
          ),
        ],
      ),
    );
  }
}
