# Campaign Format Contracts

This directory contains JSON Schema contracts for Narratoria campaign format validation and documentation.

## Schema Overview

| Schema | File | Purpose | Used In |
|--------|------|---------|---------|
| **Asset Metadata** | `asset-metadata.schema.json` | ObjectBox metadata wrapper for all ingested campaign files | ObjectBox indexing layer |
| **Campaign Manifest** | `manifest.schema.json` | Campaign package metadata | `manifest.json` (campaign root) |
| **NPC Profile** | `npc-profile.schema.json` | Non-player character definitions | `characters/npcs/{name}/profile.json` |
| **Plot Beats** | `plot-beats.schema.json` | Story beat definitions with conditions | `plot/beats.json` |
| **Player Template** | `player-template.schema.json` | Player character creation constraints | `characters/player/template.json` |

---

## Asset Metadata Schema

**File**: `asset-metadata.schema.json`
**Purpose**: Unified metadata structure for all campaign assets ingested into ObjectBox

### Core Fields (All Assets)
- `path` - Original filesystem path
- `type` - Asset type: `image`, `audio`, `prose`, `structured`
- `keywords` - Semantic search keywords (auto-extracted or from `.keywords.txt`)
- `generated` - Boolean flag for AI-generated content
- `checksum` - SHA-256 hash for duplicate detection
- `data` - Asset content (text or base64-encoded binary)

### Provenance (Generated Content)
When `generated: true`:
- `source_model` - LLM model (e.g., "ollama/gemma:2b")
- `generated_at` - ISO8601 timestamp
- `seed_data` - Original sparse data
- `version` - Campaign format version

### Type-Specific Metadata
- **Image**: format, width, height, alt_text
- **Audio**: format, duration_seconds, bitrate, loop
- **Prose**: word_count, language, chunks
- **Structured**: schema_type, schema_version, entity_id

### Relationships
- `references` - Assets this asset references
- `referenced_by` - Assets referencing this asset
- `entity_links` - Semantic entity connections

**Example Usage**:
```json
{
  "path": "art/characters/wizard.png",
  "type": "image",
  "keywords": ["wizard", "archmage", "merlin"],
  "generated": false,
  "checksum": "sha256:abc123...",
  "image_metadata": {
    "format": "png",
    "width": 1024,
    "height": 1024
  }
}
```

---

## Campaign Manifest Schema

**File**: `manifest.schema.json`
**Purpose**: Campaign package metadata and configuration

### Required Fields
- `title` - Campaign title
- `version` - Semver version

### Recommended Fields
- `author` - Author name(s)
- `description` - Brief description for browsing
- `genre` - Genre classification
- `tone` - Overall mood
- `content_rating` - Age-appropriateness

### Optional Fields
- `rules_hint` - Game mechanics style: `rules-light`, `narrative`, `crunchy`, `tactical`
- `hydration_guidance` - AI generation behavior guidance
- `content_warnings` - Sensitive topics
- `estimated_playtime_hours` - Playtime estimate
- `tags` - Searchable discovery tags
- `license` - Content license

**Example Usage**:
```json
{
  "title": "Chronicles of Merlin",
  "version": "2.1.0",
  "author": "Jane Storyteller",
  "description": "Epic fantasy retelling of Arthurian legend",
  "genre": "High Fantasy",
  "tone": "Epic, Introspective, Morally Complex",
  "content_rating": "Teen",
  "rules_hint": "narrative"
}
```

---

## NPC Profile Schema

**File**: `npc-profile.schema.json`
**Purpose**: Structured non-player character definitions

### Required Fields
- `name` - NPC name
- `role` - Role/archetype (e.g., "Protagonist - Court Wizard")
- `personality` - Object with `traits` array (minimum 1 trait)

### Core Optional Fields
- `age` - Age or age appearance
- `motivations` - Array of driving motivations
- `speech_patterns` - Style, examples, catchphrases
- `relationships` - Relationships with other characters
- `secrets` - Hidden information (inline or file path)
- `appearance` - Physical description
- `portrait` - Path to portrait image

### Advanced Fields
- `goals` - Specific goals with priority and status
- `stats` - Game statistics (system-dependent)
- `inventory` - Possessed items
- `metadata` - Importance tier, tags, first appearance

**Example Usage**:
```json
{
  "name": "Merlin",
  "role": "Protagonist - Court Wizard",
  "personality": {
    "traits": ["wise", "melancholic", "secretive"],
    "flaws": ["burdened by foresight"],
    "virtues": ["loyal", "patient"]
  },
  "motivations": [
    "Guide Arthur to unite the kingdoms",
    "Prevent the fall of Camelot"
  ],
  "speech_patterns": {
    "style": "Formal, archaic, uses metaphors from nature",
    "examples": [
      "The river of time flows only one direction."
    ]
  },
  "portrait": "art/characters/merlin.png"
}
```

