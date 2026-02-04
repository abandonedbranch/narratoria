# Narratoria

An AI-backed narrative-driven storyteller and game master application.

## Vision

Narratoria uses AI to **cleverly tell stories**, allowing player freedom while subtly driving the narrative forward to enhance player agency. The system presents structured choices that feel "perplexingly on-point" - as if the AI truly understands the player's character and situation.

**Core Principle**: The AI should feel like a skilled human game master who remembers everything and weaves it into the story.

## Technology Stack

- **Runtime**: Dart 3.x + Flutter (cross-platform rich desktop/mobile app)
- **AI Backend**: On-device LLMs via Ollama (Gemma 2B, Llama 3.2 3B, Qwen 2.5 3B)
- **Target Hardware**: iPhone 17 entry-level (~8GB RAM, 2s target inference)
- **Architecture**: Agent skills + CLI tools for modular workflows
- **Memory**: On-device vector storage with RAG for contextual recall

## Design Philosophy

### AI Role

The AI serves multiple roles through modular **agent skills**:

| Skill | Purpose |
|-------|---------|
| **Narrator** | Describes scenes, speaks as NPCs |
| **Game Master** | Resolves mechanics, presents challenges |
| **Story Director** | Subtly steers toward interesting outcomes |
| **Choice Generator** | Creates contextually relevant options |

### What AI Does NOT Do

**Generative AI art is explicitly out of scope for runtime.** Research showed:
- On-device image generation is resource-heavy and unreliable
- Results are inconsistent and require large models (~3.5GB+)
- Traditional game dev (sprites, pre-made assets) is faster and pixel-perfect

Campaign authors may use AI to generate art assets at authoring time, but Narratoria displays pre-made assets at runtime.

---

## Campaign Format (Spec 007 - Planned)

Campaigns are directories with a defined structure. The AI **fills gaps** based on what's provided:

```
campaign/
├── manifest.json              # Metadata, optional rules hints
├── world/
│   ├── setting.md             # World description, tone, era
│   ├── rules.md               # Game mechanics (optional)
│   └── constraints.md         # "Magic is rare", "No resurrection"
├── characters/
│   ├── npcs/
│   │   └── {name}/
│   │       ├── profile.json   # Personality, motivations, relationships
│   │       └── portrait.png   # Optional art asset
│   └── player/
│       └── template.json      # Character creation constraints
├── plot/
│   ├── premise.md             # Starting situation
│   ├── beats.json             # Key moments (optional)
│   └── endings/               # Possible conclusions (optional)
├── lore/                      # RAG-indexed background
│   ├── history.md
│   ├── factions.md
│   └── locations.md
└── assets/                    # Art, music (optional)
```

### Hydration Model

The AI responds to **campaign completeness**, not explicit settings:

| Campaign Input | AI Behavior |
|----------------|-------------|
| `"the player is a goldfish"` | Wild improvisation, chaos welcomed |
| Setting + 3 characters | Fill gaps, infer relationships |
| Full campaign with beats | Execute faithfully, minimal invention |

The more a campaign provides, the less the AI invents.

---

## Memory System (4 Tiers)

The memory system enables the "perplexingly on-point" effect:

```
TIER 1: STATIC (loaded once)
├── Campaign lore, character profiles, world rules
└── Vector-indexed for semantic search

TIER 2: INCREMENTAL (after each choice)
├── Scene summaries: "Player chose to help the beggar"
└── Appended chronologically, vector-indexed

TIER 3: WEIGHTED (affects retrieval bias)
├── NPC Sentiment: { "marta_beggar": +0.3, "guard": -0.2 }
└── Boosts/penalizes retrieval of related memories

TIER 4: EPISODIC (rare, high-detail, permanent)
├── Triumph: "Defeated the shadow dragon, saved the village"
├── Failure: "Brother died because you hesitated"
└── Stored with full context, always retrieved when relevant
```

---

## Scene Transition Pipeline

