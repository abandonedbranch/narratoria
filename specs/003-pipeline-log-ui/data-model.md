# Data Model: Realtime Pipeline Log UI

This data model describes the persisted entities implied by spec 003.

## Entity: StorySession

Single current session (auto-resume).

- **Key**: `session_id` (single value; implementation may use a constant like `"default"`)
- **Fields**:
  - `latest_story_context` (string)
  - `latest_updated_at` (UTC timestamp)
  - `llm_selection` (LlmSelection)
  - `input_buffer` (UserInputBuffer)
  - `compaction_digest` (StoryContextDigest? optional; may be appended into `latest_story_context`)
  - `schema_version` (int)

## Entity: PersistedRunRecord

Summarized run record persisted to IndexedDB.

- **Key**: `run_id` (string)
- **Fields**:
  - `run_sequence` (long, monotonic)
  - `trigger` (`Idle` | `Send`)
  - `started_at`, `ended_at` (UTC timestamps)
  - `status` (`Completed` | `Failed` | `Canceled` | `Blocked`)
  - `failure_kind` (string? nullable)
  - `failure_message` (string? nullable)
  - `effective_llm_selection` (LlmSelection? nullable)
  - `stage_summary` (map/string? optional; intended for high-level diagnostics)
  - `input_snapshot_hash` (string)

## Entity: UserInputBuffer

- `text` (string)
- `last_updated_at` (UTC timestamp)

## Entity: LlmSelection

- `provider` (string)
- `model` (string)
- `profile` (`Fast` | `Balanced` | `Quality`)

## Entity: StoryContextDigest

- `bullets` (array of **exactly 12** strings)

### Validation Rules

- `StoryContextDigest.bullets.length == 12`
- Each bullet is a “story fact”: a key moment with enough detail for an LLM to maintain useful long-term memory.

## Relationships

- StorySession `1 -> many` PersistedRunRecord (bounded by retention cap).
- Compaction: when run records exceed the cap, oldest run records are summarized into StoryContextDigest and appended to `latest_story_context`, then deleted.
