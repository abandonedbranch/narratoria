/// Parses a game.toml file into domain objects.
///
/// Handles the full game.toml structure as defined in story-ingestion.md.

import 'dart:io';
import 'package:toml/toml.dart';
import 'models.dart';

class IngestionError {
  final String message;
  final String? context;

  IngestionError(this.message, [this.context]);

  @override
  String toString() =>
      context != null ? '$message (in $context)' : message;
}

class IngestionResult {
  final Game game;
  final List<IngestionError> warnings;

  IngestionResult({required this.game, this.warnings = const []});
}

/// Parse a game.toml file and return domain objects with validation warnings.
IngestionResult ingestGameFile(String path) {
  final file = File(path);
  if (!file.existsSync()) {
    throw FileSystemException('Game file not found', path);
  }

  final content = file.readAsStringSync();
  final doc = TomlDocument.parse(content);
  final data = doc.toMap();
  final warnings = <IngestionError>[];

  // Root-level fields
  final title = _requireString(data, 'title', 'root', warnings);
  final summary = _requireString(data, 'summary', 'root', warnings);

  // Player persona (optional)
  PlayerPersona? playerPersona;
  final ppData = data['player_persona'];
  if (ppData is Map) {
    playerPersona = _parsePlayerPersona(ppData, warnings);
  }

  // Acts
  final actsList = <Act>[];
  final actsData = data['acts'];
  if (actsData is List) {
    final actTitles = <String>{};
    for (var i = 0; i < actsData.length; i++) {
      final actMap = actsData[i];
      if (actMap is Map) {
        final act = _parseAct(actMap, i, playerPersona, warnings);
        if (actTitles.contains(act.title)) {
          warnings.add(IngestionError(
              'Duplicate act title: "${act.title}"', 'acts[$i]'));
        }
        actTitles.add(act.title);
        actsList.add(act);
      }
    }
  } else {
    warnings.add(IngestionError('No acts defined', 'root'));
  }

  return IngestionResult(
    game: Game(
      title: title,
      summary: summary,
      playerPersona: playerPersona,
      acts: actsList,
    ),
    warnings: warnings,
  );
}

PlayerPersona _parsePlayerPersona(
    Map ppData, List<IngestionError> warnings) {
  final summary = ppData['summary'] as String?;
  final inventory = _parseInventory(ppData['inventory'], 'player_persona', warnings);
  final attributes = _parseAttributes(
      ppData['attributes'], 'player_persona', warnings,
      expectVerbs: true);

  if (attributes.isEmpty) {
    warnings.add(IngestionError(
        'Player persona has no attributes', 'player_persona'));
  }

  return PlayerPersona(
    summary: summary,
    inventory: inventory,
    attributes: attributes,
  );
}

