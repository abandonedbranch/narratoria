# Quickstart: Narrator Agent & Skills System (004)

## Prerequisites

- .NET SDK targeting `net10.0`
- xUnit test runner

## External Tools (`tools/`)

- Create a `tools/` directory at the repo root.
- Each tool is a discrete binary or script invoked by the agent.
- Contract:
  - Input via `stdin` (JSON): { actionId, skill, parameters, state, sessionId }
  - Output via `stdout` (JSON): { success, output, stateChanges, errors }
  - Exit code `0` on success; non-zero on failure.

### Example Stub (bash)

```bash
#!/usr/bin/env bash
# tools/inventory-tool
set -euo pipefail
input=$(cat)
cat <<JSON
{"success":true,"output":{"message":"ok"},"stateChanges":[],"errors":[]}
JSON
```

Make it executable:

```bash
chmod +x tools/inventory-tool
```

## Plan Execution Flow

1. Build context (player action + current state).
2. Generate plan via UnifiedInference.
3. Validate plan against `contracts/plan-schema.json`.
4. Execute actions:
   - In-proc skills call `ISkill.Execute()`.
   - External tools are invoked from `tools/` via adapter.
5. Apply atomic `stateChanges` with provenance.
6. Generate narration.

## Testing

- Unit: skill behaviors, plan validation, state invariants.
- Integration: end-to-end agent loop with external tool adapter.
- Contract: JSON schema validation for plan and state.
