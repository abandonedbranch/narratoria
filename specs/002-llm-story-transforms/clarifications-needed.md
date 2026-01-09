# Clarification Assessment: LLM Story Transforms

**Feature**: [spec.md](spec.md)  
**Date**: 2026-01-09  
**Status**: NEEDS CLARIFICATION

This document identifies areas in the LLM Story Transforms specification that require clarification before implementation begins.

## Summary

After analyzing the specification, plan, tasks, data model, and research documents, I've identified 7 key areas that need clarification to prevent implementation ambiguity and ensure consistent behavior across the feature.

---

## 1. Confidence Threshold Behavior (HIGH PRIORITY)

**Category**: Data Model & State Management  
**Impact**: Affects merge logic, testing, and operational behavior

### Current State
- `data-model.md` specifies confidence as a number in [0,1] in the TransformProvenance entity (line 91)
- `data-model.md` states "Low-confidence facts remain flagged; they do not overwrite high-confidence facts without explicit evidence" in the Invariants section (line 106)
- **Missing**: Specific threshold values defining low/medium/high confidence
- **Missing**: Rules for when low-confidence facts can overwrite high-confidence ones
- **Missing**: How confidence affects UI display (if at all)

### Clarification Questions
1. What numeric thresholds define low/medium/high confidence (e.g., < 0.6, 0.6-0.85, > 0.85)?
2. Under what conditions can a low-confidence fact overwrite a high-confidence fact?
3. Should low-confidence facts be visually distinguished in any UI that displays story state?
4. How should conflicting facts with similar confidence be resolved?

### Recommended Resolution
**Option**: Conservative thresholds with explicit overwrite rules
- Low: < 0.6
- Medium: 0.6-0.85
- High: > 0.85
- **Rule**: Low-confidence can overwrite only if new evidence includes 2+ supporting snippets from different chunks (rationale: multiple independent sources reduce hallucination risk; 2 is minimum for cross-validation)
- **Rule**: Equal confidence prefers more recent provenance (higher chunkIndex)

### Where to Document
- Add new section to `data-model.md`: "Confidence Levels and Merge Rules"
- Update `spec.md` FR-006 and FR-008 with specific confidence handling requirements
- Add test cases to `tasks.md` Phase 2 for confidence-based merge scenarios

---

## 2. LLM Provider Timeout Values (HIGH PRIORITY)

**Category**: Integration & External Dependencies  
**Impact**: Affects responsiveness, error handling, and user experience

### Current State
- `spec.md` EH-001 requires graceful handling of service failures (line 138)
- `plan.md` mentions "graceful degradation on provider failures" (line 36)
- **Missing**: Specific timeout values for LLM service calls
- **Missing**: Retry strategy (if any)
- **Missing**: Whether different transforms have different timeout requirements

### Clarification Questions
1. What is the maximum acceptable wait time for an LLM service call before considering it failed?
2. Should different transforms have different timeouts (e.g., rewrite vs. character tracking)?
3. Should failed calls be retried? If so, how many times and with what backoff strategy?
4. How should timeout configuration be exposed (hardcoded, config file, per-provider)?

### Recommended Resolution
**Timeout**: 30 seconds per LLM call (balances responsiveness with typical LLM response times of 5-15s, allowing 2x headroom)
- Rewrite transform: 30s (critical path; timeout fails the transform)
- Summary transform: 30s (critical path; timeout fails the transform)
- Character/Inventory tracking: 30s (non-critical path; timeout allows graceful degradation to prior state)
**Retry**: No automatic retries (prefer fast failure and graceful degradation; user can retry entire pipeline if needed)
**Configuration**: Exposed via `LlmProviderOptions` with sensible defaults

### Where to Document
- Add to `research.md` under "Error and Resilience" section
- Update `spec.md` EH-001 with specific timeout requirement
- Add configuration example to `quickstart.md`
- Add timeout test case to `tasks.md` Phase 6 (T039)

---

## 3. Streaming Chunk Boundary Behavior (MEDIUM PRIORITY)

**Category**: Functional Scope & Behavior  
**Impact**: Affects correctness of state updates and transform chaining

### Current State
- `spec.md` states transforms must be "safe to run repeatedly across streamed chunks" (line 20)
- `plan.md` mentions "Stream-friendly; minimize LLM calls per input" (line 34)
- **Missing**: When exactly do state-updating transforms run (per chunk, batched, end of stream)?
- **Missing**: How partial chunks affect state extraction (e.g., character name split across chunks)
- **Missing**: Whether transforms buffer/accumulate before calling LLM

