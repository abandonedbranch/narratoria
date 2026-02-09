# Wizard Runner Campaign

A sample campaign demonstrating Narratoria's semantic, RAG-friendly campaign format. Optimized for Phi-4-mini's efficient content ingestion and narrative generation within constrained token budgets.

## Campaign Philosophy

1. **Respect Human Artistry**: Generated content accelerates creative work, never replaces human authorship.
2. **Radical Transparency**: All AI-generated content marked with `generated: true`.
3. **Human Override**: Authors can refine, replace, or delete any generated content.
4. **Attribution and Credit**: Generated content sources disclosed when campaigns are shared.
5. **Preserve Intent**: AI prioritizes author's explicit intent when enriching sparse data.

## Semantic Filesystem Layout

**Core (Required)**
- `manifest.json` — Campaign metadata, ingestion hints, required files
- `plot/premise.md` — Campaign hook and thematic setup (≤512 tokens)
- `plot/beats.md` — Key story moments and branching logic
- `world/overview.md` — Setting, atmosphere, visual tone
- `characters/leory.md` — Primary NPC definition

**Enrichment (Optional)**
- `world/locations.md` — Key locations and their semantic nature
- `plot/encounters.md` — Challenge archetypes and variations
- `characters/roles.md` — NPC archetypes and interaction patterns
- `lore/` — Campaign knowledge base (chunked by topic)
- `mechanics/` — Game rules, systems, progression
- `items/` — Objects, treasures, resources
- `assets/` — Art, music, audio references and credits

## Phi-4-mini Ingestion Pattern

This campaign is structured for efficient RAG injection:
- Each file sized for 1-3 NDJSON chunks (≤512 tokens per chunk)
- Clear semantic intent enables targeted retrieval
- Minimal boilerplate; maximum signal density
- Optional sections support progressive elaboration without core dependency
