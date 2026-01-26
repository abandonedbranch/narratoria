// Player Prompt Model
// T020: Represents player input for narrator processing

/// A player's natural language prompt.
class PlayerPrompt {
  /// The raw text input from the player
  final String text;
  
  /// Timestamp when the prompt was submitted
  final DateTime submittedAt;
  
  /// Optional context from previous choices (narrative_choice selection)
  final String? choiceContext;

  PlayerPrompt({
    required this.text,
    DateTime? submittedAt,
    this.choiceContext,
  }) : submittedAt = submittedAt ?? DateTime.now();

  /// Create a prompt from a narrative choice selection
  factory PlayerPrompt.fromChoice({
    required String choiceId,
    required String choiceLabel,
  }) {
    return PlayerPrompt(
      text: choiceLabel,
      choiceContext: choiceId,
    );
  }

  /// Normalize the prompt text for matching
  String get normalized => text.trim().toLowerCase();

  @override
  String toString() => 'PlayerPrompt("$text")';
}
