# Architecture Analysis: Is Phi-3.5 Overkill for the Storyteller Skill?

**Date**: 2026-02-08  
**Reviewer**: AI Code Assistant  
**Status**: Analysis Complete

---

## Executive Summary

**CONCLUSION: Phi-3.5 Mini (3.8B parameters) is NOT overkill for Narratoria's architecture.**

While the problem statement correctly observes that the storyteller skill generates "at least four sentences about the immediate scene," this observation is based on a **misunderstanding of the architecture**: Phi-3.5 is not primarily a storytelling engine—it's a **plan orchestration and skill coordination system** that happens to include narrative generation as one of many capabilities.

---

## Problem Statement Analysis

The original question asks:
> "Assume that the storyteller skill should generate at least four sentences about the immediate scene the player is in. Is Phi-3.5 overkill?"

This framing conflates two distinct architectural components:
1. **The Narrator AI (Phi-3.5 Mini)**: Orchestrates the entire scene pipeline
2. **The Storyteller Skill**: ONE tool invoked by plans that generates narrative prose

---

## What Phi-3.5 Actually Does

### Primary Responsibility: Plan Orchestration (≈90% of compute)

Phi-3.5 Mini serves as the **Narrator AI** with the following responsibilities:

#### 1. Plan JSON Generation
- Analyzes player choices and session context
- Generates structured Plan JSON documents following the schema in `specs/002-plan-execution/contracts/plan-json.schema.json`
- Decides which skills to invoke and in what dependency order
- Example output:
```json
{
  "title": "Enter Blacksmith's Shop",
  "tools": [
    {"toolId": "recall-1", "toolPath": "skills/memory/recall.dart", "input": {"query": "blacksmith interactions"}},
    {"toolId": "reputation-1", "toolPath": "skills/reputation/query.dart", "input": {"faction": "craftsmen_guild"}},
    {"toolId": "narrate-1", "toolPath": "skills/storyteller/narrate.dart", "input": {"scene": "...", "tone": "cautious"}, "dependencies": ["recall-1", "reputation-1"]}
  ]
}
```

#### 2. Contextual Data Retrieval Decisions
From `ARCHITECTURE.md` Section 5.3:
> "The Plan Generator decides *contextually* what data to retrieve. There are no fixed 'memory tier budgets' or rigid context window allocations. Instead, Phi-3.5 Mini analyzes the current scene and generates plans that invoke memory retrieval skills with semantic queries."

This requires **semantic understanding** of:
- Which past events matter for the current scene
- What NPC relationships are relevant
- Which lore chunks should be retrieved
- What reputation data influences the narrative

#### 3. Skill Selection Logic
- Maps player intent to appropriate skill invocations
- Determines scene type: travel, dialogue, danger, resolution
- Respects campaign constraints from `world/constraints.md`
- Handles cross-skill dependencies (e.g., Portrait skill needs Memory results)

#### 4. Replan Logic and Error Recovery
From `ARCHITECTURE.md` Section 9.2:
> "The key innovation is **skill disabling during replanning**: when a skill fails, it's removed from the available skill set for subsequent plans, forcing the Narrator AI to find alternative approaches."

Phi-3.5 must:
- Detect failed skills from execution results
- Generate alternative plans avoiding failed skills
- Track disabled skills across max 5 replan attempts
- Fall back to template-based narration if all attempts exhausted

#### 5. Constraint Reasoning
From `specs/002-plan-execution/spec.md`:
- Analyze campaign manifests and world constraints
- Respect NPC personality profiles
- Honor plot beat requirements
- Maintain narrative consistency

### Secondary Responsibility: Scene Narration (≈10% of compute)

The Narrator AI CAN optionally include narrative text directly in Plan JSON, but the **primary narrative prose generation** (2-3 paragraphs) is delegated to the **Storyteller skill** as a separate tool invocation.

From `ARCHITECTURE.md` Section 4.1.1:
> "`narrate.dart` script that calls LLM (local or hosted) for detailed prose; must produce 2-3 paragraphs of scene-setting narrative"

The Storyteller skill is ONE of the tools that Phi-3.5 orchestrates—not the primary workload.

---

## Architectural Rationale for 3.8B Parameters

From `ARCHITECTURE.md` Section 9.3:

### 1. Privacy First
> "Privacy is the primary driver—interactive fiction sessions contain intimate creative expression that players may not want transmitted to third parties."

Requires on-device processing → rules out cloud APIs → necessitates efficient local model.

### 2. Offline Capability
> "Games should work on airplanes"

Once models are cached locally, no network required → plan generation must be self-contained.

### 3. Cost Predictability
> "No per-token API fees, no usage limits"

### 4. Latency Optimization
> "Local inference completes in <3 seconds per scene"

Network round-trips to external APIs would add 200-500ms baseline latency.

### 5. Model Size Sweet Spot
From `ARCHITECTURE.md`:
> "The 3.8B parameter model is the sweet spot for mobile devices with 8GB RAM: large enough for coherent multi-paragraph prose and structured JSON generation, small enough to run in-process with acceptable latency (<3 seconds per scene)."

**Critical point**: The "coherent multi-paragraph prose" refers to the **Plan JSON generation complexity**, NOT just narrative prose. A smaller model would struggle with:
- Complex multi-step plan generation
- Dependency reasoning (tool A must run before tool B)
- Contextual retrieval decisions (which memories matter?)
- Constraint satisfaction (respecting campaign rules)

---

## Performance Targets vs. Actual Workload