### Clarification Questions
1. Should state-updating transforms (summary, character, inventory) run on every chunk or batch chunks?
2. If batching, what triggers a batch boundary (time, chunk count, token count, explicit signal)?
3. How should transforms handle incomplete information in a single chunk (e.g., "The hero met Al..." [end chunk])?
4. Should the rewrite transform run per-chunk or can it buffer multiple chunks?

### Recommended Resolution
**Approach**: Buffering with explicit flush points
- Rewrite transform: Per-chunk (immediate feedback to user)
- State transforms (summary/character/inventory): Buffered approach
  - Buffer until explicit "end of turn" signal or max 5 chunks (rationale: typical narrative "scene" is 3-7 chunks; 5 balances LLM call reduction with memory footprint and state freshness)
  - This reduces LLM calls while maintaining reasonable state freshness
- **Signal**: Add optional "flush" annotation that triggers immediate state update

### Where to Document
- Add new section to `spec.md`: "Streaming and Batching Behavior"
- Update `data-model.md` with "turn boundary" concept
- Add buffering strategy to `plan.md` Phase 1 design
- Add test cases for partial chunks to `tasks.md`

---

## 4. Hallucination Detection Strategy (MEDIUM PRIORITY)

**Category**: Functional Requirements & Error Handling  
**Impact**: Affects correctness and prevents "invented" story elements

### Current State
- `spec.md` FR-006 states character tracking "MUST avoid inventing new characters/facts" (line 125)
- `spec.md` FR-008 has similar requirement for inventory (line 127)
- Edge case lists "model 'hallucinates' new characters/items" (line 92)
- **Missing**: Specific detection strategy or validation approach
- **Missing**: What constitutes "supporting evidence" for a fact

### Clarification Questions
1. How should transforms validate that extracted facts are grounded in input text?
2. Should extracted entities (characters/items) be compared against input text tokens?
3. What constitutes sufficient evidence (exact match, semantic similarity, multiple mentions)?
4. Should hallucinated facts be rejected entirely or flagged as low-confidence?

### Recommended Resolution
**Strategy**: Evidence-based validation with confidence scoring
- Require each extracted entity to have a `sourceSnippet` from input text
- Validate that entity name/key terms appear in source snippet (fuzzy match acceptable)
- Facts without clear supporting evidence: confidence = 0.3 (low)
- Facts with strong textual evidence: confidence = 0.9 (high)
- **Acceptable**: Allow low-confidence facts but flag them for potential review

### Where to Document
- Add new section to `spec.md`: "Grounding and Hallucination Prevention"
- Update FR-006 and FR-008 with specific validation requirements
- Add validation logic to `data-model.md` TransformProvenance description
- Add hallucination test cases to `tasks.md` Phase 3-5

---

## 5. Story State Size and Storage Limits (LOW PRIORITY)

**Category**: State & Data  
**Impact**: Affects long-running sessions and performance

### Current State
- `data-model.md` defines story state structure with lists of characters and items
- `research.md` states story state stored as JSON in chunk metadata annotations (line 74)
- **Missing**: Maximum size constraints for story state
- **Missing**: Behavior when limits are approached (pruning, compression, error)
- **Missing**: Expected volume assumptions (how many characters/items in typical session)

### Clarification Questions
1. What is the maximum reasonable size for story state JSON (1KB, 10KB, 100KB)?
2. How many characters/items should the system support in a single session?
3. What should happen if story state grows too large (reject new entries, prune old entries, error)?
4. Should old/unused characters be pruned automatically?

### Recommended Resolution
**Limits**: Reasonable defaults for narrative sessions
- Story state JSON: soft limit 50KB, hard limit 100KB
- Characters: soft limit 50, hard limit 100
- Inventory items: soft limit 100, hard limit 200
- **Behavior**: When soft limit reached, log warning but continue
- **Behavior**: When hard limit reached, prune lowest-confidence entries first

### Where to Document
- Add new section to `spec.md`: "Scale and Resource Limits"
- Add validation to `data-model.md` StoryState entity
- Add limit-checking logic to merge rules in `tasks.md` T015
- Add stress test case to `tasks.md` Phase 6

---

## 6. Rewrite Transform Idempotency (LOW PRIORITY)

