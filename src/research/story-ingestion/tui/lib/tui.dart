/// Terminal UI for the story ingestion prototype.
///
/// Renders scenes, presents options with skill check hints, handles
/// both numbered option selection and freeform text input.

import 'dart:io';
import 'engine.dart';

/// ANSI escape codes for terminal styling.
class _Ansi {
  static const reset = '\x1B[0m';
  static const bold = '\x1B[1m';
  static const dim = '\x1B[2m';
  static const italic = '\x1B[3m';

  static const cyan = '\x1B[36m';
  static const yellow = '\x1B[33m';
  static const green = '\x1B[32m';
  static const red = '\x1B[31m';
  static const magenta = '\x1B[35m';
}

class Tui {
  final GameEngine engine;
  bool _running = true;

  Tui(this.engine);

  void run() {
    _printBanner();
    _printGameInfo();
    _printDivider();

    while (_running) {
      _renderScene();
      _promptInput();
    }

    _println('');
    _println('${_Ansi.dim}Thanks for playing.${_Ansi.reset}');
  }

  // ── Rendering ──────────────────────────────────────────────────────

  void _printBanner() {
    _println('');
    _println('${_Ansi.bold}${_Ansi.cyan}'
        '╔══════════════════════════════════════════╗${_Ansi.reset}');
    _println('${_Ansi.bold}${_Ansi.cyan}'
        '║         NARRATORIA  — Story TUI          ║${_Ansi.reset}');
    _println('${_Ansi.bold}${_Ansi.cyan}'
        '╚══════════════════════════════════════════╝${_Ansi.reset}');
    _println('');
  }

  void _printGameInfo() {
    final game = engine.state.game;
    _println('${_Ansi.bold}${game.title}${_Ansi.reset}');
    _println('${_Ansi.dim}${game.summary.trim()}${_Ansi.reset}');

    final persona = game.playerPersona;
    if (persona != null) {
      _println('');
      if (persona.summary != null) {
        _println('${_Ansi.italic}${persona.summary}${_Ansi.reset}');
      }
      _printPlayerStats();
      if (persona.inventory.isNotEmpty) {
        _println('${_Ansi.dim}Starting inventory: '
            '${persona.inventory.map((i) => i.name).join(", ")}${_Ansi.reset}');
      }
    }
  }

  void _printPlayerStats() {
    final attrs = engine.state.persona?.attributes;
    if (attrs == null || attrs.isEmpty) return;

    final statLine = attrs.map((a) => '${a.name}:${a.value}').join('  ');
    _println('${_Ansi.yellow}Stats: $statLine${_Ansi.reset}');
  }

  void _printDivider() {
    _println('${_Ansi.dim}${'─' * 50}${_Ansi.reset}');
  }

  void _renderScene() {
    final state = engine.state;
    final scene = state.currentScene;
    final act = state.currentAct;

    _println('');
    _println('${_Ansi.dim}Act: ${act.title}${_Ansi.reset}');

    // Location
    if (scene.location != null) {
      _println('${_Ansi.magenta}Location: ${scene.location}${_Ansi.reset}');
    }

    // Characters present
    if (scene.charactersPresent.isNotEmpty) {
      _println(
          '${_Ansi.dim}Present: ${scene.charactersPresent.join(", ")}'
          '${_Ansi.reset}');
    }

    _println('');

    // Narrative text — word-wrap for readability
    _printWrapped(scene.narrative.trim(), width: 72);

    _println('');

    // Options
    if (scene.options.isNotEmpty) {
      _println('${_Ansi.bold}What do you do?${_Ansi.reset}');
      for (var i = 0; i < scene.options.length; i++) {
        final opt = scene.options[i];
        final hint = engine.getOptionHint(opt);
        // Color likely vs risky
        final hintColored = hint.contains('likely')
            ? ' ${_Ansi.green}$hint${_Ansi.reset}'
            : hint.contains('risky')
                ? ' ${_Ansi.red}$hint${_Ansi.reset}'
                : '';
        _println('  ${_Ansi.cyan}${i + 1}.${_Ansi.reset} '
            '${opt.text}$hintColored');
      }
      _println('');
      _println('${_Ansi.dim}Enter a number, type a freeform action, '
          'or "help" for commands.${_Ansi.reset}');
    } else {
      _println('${_Ansi.dim}No options available.${_Ansi.reset}');
      _println('${_Ansi.dim}Type "quit" to exit or "restart" to start over.'
          '${_Ansi.reset}');
    }
  }

  // ── Input handling ─────────────────────────────────────────────────

  void _promptInput() {
    stdout.write('${_Ansi.bold}> ${_Ansi.reset}');
    final line = stdin.readLineSync()?.trim() ?? '';

    if (line.isEmpty) return;

    // Meta commands
    switch (line.toLowerCase()) {
      case 'quit':
      case 'exit':
      case 'q':
        _running = false;
        return;
      case 'restart':
        engine.state.currentActIndex = 0;
        engine.state.currentSceneIndex = 0;
        engine.state.recentHistory.clear();
        _println('${_Ansi.yellow}Restarted.${_Ansi.reset}');
        _printDivider();
        return;
      case 'stats':
        _printPlayerStats();
        return;
      case 'inventory':
      case 'inv':
        _printInventory();
        return;
      case 'look':
        return; // re-renders on next loop iteration
      case 'help':
        _printHelp();
        return;
    }

    // Try numbered option selection
    final number = int.tryParse(line);
    if (number != null) {
      _handleOptionSelection(number);
      return;
    }

    // Freeform input
    _handleFreeformInput(line);
  }

