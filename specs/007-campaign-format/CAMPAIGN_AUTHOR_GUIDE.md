# Campaign Author Quick Reference Guide

Welcome to Narratoria campaign authoring! This guide helps you create story packages for the Narratoria AI storyteller.

## Campaign Format Philosophy

**The Hydration Spectrum**: The more you provide, the less the AI invents. This is a spectrum, not a toggle.

- **Minimal Campaign** (3 files): AI generates most content
- **Moderate Campaign** (10-15 files): AI fills gaps between your anchors
- **Complete Campaign** (20+ files): AI executes your vision faithfully

---

## Quick Start: 5-Minute Campaign

**Step 1**: Create a directory for your campaign:
```bash
mkdir my_campaign
cd my_campaign
```

**Step 2**: Create a `manifest.json`:
```json
{
  "title": "My Adventure",
  "version": "1.0.0",
  "author": "Your Name",
  "description": "A brief description",
  "genre": "Fantasy",
  "tone": "Adventurous",
  "content_rating": "Everyone"
}
```

**Step 3**: Create `world/setting.md`:
```markdown
# Setting

[Describe your world in 1-3 paragraphs]
```

**Step 4**: Create `plot/premise.md`:
```markdown
# Premise

[Describe the starting situation and hook]
```

**Done!** Load your campaign in Narratoria. The AI will enrich it with NPCs, plot beats, and lore.

---

## Directory Structure Reference

```
your_campaign/
├── manifest.json              # Required - campaign metadata
├── README.md                  # Optional - authorship notes
├── world/                     # Optional - world definition
│   ├── setting.md            # Setting description
│   ├── rules.md              # Custom game mechanics
│   └── constraints.md        # Absolute rules AI must respect
├── characters/               # Optional - character definitions
│   ├── npcs/                # Non-player characters
│   │   └── {npc_name}/
│   │       ├── profile.json # NPC personality, motivations, etc.
│   │       └── secrets.md   # Hidden information
│   └── player/              # Player character options
│       └── template.json    # Character creation constraints
├── plot/                     # Optional - plot structure
│   ├── premise.md           # Starting situation
│   ├── beats.json           # Key story moments
│   └── endings/             # Possible conclusions
│       └── *.md
├── lore/                     # Optional - background information
│   └── *.md                 # Any organizational structure
├── art/                      # Optional - visual assets
│   ├── characters/          # Character portraits
│   ├── locations/           # Scene backgrounds
│   └── items/               # Item illustrations
└── music/                    # Optional - audio assets
    ├── ambient/             # Background music
    └── combat/              # Combat themes
```

---

## File Formats

- **Prose Content**: Markdown (`.md`) - setting, lore, constraints
- **Structured Data**: JSON (`.json`) - profiles, beats, manifest
- **Images**: PNG, JPEG, WebP
- **Audio**: MP3, OGG, WAV, FLAC

---

## Campaign Format Creeds

**These principles guide ethical AI-assisted authorship:**

1. **Respect Human Artistry** - AI accelerates, doesn't replace
2. **Radical Transparency** - Generated content is clearly marked
3. **Human Override** - You control everything
4. **Attribution and Credit** - Generated sources disclosed
5. **Preserve Intent** - AI respects your explicit input

---

## Customizing Keywords (Advanced)

By default, Narratoria auto-extracts keywords from filenames and content. To customize:

**Create a `.keywords.txt` sidecar file** next to any asset:

```
art/characters/wizard.png
art/characters/wizard.png.keywords.txt  ← Sidecar file
```

**Format** (plain text, one keyword per line):
```
# Keywords for the wizard portrait
archmage
merlin
protagonist
wise
mentor
```

When the sidecar exists, Narratoria uses your keywords instead of auto-extraction.

---

## Understanding Generated Content

When the AI enriches your campaign, generated files:

- Include `_generated` suffix (e.g., `baker_generated.json`)
- Have `generated: true` in metadata
- Include provenance (model name, timestamp, seed data)
- **Can be modified or deleted** - you have full control

### Replacing Generated Content

To replace AI-generated content with your own:

1. Create your file without the `_generated` suffix
2. Delete or ignore the generated version
3. Re-load the campaign

The AI will **never** regenerate content once you've authored it.

---

## Example Workflows

### Workflow 1: Rapid Prototyping

**Goal**: Test an idea quickly

1. Create `manifest.json`, `world/setting.md`, `plot/premise.md`
2. Load campaign - AI generates everything else
3. Play through a session
4. Refine generated content you like, delete what you don't

**Time**: 5 minutes to playable campaign

---

### Workflow 2: Intentional Design

**Goal**: Professional interactive fiction

1. Write detailed setting, constraints, premise
2. Define key NPCs with personalities and relationships
3. Structure plot beats with conditions
4. Add lore for world depth
5. Include art/music assets if available
6. Load campaign - AI executes faithfully

