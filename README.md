# Narratoria: Local-First Blazor Storytelling Client

## Project Vision
- Deliver an in-browser Blazor experience that lets players run text adventures inspired by classic MUDs and interactive fiction.
- Treat the OpenAI-compatible API as a plug-in narrator: user supplies endpoint + key, app orchestrates prompts/responses.
- Keep everything local-first: story logs, configuration, and session metadata persist in `localStorage`.

## Developer Prereqs
- .NET 9 SDK (app and tests target `net9.0`).

## Getting Started
- Run the app: `dotnet run --project NarratoriaClient/NarratoriaClient.csproj --urls http://localhost:5000`
- Run unit + component tests: `dotnet test`
- Convention: services get unit tests; Blazor components get component tests (bUnit). Full end-to-end/browser testing will come later once the codebase stabilizes.

## Core Goals
- **Solo & Multiplayer Sessions**: Support single-player adventures plus optional peer connections so multiple clients share a story instance.
- **Narrator Pipeline**: Allow selection/configuration of models (OpenAI API or compatible) to drive the GM persona, including prompt templates and tuning.
- **State Persistence**: Store sessions, settings, and cached model replies locally; plan for sync/export/import so sessions can move across devices.
- **Extensibility**: Design with future pluggable backends in mind (self-hosted functions, hosted orchestrators) without blocking the local-first MVP.

## Narration Pipeline Architecture
The future narrator experience revolves around a hook-driven pipeline. Each stage emits lifecycle events so specialized services can mutate or inspect the shared context without hard-coded coupling.

```
┌────────────┐
│  UI Input  │
└─────┬──────┘
      ▼
┌──────────────────────────────┐
│ Stage 1: InputPreprocessor   │
│ - normalization hooks        │
│ - command/tag extraction     │
└─────┬────────────────────────┘
      │ emit: input.preprocessed
      ▼
┌──────────────────────────────┐
│ Stage 2: SafetyPolicyChecker │
│ - mode & tone enforcement    │
│ - command short-circuiting   │
└─────┬────────────────────────┘
      │ emit: safety.checked
      ▼
┌──────────────────────────────┐
│ Stage 3: PromptAssembler     │
│ - narrator/system rules      │
│ - world memory + personas    │
└─────┬────────────────────────┘
      │ emit: prompt.assembled
      ▼
┌──────────────────────────────┐
│ Stage 4: ModelRouter         │
│ - workflow-aware selection   │
│ - config-driven overrides    │
└─────┬────────────────────────┘
      │ emit: model.selected
      ▼
┌──────────────────────────────┐
│ Stage 5: LLMClient           │
│ - streaming request/response │
│ - chunked lifecycle events   │
└─────┬────────────────────────┘
      │ emit: llm.response.received
      ▼
┌──────────────────────────────┐
│ Stage 6: PostProcessor       │
│ - lore/style validation      │
│ - structured event extraction│
└─────┬────────────────────────┘
      │ emit: output.postprocessed
      ▼
┌──────────────────────────────┐
│ Stage 7: MemoryManager       │
│ - update sessions/NPCs/items │
│ - log automation decisions   │
│ - future: rolling summaries  │
└─────┬────────────────────────┘
      │ emit: state.memory.updated
      ▼
┌──────────────────────────────┐
│ Stage 8: OutputFormatter     │
│ - final reply + metadata     │
│ - UI-ready payloads          │
└─────┬────────────────────────┘
      │ emit: output.ready
      ▼
┌───────────────┐
│ UI Rendering  │
└───────────────┘
```

Each rectangular stage owns a collection of hook implementations (small, testable listeners). They mutate the pipeline context in sequence, can short-circuit on failures, and broadcast fine-grained telemetry via lifecycle events (e.g., `input.tags.detected`, `llm.response.chunk`). Components such as the status indicator, logging panel, or gameplay automation subscribe to these events to update the UI in real time or to trigger auxiliary workflows like analytics, persona adjustments, or multiplayer synchronization.

MemoryManager will initially sync detailed state (chat transcript pointers, NPC traits, inventory items). Later iterations will attach periodic, system-generated summaries when transcripts grow large, so prompt assembly can feed a concise memory while the full history remains available for exports and lore audits.

### Implementation approach (first-party .NET only)
- **Pipeline orchestration**: `INarrationService` will call a `NarrationPipeline` helper that enumerates stages as an `IAsyncEnumerable<NarrationLifecycleEvent>`. Every stage `yield return`s `StageStarting/Completed/Failed` events so Blazor components can `await foreach` and react in real time without third-party frameworks.
- **Hook wiring**: Each stage resolves its `IStageHook` implementations via the built-in DI container (`IEnumerable<IStageHook>`). Hooks mutate the shared `NarrationPipelineContext`, log via `Microsoft.Extensions.Logging`, and optionally emit sub-events by writing to the shared event stream.
- **Event transport**: Per pipeline run, a `System.Threading.Channels.Channel<NarrationLifecycleEvent>` buffers events. The pipeline writes into the channel; the UI reads via `channel.Reader.ReadAllAsync()` (the Blazor status indicator can subscribe through a simple `event EventHandler<NarrationLifecycleEvent>` shim).
- **UI updates**: Components replace the coarse “typing…” indicator with stage-specific notifications (e.g., “Checking safety…”, “Selecting model…”). Because the stream is first-party `IAsyncEnumerable`, no extra packages are needed to consume it.
- **Failure/short-circuit handling**: Hooks throw descriptive exceptions captured by the pipeline. Before bubbling, the pipeline emits a `StageFailed` event plus a terminal `output.failed`, enabling UI toast notifications while keeping the existing `NarrationStatus` fallback for compatibility.
- **Multiple listeners per stage**: Within a stage, hooks execute sequentially (or opportunistically in parallel when safe). This lets power users chain multiple model invocations or processors (e.g., Model A → Model B → final narration) simply by registering additional hooks—each listener completes before the next runs, keeping lifecycle events deterministic.
- **Future image sketch workflow**: The same pipeline can branch into an image workflow (e.g., after `PromptAssembler`). When enabled, hooks assemble an image prompt, route it to the user’s configured image model, stream `image.generated` events, and pass scene sketches to the `OutputFormatter` so artwork appears alongside narrated text.
- **Workflow system prompts**: Each workflow (Narrator, System, Image) will expose user-configurable system prompts so players can tune the narrator voice or image style (e.g., rough sketch vs. photorealistic). Those prompts feed the PromptAssembler stage and travel with export/import data.
- **Scenario export/import hooks**: Because the MemoryManager already tracks transcripts, personas, and rolling summaries, dedicated save/load hooks can serialize the current pipeline context into a portable file (for backups or device transfers) and hydrate it later. A `scenario.exported`/`scenario.imported` event pair keeps the UI informed when players save progress or resume adventures on a fresh install.

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
`SPEC.md` maintains the backlog in a Scrum-style format with acceptance criteria, status, assignee, and any supporting discussion. Developers must keep it updated when starting work (status/assignee) and when finishing (technical summary + linked tests per the unit/component convention). Treat it as the authoritative backlog for upcoming and in-progress work.

## Contributing
See `CONTRIBUTORS.md` for coding guidelines, testing expectations, and how to work with `SPEC.md`.

## License
GPL-3.0-or-later. See `LICENSE`.
