# Contributors Guide

This document explains how to contribute to Narratoria—whether you are an AI agent or a human developer.

## Ground Rules
- Follow the project vision and architecture in `README.md` (local-first, Blazor WebAssembly, OpenAI-compatible API abstraction).
- Keep the backlog current in `SPEC.md`: update status/assignee when starting work; add a technical summary and link to the tests you added when finishing.
- Target .NET 9 SDK for all projects and tests.
- Preserve local-first constraints (client-side storage, no backend dependencies); use the storage and API abstractions already in place.
- Add or update tests for any non-trivial change.
- Testing convention: services get unit tests; Blazor components get component tests (bUnit). Full end-to-end/browser tests will be reintroduced later once the codebase stabilizes.

## Code & UI Guidelines
- Use existing component and layout patterns (`NarratoriaClient/Components/`, `NarratoriaClient/Components/Layout/`).
- For storage and data, go through the service abstractions (`IClientStorageService`, `AppDataService`).
- For external calls, follow the `OpenAiChatService` pattern and do not log secrets.
- Maintain accessibility in UI updates; keep components focused and composable.

## Testing
- Run all tests: `dotnet test`
- Component tests live in `tests/NarratoriaClient.ComponentTests`; add service unit tests alongside other test projects following the same pattern.

## Workflow Expectations
- Before coding: read `README.md`, `SPEC.md`, and relevant services/components.
- During work: update `SPEC.md` with status/assignee; keep changes aligned with local-first constraints.
- After work: add tests, update `SPEC.md` with a technical summary and links to the tests you added, and ensure lint/build/tests pass.

## Questions
If something is unclear and not covered by `README.md` or existing patterns, choose the simplest local-first solution, document it briefly, and update `SPEC.md` as needed.
