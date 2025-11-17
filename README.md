# Narratoria: Local-First Blazor Storytelling Client

## Project Vision
- Deliver an in-browser Blazor experience that lets players run text adventures inspired by classic MUDs and interactive fiction.
- Treat the OpenAI-compatible API as a plug-in narrator: user supplies endpoint + key, app orchestrates prompts/responses.
- Keep everything local-first: story logs, configuration, and session metadata persist in `localStorage`.

## Developer Prereqs
- .NET 9 SDK (app and Playwright tests target `net9.0`).

## Getting Started
- Install Playwright browsers (first run): `dotnet build tests/NarratoriaClient.PlaywrightTests && ./tests/NarratoriaClient.PlaywrightTests/bin/Debug/net9.0/playwright.sh install` (or `pwsh -File .../playwright.ps1`).
- Run the app: `dotnet run --project NarratoriaClient/NarratoriaClient.csproj --urls http://localhost:5000`
- Run Playwright tests headless: `DOTNET_ENVIRONMENT=Testing dotnet test tests/NarratoriaClient.PlaywrightTests/NarratoriaClient.PlaywrightTests.csproj`
- Watch Playwright tests headful with slow-mo: `PLAYWRIGHT_HEADFUL=true PLAYWRIGHT_SLOWMO_MS=250 DOTNET_ENVIRONMENT=Testing dotnet test tests/NarratoriaClient.PlaywrightTests/NarratoriaClient.PlaywrightTests.csproj`

## Core Goals
- **Solo & Multiplayer Sessions**: Support single-player adventures plus optional peer connections so multiple clients share a story instance.
- **Narrator Pipeline**: Allow selection/configuration of models (OpenAI API or compatible) to drive the GM persona, including prompt templates and tuning.
- **State Persistence**: Store sessions, settings, and cached model replies locally; plan for sync/export/import so sessions can move across devices.
- **Extensibility**: Design with future pluggable backends in mind (self-hosted functions, hosted orchestrators) without blocking the local-first MVP.

## Early Technical Questions
- **Hosting Model**: Blazor WebAssembly only vs. ASP.NET + Blazor Server hybrid? WebAssembly aligns with local-first but complicates real-time multiplayer.
- **Communication Layer**: For peer sessions, should the client rely on WebRTC data channels, a relayed signal server, or defer multiplayer until an optional service exists?
- **API Compatibility**: How to abstract OpenAI-compatible endpoints (OpenAI, Azure, local LLM proxies) while keeping the UI approachable? Consider pluggable providers with schema validation.
- **Prompt Orchestration**: Need a structured way to compose system prompts, story state, and user actions; evaluate reusable templates or a simple DSL.
- **Security**: Safeguard API keys stored locally; consider encrypting at rest with a user passphrase and avoiding accidental network leakage.

## Client-Side Storage Options
- **localStorage**: Simple key-value store (5–10 MB quota) with synchronous API and straightforward Blazor interop. Ideal for configuration payloads and a capped session log, but large transcripts risk quota churn and synchronous access can block the UI thread if overused.
- **sessionStorage**: Mirrors localStorage semantics but scoped to a single tab/session. Useful for volatile state (e.g., optimistic UI buffers) yet unsuitable for persistence between visits.
- **IndexedDB**: Asynchronous, object-store database with higher quotas and transactional semantics. Best for large or versioned story archives, offline-capable caches, and binary assets. Blazor WebAssembly can reach it via `IJSRuntime` or libraries like `TG.Blazor.IndexedDB` and `KristofferStrube.Blazor.IndexedDB`.
- **File System Access API**: Chrome/Edge-only capability to let users grant the app a handle to a real file for long-form logs or exports. Works alongside other storage for explicit backups.
- **Cache Storage / Service Worker**: Geared toward asset caching; can help prefetch narrator prompts or templates but not primary state.
- **In-Memory + Background Sync**: Combine runtime models with periodic `localStorage`/IndexedDB snapshots to avoid blocking writes.

The MVP can lean on `localStorage` for simplicity, but plan for an IndexedDB-backed store (or hybrid) once transcripts, multiplayer metadata, and rich prompts exceed quota or require transactional safety. A thin persistence abstraction in Blazor will make it easy to swap the backend without rewriting components.

## MVP Feature Outline
- Configuration UI for API base URL, key, selected model, and request parameters.
- Story session screen with:
  - Conversation log and inline metadata (role, timestamp, tokens).
  - Input box with helper shortcuts (actions, inventory, GM directives).
  - Session controls (restart, export, import).
- Local storage manager handling versioned schemas, migration, and purge.
- Optional experimental tab for multiplayer pairing strategy discussion (placeholder UX until implementation chosen).

## Roadmap Considerations
- Phase 1: Local-only play, simple narrator, persistence in `localStorage`.
- Phase 2: Multiplayer prototype (likely WebRTC with signaling fallback).
- Phase 3: Optional backend modules (hosting events, moderation, analytics).
- Phase 4: Advanced tooling (prompt editor, timeline visualization, AI-assisted VM for rulesets).

## Future Feature Concepts
- **Scene Illustration Rendering**: Introduce a lightweight client-side renderer that generates simple scene sketches (e.g., tavern corners, forests, caverns) using SVG or Canvas primitives based on structured JSON data provided by the narrator. This avoids diffusion model costs and keeps everything deterministic and local-first. The renderer could support style packs (ink, sepia, pixel) and integrate seamlessly into the storytelling chat background for immersive visualization.

## Acceptance Criteria Backlog
See `SPEC.md` for Scrum-style acceptance criteria that track future requirements and MVP refinements.

### About SPEC.md
`SPEC.md` maintains the backlog in a Scrum-style format with acceptance criteria, status, assignee, and any supporting discussion. Developers must keep it updated when starting work (status/assignee) and when finishing (technical summary + required Playwright test). Treat it as the authoritative backlog for upcoming and in-progress work.

## Contributing
See `CONTRIBUTORS.md` for coding guidelines, testing expectations, and how to work with `SPEC.md`.

## License
GPL-3.0-or-later. See `LICENSE`.
