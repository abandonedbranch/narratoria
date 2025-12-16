## spec: stage-metadata-provider

mode:
  - compositional (collects and formats per-stage metadata from middleware/provider; no owned persistence)

behavior:
  - what: Collect duration, tokens, and model info per stage and format `NarrationStageHover` payloads for UI chips.
  - input:
      - NarrationStageTelemetry : events with stage, status, elapsed
      - Provider metrics : tokens and model per completion
  - output:
      - NarrationStageHover : { TurnId, Stage, Duration?, PromptTokens?, CompletionTokens?, Model? }
  - caller_obligations:
      - supply consistent identifiers for turns and stages
  - side_effects_allowed:
      - none (pure aggregation)

state:
  - none (stateless aggregation functions)

preconditions:
  - telemetry events and provider metrics reference known stages/turns

postconditions:
  - hover payloads include available fields; missing fields remain null

invariants:
  - deterministic merge order: telemetry then provider metrics; provider values win on overlap

failure_modes:
  - data_mismatch :: missing identifiers :: emit warning; return null hover

policies:
  - no retries or IO; aggregation is in-memory

never:
  - expose raw prompt contents

non_goals:
  - metric storage or reporting beyond aggregation

performance:
  - aggregate under 2ms per stage

observability:
  - logs:
      - trace_id, session_id, turn_id, stage, hover_emitted=true|false
  - metrics:
      - stage_hover_emitted_count

output:
  - minimal implementation only (no commentary, no TODOs)
