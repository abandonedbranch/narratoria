# AGENTS: What This Project Is

This guide helps a coding assistant quickly understand Narratoria and how to contribute safely.

## Project Snapshot
- Name: Narratoria — local-first Blazor storytelling client inspired by MUDs/interactive fiction.
- Tech: .NET 9, Blazor (client-side), C#, bUnit for components, xUnit-style unit tests.
- License: GPL-3.0-or-later.
- Backlog: Scrum-style acceptance criteria live in `SPEC.md`; keep it updated when starting/finishing work.
- Contribution norms: See `CONTRIBUTORS.md` for coding/testing guidelines.

## How It Works (high level)
- UI runs in-browser; players point the app at any OpenAI-compatible API endpoint and key.
- Narration pipeline is staged and hook-driven: InputPreprocessor → SafetyPolicyChecker → PromptAssembler → ModelRouter → LLMClient → PostProcessor → MemoryManager → OutputFormatter. Each stage emits lifecycle events for UI updates and telemetry; hooks mutate shared context and can short-circuit on failures.
- State is local-first (initially `localStorage`, planned IndexedDB hybrid) with future import/export and device sync.
- Future branches: multiplayer sessions (likely WebRTC + signaling) and optional image sketch workflow via the same pipeline.

## Run and Test
- Run app: `dotnet run --project NarratoriaClient/NarratoriaClient.csproj --urls http://localhost:5000`
- Tests: `dotnet test` (services → unit tests, Blazor components → bUnit; E2E TBD)

## Repo Landmarks
- `NarratoriaClient/` — Blazor client source.
- `SPEC.md` — authoritative backlog and acceptance criteria (update status/assignee/summary/tests).
- `CONTRIBUTORS.md` — coding standards and workflow expectations.
- `scripts/` — helper scripts (if present).
- `tests/` — test projects.

## Assistant Norms
- Keep changes aligned with acceptance criteria in `SPEC.md`; update it when starting/finishing work.
- Prefer small, testable hooks per pipeline stage; emit lifecycle events instead of adding tight coupling.
- Preserve local-first assumptions (no surprise network calls beyond the configured API endpoint; safeguard API keys).
- Add component tests for Blazor UI changes and unit tests for service logic where practical.
