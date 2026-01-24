# Implementation Plan: Narrator Agent & Skills System

**Branch**: `004-narrator-agent-skills` | **Date**: 2026-01-24 | **Spec**: specs/004-narrator-agent-skills/spec.md
**Input**: Feature specification from specs/004-narrator-agent-skills/spec.md

## Summary

Agent-based narrator that generates a structured JSON plan, validates it, and executes plan actions via a unified skill system. Skills may be implemented in-process or as external CLI tools in a `tools/` directory (discrete binaries) that accept JSON input and emit JSON output. The agent loop orchestrates: Context → LLM Plan Generation (UnifiedInference) → JSON Plan → Skill/Tool Execution → Atomic State Update → Narration.

## Technical Context

**Language/Version**: C# on .NET `net10.0` (C# 13)
**Primary Dependencies**: UnifiedInference (spec 001), System.Text.Json, Microsoft.Extensions.Logging
**Storage**: N/A (persistence out of scope; state must be JSON-serializable)
**Testing**: xUnit for unit/integration; deterministic tests with injected time/IO
**Target Platform**: .NET library + external CLI tools (macOS/Linux/Windows)
**Project Type**: Single library with skill registry; optional `tools/` folder for binaries
**Performance Goals**: Plan execution ≤ 3s p90; individual skill ≤ 10s default timeout
**Constraints**: Cancellation honored; atomic updates per skill; consistent intermediate state across skills
**Scale/Scope**: Single-session state in memory; no distributed coordination

### External Tools Contract (CLI)

- Location: `tools/` at repo/app root; each tool is a discrete binary or script.
- Invocation: tool executed by agent with JSON input via stdin; JSON output via stdout.
- IO Format:
  - Input JSON: { "actionId": string, "skill": string, "parameters": object, "state": GameState, "sessionId": string }
  - Output JSON: { "success": bool, "output": object, "stateChanges": StateChange[], "errors": string[] }
- Exit Codes: 0 success; non-zero failure (agent reads JSON output for details).
- Timeouts: Enforced by agent (default 10s per tool); cancellation via graceful signal where supported.
- Provenance: Agent annotates stateChanges.provenance with tool name and timestamp.

## Constitution Check

- Specs are authoritative; no silent drift.
- Interfaces are tiny; composition over inheritance.
- Prefer immutability; IO/time/random isolated behind interfaces.
- Async/cancellation is explicit; no fire-and-forget.
- Tests are deterministic; no external non-determinism.

## Project Structure

### Documentation (this feature)

specs/004-narrator-agent-skills/
- plan.md              # This file (/speckit.plan output)
- research.md          # Phase 0 output (/speckit.plan)
- data-model.md        # Phase 1 output (/speckit.plan)
- quickstart.md        # Phase 1 output (/speckit.plan)
- contracts/           # Phase 1 output (schemas + OpenAPI)

### Source Code (repository root)

src/
- Narrator/
  - AgentLoop/
  - Planning/
  - Skills/
  - Execution/            # Skill registry, in-proc + external tool adapter
- lib/UnifiedInference/     # Existing (spec 001)

tools/                        # External CLI tools (discrete binaries)
- inventory-tool            # Example tool (add/remove/query)
- quest-tool                # Example tool (create/update/complete)
- ...                       # Other skills as needed

tests/
- unit/
- integration/
- contract/                 # Schema validation, plan validation

**Structure Decision**: Single .NET library (src/Narrator) with a skill registry and an external tool adapter to execute CLI tools from tools/. Tests organized by unit/integration/contract.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|--------------------------------------|
| External CLI tools | Enables polyglot skills and offline composition | Forcing all skills in-proc restricts ecosystem and experimentation |