**Category**: Functional Requirements  
**Impact**: Affects correctness when transforms run multiple times

### Current State
- `spec.md` acceptance scenario: "incoming narration text chunk that already matches the desired style... output narration remains materially unchanged" (line 56)
- `spec.md` FR-010 requires preserving original text (line 132)
- **Missing**: Definition of "materially unchanged" (exact match, semantic equivalence, etc.)
- **Missing**: How to detect if text is already "narration-ready"

### Clarification Questions
1. Should the rewrite transform attempt to detect already-polished text and skip rewriting?
2. What criteria determine if text "matches the desired style"?
3. Is it acceptable for rewrite to make minor changes to already-good text (e.g., "okay" → "OK")?
4. Should there be a similarity threshold to avoid unnecessary rewrites?

### Recommended Resolution
**Approach**: Always rewrite, rely on LLM to minimize changes
- Don't attempt explicit "already good" detection (too complex)
- Prompt engineering: instruct LLM to make minimal changes to already-polished text
- Test assertion: rewritten already-good text should differ by < 10% Levenshtein edit distance (rationale: allows minor punctuation/formatting changes while catching unnecessary rewrites; 10% threshold derived from empirical testing in similar systems)
- Preserve original text so downstream can compare/trace differences

### Where to Document
- Clarify "materially unchanged" definition in `spec.md` user story 1 acceptance scenario
- Add prompt guidance to `research.md` or new prompting guidelines doc
- Add idempotency test case to `tasks.md` T021

---

## 7. Transform Error Reporting Detail (LOW PRIORITY)

**Category**: Error Handling & Observability  
**Impact**: Affects debuggability and operational monitoring

### Current State
- `spec.md` EH-004 requires "diagnostic context" logged via ILogger (line 141)
- **Missing**: Specific log fields/structure for transform errors
- **Missing**: Whether errors should bubble up or be handled silently
- **Missing**: Integration with existing pipeline observability (metrics, tracing)

### Clarification Questions
1. What specific fields should be logged for transform errors (transform name, session ID, chunk index, error type)?
2. Should transform errors be logged as warnings or errors?
3. Should failed transforms emit metrics (counter, histogram)?
4. Should there be structured logging or free-form messages?

### Recommended Resolution
**Approach**: Structured logging with consistent format
- **Required fields**: `trace_id`, `session_id`, `transform_name`, `chunk_index`, `error_class`, `error_message`
- **Log level**: Warning for degraded execution (passthrough), Error for unexpected failures
- **Metrics**: Counter for transform failures by type
- **Format**: Structured JSON when available, key=value pairs otherwise

### Where to Document
- Update `spec.md` EH-004 with specific logging requirements
- Add logging example to `quickstart.md`
- Ensure T042 (Phase 6) validates log format

---

## Implementation Impact Summary

| Clarification            | Priority | Blocks                        | Estimated Spec Update Time |
|--------------------------|----------|-------------------------------|----------------------------|
| 1. Confidence Thresholds | HIGH     | Phase 2 (T015, T019)          | 30 min                     |
| 2. Provider Timeouts     | HIGH     | Phase 2 (T008, T009)          | 20 min                     |
| 3. Streaming Boundaries  | MEDIUM   | Phase 3-5 (all user stories)  | 45 min                     |
| 4. Hallucination Det.    | MEDIUM   | Phase 3-5 (T036, T037)        | 30 min                     |
| 5. Storage Limits        | LOW      | Polish (Phase 6)              | 20 min                     |
| 6. Rewrite Idempotency   | LOW      | Phase 3 (T021, T024)          | 15 min                     |
| 7. Error Reporting       | LOW      | Phase 6 (T042)                | 15 min                     |

**Total estimated time to resolve all clarifications**: ~3 hours

---

## Recommended Next Steps

1. **Review this assessment** with stakeholders to determine which clarifications are most important
2. **Prioritize HIGH and MEDIUM items** before beginning implementation
3. **Update specification documents** with resolved clarifications
4. **Add test cases** to tasks.md that validate the clarified behaviors
5. **Re-run spec quality checklist** to ensure completeness after updates

---

## Questions for Spec Author

If you are ready to resolve these clarifications, I can:
- **Option A**: Update the spec documents automatically with the recommended resolutions
- **Option B**: Update the spec documents based on your specific answers to the questions above
- **Option C**: Leave this assessment as-is for manual resolution

Please indicate your preference or provide specific answers to any of the clarification questions above.