Act _parseAct(Map actMap, int actIndex, PlayerPersona? persona,
    List<IngestionError> warnings) {
  final ctx = 'acts[$actIndex]';
  final title = _requireString(actMap, 'title', ctx, warnings);
  final summary = _requireString(actMap, 'summary', ctx, warnings);

  // Scenes
  final scenes = <Scene>[];
  final scenesData = actMap['scenes'];
  if (scenesData is List) {
    final sceneTitles = <String>{};
    for (var i = 0; i < scenesData.length; i++) {
      final sceneMap = scenesData[i];
      if (sceneMap is Map) {
        final scene =
            _parseScene(sceneMap, i, ctx, persona, warnings);
        if (sceneTitles.contains(scene.title)) {
          warnings.add(IngestionError(
              'Duplicate scene title: "${scene.title}"',
              '$ctx.scenes[$i]'));
        }
        sceneTitles.add(scene.title);
        scenes.add(scene);
      }
    }
  }

  // Locations
  final locations = <Location>[];
  final locsData = actMap['locations'];
  if (locsData is List) {
    for (var i = 0; i < locsData.length; i++) {
      final locMap = locsData[i];
      if (locMap is Map) {
        locations.add(Location(
          title: _requireString(locMap, 'title', '$ctx.locations[$i]', warnings),
          summary:
              _requireString(locMap, 'summary', '$ctx.locations[$i]', warnings),
        ));
      }
    }
  }

  // Characters
  final characters = <Character>[];
  final charsData = actMap['characters'];
  if (charsData is List) {
    for (var i = 0; i < charsData.length; i++) {
      final charMap = charsData[i];
      if (charMap is Map) {
        characters.add(_parseCharacter(charMap, i, ctx, warnings));
      }
    }
  }

  // Validate references
  final locationTitles = locations.map((l) => l.title).toSet();
  final characterTitles = characters.map((c) => c.title).toSet();
  final sceneTitlesSet = scenes.map((s) => s.title).toSet();

  for (final scene in scenes) {
    if (scene.location != null && !locationTitles.contains(scene.location)) {
      warnings.add(IngestionError(
          'Scene "${scene.title}" references unknown location "${scene.location}"',
          ctx));
    }
    for (final cp in scene.charactersPresent) {
      if (!characterTitles.contains(cp)) {
        warnings.add(IngestionError(
            'Scene "${scene.title}" references unknown character "$cp"',
            ctx));
      }
    }
    for (final option in scene.options) {
      if (option.transition != null &&
          !sceneTitlesSet.contains(option.transition)) {
        warnings.add(IngestionError(
            'Option in "${scene.title}" transitions to unknown scene "${option.transition}"',
            ctx));
      }
    }
    if (scene.transitions.defaultScene != null &&
        !sceneTitlesSet.contains(scene.transitions.defaultScene)) {
      warnings.add(IngestionError(
          'Scene "${scene.title}" default transition to unknown scene "${scene.transitions.defaultScene}"',
          ctx));
    }
  }

  return Act(
    title: title,
    summary: summary,
    scenes: scenes,
    locations: locations,
    characters: characters,
  );
}

Scene _parseScene(Map sceneMap, int sceneIndex, String actCtx,
    PlayerPersona? persona, List<IngestionError> warnings) {
  final ctx = '$actCtx.scenes[$sceneIndex]';
  final title = _requireString(sceneMap, 'title', ctx, warnings);
  final summary = _requireString(sceneMap, 'summary', ctx, warnings);
  final location = sceneMap['location'] as String?;
  final charactersPresent = _toStringList(sceneMap['characters_present']);
  final narrative = _requireString(sceneMap, 'narrative', ctx, warnings);

  // Options
  final options = <Option>[];
  final optionsData = sceneMap['options'];
  if (optionsData is List) {
    for (var i = 0; i < optionsData.length; i++) {
      final optMap = optionsData[i];
      if (optMap is Map) {
        options.add(_parseOption(optMap, i, ctx, persona, warnings));
      }
    }
  }

  // Transitions
  Transitions transitions = Transitions();
  final transData = sceneMap['transitions'];
  if (transData is Map) {
    transitions = Transitions(
        defaultScene: transData['default'] as String?);
  }

  return Scene(
    title: title,
    summary: summary,
    location: location,
    charactersPresent: charactersPresent,
    narrative: narrative,
    options: options,
    transitions: transitions,
  );
}

