# Tri-Phase Agentic Prototype With Story Ingestion

This prototype demonstrates a tri-phase agentic architecture for interactive storytelling, incorporating story ingestion and retrieval-augmented generation (RAG). The system consists of three main components.

## 1. Story Ingestion

A deterministic process that parses `game.toml` into structured data and stores it in a vector database for later retrieval. **Unstructured text ingestion is explicitly out of scope for this prototype.**

### File Format

**Prototype Decision:** For this initial prototype, we use a **single monolithic TOML file** (`game.toml`) containing all game content. This simplifies implementation and validation while allowing the format to be proven out. Future versions may support modular file structures.

**Format Benefits:**
- **Deterministic parsing** - TOML v1.0.0 spec provides unambiguous structure
- **Built-in validation** - Standard TOML parsers catch syntax errors immediately
- **Type safety** - Clear data types (strings, arrays, tables, inline tables)
- **LLM-friendly** - Regular patterns in array-of-tables syntax
- **Single-file simplicity** - Easy to validate, version, and distribute

The ingestion process parses the TOML file using a standard parser (e.g., Dart's `toml` package) and converts the structured data directly into entities for the vector database. All game content—acts, scenes, characters, locations, dialogue, and options—is defined within this single file using TOML's array-of-tables notation. No NLP or LLM-based extraction is used.

#### The `summary` and `prompt` Pattern

Every major object in `game.toml` (game, player persona, acts, scenes, locations, characters) has a required `summary` field and an optional `prompt` field.

- **`summary`** (required): A human-readable description of the entity. This is the canonical text used for vector embedding, chunk content, and direct display. It is the **source of truth** for what this entity is.

- **`prompt`** (optional): A short LLM generation directive that tells the small language model *how* to present or remix this entity's content. When present, `prompt` is passed to the model alongside the `summary` to guide tone, style, or flavor during nondeterministic narrative generation.

The distinction is important: `summary` describes **what** something is; `prompt` describes **how** to narrate it. If `prompt` is omitted, the engine uses `summary` alone and the model applies its default constrained-generation behavior.

**Example:**
```toml
[[acts.locations]]
title = "The Broken Wheel Tavern"
summary = "A dimly lit tavern with low ceilings and the smell of stale ale. Regulars huddle at corner tables."
prompt = "Emphasize the claustrophobic warmth and muffled conversations. Use sensory details: smell, sound, dim light."
```

#### game.toml Structure

The root file containing the entire game definition. Uses TOML v1.0.0 syntax with clear hierarchical structure.

**Root-level fields:**

```toml
title = "Game Title"           # Required: Game name
summary = '''...'''            # Required: High-level game description
prompt = "..."                 # Optional: LLM generation guidance for overall tone

# Optional: Player character definition
[player_persona]
summary = "..."               # Player background and constraints
inventory = []                # Starting items: strings or rich item objects

[[player_persona.attributes]]  # Array of attribute definitions
name = "ATTR1"                 # Attribute abbreviation
keywords = ["word1", "word2"]  # Contextual keywords for LLM
verbs = ["verb1", "verb2"]     # Optional: allowed player verbs for intent matching
value = 5                       # Required: integer value for deterministic checks

# Acts are defined as array of tables
[[acts]]
title = "Act Title"
summary = '''...'''
prompt = "..."                 # Optional: LLM generation guidance for this act

# Scenes nested within acts
[[acts.scenes]]
title = "Scene Title"
summary = "..."
prompt = "..."                 # Optional: LLM generation guidance for this scene
location = "Location Name"
characters_present = ["Character Name"]
narrative = '''Freeform narrative text combining description, dialogue, and atmosphere. Can be purely descriptive or include character dialogue inline.'''

# Options for this scene
[[acts.scenes.options]]
text = "Option text"
transition = "Target Scene"     # Optional: scene to transition to
outcome = "Outcome text"        # Optional: immediate result description
skill_check = { stat = "WIS", difficulty = 3 }  # Optional: skill check
on_success = "Success text"    # Required if skill_check present
on_fail = "Failure text"       # Required if skill_check present

# Scene transitions
[acts.scenes.transitions]
default = "Default Scene Name"

# Locations in this act
[[acts.locations]]
title = "Location Name"
summary = "Location description"
prompt = "..."                       # Optional: LLM generation guidance

# Characters in this act
[[acts.characters]]
title = "Character Name"
summary = "Character description"
prompt = "..."                       # Optional: LLM generation guidance
role = "Character's role in the act"
inventory = ["item1", "item2"]     # Required: can be empty, strings, or rich item objects
attributes = []                      # Required if no attributes are defined below

[[acts.characters.attributes]]
name = "ATTR1"                       # Required if attributes present
keywords = ["word1", "word2"]       # Required if attributes present
value = 10                            # Required if attributes present
```

#### Player Persona Table

Optional root-level table defining player character constraints, starting inventory, and the skill attribute system. Unlike character definitions (which describe NPCs), the player persona establishes thematic boundaries, narrative framing, and provides semantic context for the game's attributes.

```toml
[player_persona]
summary = '''The player is a poor farmer turned reluctant hero, haunted by the loss of their family. NPCs may reference their humble origins with sympathy or dismissal. Challenges should offer options leveraging resourcefulness despite limited wealth. Decisions addressing loss or preventing similar tragedies carry added emotional weight.'''
prompt = "Frame the player as weary but determined. Use language of labor and hardship for physical descriptions."

# Inventory can use simple strings
inventory = [
  "rusty hoe",
  "worn cloak",
  "family locket"
]
# OR rich item objects
inventory = [
  { name = "rusty hoe", summary = "An old farming tool, worn smooth from years of field work. Still useful for both digging and defense.", verbs = ["dig", "swing", "lean"] },
  { name = "worn cloak", summary = "A thick wool cloak in faded brown, mended many times. Keeps out wind and provides comfort.", verbs = ["wrap", "hide", "wear"] },
  { name = "family locket", summary = "A small tin locket containing a portrait you cannot quite make out. The metal is warm to the touch.", verbs = ["open", "read", "hold"] }
]

# Attributes with contextual keywords for LLM understanding
[[player_persona.attributes]]
name = "STR"
keywords = ["strength", "power", "physical", "combat", "lifting", "endurance"]
verbs = ["attack", "strike", "bash", "shove", "lift", "break"]
value = 6

[[player_persona.attributes]]
name = "DEX"
keywords = ["dexterity", "agility", "stealth", "precision", "dodge", "reflexes"]
verbs = ["dodge", "evade", "sneak", "hide", "pickpocket", "disable"]
value = 6

[[player_persona.attributes]]
name = "INT"
keywords = ["intelligence", "knowledge", "reasoning", "logic", "analysis", "learning"]
verbs = ["study", "analyze", "decode", "solve", "deduce", "plan"]
value = 6

[[player_persona.attributes]]
name = "WIS"
keywords = ["wisdom", "perception", "insight", "intuition", "awareness", "judgment"]
verbs = ["notice", "sense", "read", "track", "observe", "listen"]
value = 6

[[player_persona.attributes]]
name = "CHA"
keywords = ["charisma", "persuasion", "influence", "charm", "leadership", "social"]
verbs = ["persuade", "charm", "intimidate", "lead", "command", "inspire"]
value = 6

[[player_persona.attributes]]
name = "CON"
keywords = ["constitution", "resilience", "stamina", "health", "fortitude", "toughness"]
verbs = ["endure", "resist", "withstand", "hold", "push", "shrug"]
value = 6
```

**Fields:**

- `summary` (required): Player background, personality constraints, and narrative framing guidance for the RAG engine

- `prompt` (optional): LLM generation directive guiding how the model should narrate the player's actions and state. Used at ingestion time to pre-generate flavor variations.

- `inventory` (optional): Array of starting items. Each item can be either a simple string name or a rich object with `name`, `summary`, and `verbs` fields. Rich items provide detailed descriptions and interaction hints to the player.

- `attributes` (required if player_persona present): Array of attribute tables defining the skill system. Each attribute has:
  - `name` (required): Short abbreviation used in skill checks (e.g., "STR", "WIS")
  - `keywords` (required): Contextual keywords that help the small LLM understand what this attribute represents. These words inform narrative flavor generation.
  - `verbs` (optional): Allowed player input verbs that map to this attribute for freeform checks.
  - `value` (required): Integer used for deterministic skill checks.

**Attribute Keyword Usage:**

The keywords serve multiple purposes:

1. **LLM Context**: When generating narrative text, the model uses keywords to choose appropriate descriptive language. For example, with `STR keywords = ["strength", "power", "physical"]`, low STR might generate "you lack the physical power" rather than inventing unrelated descriptions.

2. **Validation**: The ingestion process can verify that skill checks only reference defined attribute names.

3. **Theme Customization**: Authors can create entirely custom attribute systems:

**Fantasy Example:**
```toml
[[player_persona.attributes]]
name = "STR"
keywords = ["strength", "power", "might", "combat", "physical"]
```

**Sci-fi Example:**
```toml
[[player_persona.attributes]]
name = "TECH"
keywords = ["technology", "engineering", "hacking", "systems", "circuits"]

[[player_persona.attributes]]
name = "PILOT"
keywords = ["piloting", "navigation", "spatial awareness", "reflexes", "control"]
```

**Mystery Example:**
```toml
[[player_persona.attributes]]
name = "LOGIC"
keywords = ["reasoning", "deduction", "analysis", "patterns", "clues"]

[[player_persona.attributes]]
name = "INTUITION"
keywords = ["instinct", "gut feeling", "hunches", "reading people", "subconscious"]
```

**Horror Example:**
```toml
[[player_persona.attributes]]
name = "NERVE"
keywords = ["courage", "composure", "sanity", "fear resistance", "willpower"]

[[player_persona.attributes]]
name = "AWARENESS"
keywords = ["perception", "noticing", "alertness", "danger sense", "observation"]
```

The attribute names define what's valid in skill checks throughout `[[acts.scenes.options]]`, while keywords enrich the LLM's understanding of each attribute's semantic meaning.

#### Identity and Reference Rules

To keep ingestion deterministic, all references use **canonical titles** with strict matching. The ingestion layer also generates stable IDs for chunks.

**Canonical titles**
- Titles are the canonical identifiers used in references (e.g., `location = "Tower Entrance"`).
- Titles must be unique within their scope:
  - `Act.title` must be unique in the game.
  - `Scene.title` must be unique within its act.
  - `Character.title` and `Location.title` must be unique within their act.
- References are **exact string matches** (case-sensitive) to titles in the same act unless otherwise noted.

**Scene references**
- `transition` and `[acts.scenes.transitions].default` must match a `Scene.title` in the **same act**.
- Cross-act transitions are not supported in this prototype.

**Character references**
- `characters_present` is an array of `Character.title` values only (no inline descriptions). Any descriptive text belongs in the character definition or scene narrative.

**Location references**
- `location` must match a `Location.title` in the same act.

**Chunk IDs (generated, deterministic)**
- Game: `game:{slug(game_title)}`
- Act: `act:{slug(game_title)}:{slug(act_title)}`
- Scene: `scene:{slug(game_title)}:{slug(act_title)}:{slug(scene_title)}`
- Character: `character:{slug(game_title)}:{slug(act_title)}:{slug(character_title)}`
- Location: `location:{slug(game_title)}:{slug(act_title)}:{slug(location_title)}`

**Slug rules**
- Lowercase, trim, replace any run of non-alphanumeric characters with a single `-`.
- Strip leading/trailing `-`.
- Example: "Oath of the Tower" -> `oath-of-the-tower`.

#### Acts Array

Acts are defined using TOML's array-of-tables syntax (`[[acts]]`). Each act contains nested scenes, locations, and characters.

```toml
[[acts]]
title = "Act Name"
summary = '''Act summary describing the narrative arc and key events'''
prompt = "Optional guidance for how to narrate this act's events"

# Scenes within this act
[[acts.scenes]]
title = "Scene Name"
summary = "Scene summary"
prompt = "Optional guidance for scene narration tone and style"
location = "Location Name"
characters_present = ["Character 1", "Character 2"]
narrative = '''Narrative description'''

# ... options and dialogue ...

# Locations featured in this act
[[acts.locations]]
title = "Location Name"
summary = "Physical description and atmosphere"
prompt = "Optional guidance for how to describe this location"

# Characters appearing in this act
[[acts.characters]]
title = "Character Name"
summary = "Physical and personality description"
prompt = "Optional guidance for how to portray this character"
role = "Character's narrative role in this act"
inventory = []
attributes = []
```

#### Scenes Structure

Scenes are the granular narrative units providing player choices and outcomes. Defined as nested arrays within acts using `[[acts.scenes]]` notation.

```toml
[[acts.scenes]]
title = "Tavern Meeting"
summary = "The player encounters various NPCs in a bustling tavern"
location = "The Broken Wheel Tavern"
characters_present = [
  "Merchant Gareth",
  "Bard Lira",
  "Grizzled Veteran"
]

narrative = '''
The tavern is warm and bustling with the evening crowd. Gareth sits in the corner, eyeing newcomers with interest. Lira plays a haunting melody in the corner.

Gareth catches your eye and calls out: "I'm looking for someone brave enough to retrieve something... valuable. Interested?"

Lira sets down her lute as you pass. "Heard any good stories on your travels? The people here are always hungry for tales."

The Grizzled Veteran glances up from his drink. "You've got the look of someone who's seen some action. Buy me a drink and I'll tell you what I know about the roads ahead."
'''

# Player options
[[acts.scenes.options]]
text = "Approach Gareth and ask about his offer"
transition = "Merchant's Deal"
outcome = "You make your way to Gareth's table. He sizes you up with a calculating gaze."

[[acts.scenes.options]]
text = "Sit with Lira and exchange stories"
outcome = "Lira smiles warmly as you approach. She sets down her lute, eager to hear your tales."

[[acts.scenes.options]]
text = "Join the Grizzled Veteran at his table"
transition = "War Stories"
outcome = "The veteran nods approvingly as you sit down. He signals the barkeep for two drinks."

[[acts.scenes.options]]
text = "Order a drink and observe the room"
outcome = "You settle at the bar with a mug of ale, watching the patrons carefully. Nothing suspicious catches your eye."

# Options with skill checks
[[acts.scenes.options]]
text = "I heard rumors of bandits on the southern road. Is that true?"
skill_check = { stat = "WIS", difficulty = 3 }
on_success = "The veteran's expression hardens. 'Aye, more than bandits. Something darker moves in those hills,' he says, leaning closer to share what he knows."
on_fail = "The room goes quiet for a moment. You've asked the wrong question. The patrons return to their drinks, but you feel eyes watching you."

[[acts.scenes.options]]
text = "That tune you're playing—I recognize it from the old days."
transition = "Bard's Tale"
skill_check = { stat = "CHA", difficulty = 4 }
on_success = "Lira's eyes light up. 'You know the Song of the Forgotten? Then you must hear the full story,' she says, launching into an ancient ballad."
on_fail = "Lira looks puzzled. 'I don't think you're remembering correctly, friend. But I appreciate your interest.'"

[[acts.scenes.options]]
text = "Attempt to pickpocket Gareth's coin purse"
skill_check = { stat = "DEX", difficulty = 5 }
on_success = "Your fingers deftly slip the coin purse from his belt. He doesn't notice, but you catch the veteran watching you with disapproval."
on_fail = "Gareth's hand clamps down on your wrist. 'I don't take kindly to thieves,' he growls."

# Default transition if no option specifies one
[acts.scenes.transitions]
default = "Tavern Night"
```

**Option Fields:**
- `text` (required): The action or dialogue the player chooses
- `transition` (optional): Target scene name to transition to
- `outcome` (optional): Immediate narrative result of the choice
- `skill_check` (optional): Inline table with `stat` and `difficulty` fields
- `on_success` (required if `skill_check` present): Success outcome text
- `on_fail` (required if `skill_check` present): Failure outcome text

**Option Resolution Order:**
1. If the chosen option has `skill_check`, resolve the check and present `on_success` or `on_fail`.
2. If the chosen option has `outcome`, present it after any skill check result.
3. If the chosen option has `transition`, move to that scene.
4. Otherwise, fall back to `[acts.scenes.transitions].default`.

If a `skill_check` is present, `on_success` and `on_fail` are required. `transition` can be used with or without `skill_check`.

#### Skill System Defaults

To keep this prototype consistent and easy to validate, use the following defaults unless overridden by a future spec:

- Attribute values are integers in the range 1-10.
- `difficulty` is an integer in the range 1-10.
- Skill check is deterministic: compare the attribute `value` to `difficulty` and succeed on `value >= difficulty`.
- UI hinting uses the same threshold check: if `value >= difficulty`, mark the option as "likely"; otherwise "risky".

#### Freeform Skill Checks (Player Input)

The system must support player-typed actions that are not pre-authored options (for example: `> attack leory`). These checks are deterministic and the LLM only narrates the already-determined outcome.

**Flow**
1. Parse intent and target (e.g., `attack` + `Leory`).
2. Require a target. If missing, return a prompt like "Study what?".
3. Validate verb and target:
   - If verb is not in any `player_persona.attributes[*].verbs`, return "you can't do that".
   - If target is not present in the current scene, return "that isn't here".
4. Allow the player to choose which stat to roll with. The verb is only used for intent matching and narration flavor.
3. Validate target attributes:
  - If target has `attributes = []`, return an error and do not roll (e.g., "you cannot roll to ATTACK Leory").
5. Determine outcome deterministically:
  - Target defense uses relevant stat mirror: `defense = target[chosen_stat].value`.
  - If the target lacks the chosen stat, use `defense = max(target.attributes[].value)`.
  - Succeed if `player_stat.value >= defense`.
6. Generate narration using the LLM based only on the computed outcome.

**LLM role**
- The LLM never decides success or failure.
- The LLM receives the action, target, and computed outcome, then narrates 2-3 sentences.

**Prompt structure (suggested)**
- `ACTION`: verb + target
- `STAT`: chosen attribute name and value
- `RESULT`: success/failure
- `CONTEXT`: current scene narrative and any relevant character/location notes
- `CONSTRAINTS`: max two brief paragraphs; no new facts

**Example (freeform)**
- Player input: `> attack leory`
- Player chooses stat: `STR`
- Deterministic result: failure
- LLM output: short narrative of Leory overwhelming the player.

#### Character Inventory and Attributes

All `[[acts.characters]]` entries must define:

- `inventory` (may be an empty array)
- `attributes` (either an empty array, or an array of attribute tables with `name`, `keywords`, and `value`)

Characters do not define `verbs`. Verbs are only used on `player_persona` for intent matching.

If a character has `attributes = []`, any attempt to roll against that character fails validation and must return an error (for example, "you cannot roll to ATTACK Leory").

To make rolls against a character almost certainly fail (but still technically possible), define very high relevant attributes for that character and use `inventory` to hold any rare or story-sensitive loot that could appear on a success.

## 2. Data Structures and Storage

### Parsing TOML into Domain Objects

The ingestion process parses `game.toml` into structured domain objects that represent the game's narrative graph. Using Dart's `toml` package:

```dart
// Parse TOML file
var tomlDoc = TomlDocument.loadSync('game.toml');
var data = tomlDoc.toMap();

// Create domain objects
var game = Game(
  title: data['title'],
  summary: data['summary'],
  playerPersona: data['player_persona'] != null 
    ? PlayerPersona.fromMap(data['player_persona'])
    : null,
  acts: (data['acts'] as List).map((a) => Act.fromMap(a)).toList(),
);
```

**Core Domain Objects:**

```dart
class Game {
  String title;
  String summary;
  String? prompt;              // Optional: LLM generation guidance
  PlayerPersona? playerPersona;
  List<Act> acts;
}

class PlayerPersona {
  String summary;
  String? prompt;            // Optional: LLM generation guidance
  List<InventoryItem>? inventory;  // Can be simple strings or rich items
  List<Attribute> attributes;
}

class InventoryItem {
  String name;               // Item name
  String? summary;           // Optional: detailed description
  List<String>? verbs;       // Optional: allowed interactions (e.g., ["light", "burn"])
  
  // Constructor supports both string and object formats
  InventoryItem(this.name, {this.summary, this.verbs});
}

class Attribute {
  String name;              // e.g., "STR", "WIS"
  List<String> keywords;    // e.g., ["strength", "power", "physical"]
  int value;                // e.g., 6
}

class Act {
  String title;
  String summary;
  String? prompt;              // Optional: LLM generation guidance
  List<Scene> scenes;
  List<Location> locations;
  List<Character> characters;
}

class Scene {
  String title;
  String summary;
  String? prompt;              // Optional: LLM generation guidance
  String? location;
  List<String> charactersPresent;
  String narrative;
  List<Option> options;
  Transitions transitions;
}

class Option {
  String text;
  String? transition;
  String? outcome;
  SkillCheck? skillCheck;
  String? onSuccess;
  String? onFail;
}

class SkillCheck {
  String stat;       // e.g., "WIS", "DEX"
  int difficulty;    // e.g., 3, 5
}

class Transitions {
  String? defaultScene;
}

class Location {
  String title;
  String summary;
  String? prompt;              // Optional: LLM generation guidance
}

class Character {
  String title;
  String summary;
  String? prompt;              // Optional: LLM generation guidance
  String? role;
  List<InventoryItem>? inventory;  // Can be strings or rich items
  List<Attribute> attributes;
}
```

#### Parsing Rich Inventory Items

The inventory field can contain either simple strings or rich item objects. When parsing TOML, handle both formats:

```dart
List<InventoryItem> parseInventory(dynamic rawInventory) {
  if (rawInventory == null) return [];
  
  return (rawInventory as List).map((item) {
    if (item is String) {
      // Simple string format
      return InventoryItem(item);
    } else if (item is Map) {
      // Rich item format with name, summary, verbs
      return InventoryItem(
        item['name'] as String,
        summary: item['summary'] as String?,
        verbs: (item['verbs'] as List<dynamic>?)?.cast<String>(),
      );
    }
    throw FormatException('Invalid inventory item: $item');
  }).toList();
}
```

This enables authors to mix formats in the same game and provides graceful backward compatibility with existing simple-string inventories.

### Vector Database Storage Strategy

The parsed domain objects are converted into **chunks** suitable for vector embedding and retrieval. Each entity type becomes a separate document in the vector database with rich metadata for filtering and relationship traversal.

#### Chunking Strategy

**1. Game-Level Chunk (Single Document)**
```json
{
  "id": "game:wizard-run",
  "type": "game",
  "title": "Wizard Run",
  "content": "The player must navigate a tower with infinite rooms...",
  "prompt": null,
  "metadata": {
    "has_player_persona": true,
    "act_count": 3,
    "attributes": ["STR", "DEX", "INT", "WIS", "CHA", "CON"],
    "starting_inventory": ["torch", "rope", "waterskin"]
  }
}
```

**2. Act Chunks (One per Act)**
```json
{
  "id": "act:tower-entrance",
  "type": "act",
  "title": "The Tower Entrance",
  "content": "The player arrives at the base of the tower...",
  "prompt": "Set a tone of ancient mystery and foreboding grandeur.",
  "metadata": {
    "game": "wizard-run",
    "act_index": 0,
    "scene_count": 2,
    "location_count": 1,
    "character_count": 1
  }
}
```

**3. Scene Chunks (One per Scene)**
```json
{
  "id": "scene:oath-of-the-tower",
  "type": "scene",
  "title": "Oath of the Tower",
  "content": "The entrance is a grand archway... 'Welcome, brave soul,' the Spectral Guardian says...",
  "prompt": null,
  "metadata": {
    "game": "wizard-run",
    "act": "tower-entrance",
    "location": "Tower Entrance",
    "characters": ["Spectral Guardian"],
    "has_skill_checks": false,
    "option_count": 4,
    "default_transition": "First Floor"
  }
}
```

**4. Character Chunks (One per Character)**
```json
{
  "id": "character:spectral-guardian",
  "type": "character",
  "title": "Spectral Guardian",
  "content": "The Spectral Guardian is a mysterious figure who serves as the gatekeeper...",
  "prompt": "Speak in a hollow, echoing voice. Use formal, archaic phrasing.",
  "metadata": {
    "game": "wizard-run",
    "act": "tower-entrance",
    "role": "Gatekeeper and guide for tower entrants",
    "appears_in_scenes": ["oath-of-the-tower"]
  }
}
```

**5. Location Chunks (One per Location)**
```json
{
  "id": "location:tower-entrance",
  "type": "location",
  "title": "Tower Entrance",
  "content": "A grand archway made of ancient stone, covered in mystical runes...",
  "prompt": "Emphasize scale and the weight of ages. Use cold, mineral imagery.",
  "metadata": {
    "game": "wizard-run",
    "act": "tower-entrance",
    "featured_in_scenes": ["oath-of-the-tower", "first-floor"]
  }
}
```

**6. Player Persona Chunk (Single Document)**
```json
{
  "id": "player-persona:wizard-run",
  "type": "player_persona",
  "title": "Player Character",
  "content": "The player is a mundane human, possessing no inherent magical or technological abilities.",
  "prompt": "Frame the player as weary but determined. Use language of labor and hardship.",
  "metadata": {
    "game": "wizard-run",
    "attributes": ["STR", "DEX", "INT", "WIS", "CHA", "CON"],
    "inventory": ["torch", "rope", "waterskin"]
  }
}
```

#### Metadata Schema (JSON)

All chunks store JSON metadata with consistent keys for filtering. Use only the keys listed here.

- Common: `game`, `type`, `title`, `prompt`
- Game: `has_player_persona`, `act_count`, `attributes`, `starting_inventory`
- Act: `act_index`, `scene_count`, `location_count`, `character_count`
- Scene: `act`, `location`, `characters`, `has_skill_checks`, `option_count`, `default_transition`
- Character: `act`, `role`, `appears_in_scenes`
- Location: `act`, `featured_in_scenes`

All list values are arrays of canonical titles (not slugs), and `game`/`act` values use slugs.

### Embedding and Indexing

Each chunk's `content` field is passed through an embedding model (e.g., a 1.5B sentence transformer) to create a dense vector representation. These vectors enable semantic search during gameplay.

**Vector Database Schema (e.g., using ObjectBox Vector Search or similar):**

```dart
@Entity()
class GameChunk {
  @Id()
  int id = 0;
  
  String chunkId;      // e.g., "scene:oath-of-the-tower"
  String type;         // e.g., "scene", "character", "location"
  String title;
  String content;      // The text to be embedded (from summary)
  String? prompt;      // Optional: LLM generation directive
  
  @Property(type: PropertyType.floatVector)
  List<double>? embedding;  // 384-dim or 768-dim vector
  
  String metadataJson; // Store metadata as JSON for filtering
  String game;         // Denormalized for fast filtering
}
```

### Retrieval Strategy During Gameplay

When the player is in a scene, the RAG engine retrieves relevant context:

```dart
Future<List<GameChunk>> getRelevantContext(String currentScene, String playerInput) async {
  // currentScene is the canonical Scene.title (not a slug)
  // 1. Get current scene (exact match)
  var scene = await db.query(GameChunk_()
    ..chunkId.equals('scene:$currentScene')).find();
  
  // 2. Semantic search for player input
  var inputEmbedding = await embedder.embed(playerInput);
  var semanticMatches = await db.findNearestNeighbors(
    embedding: inputEmbedding,
    maxResults: 5,
    filter: GameChunk_()..game.equals(currentGameId)
  );
  
  // 3. Get related entities (characters, locations in current scene)
  var relatedChunks = await db.query(GameChunk_()
      ..type.oneOf(['character', 'location'])
      ..game.equals(currentGameId)
      ..metadataJson.contains(currentScene)
    ).find();
  
  return [scene, ...semanticMatches, ...relatedChunks];
}
```

### Benefits of This Approach

1. **Semantic Search** - Player questions like "Who is the guardian?" find relevant character chunks
2. **Contextual Filtering** - Only retrieve chunks from current game/act
3. **Relationship Traversal** - Metadata enables graph-like queries (e.g., "all characters in this act")
4. **Scalable** - Each chunk is independently embeddable and searchable
5. **Deterministic Navigation** - Scene options provide explicit next-scene transitions
6. **Flexible Generation** - Narrative chunks provide example tone/style for LLM to emulate

## 3. RAG-Enhanced Engine

The narrative generation engine uses a small language model (≤1B parameters) constrained by authored content to generate dynamic, contextually appropriate responses. The key design principle: **authored content is law, not suggestion**.

### Constraint-Based Generation Strategy

Small models (like Phi-3-mini 1B, Qwen2.5 0.5B, or Llama 3.2 1B) can effectively stay faithful to source material when properly constrained:

**1. Retrieved Content as Hard Rules**
- Scene narrative = canonical description (must not contradict)
- Character definitions = personality boundaries (must stay in character)
- Location details = environmental constraints (cannot add contradictory elements)
- Options = exhaustive choice list (cannot invent new options)

**2. Game State as Flavor Injection**
- Player attributes → narrative modifiers (low STR = exhaustion, high WIS = insights)
- Inventory → mention relevant items naturally
- Previous choices → reference past decisions for continuity
- Skill check results → reflect success/failure outcomes

**3. Generation is Remixing, Not Creating**
The model doesn't invent story beats—it **presents authored content filtered through current game state**.

### Prompt Structure for Constrained Generation

The key is structuring the prompt so the model understands its role is **translation and flavor**, not invention.

**Example Prompt Template:**

```
# ROLE
You are a narrative presenter for an interactive story. Your job is to present the author's pre-written content naturally while reflecting the player's current state.

# RULES
- NEVER invent story events, character actions, or new locations
- ONLY use information from the SCENE CONTENT below
- Add natural flavor based on PLAYER STATE without changing facts
- Keep responses 2-3 sentences maximum
- Match the tone and style of the scene content

# PLAYER STATE
Name: [Player name or "You"]
Location: Mountain Pass
Attributes: STR=2 (strength, power, physical), DEX=5 (agility, reflexes), WIS=6 (perception, insight)
Recent: [Just fought bandits, took damage]
Inventory: [rope, torch, waterskin (empty)]

# SCENE CONTENT
Title: Mountain Ascent
Location: Rocky mountain path with steep cliffs and thin air
Narrative: "The path winds upward between jagged rocks. Cold wind howls through the pass. Your legs burn from the climb, and your breath comes in ragged gasps. The summit is still hours away."
Characters Present: None

# AUTHOR'S OPTIONS
1. Push forward despite exhaustion
2. Rest here and catch your breath
3. Look for an easier route (requires WIS 4)

# PLAYER ACTION
[Player selected: "Push forward despite exhaustion"]

# YOUR TASK
Present this scene moment naturally, incorporating:
- Player's low STR (2) making the climb harder
- Player's recent combat and injury
- The author's description of exhaustion
- The fact they chose to push forward

Keep the author's facts unchanged. Add appropriate fatigue details.
```

**Model Response (1B model):**

> "Your legs tremble with each step up the rocky path. The cold wind bites at your face as you push onward, ignoring the burning in your muscles and the ache from the bandit's blade. Your breath comes in short, painful gasps—the thin mountain air offering little relief. The summit gleams in the distance, still hours of grueling climb ahead."

**Analysis:**
- ✅ Uses author's exact environmental details (rocky path, cold wind, burning, ragged gasps)
- ✅ Incorporates low STR (trembling legs, struggling)
- ✅ References recent combat (ache from bandit's blade)
- ✅ Maintains authored tone (atmospheric, challenging)
- ❌ Does NOT invent new story elements (no sudden avalanche, no rescue eagle)

### Attribute-Based Narrative Modifiers

The game state modifies **how** authored content is presented, not **what** happens:

**Low STR (1-3):**
- Physical challenges: "struggle," "strain," "barely manage"
- Combat: "your blow glances off," "you stumble back"
- Exertion: "exhausted," "trembling," "gasping"

**High STR (8-10):**
- Physical challenges: "easily," "with confidence," "powerful"
- Combat: "your strike lands solidly," "you stand firm"
- Exertion: "breathing steadily," "hardly winded"

**Low WIS (1-3):**
- Observations: "you notice nothing unusual," "it seems ordinary"
- Insight: "you're puzzled," "the meaning escapes you"
- Social: "you miss the subtext," "their expression is unreadable"

**High WIS (8-10):**
- Observations: "you notice [detail]," "something feels off"
- Insight: "you realize [connection]," "the pattern becomes clear"
- Social: "you sense their hesitation," "you read their intent"

**Implementation Example:**

```dart
String getAttributeModifier(String attribute, int value, String context) {
  // For STR in physical context
  if (attribute == 'STR' && context == 'exertion') {
    if (value <= 3) return 'Your muscles burn with effort. ';
    if (value >= 8) return 'You handle the strain easily. ';
  }
  
  // For WIS in observation context
  if (attribute == 'WIS' && context == 'observation') {
    if (value <= 3) return 'Nothing seems unusual. ';
    if (value >= 8) return 'You notice subtle details others might miss. ';
  }
  
  return ''; // No modifier for mid-range attributes
}
```

### Scene Presentation Flow

**1. Retrieve Context**
```dart
var chunks = await getRelevantContext(currentScene, playerInput);
var sceneChunk = chunks.firstWhere((c) => c.type == 'scene');
var characterChunks = chunks.where((c) => c.type == 'character').toList();
var locationChunk = chunks.firstWhere((c) => c.type == 'location', orElse: null);
```

**2. Build Constrained Prompt**
```dart
var prompt = '''
# SCENE CONTENT
${sceneChunk.content}

# PLAYER STATE
${formatPlayerState(gameState)}

# RECENT EVENTS
${gameState.recentHistory.join('\n')}

# YOUR TASK
Present the scene naturally, reflecting the player's ${getRelevantAttributes(sceneChunk)} while using ONLY the authored content above.
''';
```

**3. Generate with Temperature Constraints**
```dart
var response = await smallLLM.generate(
  prompt,
  temperature: 0.3,  // Low temperature = stay close to source material
  maxTokens: 100,    // Force brevity
  stopSequences: ['\n\n', '---'],  // Prevent rambling
);
```

**4. Validate Output**
```dart
// Simple heuristic checks
if (response.contains('dragon') && !sceneChunk.content.contains('dragon')) {
  // Model invented something - retry with stronger constraints
  return await regenerateWithStricterPrompt();
}
```

### Options as Guardrails

The authored options serve as **hard boundaries** for player agency:

```dart
// When presenting options, use exact authored text
for (var option in scene.options) {
  // Check if skill check is possible
  if (option.skillCheck != null) {
    var playerStat = gameState.attributes[option.skillCheck.stat];
    var possible = playerStat.value >= option.skillCheck.difficulty;
    
    // Present option with contextual flavor
    print('${option.text} ${possible ? '' : '(requires ${option.skillCheck.stat} ${option.skillCheck.difficulty})'}');
  } else {
    print(option.text);
  }
}

// NEVER allow model to generate new options
// Player can ONLY choose from authored options
```

### Handling Skill Checks with Authored Outcomes

When a skill check occurs, use **only** the author's success/fail text:

```dart
Future<String> resolveSkillCheck(Option option, Player player) async {
  var stat = player.attributes[option.skillCheck.stat];
  var success = stat.value >= option.skillCheck.difficulty;
  
  // Use authored outcome text directly
  var outcomeText = success ? option.onSuccess : option.onFail;
  
  // Model only adds natural-language framing
  var prompt = '''
Present this skill check result naturally:
Stat: ${option.skillCheck.stat} (player has $stat)
Result: ${success ? 'SUCCESS' : 'FAILURE'}
Outcome: $outcomeText

Add 1 sentence describing the attempt, then present the outcome.
''';
  
  return await smallLLM.generate(prompt, temperature: 0.4, maxTokens: 80);
}
```

**Example Output:**

> "You carefully examine the ancient runes, drawing on your knowledge of forgotten languages. The symbols rearrange themselves, revealing their true meaning. You gain insight into the tower's nature."

- Sentence 1: Generated flavor (examining, using knowledge)
- Sentence 2-3: Author's exact `onSuccess` text

### Benefits of Constraint-Based Generation

1. **Author Control** - Story beats remain exactly as written
2. **Dynamic Feel** - Player state creates natural variation in presentation
3. **Small Model Viability** - Simple task (remix/present) vs. creative writing
4. **Deterministic Outcomes** - Skill checks use authored success/fail text
5. **Consistent Quality** - Authored prose sets the quality bar
6. **Fast Inference** - 1B models run locally, <100ms generation time

### Fallback: Template-Based Generation

For even smaller models (<500M) or ultra-constrained environments, use template-based generation:

```dart
String presentScene(Scene scene, PlayerState state) {
  var parts = <String>[];
  
  // Environment (always)
  parts.add(scene.narrative);
  
  // Attribute-based observation
  if (state.attributes['WIS'] >= 6 && scene.location != null) {
    parts.add('You notice ${getLocationDetail(scene.location)}.');
  }
  
  // Physical state reflection
  if (state.health < 50 && scene.narrative.contains('climb')) {
    parts.add('Your injuries make every step more difficult.');
  }
  
  return parts.join(' ');
}
```

This hybrid approach uses the LLM only for complex narrative moments, falling back to deterministic templates for routine presentation.

## 4. TUI Interface

A terminal-based user interface that allows users to interact with the system, input commands, and view generated narratives.

### Inventory Command

When the player enters `inv` or `inventory`, the system displays all carrying items in a readable format. For each item:

1. **Item name** (bold or emphasized)
2. **Summary** (if present) on the next line, indented
3. **Verbs** (if present) on the next line, formatted as: `Can be used to: verb1, verb2, verb3`

**Display Format Example:**

```
Torch
A sturdy wooden handle with a bundle of kindling at the top, used to illuminate a room and provide warmth.
Can be used to: light, burn, illuminate

Rope
A coil of strong hemp rope, worn but serviceable. Could be used for climbing or securing objects.
Can be used to: tie, climb, secure, lasso

Waterskin
A canvas waterskin, currently full of cool water. Essential for long journeys.
Can be used to: drink, pour, fill
```

**Implementation Notes:**

- If an item has no `summary`, only display the name and verbs (if present)
- If an item has no `verbs`, only display the name and summary (if present)
- Simple string inventory items (legacy format) display as just the name with no additional details
- The verbs list provides semantic hints to the player about how they might interact with objects in freeform input