  void _handleOptionSelection(int number) {
    final result = engine.resolveOptionFull(number);
    if (result == null) {
      _println('${_Ansi.red}Invalid option. Choose 1-'
          '${engine.state.currentScene.options.length}.${_Ansi.reset}');
      return;
    }

    _printDivider();

    // Skill check feedback
    if (result.skillCheckResult != null) {
      final sc = result.skillCheckResult!;
      final tag = sc.success
          ? '${_Ansi.green}SUCCESS${_Ansi.reset}'
          : '${_Ansi.red}FAILURE${_Ansi.reset}';
      _println(
          '${_Ansi.dim}[${sc.statName} check: '
          '${sc.playerValue} vs ${sc.difficulty}] $tag');
    }

    // Narrative
    if (result.narrativeText.isNotEmpty) {
      _println('');
      _printWrapped(result.narrativeText, width: 72);
    }

    // Navigate
    if (result.nextSceneTitle != null) {
      if (!engine.navigateToScene(result.nextSceneTitle!)) {
        _println('');
        _println('${_Ansi.yellow}The path leads beyond what is written...'
            '${_Ansi.reset}');
        _handleEndOfContent();
      }
    } else {
      _handleEndOfContent();
    }
  }

  void _handleFreeformInput(String input) {
    final result = engine.resolveFreeform(input);

    _printDivider();

    if (!result.resolved) {
      _println('${_Ansi.dim}${result.message}${_Ansi.reset}');
      return;
    }

    // Skill check feedback
    if (result.skillCheckResult != null) {
      final sc = result.skillCheckResult!;
      final tag = sc.success
          ? '${_Ansi.green}SUCCESS${_Ansi.reset}'
          : '${_Ansi.red}FAILURE${_Ansi.reset}';
      _println(
          '${_Ansi.dim}[${sc.statName} check: '
          '${sc.playerValue} vs ${sc.difficulty}] $tag');
    }

    _println('');
    _printWrapped(result.message, width: 72);
  }

  void _handleEndOfContent() {
    if (engine.advanceAct()) {
      _println('');
      _println('${_Ansi.bold}${_Ansi.yellow}'
          '═══ Advancing to next act ═══${_Ansi.reset}');
    } else {
      _println('');
      _println('${_Ansi.bold}${_Ansi.cyan}'
          'You have reached the end of the story.${_Ansi.reset}');
      _println(
          '${_Ansi.dim}Type "restart" to play again or "quit" to exit.'
          '${_Ansi.reset}');
    }
  }

  void _printInventory() {
    final inv = engine.state.persona?.inventory;
    if (inv == null || inv.isEmpty) {
      _println('${_Ansi.dim}Your pack is empty.${_Ansi.reset}');
      return;
    }
    _println('${_Ansi.bold}Inventory:${_Ansi.reset}');
    for (final item in inv) {
      _println('${_Ansi.cyan}${item.name}${_Ansi.reset}');
      if (item.summary != null) {
        _printWrapped(item.summary!, width: 68);
      }
      if (item.verbs.isNotEmpty) {
        _println('${_Ansi.dim}Can be used to: ${item.verbs.join(", ")}${_Ansi.reset}');
      }
      _println('');
    }
  }

  void _printHelp() {
    _println('');
    _println('${_Ansi.bold}Commands:${_Ansi.reset}');
    _println('  ${_Ansi.cyan}1-N${_Ansi.reset}'
        '       Select a numbered option');
    _println('  ${_Ansi.cyan}verb target${_Ansi.reset}'
        ' Freeform action (e.g., "attack leory")');
    _println('  ${_Ansi.cyan}look${_Ansi.reset}'
        '      Re-read the current scene');
    _println('  ${_Ansi.cyan}stats${_Ansi.reset}'
        '     Show your attributes');
    _println('  ${_Ansi.cyan}inventory${_Ansi.reset}'
        ' Show your items');
    _println('  ${_Ansi.cyan}restart${_Ansi.reset}'
        '   Restart the game');
    _println('  ${_Ansi.cyan}help${_Ansi.reset}'
        '      Show this help');
    _println('  ${_Ansi.cyan}quit${_Ansi.reset}'
        '      Exit the game');
    _println('');
  }

  // ── Utilities ──────────────────────────────────────────────────────

  void _println(String s) => stdout.writeln(s);

  void _printWrapped(String text, {int width = 72}) {
    for (final paragraph in text.split('\n')) {
      if (paragraph.trim().isEmpty) {
        _println('');
        continue;
      }
      final words = paragraph.trim().split(RegExp(r'\s+'));
      final buffer = StringBuffer();
      for (final word in words) {
        if (buffer.length + word.length + 1 > width && buffer.isNotEmpty) {
          _println(buffer.toString());
          buffer.clear();
        }
        if (buffer.isNotEmpty) buffer.write(' ');
        buffer.write(word);
      }
      if (buffer.isNotEmpty) _println(buffer.toString());
    }
  }
}
