# Quickstart: Realtime Pipeline Log UI

This quickstart is a target for the implementation described in `plan.md`.

## What you’ll get

- A pipeline log view that streams per-run telemetry.
- Idle execution (500ms debounce) and Send execution.
- Persisted story session (single auto-resume) stored in client IndexedDB.
- Bounded persisted run history with compaction into a 12-bullet story-fact digest.

## Running tests

- Unit tests: `dotnet test tests/Narratoria.Tests/Narratoria.Tests.csproj`

## Demo flow (once implemented)

1. Open the pipeline log UI.
2. Type into the input; pause for 500ms to trigger Pipeline A.
3. Click Send to trigger Pipeline B (input disabled during send).
4. Refresh the page to verify restore of latest story context + recent run history.
5. Generate enough runs to exceed the retention cap; verify compaction occurs and the digest contains exactly 12 bullets.