```
1. PLAYER MAKES CHOICE
   └─► "I help the beggar"

2. MEMORY UPDATE
   ├─► Scene summary stored (Tier 2)
   ├─► Sentiment updated: marta_beggar += 0.3 (Tier 3)
   └─► Check for episodic trigger (triumph/failure?)

3. NEXT SCENE RULES
   ├─► What type of scene follows? (travel, dialogue, danger)
   ├─► What memories are relevant? (query with bias weights)
   └─► Is there a plot beat to hit? (check campaign beats)

4. MEMORY RETRIEVAL
   ├─► Semantic search: "beggar, marta, helped, secret"
   ├─► Apply sentiment weights: boost marta-related content
   └─► Check episodic memory: any past triumphs/failures?

5. PROSE GENERATION
   └─► LLM outputs 2-3 paragraphs of scene-setting prose

6. CHOICE GENERATION
   └─► LLM outputs 3-4 choices:
       - Contextually grounded (memory)
       - Character-appropriate (profile)
       - Narratively interesting (story director)
       - Mechanically valid (rules)

7. PLAYER SEES
   ┌─────────────────────────────────────────────────┐
   │  The barmaid looks at you suspiciously...       │
   │                                                 │
   │  [A] Mention that Marta sent you                │ ← Memory-driven!
   │  [B] Order a drink and change the subject       │
   │  [C] Flash your sigil and demand answers        │
   │  [D] Leave quietly and try another approach     │
   └─────────────────────────────────────────────────┘
```

Choice A ("Mention that Marta sent you") only appears because the memory system knows you helped Marta and she mentioned the tunnels.

---

## Game Mechanics

### Default Rules-Light System

Narratoria uses a simple, original rules-light system (avoiding copyrighted systems):

```
FATE ROLL (when outcome is uncertain)
─────────────────────────────────────
Roll 2d6 + modifiers:
  2-6:   Failure (complication)
  7-9:   Partial success (cost or twist)
  10-12: Full success

Modifiers:
  +1 if relevant skill/trait
  +1 if NPC favors you (positive sentiment)
  -1 if NPC distrusts you (negative sentiment)
  +1 if emotionally resonant moment (episodic callback)
```

Campaigns may specify alternative rule systems or the AI may infer from setting/tone.

---

## Project Structure

```
narratoria/
├── specs/                    # Five-spec architecture
│   ├── 001-tool-protocol/    # NDJSON tool communication
│   ├── 002-plan-execution/   # Plan JSON schema and execution
│   ├── 003-skills-framework/ # Skill discovery and config
│   ├── 004-narratoria-skills/# Individual skill specs
│   ├── 005-dart-implementation/
│   └── 006-skill-state-persistence/
├── src/
│   └── research/             # Python prototypes for AI exploration
├── .claude/commands/         # Speckit workflow commands
└── .specify/                 # Templates and constitution
```

---

## Research Findings (src/research/)

Explored on-device AI for iPhone 17 targeting:

### Image Generation (Abandoned for Runtime)

| Approach | Result |
|----------|--------|
| Stable Diffusion FP32 | Too large (~4GB), slow |
| Stable Diffusion FP16 | MPS instability, black images |
| Tiny SD | Acceptable quality, still ~2GB |
| IP-Adapter | MPS bug (`'tuple' object has no attribute 'shape'`) |
| img2img | Works but loses identity, adds characters |
| Inpainting | Mask positioning unreliable, artifacts |

**Conclusion**: Generative AI art is not suitable for on-device runtime. Use pre-made assets.

### Text Generation (Primary Focus)

Small models (2B-3B) can run on-device for narrative generation. Focus is now on:
- Efficient prompt engineering
- RAG-based memory for context
- Structured choice generation

---

## Specifications

| Spec | Description |
|------|-------------|
| 001 | NDJSON protocol for tool communication |
| 002 | Plan JSON schema and execution semantics |
| 003 | Skill discovery and configuration |
| 004 | Individual Narratoria skill specifications |
| 005 | Dart/Flutter implementation |
| 006 | Skill state persistence |
| 007 | Campaign format (planned) |
| 008 | Narrative engine (planned) |

---

## Development

```bash
# Flutter (when lib/ is implemented)
flutter test
flutter analyze

# Python research prototypes
cd src/research
pip install -r requirements.txt
python image_tui.py        # Character portrait exploration
python ip_adapter_tui.py   # Reference art derivatives (deprecated)
python inpainting_tui.py   # Localized modifications (deprecated)
```

---

## Key Insights

1. **Memory makes magic**: The RAG system is what makes choices feel omniscient
2. **Hydration over configuration**: AI fills gaps based on campaign density, not flags
3. **Skills over prompts**: Modular agent skills beat monolithic system prompts
4. **Art is solved**: Traditional game dev handles assets; AI handles narrative
5. **Rules inference**: Deduce mechanics from tone/setting, don't implement every TTRPG

---

## Next Steps

1. Draft **Spec 007: Campaign Format** - Define manifest schema, content structure
2. Draft **Spec 008: Narrative Engine** - Scene pipeline, memory tiers, choice generation
3. Prototype **Memory System** - SQLite + vector embeddings for RAG
4. Prototype **Choice Generator** - LLM integration with memory augmentation
