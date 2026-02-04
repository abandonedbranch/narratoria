# Example: Complete Campaign Structure

This example shows a **fully-authored** Narratoria campaign with defined NPCs, plot beats, lore, and creative assets. The AI executes this campaign faithfully, respecting author intent.

## Directory Structure

```
chronicles_of_merlin/
├── manifest.json
├── README.md
├── world/
│   ├── setting.md
│   ├── rules.md
│   └── constraints.md
├── characters/
│   ├── npcs/
│   │   ├── merlin/
│   │   │   ├── profile.json
│   │   │   └── secrets.md
│   │   ├── arthur/
│   │   │   ├── profile.json
│   │   │   └── relationships.json
│   │   └── morgana/
│   │       ├── profile.json
│   │       └── motivations.md
│   └── player/
│       └── template.json
├── plot/
│   ├── premise.md
│   ├── beats.json
│   └── endings/
│       ├── redemption.md
│       ├── betrayal.md
│       └── sacrifice.md
├── lore/
│   ├── history/
│   │   ├── ancient_war.md
│   │   └── founding_of_camelot.md
│   ├── magic/
│   │   ├── magic_system.md
│   │   ├── forbidden_spells.md
│   │   └── artifacts.md
│   └── locations/
│       ├── camelot.md
│       ├── avalon.md
│       └── dark_forest.md
├── art/
│   ├── characters/
│   │   ├── merlin.png
│   │   ├── merlin.png.keywords.txt
│   │   ├── arthur.png
│   │   ├── arthur.png.keywords.txt
│   │   ├── morgana.png
│   │   └── morgana.png.keywords.txt
│   ├── locations/
│   │   ├── camelot_throne_room.jpg
│   │   ├── avalon_misty_shore.jpg
│   │   └── dark_forest_clearing.webp
│   └── items/
│       ├── excalibur.png
│       └── staff_of_merlin.png
└── music/
    ├── ambient/
    │   ├── camelot_peace.mp3
    │   ├── avalon_mystery.ogg
    │   └── forest_danger.wav
    └── combat/
        ├── battle_theme.mp3
        └── boss_encounter.flac
```

## Sample File Contents

### `manifest.json`

```json
{
  "title": "Chronicles of Merlin",
  "version": "2.1.0",
  "author": "Jane Storyteller",
  "description": "An epic fantasy retelling of Arthurian legend from Merlin's perspective",
  "genre": "High Fantasy",
  "tone": "Epic, Introspective, Morally Complex",
  "content_rating": "Teen (violence, moral complexity)",
  "rules_hint": "narrative",
  "hydration_guidance": "Execute faithfully - all content is intentionally crafted"
}
```

### `characters/npcs/merlin/profile.json`

```json
{
  "name": "Merlin",
  "role": "Protagonist - Court Wizard",
  "age": "Unknown (appears elderly)",
  "personality": {
    "traits": ["wise", "melancholic", "secretive", "compassionate"],
    "flaws": ["burdened by foresight", "reluctant to share truth"],
    "virtues": ["loyal", "patient", "protective"]
  },
  "motivations": [
    "Guide Arthur to unite the kingdoms",
    "Prevent the fall of Camelot (which he has foreseen)",
    "Atone for past mistakes with Morgana"
  ],
  "speech_patterns": {
    "style": "Formal, archaic, uses metaphors from nature",
    "examples": [
      "The river of time flows only one direction, young king.",
      "Even the mightiest oak was once a fragile acorn."
    ]
  },
  "relationships": {
    "arthur": "Mentor and protector, loves like a son",
    "morgana": "Former student, now enemy - feels guilt and sorrow",
    "uther": "Complicated - served reluctantly, morally opposed"
  },
  "secrets": "See secrets.md",
  "portrait": "art/characters/merlin.png"
}
```

### `art/characters/merlin.png.keywords.txt`

