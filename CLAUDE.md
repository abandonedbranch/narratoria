# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Narratoria is an AI-backed narrative-driven storyteller and game master application. It's a Dart+Flutter cross-platform rich desktop app that leverages agents with skills and CLI tools to execute workflows for narrator AI.

## Build and Test Commands

```bash
# .NET tests (legacy, transitioning to Dart)
dotnet restore
dotnet test tests --configuration Release --no-restore

# Future Flutter commands (when lib/ is implemented)
flutter test
flutter test test/specific_test.dart  # Run single test
flutter analyze
```

## Architecture

This project follows a **specification-first, three-spec architecture**:

### Specification Hierarchy (`specs/`)

1. **001-tool-protocol-spec**: NDJSON protocol for tool communication
   - 6 event types: `log`, `state_patch`, `asset`, `ui_event`, `error`, `done`
   - Tools run as independent OS processes with stdin/stdout pipes
   - OpenAPI contract: `specs/001-tool-protocol-spec/contracts/tool-protocol.openapi.yaml`

2. **002-plan-generation-skills**: Plan generation and skill execution
   - JSON schema for executable plans
   - Agent Skills Standard (`skill.json`, `config-schema.json`, `prompt.md`)
   - Plan executor with topological sort, parallel execution, retry logic
   - Replan loop (max 5 attempts) with skill error tracking
   - Contracts: `specs/002-plan-generation-skills/contracts/`

3. **003-dart-flutter-implementation**: Flutter UI and runtime
   - Material Design 3 (dark theme)
   - Provider pattern for state management
   - SQLite via sqflite for persistence

### Core Architectural Patterns

- **Protocol-Boundary Isolation**: All tool interactions via NDJSON; tools are language-agnostic OS processes
- **Single-Responsibility Tools**: Each tool performs one well-defined task
- **Graceful Degradation**: System continues functioning when features fail
- **Bounded Retry Loops**: Per-tool (3), per-plan-execution (3), per-plan-generation (5)
- **Skill Error States**: `healthy`, `degraded`, `temporaryFailure`, `permanentFailure`

## Technology Stack

- **Runtime**: Dart 3.x + Flutter (latest stable)
- **UI**: Material Design 3 (dark theme)
- **State**: Provider (ChangeNotifier pattern)
- **Database**: SQLite (sqflite)
- **LLM Backend**: Ollama for local models (Gemma 2B, Llama 3.2 3B, Qwen 2.5 3B)
- **Tools**: Any language (Rust, Go, Python, Dart) - must comply with Spec 001

## Core Principles (Constitution v1.2.0)

See `.specify/memory/constitution.md` for full governance document.

1. **Dart+Flutter First**: All client logic in idiomatic Dart/Flutter
2. **Protocol-Boundary Isolation**: Tools run as independent processes via NDJSON
3. **Single-Responsibility Tools**: One well-defined task per tool
4. **Graceful Degradation**: Features degrade without crashing; bounded retries
5. **Testability and Composability**: Unit-testable modules, contract tests, integration tests

## Key Directories

- `specs/` - Three-spec architecture with contracts, plans, and checklists
- `.claude/commands/` - Speckit command files for AI workflow
- `.specify/memory/` - Project constitution
- `.specify/templates/` - Templates for specs, plans, tasks, checklists
- `.specify/scripts/bash/` - Automation scripts

## Speckit Workflow

Use speckit commands for specification-driven development:

```
/speckit.specify    # Create/update feature specification
/speckit.clarify    # Ask clarification questions, encode answers into spec
/speckit.plan       # Generate implementation plan from spec
/speckit.tasks      # Generate dependency-ordered tasks.md
/speckit.implement  # Execute implementation plan
/speckit.analyze    # Cross-artifact consistency analysis
/speckit.checklist  # Generate implementation checklist
```

## Contract Locations

- Tool Protocol: `specs/001-tool-protocol-spec/contracts/tool-protocol.openapi.yaml`
- Plan JSON Schema: `specs/002-plan-generation-skills/contracts/plan-json.schema.json`
- Execution Result: `specs/002-plan-generation-skills/contracts/execution-result.schema.json`
- Skill Manifest: `specs/002-plan-generation-skills/contracts/skill-manifest.schema.json`
