# Agent Instructions for Narratoria

## Overview
This document provides guidance for AI agents and automated tools contributing to the Narratoria project.

## Primary Reference Document
**All contributions must align with the project vision, architecture, and technical decisions outlined in [README.md](./README.md).**

Before making any changes or additions to the codebase, agents should:
1. Read and understand the complete [README.md](./README.md)
2. Follow the established architectural patterns (local-first, Blazor WebAssembly, OpenAI-compatible API abstraction)
3. Respect the project's core goals and roadmap phases

## Key Architectural Principles from README.md

### Local-First Philosophy
- All features must work without requiring a backend server
- Data persists in browser storage (localStorage for MVP, IndexedDB for future)
- API keys and sensitive data remain client-side only
- Export/import capabilities for cross-device portability

### Technology Stack
- **Runtime**: .NET 9 SDK (all projects target `net9.0`)
- **Framework**: ASP.NET Blazor (WebAssembly preferred for local-first alignment)
- **Storage**: localStorage (MVP), planned migration to IndexedDB
- **API Integration**: OpenAI-compatible endpoints with pluggable provider architecture
- **State Management**: Local component state with persistence layer abstraction

### Code Organization
- Components live in `NarratoriaClient/Components/`
- Services in `NarratoriaClient/Services/`
- Layout components in `NarratoriaClient/Components/Layout/`
- JavaScript interop in `NarratoriaClient/wwwroot/js/`

## Contribution Guidelines

### When Adding Features
1. Check if the feature aligns with the current roadmap phase (see README.md)
2. Ensure local-first principles are maintained
3. Use the existing `IClientStorageService` abstraction for persistence
4. Follow established naming conventions (`.razor` for components, `.cs` for services)
5. Add corresponding CSS files for component-specific styles

### When Modifying Storage
- Abstract storage operations through service interfaces
- Maintain versioned schema compatibility
- Plan for localStorage → IndexedDB migration path
- Keep synchronous operations minimal to avoid UI blocking

### When Working with APIs
- Use the `OpenAiChatService` pattern for external API calls
- Support OpenAI-compatible endpoints generically
- Never expose API keys in logs or error messages
- Design for offline-first with graceful degradation

### When Creating UI Components
- Follow the existing component structure (`.razor` + `.razor.css`)
- Use the established layout system (`DockLayout`, `Grid`, `Row`, `Column`)
- Maintain accessibility standards
- Keep components focused and composable

### Testing Considerations
- Test storage quota limits and quota exhaustion scenarios
- Validate API provider abstraction with multiple endpoints
- Verify prompt composition and state serialization
- Test import/export round-trip fidelity

## Files to Review Before Contributing
- [README.md](./README.md) - **Primary reference for all architectural decisions**
- `NarratoriaClient/Services/AppDataService.cs` - Data model and persistence patterns
- `NarratoriaClient/Services/OpenAiChatService.cs` - API integration pattern
- `NarratoriaClient/Components/Layout/` - Layout component conventions

## What to Avoid
- Server-side dependencies that break local-first principle
- Hard-coded API endpoints or credentials
- Blocking synchronous storage operations in the UI thread
- Breaking changes to existing storage schemas without migration paths
- Features that skip the abstraction layers (direct localStorage access outside services)

## Questions and Ambiguities
If the README.md doesn't address a technical question:
1. Follow established patterns in the existing codebase
2. Prefer simpler, local-first solutions over complex architectures
3. Document new patterns clearly for future contributors
4. Consider opening the discussion in comments or documentation updates

## Summary
**The [README.md](./README.md) is the authoritative source for project vision, architecture, and technical direction. All agent contributions must honor the local-first, Blazor-based, OpenAI-compatible design established there.**