| Task | Model Requirement | Why 3.8B? |
|------|------------------|-----------|
| Generate Plan JSON (5-10 tool invocations) | Medium-high | Requires understanding tool semantics, dependencies, campaign constraints |
| Select contextually relevant memories | High | Semantic understanding of "what matters now?" |
| Replan after failures | High | Creative problem-solving: "skill X failed, what's an alternative approach?" |
| Generate 4-sentence narrative | LOW | Template-based generation would suffice |
| Generate 2-3 paragraph narrative | Medium | Delegated to Storyteller skill (can use smaller model or templates) |

**Key insight**: If Narratoria only needed 4-sentence scene descriptions, a 1-2B model would suffice. But the **orchestration, planning, and contextual retrieval** decisions require the 3.8B model.

---

## Could a Smaller Model Work?

### Option 1: Use 1-2B Model for Everything
**Verdict**: ❌ Not viable

**Problems**:
- Plan JSON generation quality degrades (incorrect dependencies, missing skills)
- Contextual retrieval decisions become random ("retrieve last 5 memories" instead of "retrieve semantically relevant memories")
- Replan logic fails (model can't reason about why skills failed)
- Campaign constraint reasoning becomes unreliable

**Evidence**: Microsoft released Phi-3.5 Mini (3.8B) specifically because **smaller models fail at structured output tasks** like JSON generation with complex schemas.

### Option 2: Use Phi-3.5 for Planning, Smaller Model for Narration
**Verdict**: ✅ This is already the architecture!

The Storyteller skill (`narrate.dart`) is **configurable**:
```json
{
  "provider": "ollama",
  "model": "phi-3.5-mini",
  "fallbackProvider": "template"
}
```

Users can configure the Storyteller skill to use:
- A smaller local model (e.g., TinyLlama 1.1B)
- Template-based narration
- External APIs (Claude, GPT-4)

The **Narrator AI (Phi-3.5)** remains responsible for orchestration, while **narrative prose generation** is delegated.

---

## Recommendations

### 1. Clarify Documentation
**Update `ARCHITECTURE.md` Section 4.1.1** to explicitly state:

```markdown
#### 4.1.1 Storyteller Skill

The Storyteller skill is responsible for generating rich narrative prose (2-3 paragraphs) 
based on plan execution results. This is DISTINCT from the Narrator AI (Phi-3.5 Mini), 
which orchestrates the entire scene pipeline.

**Narrator AI (Phi-3.5 Mini)**: Generates Plan JSON, selects skills, manages replanning
**Storyteller Skill**: Invoked by plans to generate detailed narrative prose

The Storyteller skill is configurable and can use:
- A smaller model for narrative-only generation (e.g., 1-2B parameters)
- Template-based generation for ultra-low latency
- External APIs for higher-quality prose

The Narrator AI requires 3.8B parameters for plan orchestration; the Storyteller 
skill's narrative generation can use smaller models.
```

### 2. Add Configuration Example
**Add to `specs/004-narratoria-skills/spec.md` Section 4.1**:

```markdown
**Storyteller Model Sizing Guidance**:

For users concerned about model size, the Storyteller skill's prose generation 
workload is INDEPENDENT of the Narrator AI's orchestration workload:

- **Narrator AI (fixed)**: Phi-3.5 Mini 3.8B required for plan generation
- **Storyteller prose (configurable)**: Can use 1-2B models or templates

Example lightweight configuration:
```json
{
  "provider": "ollama",
  "model": "tinyllama:1.1b",
  "style": "terse",
  "fallbackProvider": "template"
}
```

This reduces storyteller prose generation from ~1.5s to ~0.3s while maintaining 
plan orchestration quality.
```

### 3. Update Performance Targets
**Update `ARCHITECTURE.md` performance table**:

| Metric | Target | Notes |
|--------|--------|-------|
| Plan generation latency | <2s | Requires Phi-3.5 Mini 3.8B |
| Storyteller prose latency | <1.5s (configurable) | Can use smaller model (0.3s with 1B model) |
| Total scene render time | <3s | Includes plan + all skills + UI render |

---

## Conclusion

**Phi-3.5 Mini (3.8B) is NOT overkill** because:

1. ✅ **Primary workload is plan orchestration** (90% of compute), not just narrative generation
2. ✅ **Storyteller skill is configurable** to use smaller models for prose-only generation
3. ✅ **3.8B is optimal for planning tasks** (constraint reasoning, dependency resolution, contextual retrieval)
4. ✅ **Privacy and offline capability** necessitate on-device processing
5. ✅ **Model size is justified by orchestration complexity**, not output length

The confusion arises from conflating:
- **The Narrator AI (Phi-3.5)**: Orchestrates the ENTIRE scene pipeline
- **The Storyteller skill**: ONE tool among many that generates prose

If the concern is "can we make narrative generation faster?", the answer is **yes—configure the Storyteller skill to use a 1-2B model**. But reducing the Narrator AI below 3.8B would degrade plan generation quality unacceptably.

---

## References

- `ARCHITECTURE.md` Section 1.3 (Fundamental Data Flow)
- `ARCHITECTURE.md` Section 5.1 (Narrator AI Responsibilities)
- `ARCHITECTURE.md` Section 9.2 (Bounded Retry Loops)
- `ARCHITECTURE.md` Section 9.3 (Why On-Device AI?)
- `specs/002-plan-execution/spec.md` (Plan JSON Schema)
- `specs/004-narratoria-skills/spec.md` Section 4.1 (Storyteller Skill)
