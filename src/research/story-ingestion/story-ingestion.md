```game.toml
# TOML Format Example

title = "Wizard Run"

summary = '''
The player must navigate a tower with infinite rooms and levels, each containing unique challenges, puzzles, and a unique item the player must collect for the final confrontation. The tower is a magical construct created by a powerful wizard, and it serves as a test for adventurers seeking to prove their worth. The player must use their wits, skills, and the items they collect to progress through the tower and ultimately confront the evil wizard at the top.
'''

[player_persona]
description = "The player is a mundane human, possessing no inherent magical or technological abilities."
inventory = ["torch", "rope", "waterskin"]

[[player_persona.attributes]]
name = "STR"
keywords = ["strength", "power", "physical", "combat", "lifting", "endurance"]

[[player_persona.attributes]]
name = "DEX"
keywords = ["dexterity", "agility", "stealth", "precision", "dodge", "reflexes"]

[[player_persona.attributes]]
name = "INT"
keywords = ["intelligence", "knowledge", "reasoning", "logic", "analysis", "learning"]

[[player_persona.attributes]]
name = "WIS"
keywords = ["wisdom", "perception", "insight", "intuition", "awareness", "judgment"]

[[player_persona.attributes]]
name = "CHA"
keywords = ["charisma", "persuasion", "influence", "charm", "leadership", "social"]

[[player_persona.attributes]]
name = "CON"
keywords = ["constitution", "resilience", "stamina", "health", "fortitude", "toughness"]

# Acts are defined as array of tables
[[acts]]
title = "The Tower Entrance"
summary = '''
The player arrives at the base of the tower, which is shrouded in mist and emanates an aura of mystery and danger. The entrance is guarded by a magical barrier that requires the player to swear an oath to defeat the wizard, promising great power and knowledge in exchange for success.
'''

# Scenes within this act
[[acts.scenes]]
title = "Oath of the Tower"
summary = "The player is approached by a spectral guardian who explains the rules of the tower and the oath they must take to enter."
location = "Tower Entrance"
characters_present = ["Spectral Guardian: The mysterious gatekeeper of the tower, bound to serve and guide those who take the oath"]

narrative = '''
The entrance is a grand archway made of ancient stone, covered in mystical runes that glow faintly in the mist. The air is thick with anticipation, and the sound of distant echoes can be heard from within the tower. A spectral figure materializes before you, its translucent form shimmering in the ethereal light.

"Welcome, brave soul," the Spectral Guardian says. "To enter the wizard's tower, you must swear an oath that will shape your journey. Choose wisely—your oath cannot be undone."
'''

# Options for this scene
[[acts.scenes.options]]
text = "Oath of Strength: \"I swear to overcome all challenges through might and martial prowess.\""

[[acts.scenes.options]]
text = "Oath of Wisdom: \"I swear to unravel all mysteries through intellect and cunning.\""

[[acts.scenes.options]]
text = "Oath of Stealth: \"I swear to navigate all dangers through agility and guile.\""

[[acts.scenes.options]]
text = "Oath of Balance: \"I swear to face all trials with a harmony of body, mind, and spirit.\""

# Scene transitions
[acts.scenes.transitions]
default = "First Floor"

# Second scene in this act
[[acts.scenes]]
title = "First Floor"
summary = "The player steps through the barrier and finds themselves in a dimly lit chamber with three doors, each leading to a different path."

narrative = '''
The magical barrier dissolves as you speak your oath, and you step into the tower's first chamber. The stone walls are cold to the touch, and three ornate doors stand before you, each carved with different symbols that pulse with magical energy.
'''

# Options with transitions
[[acts.scenes.options]]
text = "Enter the left door (Strength path)"
transition = "Combat Chamber"
outcome = "You step through into a martial training ground. The door seals behind you."

[[acts.scenes.options]]
text = "Enter the middle door (Wisdom path)"
transition = "Puzzle Room"
outcome = "You step through into a chamber filled with ancient riddles and mechanisms."

[[acts.scenes.options]]
text = "Enter the right door (Stealth path)"
transition = "Shadow Corridor"
outcome = "You step through into a dimly lit passage filled with traps and hidden dangers."

# Options with skill checks
[[acts.scenes.options]]
text = "Examine the door carvings more closely"
skill_check = { stat = "WIS", difficulty = 3 }
on_success = "The player learns that the magic tower exists between multiple demi-planes and joins reality together, revealing hidden knowledge about the paths ahead."
on_fail = "The runes glow brighter, momentarily blinding you. You learn nothing useful."

[[acts.scenes.options]]
text = "Touch the magical symbols"
skill_check = { stat = "INT", difficulty = 2 }
on_success = "The symbols rearrange themselves, revealing their true meaning. You gain insight into the tower's nature."
on_fail = "A surge of magical energy knocks you back. You take 1 damage but are otherwise unharmed."

[acts.scenes.transitions]
default = "First Trial"

# Locations in this act
[[acts.locations]]
title = "Tower Entrance"
summary = "A grand archway made of ancient stone, covered in mystical runes that glow faintly in the mist. The air is thick with anticipation, and the sound of distant echoes can be heard from within the tower."

# Characters in this act
[[acts.characters]]
title = "Spectral Guardian"
summary = "The Spectral Guardian is a mysterious figure who serves as the gatekeeper of the tower. They are bound to the tower and cannot leave, but they can offer guidance and information to those who take the oath."
role = "Gatekeeper and guide for tower entrants"
```

