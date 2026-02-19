/// Game engine: state management, skill checks, scene navigation,
/// freeform input resolution.
///
/// This is the deterministic core — no LLM dependency.

import 'models.dart';

/// Tracks mutable game state during a playthrough.
class GameState {
  final Game game;
  int currentActIndex = 0;
  int currentSceneIndex = 0;
  final List<String> recentHistory = [];

  GameState(this.game);

  Act get currentAct => game.acts[currentActIndex];
  Scene get currentScene => currentAct.scenes[currentSceneIndex];

  PlayerPersona? get persona => game.playerPersona;

  /// Look up a player attribute value by stat name.
  int? getPlayerStat(String statName) {
    final attr = persona?.attributes
        .where((a) => a.name == statName)
        .firstOrNull;
    return attr?.value;
  }

  /// Find a character in the current act by title.
  Character? findCharacter(String title) {
    return currentAct.characters
        .where((c) => c.title == title)
        .firstOrNull;
  }

  /// Find characters present in the current scene.
  List<Character> get presentCharacters {
    return currentScene.charactersPresent
        .map((name) => findCharacter(name))
        .whereType<Character>()
        .toList();
  }

  /// Find the current location object.
  Location? get currentLocation {
    final locName = currentScene.location;
    if (locName == null) return null;
    return currentAct.locations
        .where((l) => l.title == locName)
        .firstOrNull;
  }
}

/// Result of resolving a skill check.
class SkillCheckResult {
  final bool success;
  final String statName;
  final int playerValue;
  final int difficulty;
  final String outcomeText;

  SkillCheckResult({
    required this.success,
    required this.statName,
    required this.playerValue,
    required this.difficulty,
    required this.outcomeText,
  });
}

/// Result of resolving a player's option selection.
class OptionResult {
  final String narrativeText;
  final String? nextSceneTitle;
  final SkillCheckResult? skillCheckResult;

  OptionResult({
    required this.narrativeText,
    this.nextSceneTitle,
    this.skillCheckResult,
  });
}

/// Result of resolving a freeform player action.
class FreeformResult {
  final bool resolved;
  final String message;
  final SkillCheckResult? skillCheckResult;

  FreeformResult({
    required this.resolved,
    required this.message,
    this.skillCheckResult,
  });
}

/// Core game logic engine.
class GameEngine {
  final GameState state;

  GameEngine(this.state);

  /// Resolve a numbered option selection (1-based).
  OptionResult? resolveOption(int optionNumber) {
    final scene = state.currentScene;
    if (optionNumber < 1 || optionNumber > scene.options.length) {
      return null;
    }

    final option = scene.options[optionNumber - 1];
    final parts = <String>[];
    SkillCheckResult? checkResult;

    // 1. Skill check resolution
    if (option.skillCheck != null) {
      checkResult = _resolveSkillCheck(option.skillCheck!);
      parts.add(checkResult.outcomeText);
    }

    // 2. Outcome text (after skill check result)
    if (option.outcome != null) {
      parts.add(option.outcome!);
    }

    // 3. Determine next scene
    String? nextScene = option.transition ?? scene.transitions.defaultScene;

    final narrative = parts.isNotEmpty ? parts.join('\n\n') : option.text;

    state.recentHistory.add('> ${option.text}');
    if (narrative.isNotEmpty) {
      state.recentHistory.add(narrative);
    }

    return OptionResult(
      narrativeText: narrative,
      nextSceneTitle: nextScene,
      skillCheckResult: checkResult,
    );
  }

  /// Resolve a skill check deterministically.
  SkillCheckResult _resolveSkillCheck(SkillCheck check) {
    final playerValue = state.getPlayerStat(check.stat) ?? 0;
    final success = playerValue >= check.difficulty;

    return SkillCheckResult(
      success: success,
      statName: check.stat,
      playerValue: playerValue,
      difficulty: check.difficulty,
      outcomeText: '', // filled by caller with on_success/on_fail
    );
  }