**Time**: Hours to days, depending on scope

---

### Workflow 3: Hybrid Approach

**Goal**: Balance speed and control

1. Write core elements you care about (e.g., main NPCs, key plot beats)
2. Let AI generate supporting content (minor NPCs, lore details)
3. Review generated content, keep what works
4. Replace generated content that doesn't match your vision

**Time**: 30 minutes to playable, iterate as needed

---

## NPC Profile Example

```json
{
  "name": "Morgana",
  "role": "Antagonist - Sorceress",
  "personality": {
    "traits": ["ambitious", "betrayed", "powerful"],
    "flaws": ["vengeful", "prideful"],
    "virtues": ["intelligent", "determined"]
  },
  "motivations": [
    "Claim the throne of Camelot",
    "Revenge against Merlin",
    "Prove magic users deserve respect"
  ],
  "speech_patterns": {
    "style": "Regal, sharp, uses courtly language with edge",
    "examples": [
      "You speak of loyalty, yet you hide truth behind smiles.",
      "The crown is mine by blood and power, not your laws."
    ]
  },
  "relationships": {
    "merlin": "Former mentor, now enemy",
    "arthur": "Half-brother, rival for throne"
  },
  "secrets": "Knows Arthur's true parentage",
  "portrait": "art/characters/morgana.png"
}
```

---

## Plot Beat Example

```json
{
  "beats": [
    {
      "id": "beat_001",
      "title": "The Betrayal Revealed",
      "description": "Arthur learns the truth about his parentage",
      "conditions": {
        "scene_count": { "min": 10, "max": 15 },
        "player_choice": "investigated_past"
      },
      "priority": "critical",
      "consequences": [
        "Relationship with Uther: destroyed",
        "Arthur gains 'Identity Crisis' trait"
      ]
    }
  ]
}
```

---

## World Constraints Example

```markdown
# World Constraints

These are absolute rules the AI must respect:

1. **No Resurrection**: Death is permanent
2. **Magic is Rare**: Only 1 in 10,000 have magical ability
3. **Medieval Technology**: No gunpowder or advanced machinery
4. **Prophecy is Real**: Visions always come true (though interpretation varies)
```

---

## Tips for Great Campaigns

### Do:
✅ Write clear, concise setting descriptions
✅ Define NPC motivations (the "why" matters)
✅ Use constraints to set boundaries
✅ Test with minimal content first, then expand
✅ Review generated content before publishing
✅ Use keywords to improve asset discovery

### Don't:
❌ Overspecify - leave room for player agency
❌ Contradict yourself in different files
❌ Include sensitive/private data in campaigns
❌ Assume AI will "figure out" implicit intent
❌ Forget to test your campaign before sharing

---

## Campaign Size Guidelines

| Campaign Type | Files | Load Time | AI Generation |
|---------------|-------|-----------|---------------|
| **Minimal** | 3-5 | ~10 sec | Most content |
| **Moderate** | 10-20 | ~7 sec | Gaps and details |
| **Complete** | 20-50 | ~5 sec | Nothing |
| **Epic** | 50+ | ~10 sec | Nothing |

**Target Hardware**: 8GB RAM device

**Campaign Size Limit**: 100MB (includes assets)

---

## Troubleshooting

### "Campaign failed to load"
- Check that `manifest.json` is valid JSON
- Ensure at least one content file exists

### "Asset not found during gameplay"
- Verify file paths in profiles match actual files
- Check for typos in asset references

### "Generated content doesn't match my vision"
- Add more constraints in `world/constraints.md`
- Provide detailed examples in your prose
- Replace generated files with your own

### "Keywords aren't working"
- Check `.keywords.txt` format (one per line, no commas)
- Ensure sidecar filename matches asset exactly
- Reload campaign after adding sidecar

---

## Sharing Your Campaign

When sharing campaigns with others:

1. Include the `README.md` with authorship notes
2. Disclose which content is AI-generated (required by creeds)
3. Specify content rating appropriately
4. Consider licensing (e.g., Creative Commons)
5. Test on a fresh Narratoria installation

**Generated Content Attribution**:
Always credit the AI model used (e.g., "NPCs generated by Ollama Qwen 2.5 3B").

---

## Resources

- **Full Specification**: `specs/007-campaign-format/spec.md`
- **Metadata Schema**: `specs/007-campaign-format/contracts/asset-metadata.schema.json`
- **Minimal Example**: `specs/007-campaign-format/examples/minimal-campaign.md`
- **Complete Example**: `specs/007-campaign-format/examples/complete-campaign.md`

---

**Happy authoring!** 🎭✨

For questions or feedback: [Narratoria Community Forums](https://narratoria.dev/community)
