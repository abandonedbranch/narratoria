import 'dart:io';
import 'package:tui/ingestion.dart';
import 'package:tui/engine.dart';
import 'package:tui/tui.dart';

void main(List<String> arguments) {
  // Determine game file path
  final gamePath = arguments.isNotEmpty
      ? arguments.first
      : _findDefaultGameFile();

  if (gamePath == null) {
    stderr.writeln('Usage: dart run tui [path/to/game.toml]');
    stderr.writeln('No game.toml found in current directory or parent.');
    exit(1);
  }

  // Ingest
  final IngestionResult result;
  try {
    result = ingestGameFile(gamePath);
  } on FileSystemException catch (e) {
    stderr.writeln('Error: ${e.message} — ${e.path}');
    exit(1);
  } on FormatException catch (e) {
    stderr.writeln('TOML parse error: $e');
    exit(1);
  }

  // Print warnings
  if (result.warnings.isNotEmpty) {
    stderr.writeln('Ingestion warnings:');
    for (final w in result.warnings) {
      stderr.writeln('  ⚠ $w');
    }
    stderr.writeln('');
  }

  // Validate minimum content
  if (result.game.acts.isEmpty) {
    stderr.writeln('Error: game has no acts defined.');
    exit(1);
  }
  if (result.game.acts.first.scenes.isEmpty) {
    stderr.writeln('Error: first act has no scenes.');
    exit(1);
  }

  // Boot engine and TUI
  final state = GameState(result.game);
  final engine = GameEngine(state);
  final tui = Tui(engine);
  tui.run();
}

/// Look for game.toml in current dir and the parent story-ingestion dir.
String? _findDefaultGameFile() {
  final candidates = [
    'game.toml',
    '../game.toml',
    '../wizardrungame.toml',
    'wizardrungame.toml',
  ];
  for (final c in candidates) {
    if (File(c).existsSync()) return c;
  }
  return null;
}
