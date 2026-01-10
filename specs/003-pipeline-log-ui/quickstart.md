# Quickstart: Realtime Pipeline Log UI

This quickstart is a target for the implementation described in `plan.md`.

## What you’ll get

- A pipeline log view that streams per-run telemetry.
- Idle execution (500ms debounce) and Send execution.
- Persisted story session (single auto-resume) stored in client IndexedDB.
- Bounded persisted run history with compaction into a 12-bullet story-fact digest.

## Running tests

- Unit tests: `dotnet test tests/Narratoria.Tests/Narratoria.Tests.csproj`

## Running the web app (once implemented)

Start the Blazor Server host on the URLs expected by the E2E tests:

- `dotnet run --project src/Narratoria.Web/Narratoria.Web.csproj --urls "https://localhost:5001;http://localhost:5224"`

Then open the pipeline log UI:

- `http://localhost:5224/pipeline-log`

## Running Playwright E2E tests (once implemented)

1. Build the Playwright test project:
	- `dotnet build tests/Narratoria.PlaywrightTests/PlaywrightTests.csproj`
2. Install Playwright browsers (first time only, or after Playwright updates):
	- `pwsh tests/Narratoria.PlaywrightTests/bin/Debug/net10.0/playwright.ps1 install`
3. Ensure the web app is running (see above).
4. Run the E2E tests:
	- `dotnet test tests/Narratoria.PlaywrightTests/PlaywrightTests.csproj`

## Demo flow (once implemented)

1. Open the pipeline log UI.
2. Type into the input; pause for 500ms to trigger Pipeline A.
3. Click Send to trigger Pipeline B (input disabled during send).
4. Refresh the page to verify restore of latest story context + recent run history.
5. Generate enough runs to exceed the retention cap; verify compaction occurs and the digest contains exactly 12 bullets.