Option _parseOption(Map optMap, int optIndex, String sceneCtx,
    PlayerPersona? persona, List<IngestionError> warnings) {
  final ctx = '$sceneCtx.options[$optIndex]';
  final text = _requireString(optMap, 'text', ctx, warnings);
  final transition = optMap['transition'] as String?;
  final outcome = optMap['outcome'] as String?;
  final onSuccess = optMap['on_success'] as String?;
  final onFail = optMap['on_fail'] as String?;

  SkillCheck? skillCheck;
  final scData = optMap['skill_check'];
  if (scData is Map) {
    final stat = scData['stat'] as String? ?? '';
    final difficulty = (scData['difficulty'] as num?)?.toInt() ?? 1;

    // Validate stat name against player_persona attributes
    if (persona != null) {
      final validStats = persona.attributes.map((a) => a.name).toSet();
      if (!validStats.contains(stat)) {
        warnings.add(IngestionError(
            'Skill check uses unknown stat "$stat"', ctx));
      }
    }

    if (difficulty < 1 || difficulty > 10) {
      warnings.add(IngestionError(
          'Skill check difficulty $difficulty out of range 1-10', ctx));
    }

    skillCheck = SkillCheck(stat: stat, difficulty: difficulty);

    if (onSuccess == null) {
      warnings.add(
          IngestionError('skill_check present but on_success missing', ctx));
    }
    if (onFail == null) {
      warnings.add(
          IngestionError('skill_check present but on_fail missing', ctx));
    }
  }

  return Option(
    text: text,
    transition: transition,
    outcome: outcome,
    skillCheck: skillCheck,
    onSuccess: onSuccess,
    onFail: onFail,
  );
}

Character _parseCharacter(
    Map charMap, int charIndex, String actCtx,
    List<IngestionError> warnings) {
  final ctx = '$actCtx.characters[$charIndex]';
  final title = _requireString(charMap, 'title', ctx, warnings);
  final summary = _requireString(charMap, 'summary', ctx, warnings);
  final role = charMap['role'] as String?;
  final inventory = _parseInventory(charMap['inventory'], ctx, warnings);
  final attributes =
      _parseAttributes(charMap['attributes'], ctx, warnings);

  return Character(
    title: title,
    summary: summary,
    role: role,
    inventory: inventory,
    attributes: attributes,
  );
}

List<Attribute> _parseAttributes(dynamic data, String ctx,
    List<IngestionError> warnings,
    {bool expectVerbs = false}) {
  if (data == null || data is! List) return [];

  final attrs = <Attribute>[];
  for (var i = 0; i < data.length; i++) {
    final attrMap = data[i];
    if (attrMap is! Map) continue;

    final name = attrMap['name'] as String? ?? '';
    final keywords = _toStringList(attrMap['keywords']);
    final verbs = expectVerbs ? _toStringList(attrMap['verbs']) : <String>[];
    final value = (attrMap['value'] as num?)?.toInt() ?? 0;

    if (name.isEmpty) {
      warnings.add(IngestionError('Attribute missing name', '$ctx.attributes[$i]'));
    }
    if (value < 1 || value > 10) {
      warnings.add(IngestionError(
          'Attribute "$name" value $value out of range 1-10',
          '$ctx.attributes[$i]'));
    }

    attrs.add(Attribute(
      name: name,
      keywords: keywords,
      verbs: verbs,
      value: value,
    ));
  }
  return attrs;
}

// Helpers

String _requireString(
    Map data, String key, String ctx, List<IngestionError> warnings) {
  final val = data[key];
  if (val is String && val.isNotEmpty) return val.trim();
  warnings.add(IngestionError('Missing or empty required field "$key"', ctx));
  return '';
}

List<String> _toStringList(dynamic data) {
  if (data is List) {
    return data.whereType<String>().toList();
  }
  return [];
}

/// Parse inventory supporting both simple strings and rich item objects.
List<InventoryItem> _parseInventory(
    dynamic data, String ctx, List<IngestionError> warnings) {
  if (data == null || data is! List) return [];

  final items = <InventoryItem>[];
  for (var i = 0; i < data.length; i++) {
    final item = data[i];
    
    if (item is String) {
      // Simple string format
      items.add(InventoryItem(name: item.trim()));
    } else if (item is Map) {
      // Rich item object format
      final name = item['name'] as String? ?? '';
      final summary = item['summary'] as String?;
      final verbs = _toStringList(item['verbs']);
      
      if (name.isEmpty) {
        warnings.add(IngestionError(
            'Inventory item missing name', '$ctx.inventory[$i]'));
      } else {
        items.add(InventoryItem(
          name: name.trim(),
          summary: summary?.trim(),
          verbs: verbs,
        ));
      }
    }
  }
  return items;
}