---

## Plot Beats Schema

**File**: `plot-beats.schema.json`
**Purpose**: Define key story moments with trigger conditions

### Structure
Root object contains `beats` array. Each beat includes:

### Required Fields
- `id` - Unique identifier (pattern: `beat_NNN`)
- `title` - Brief beat title
- `description` - Detailed description of what happens

### Conditions (All Optional)
- `scene_count` - Min/max scene range for trigger window
- `requires_beat` - Prerequisite beat ID(s)
- `requires_any_beat` - At least one of these beats
- `player_choice` - Specific player choice required
- `player_state` - Required player condition
- `npc_state` - Required NPC states
- `world_state` - Required world conditions

### Additional Fields
- `priority` - `critical`, `high`, `medium`, `low`, `optional`
- `consequences` - Array of changes after beat completes
- `outcomes` - Possible outcomes with probabilities
- `dialogue` - Key dialogue lines
- `location` - Where beat occurs
- `mood` - Emotional tone
- `music` / `background` - Optional asset paths
- `skippable` - Can skip if conditions become impossible
- `timeout` - Timeout behavior with fallback

**Example Usage**:
```json
{
  "beats": [
    {
      "id": "beat_001",
      "title": "Vision of the Future",
      "description": "Merlin has a prophetic vision showing Camelot in flames",
      "conditions": {
        "scene_count": { "min": 2, "max": 5 },
        "player_state": "established in Camelot"
      },
      "priority": "critical",
      "consequences": [
        "Merlin becomes more secretive",
        "Player gains 'Burden of Knowledge' trait"
      ]
    }
  ]
}
```

---

## Player Template Schema

**File**: `player-template.schema.json`
**Purpose**: Define player character creation constraints and options

### Character Creation
- `mode` - `freeform`, `guided`, or `preset`
- `guidance` - Guidance text shown during creation

### Options (All Optional, Empty = Any)
- `allowed_races` - Available races/species
- `allowed_classes` - Available classes/professions
- `allowed_backgrounds` - Available character backgrounds

### Attributes
- `required_attributes` - Must define during creation
- `optional_attributes` - Can optionally define

### Starting Configuration
- `starting_location` - Where player begins
- `starting_items` - Items all players start with
- `starting_stats` - Point pool and attribute ranges

### Constraints
- `no_magic` - Player cannot use magic
- `must_be_human` - Player must be human
- `fixed_background` - Specific required background
- `custom` - Natural language constraints

### Guidance
- `themes` - Suggested character themes
- `prompts` - Questions to guide creation

**Example Usage**:
```json
{
  "character_creation": {
    "mode": "guided",
    "guidance": "You are a traveler arriving in Camelot."
  },
  "allowed_classes": [
    {
      "name": "Knight",
      "description": "Honorable warrior",
      "abilities": ["swordsmanship", "leadership"]
    }
  ],
  "starting_location": "Gates of Camelot",
  "constraints": {
    "custom": ["Must be from outside Camelot"]
  }
}
```

---

## Validation

All schemas can be used with standard JSON Schema validators:

```bash
# Using ajv-cli
ajv validate -s manifest.schema.json -d path/to/manifest.json
ajv validate -s npc-profile.schema.json -d path/to/profile.json
```

```javascript
// Using JavaScript
const Ajv = require('ajv');
const ajv = new Ajv();
const schema = require('./manifest.schema.json');
const valid = ajv.validate(schema, campaignManifest);
```

---

## Schema Versioning

All schemas include:
- `$schema`: JSON Schema draft version
- `$id`: Canonical schema URI

Future schema versions will be tracked via the campaign format version in manifests.

---

## Campaign README Template

**File**: `campaign-readme.template.md`

Markdown template for campaign README files. Includes:
- Campaign Format Creeds
- Authorship notes (human vs. AI-generated content)
- Keyword customization instructions
- License information

**Usage**: Copy and populate with campaign-specific information.

---

## Related Documentation

- **Main Specification**: `../spec.md`
- **Author Guide**: `../CAMPAIGN_AUTHOR_GUIDE.md`
- **Minimal Campaign Example**: `../examples/minimal-campaign.md`
- **Complete Campaign Example**: `../examples/complete-campaign.md`

---

**Campaign Format Version**: 1.0.0
**Last Updated**: 2026-02-03