# Tri-Phase Agentic Prototype With Story Ingestion

This prototype demonstrates a tri-phase agentic architecture for interactive storytelling, incorporating story ingestion and retrieval-augmented generation (RAG). The system consists of three main components.

## 1. Story Ingestion

A process that takes raw story text, extracts structured information (entities, relationships, events), and stores it in a vector database for later retrieval.

### File Format

**Prototype Decision:** For this initial prototype, we use a **single monolithic TOML file** (`game.toml`) containing all game content. This simplifies implementation and validation while allowing the format to be proven out. Future versions may support modular file structures.


**Format Benefits:**
- **Deterministic parsing** - TOML v1.0.0 spec provides unambiguous structure
- **Built-in validation** - Standard TOML parsers catch syntax errors immediately
- **Type safety** - Clear data types (strings, arrays, tables, inline tables)
- **LLM-friendly** - Regular patterns in array-of-tables syntax
- **Single-file simplicity** - Easy to validate, version, and distribute

The ingestion process parses the TOML file using a standard parser (e.g., Dart's `toml` package) and converts the structured data directly into entities for the vector database. All game content—acts, scenes, characters, locations, dialogue, and options—is defined within this single file using TOML's array-of-tables notation.

#### game.toml Structure

The root file containing the entire game definition. Uses TOML v1.0.0 syntax with clear hierarchical structure.

**Root-level fields:**

```toml
title = "Game Title"           # Required: Game name
summary = '''...'''            # Required: High-level game description

# Optional: Player character definition
[player_persona]
description = "..."            # Player background and constraints
inventory = ["item1", "item2"] # Starting items (optional)

[[player_persona.attributes]]  # Array of attribute definitions
name = "ATTR1"                 # Attribute abbreviation
keywords = ["word1", "word2"]  # Contextual keywords for LLM

# Acts are defined as array of tables
[[acts]]
title = "Act Title"
summary = '''...'''

# Scenes nested within acts
[[acts.scenes]]
title = "Scene Title"
summary = "..."
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

# Characters in this act
[[acts.characters]]
title = "Character Name"
summary = "Character description"
role = "Character's role in the act"
```

#### Player Persona Table

Optional root-level table defining player character constraints, starting inventory, and the skill attribute system. Unlike character definitions (which describe NPCs), the player persona establishes thematic boundaries, narrative framing, and provides semantic context for the game's attributes.

```toml
[player_persona]
description = '''The player is a poor farmer turned reluctant hero, haunted by the loss of their family. NPCs may reference their humble origins with sympathy or dismissal. Challenges should offer options leveraging resourcefulness despite limited wealth. Decisions addressing loss or preventing similar tragedies carry added emotional weight.'''

inventory = [
  "rusty hoe",
  "worn cloak",
  "family locket"
]

# Attributes with contextual keywords for LLM understanding
[[player_persona.attributes]]
name = "STR"
keywords = ["strength", "power", "physical", "combat", "lifting", "endurance"]

[[player_persona.attributes]]
name = "DEX"
keywords = ["dexterity", "agility", "stealth", "precision", "dodge", "reflexes"]

[[player_persona.attributes]]
name = "INT"
keywords = ["intelligence", "knowledge", "reasoning", "logic", "analysis", "learning"]

[[player_persona.attributes]]
name = "WIS"
keywords = ["wisdom", "perception", "insight", "intuition", "awareness", "judgment"]

[[player_persona.attributes]]
name = "CHA"
keywords = ["charisma", "persuasion", "influence", "charm", "leadership", "social"]

[[player_persona.attributes]]
name = "CON"
keywords = ["constitution", "resilience", "stamina", "health", "fortitude", "toughness"]
```

**Fields:**

- `description` (optional): Player background, personality constraints, and narrative framing guidance for the RAG engine

- `inventory` (optional): Array of starting items the player possesses. These can be referenced in scene narratives and used in gameplay. The LLM can naturally mention relevant inventory items when contextually appropriate.

- `attributes` (required if player_persona present): Array of attribute tables defining the skill system. Each attribute has:
  - `name` (required): Short abbreviation used in skill checks (e.g., "STR", "WIS")
  - `keywords` (required): Contextual keywords that help the small LLM understand what this attribute represents. These words inform narrative flavor generation.

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

#### Acts Array

Acts are defined using TOML's array-of-tables syntax (`[[acts]]`). Each act contains nested scenes, locations, and characters.

```toml
[[acts]]
title = "Act Name"
summary = '''Act summary describing the narrative arc and key events'''

# Scenes within this act
[[acts.scenes]]
title = "Scene Name"
summary = "Scene summary"
location = "Location Name"
characters_present = ["Character 1", "Character 2"]
narrative = '''Narrative description'''

# ... options and dialogue ...

# Locations featured in this act
[[acts.locations]]
title = "Location Name"
summary = "Physical description and atmosphere"

# Characters appearing in this act
[[acts.characters]]
title = "Character Name"
summary = "Physical and personality description"
role = "Character's narrative role in this act"
```

#### Scenes Structure

Scenes are the granular narrative units providing player choices and outcomes. Defined as nested arrays within acts using `[[acts.scenes]]` notation.

```toml
[[acts.scenes]]
title = "Tavern Meeting"
summary = "The player encounters various NPCs in a bustling tavern"
location = "The Broken Wheel Tavern"
characters_present = [
  "Merchant Gareth: A wealthy trader seeking rare goods",
  "Bard Lira: A charismatic performer gathering rumors",
  "Grizzled Veteran: An old warrior nursing a drink"
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
  PlayerPersona? playerPersona;
  List<Act> acts;
}

class PlayerPersona {
  String? description;
  List<String>? inventory;  // ["torch", "rope", "waterskin"]
  List<Attribute> attributes;
}

class Attribute {
  String name;              // e.g., "STR", "WIS"
  List<String> keywords;    // e.g., ["strength", "power", "physical"]
}

class Act {
  String title;
  String summary;
  List<Scene> scenes;
  List<Location> locations;
  List<Character> characters;
}

class Scene {
  String title;
  String summary;
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
}

class Character {
  String title;
  String summary;
  String? role;
}
```

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
  "metadata": {
    "game": "wizard-run",
    "attributes": ["STR", "DEX", "INT", "WIS", "CHA", "CON"],
    "inventory": ["torch", "rope", "waterskin"]
  }
}
```

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
  String content;      // The text to be embedded
  
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
    var possible = playerStat >= option.skillCheck.difficulty;
    
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
  var roll = Random().nextInt(10) + 1 + stat;  // Simple d10 + stat
  
  var success = roll >= option.skillCheck.difficulty;
  
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

A terminal-based user interface that allows users to interact with the system, input commands, and view generated narratives.