```
# Keywords for Merlin's portrait
merlin
wizard
archmage
protagonist
elderly
staff
blue_robes
wise_eyes
white_beard
mentor
```

### `plot/beats.json`

```json
{
  "beats": [
    {
      "id": "beat_001",
      "title": "Vision of the Future",
      "description": "Merlin has a prophetic vision showing Camelot in flames and Arthur dead. This establishes the central tension.",
      "conditions": {
        "scene_count": { "min": 2, "max": 5 },
        "player_state": "established in Camelot"
      },
      "priority": "critical",
      "consequences": [
        "Merlin becomes more secretive",
        "Player gains 'Burden of Knowledge' trait"
      ]
    },
    {
      "id": "beat_002",
      "title": "Morgana's Turn",
      "description": "Morgana discovers her magical abilities and feels betrayed that Merlin didn't tell her sooner.",
      "conditions": {
        "requires_beat": "beat_001",
        "player_choice": "kept_magic_secret"
      },
      "priority": "high",
      "consequences": [
        "Relationship with Morgana: deteriorates",
        "Morgana begins path to antagonist"
      ]
    }
  ]
}
```

### `world/constraints.md`

```markdown
# World Constraints

These are absolute rules the AI must respect:

1. **Magic is Rare**: Only 1 in 10,000 people have magical ability
2. **Magic is Feared**: Common folk fear and mistrust magic users
3. **No Resurrection**: Death is permanent - no resurrection magic exists
4. **Medieval Technology**: No gunpowder, no advanced machinery
5. **Prophecy is Immutable**: Merlin's visions always come true, though interpretation varies
6. **Excalibur Cannot Break**: The sword is indestructible (plot requirement)
```

### `lore/magic/magic_system.md`

```markdown
# The Magic System of Camelot

Magic in this world flows from the Old Religion, an ancient pact between humans and the forces of nature...

[Detailed magic system documentation - 2000+ words]
```

## What Happens During Ingestion

1. **Narratoria ingests all files** into ObjectBox
2. **Detects rich data** (> 20 content files, no enrichment needed)
3. **Indexes for semantic search**:
   - NPCs with full personality profiles
   - Plot beats with conditions and consequences
   - Lore entries chunked for RAG retrieval
   - Art assets linked via keywords and entity relationships
   - Music assets tagged for scene types

4. **Builds relationship graph**:
   - `merlin.png` ← referenced by → `characters/npcs/merlin/profile.json`
   - `magic_system.md` ← entity-linked → `forbidden_spells.md`
   - `beat_001` → triggers → `beat_002` (plot dependency)

5. **No generation occurs** - all content is human-authored
6. **Campaign playable immediately** with faithful execution

## Metadata Example: Human-Authored Portrait with Sidecar

**Asset**: `art/characters/merlin.png`
**Sidecar**: `art/characters/merlin.png.keywords.txt`

**ObjectBox Metadata**:
```json
{
  "path": "art/characters/merlin.png",
  "type": "image",
  "keywords": [
    "merlin", "wizard", "archmage", "protagonist",
    "elderly", "staff", "blue_robes", "wise_eyes",
    "white_beard", "mentor"
  ],
  "generated": false,
  "checksum": "sha256:7f3a2b1c...",
  "created_at": "2026-02-03T15:30:00Z",
  "modified_at": "2026-02-03T15:30:00Z",
  "data": "<base64-encoded PNG>",
  "image_metadata": {
    "format": "png",
    "width": 1024,
    "height": 1024,
    "alt_text": "Elderly wizard Merlin with long white beard, blue robes, holding an ancient gnarled staff"
  },
  "relationships": {
    "references": [],
    "referenced_by": ["characters/npcs/merlin/profile.json"],
    "entity_links": ["merlin", "wizard", "protagonist"]
  }
}
```

---

**Time to Play**: ~5 seconds from campaign selection to first scene.

**Author Control**: 100% - AI invents nothing, executes the authored vision faithfully.
