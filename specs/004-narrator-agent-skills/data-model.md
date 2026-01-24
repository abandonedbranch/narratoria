# Data Model: Narrator Agent & Skills System (004)

## Entities

- NarratorContext
  - playerAction: string
  - currentState: GameState
  - sessionId: string
  - preferences: NarratorPreferences

- NarratorResult
  - narration: string
  - updatedState: GameState
  - planSummary: string
  - errors: string[]

- NarratorPlan
  - actions: SkillAction[]
  - reasoning: string
  - dependencies: Record<string, string[]>
  - timeoutSeconds?: number

- SkillAction
  - actionId: string
  - skillName: string
  - parameters: object
  - toolBinary?: string

- GameState
  - inventory: InventoryState
  - quests: QuestLog
  - npcs: NpcRegistry
  - reputation: ReputationMap
  - worldState: WorldState
  - playerChoices: ChoiceHistory
  - metadata: StateMetadata

- InventoryState
  - items: Record<string, InventoryItem>

- InventoryItem
  - id: string
  - name: string
  - description: string
  - quantity: int (≥ 0)
  - metadata: Record<string, any>
  - acquiredAt: timestamp

- QuestLog
  - activeQuests: Quest[]
  - completedQuests: Quest[]

- Quest
  - id: string
  - title: string
  - description: string
  - objectives: Objective[]
  - status: enum { Active, Completed, Failed }
  - createdAt: timestamp
  - completedAt?: timestamp
  - prerequisites?: string[]

- Objective
  - id: string
  - description: string
  - completed: bool
  - completedAt?: timestamp

- NpcRegistry
  - npcs: Record<string, Npc>

- Npc
  - id: string
  - name: string
  - description: string
  - attributes: Record<string, any>
  - relationship: RelationshipStatus
  - dialogueHistory: DialogueEntry[]
  - lastInteractionAt: timestamp

- DialogueEntry
  - speaker: string
  - text: string
  - context: string
  - timestamp: timestamp

- RelationshipStatus
  - score: int (-100..100)
  - status: enum { Hostile, Unfriendly, Neutral, Friendly, Allied }
  - effects: string[]
  - provenance: ReputationChange[]

- ReputationChange
  - reason: string
  - delta: int
  - timestamp: timestamp

- ReputationMap
  - factions: Record<string, RelationshipStatus>
  - npcs: Record<string, RelationshipStatus>

- WorldState
  - currentLocation: string
  - timeOfDay: string
  - flags: Record<string, bool>
  - environmentalState: Record<string, any>

- ChoiceHistory
  - choices: Choice[]

- Choice
  - id: string
  - description: string
  - selected: string
  - alternatives: string[]
  - consequences: string[]
  - timestamp: timestamp

- StateMetadata
  - version: string (semver)
  - sessionId: string
  - createdAt: timestamp
  - lastUpdatedAt: timestamp

## Invariants

- Inventory quantities are non-negative.
- Quest status transitions: Active → Completed/Failed; no reactivation.
- Relationship scores stay within bounds; statuses derived consistently.
- Dialogue history is append-only.
- Every state change records provenance.
