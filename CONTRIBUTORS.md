# Contributors Guide

This document explains how to contribute to Narratoria—whether you are an AI agent or a human developer.

## Ground Rules
- Follow the project vision and architecture in `README.md` (local-first, Blazor WebAssembly, OpenAI-compatible API abstraction).
- Keep the backlog current in `SPEC.md`: update status/assignee when starting work; add a technical summary and required Playwright test when finishing.
- Target .NET 9 SDK for all projects and tests.
- Preserve local-first constraints (client-side storage, no backend dependencies); use the storage and API abstractions already in place.
- Add or update tests for any non-trivial change; new features must include a Playwright test proving behavior before marking Done.

## Code & UI Guidelines
- Use existing component and layout patterns (`NarratoriaClient/Components/`, `NarratoriaClient/Components/Layout/`).
- For storage and data, go through the service abstractions (`IClientStorageService`, `AppDataService`).
- For external calls, follow the `OpenAiChatService` pattern and do not log secrets.
- Maintain accessibility in UI updates; keep components focused and composable.

## Testing
- Playwright: `DOTNET_ENVIRONMENT=Testing dotnet test tests/NarratoriaClient.PlaywrightTests/NarratoriaClient.PlaywrightTests.csproj`
- Watch runs: `PLAYWRIGHT_HEADFUL=true PLAYWRIGHT_SLOWMO_MS=250 DOTNET_ENVIRONMENT=Testing dotnet test tests/NarratoriaClient.PlaywrightTests/NarratoriaClient.PlaywrightTests.csproj`
- Ensure browsers are installed via the provided Playwright install script or CLI.

## Workflow Expectations
- Before coding: read `README.md`, `SPEC.md`, and relevant services/components.
- During work: update `SPEC.md` with status/assignee; keep changes aligned with local-first constraints.
- After work: add tests, update `SPEC.md` with a technical summary and link to the new Playwright test, and ensure lint/build/tests pass.

## Questions
If something is unclear and not covered by `README.md` or existing patterns, choose the simplest local-first solution, document it briefly, and update `SPEC.md` as needed.
