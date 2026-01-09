# Data Model: LLM Story Transforms

**Feature**: [spec.md](spec.md)  
**Plan**: [plan.md](plan.md)  
**Date**: 2026-01-08

> This data model describes the shape of story state carried through the pipeline.
> It is intentionally implementation-agnostic; types may be expressed as records/DTOs and serialized as JSON.

## Entities

### StoryState

Represents the current story facts for a session.

**Fields**

- `sessionId` (string, required): stable identifier for a story session
- `summary` (string, optional): current recap of story events
- `characters` (list<CharacterRecord>, required; may be empty)
- `inventory` (InventoryState, required)
- `version` (integer, required): monotonically increasing revision number
- `lastUpdated` (string timestamp, optional): last update time

**Validation rules**

- `sessionId` must be non-empty.
- `version` increments by 1 per successful update.

### CharacterRecord

Represents a known character and what is known about them.

**Fields**

- `id` (string, required): stable identifier (may be derived from name)
- `displayName` (string, required)
- `aliases` (list<string>, optional)
- `traits` (map<string,string>, optional): e.g., role, appearance, temperament
- `relationships` (list<Relationship>, optional)
- `lastSeen` (string, optional): short textual context
- `provenance` (TransformProvenance, required)

**Validation rules**

- `id` and `displayName` must be non-empty.

### Relationship

Represents a relationship fact between two characters.

**Fields**

- `otherCharacterId` (string, required)
- `relation` (string, required): e.g., "ally", "enemy", "sibling"
- `provenance` (TransformProvenance, required)

### InventoryState

Represents the player inventory.

**Fields**

- `items` (list<InventoryItem>, required; may be empty)
- `provenance` (TransformProvenance, required)

### InventoryItem

Represents a single inventory item.

**Fields**

- `id` (string, required)
- `displayName` (string, required)
- `quantity` (integer, optional; default 1)
- `notes` (string, optional)
- `provenance` (TransformProvenance, required)

**Validation rules**

- `quantity` must be >= 0 when present.

### TransformProvenance

Tracks where a fact came from.

**Fields**

- `sourceSnippet` (string, optional): quoted input excerpt supporting the fact
- `chunkIndex` (integer, optional): order within the stream
- `confidence` (number in [0,1], required)
- `transformName` (string, required)

## State Transitions

- On each processed input chunk (or buffered chunk batch), the transforms may update `StoryState`.
- Rewrite transform modifies narration text but does not need to mutate story state.
- Summary transform updates `summary`.
- Character transform merges character updates into `characters`.
- Inventory transform merges updates into `inventory`.

## Invariants

- Original narration input is preserved (at minimum as an annotation on output chunks).
- StoryState updates are merge-based and never silently destructive.
- Low-confidence facts remain flagged; they do not overwrite high-confidence facts without explicit evidence.