  /// Resolve a numbered option with full skill check text.
  OptionResult? resolveOptionFull(int optionNumber) {
    final scene = state.currentScene;
    if (optionNumber < 1 || optionNumber > scene.options.length) {
      return null;
    }

    final option = scene.options[optionNumber - 1];
    final parts = <String>[];
    SkillCheckResult? checkResult;

    // 1. Skill check
    if (option.skillCheck != null) {
      final check = option.skillCheck!;
      final playerValue = state.getPlayerStat(check.stat) ?? 0;
      final success = playerValue >= check.difficulty;
      final outcomeText = success
          ? (option.onSuccess ?? '')
          : (option.onFail ?? '');

      checkResult = SkillCheckResult(
        success: success,
        statName: check.stat,
        playerValue: playerValue,
        difficulty: check.difficulty,
        outcomeText: outcomeText,
      );
      parts.add(outcomeText);
    }

    // 2. Outcome text
    if (option.outcome != null) {
      parts.add(option.outcome!);
    }

    // 3. Determine next scene
    String? nextScene = option.transition ?? scene.transitions.defaultScene;

    final narrative = parts.isNotEmpty ? parts.join('\n\n') : '';

    state.recentHistory.add('> ${option.text}');
    if (narrative.isNotEmpty) {
      state.recentHistory.add(narrative);
    }

    return OptionResult(
      narrativeText: narrative,
      nextSceneTitle: nextScene,
      skillCheckResult: checkResult,
    );
  }

  /// Navigate to a scene by title within the current act.
  /// Returns true if navigation succeeded.
  bool navigateToScene(String sceneTitle) {
    final idx = state.currentAct.scenes
        .indexWhere((s) => s.title == sceneTitle);
    if (idx >= 0) {
      state.currentSceneIndex = idx;
      return true;
    }
    return false;
  }

  /// Advance to the next act. Returns true if there was a next act.
  bool advanceAct() {
    if (state.currentActIndex + 1 < state.game.acts.length) {
      state.currentActIndex++;
      state.currentSceneIndex = 0;
      return true;
    }
    return false;
  }

  /// Parse and resolve freeform player input (e.g., "attack leory").
  FreeformResult resolveFreeform(String input) {
    final persona = state.persona;
    if (persona == null) {
      return FreeformResult(
          resolved: false, message: 'No player persona defined.');
    }

    final words = input.trim().toLowerCase().split(RegExp(r'\s+'));
    if (words.isEmpty) {
      return FreeformResult(resolved: false, message: 'What do you want to do?');
    }

    final verb = words.first;

    // Find the attribute matching this verb
    Attribute? matchedAttr;
    for (final attr in persona.attributes) {
      if (attr.verbs.map((v) => v.toLowerCase()).contains(verb)) {
        matchedAttr = attr;
        break;
      }
    }

    if (matchedAttr == null) {
      return FreeformResult(
          resolved: false, message: "You can't do that.");
    }

    // Extract target (everything after the verb)
    if (words.length < 2) {
      return FreeformResult(
          resolved: false,
          message: '${_capitalize(verb)} what?');
    }

    final targetName = words.sublist(1).join(' ');

    // Find target in current scene's characters
    final presentChars = state.presentCharacters;
    Character? target;
    for (final ch in presentChars) {
      if (ch.title.toLowerCase().contains(targetName)) {
        target = ch;
        break;
      }
    }

    if (target == null) {
      return FreeformResult(
          resolved: false, message: "That isn't here.");
    }

    // Check if target has attributes
    if (target.attributes.isEmpty) {
      return FreeformResult(
          resolved: false,
          message:
              'You cannot roll to ${verb.toUpperCase()} ${target.title}.');
    }

    // Determine defense: mirror stat or max
    final playerValue = state.getPlayerStat(matchedAttr.name) ?? 0;
    int defense;
    final mirrorAttr = target.attributes
        .where((a) => a.name == matchedAttr!.name)
        .firstOrNull;
    if (mirrorAttr != null) {
      defense = mirrorAttr.value;
    } else {
      defense = target.attributes
          .map((a) => a.value)
          .reduce((a, b) => a > b ? a : b);
    }

    final success = playerValue >= defense;
    final resultText = success
        ? 'You $verb ${target.title} successfully!'
        : '${target.title} resists your attempt to $verb.';

    final checkResult = SkillCheckResult(
      success: success,
      statName: matchedAttr.name,
      playerValue: playerValue,
      difficulty: defense,
      outcomeText: resultText,
    );

    state.recentHistory.add('> $input');
    state.recentHistory.add(resultText);

    return FreeformResult(
      resolved: true,
      message: resultText,
      skillCheckResult: checkResult,
    );
  }

  /// Get UI hint for an option's skill check.
  String getOptionHint(Option option) {
    if (option.skillCheck == null) return '';
    final check = option.skillCheck!;
    final playerValue = state.getPlayerStat(check.stat) ?? 0;
    if (playerValue >= check.difficulty) {
      return '[likely - ${check.stat}]';
    } else {
      return '[risky - ${check.stat} ${check.difficulty}]';
    }
  }
}

String _capitalize(String s) =>
    s.isEmpty ? s : s[0].toUpperCase() + s.substring(1);
