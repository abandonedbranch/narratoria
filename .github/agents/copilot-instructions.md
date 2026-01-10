# narratoria Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-01-08

## Active Technologies
- C# / .NET 10 + Existing `Narratoria.Pipeline` core; ASP.NET Core + Blazor Server UI host (browser UI required for IndexedDB); JSON serialization (System.Text.Json) (003-pipeline-log-ui)
- Client IndexedDB (single session auto-resume; bounded run-record store + compaction digest) (003-pipeline-log-ui)

- C# / .NET 10 (`net10.0`) + Official OpenAI .NET library (NuGet), `HttpClient`, `System.Text.Json` (002-llm-story-transforms)

## Project Structure

```text
src/
tests/
```

## Commands

# Add commands for C# / .NET 10 (`net10.0`)

## Code Style

C# / .NET 10 (`net10.0`): Follow standard conventions

## Recent Changes
- 003-pipeline-log-ui: Added C# / .NET 10 + Existing `Narratoria.Pipeline` core; ASP.NET Core + Blazor Server UI host (browser UI required for IndexedDB); JSON serialization (System.Text.Json)

- 002-llm-story-transforms: Added C# / .NET 10 (`net10.0`) + Official OpenAI .NET library (NuGet), `HttpClient`, `System.Text.Json`

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
