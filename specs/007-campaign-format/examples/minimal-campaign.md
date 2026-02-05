# Example: Minimal Campaign Structure

This example shows the **absolute minimum** required for a playable Narratoria campaign. The on-device LLM will enrich this sparse data with generated NPCs, plot beats, and lore.

## Directory Structure

```
i_am_bread/
├── manifest.json
├── world/
│   └── setting.md
└── plot/
    └── premise.md
```

## File Contents

### `manifest.json`

```json
{
  "title": "I Am Bread",
  "version": "1.0.0",
  "author": "Example Author",
  "description": "A whimsical tale of a bread elemental in a magical bakery",
  "genre": "Fantasy Comedy",
  "tone": "Lighthearted, Whimsical",
  "content_rating": "Everyone"
}
```

### `world/setting.md`

```markdown
# Setting: The Magical Bakery

You exist as a sentient loaf of bread in a whimsical bakery where all baked goods have souls. The bakery is run by a mysterious baker who never speaks, only hums ancient tunes.

The world operates on "Bread Logic" - things that make sense to bread might not make sense to humans. Warmth is life, staleness is death, and butter is currency.
```

### `plot/premise.md`

```markdown
# Premise

You wake up on the cooling rack, fresh from the oven, confused and disoriented. The other baked goods whisper about "The Great Consumption" - a ritual where baked goods are taken away, never to return.

You must discover the truth about your existence and decide whether to accept your fate or rebel against the baker's grand design.
```

## What Happens During Ingestion

1. **Narratoria ingests these 3 files** into ObjectBox
2. **Detects sparse data** (< 3 content files triggers enrichment)
3. **On-device LLM generates**:
   - NPCs (e.g., "Croissant Claude", "Baguette Betty", "The Silent Baker")
   - Plot beats (e.g., "Discovery of the Butter Vault", "Meeting the Elder Sourdough")
   - Lore entries (e.g., "History of the Bakery", "The Bread Pantheon")
   - World rules (default rules-light system)

4. **All generated assets marked** with `generated: true` in metadata
5. **Campaign becomes playable** with full narrative structure

## Resulting Campaign Structure (After Enrichment)

```
i_am_bread/
├── manifest.json
├── README.md (generated)
├── world/
│   ├── setting.md
│   ├── rules_generated.md
│   └── constraints_generated.md
├── characters/
│   ├── npcs/
│   │   ├── croissant_claude_generated.json
│   │   ├── baguette_betty_generated.json
│   │   └── silent_baker_generated.json
│   └── player/
│       └── template_generated.json
├── plot/
│   ├── premise.md
│   ├── beats_generated.json
│   └── endings/
│       ├── acceptance_generated.md
│       └── rebellion_generated.md
└── lore/
    ├── history_generated.md
    ├── bread_pantheon_generated.md
    └── butter_economy_generated.md
```

**Key Observation**: Human-authored files remain unchanged. Generated files have `_generated` suffix and `generated: true` in metadata.

---

**Time to Play**: ~10 seconds from campaign selection to first playable scene.
