/// Domain model classes for the story ingestion prototype.
///
/// These mirror the spec's data structures from story-ingestion.md Section 2.

class InventoryItem {
  final String name;
  final String? summary;
  final List<String> verbs;

  InventoryItem({
    required this.name,
    this.summary,
    this.verbs = const [],
  });

  @override
  String toString() => name;
}

class Attribute {
  final String name;
  final List<String> keywords;
  final List<String> verbs;
  final int value;

  Attribute({
    required this.name,
    required this.keywords,
    this.verbs = const [],
    required this.value,
  });
}

class PlayerPersona {
  final String? summary;
  final List<InventoryItem> inventory;
  final List<Attribute> attributes;

  PlayerPersona({
    this.summary,
    this.inventory = const [],
    required this.attributes,
  });
}

class SkillCheck {
  final String stat;
  final int difficulty;

  SkillCheck({required this.stat, required this.difficulty});
}

class Option {
  final String text;
  final String? transition;
  final String? outcome;
  final SkillCheck? skillCheck;
  final String? onSuccess;
  final String? onFail;

  Option({
    required this.text,
    this.transition,
    this.outcome,
    this.skillCheck,
    this.onSuccess,
    this.onFail,
  });
}

class Transitions {
  final String? defaultScene;

  Transitions({this.defaultScene});
}

class Scene {
  final String title;
  final String summary;
  final String? location;
  final List<String> charactersPresent;
  final String narrative;
  final List<Option> options;
  final Transitions transitions;

  Scene({
    required this.title,
    required this.summary,
    this.location,
    this.charactersPresent = const [],
    required this.narrative,
    this.options = const [],
    Transitions? transitions,
  }) : transitions = transitions ?? Transitions();
}

class Location {
  final String title;
  final String summary;

  Location({required this.title, required this.summary});
}

class Character {
  final String title;
  final String summary;
  final String? role;
  final List<InventoryItem> inventory;
  final List<Attribute> attributes;

  Character({
    required this.title,
    required this.summary,
    this.role,
    this.inventory = const [],
    this.attributes = const [],
  });
}

class Act {
  final String title;
  final String summary;
  final List<Scene> scenes;
  final List<Location> locations;
  final List<Character> characters;

  Act({
    required this.title,
    required this.summary,
    this.scenes = const [],
    this.locations = const [],
    this.characters = const [],
  });
}

class Game {
  final String title;
  final String summary;
  final PlayerPersona? playerPersona;
  final List<Act> acts;

  Game({
    required this.title,
    required this.summary,
    this.playerPersona,
    required this.acts,
  });
}
