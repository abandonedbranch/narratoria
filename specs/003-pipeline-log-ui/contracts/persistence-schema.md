# Contracts: Persistence (IndexedDB)

This folder defines the persistence contracts implied by spec 003. This is not an HTTP API.

## Object Store: `story_session`

Single-record store.

- **Primary key**: `session_id` (string)
- **Record shape**:
  - `session_id`: string
  - `schema_version`: number
  - `latest_story_context`: string
  - `latest_updated_at`: string (ISO-8601)
  - `llm_selection`: { provider, model, profile }
  - `input_buffer`: { text, last_updated_at }

## Object Store: `run_records`

Bounded history (fixed maximum count).

- **Primary key**: `run_id` (string)
- **Indexes** (recommended):
  - `run_sequence` (monotonic)
  - `started_at`
- **Record shape**:
  - `run_id`: string
  - `run_sequence`: number
  - `trigger`: "Idle" | "Send"
  - `started_at`: string (ISO-8601)
  - `ended_at`: string (ISO-8601)
  - `status`: "Completed" | "Failed" | "Canceled" | "Blocked"
  - `failure_kind`?: string
  - `failure_message`?: string
  - `input_snapshot_hash`: string
  - `effective_llm_selection`?: { provider, model, profile }
  - `stage_summary`?: object

## Compaction Contract

When `run_records` would exceed the cap:

1. Select the oldest records that need to be removed.
2. Produce a `StoryContextDigest` with:
   - `bullets`: array of **exactly 12** strings (story facts)
3. Append the digest bullets into `latest_story_context` (exact formatting is implementation-defined but must preserve the 12 bullet facts).
4. Delete the compacted `run_records`.

## Versioning

- `schema_version` MUST be stored in `story_session`.
- Migrations MUST be explicit; if migration fails, the app should fall back to a clean session and surface a non-fatal error in telemetry.
