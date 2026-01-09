# Research: LLM Story Transforms

**Feature**: [spec.md](spec.md)  
**Plan**: [plan.md](plan.md)  
**Date**: 2026-01-08

## Provider Strategy

### Decision: Two injectable provider implementations behind a tiny interface

- **OpenAI provider**: uses the official OpenAI .NET library (SDK-managed HTTP)
- **Hugging Face provider**: uses `HttpClient` to call the Hugging Face Inference REST API directly

**Rationale**

- Keeps pipeline transforms IO-free except through a single injected abstraction.
- Enables deterministic tests by swapping in an in-memory fake provider.
- Avoids overfitting transforms to one vendor’s request/response formats.

**Alternatives considered**

- A single "universal" REST client: rejected because OpenAI SDK is preferred and HF payloads vary by task/model.
- A framework agent/orchestration library (LangChain, Semantic Kernel): rejected for now due to added surface area and non-trivial abstractions not required by the spec.

## Hugging Face Inference REST API

### Decision: Call the model endpoint directly with `inputs` + optional `parameters`

**Rationale**

- Hugging Face’s Inference API commonly accepts a JSON body shaped like:
  - `inputs`: the input string (we supply a composed prompt)
  - `parameters`: optional generation controls (e.g., max tokens, temperature)
  - `options`: optional flags (e.g., wait_for_model)
- Using a typed request/response model keeps the integration maintainable and testable.

**Alternatives considered**

- Using a hosted Inference Endpoint with custom schema: deferred; plan targets the public API shape first.

## OpenAI .NET SDK

### Decision: Use the official OpenAI .NET SDK as the OpenAI integration

**Rationale**

- Satisfies the user requirement to use the official library.
- The SDK handles base URL, auth headers, and request construction, reducing custom HTTP code.

**Alternatives considered**

- Raw `HttpClient` for OpenAI: rejected due to requirement to use the official library.
- Azure OpenAI SDK: out of scope unless explicitly requested.

## Prompt and Output Strategy

### Decision: Use structured outputs for state extraction transforms

- Rewrite transform outputs natural language narration.
- Summary transform outputs natural language summary.
- Character and inventory transforms output structured updates suitable for parsing and merging into story state.

**Rationale**

- Structured outputs reduce ambiguity and support provenance/confidence requirements.
- Enables robust error handling when model responses are malformed (keep prior state unchanged).

**Alternatives considered**

- Free-form parsing with regex: rejected due to brittleness.

## State Handling Across Streaming

### Decision: Store story state as JSON in `PipelineChunkMetadata.Annotations`

**Rationale**

- This repo’s pipeline types currently support chunk metadata annotations as string key/value.
- JSON allows structured state (summary/characters/inventory) without new chunk types.
- Keeps transforms composable and side-effect free (state is carried forward with the stream).

**Alternatives considered**

- Adding new chunk types for summary/state: deferred; would expand public surface area and requires more compatibility handling.

## Error and Resilience

### Decision: Degrade gracefully on provider failure

- If provider call fails or parsing fails, pass through the best-available narration text and keep prior state unchanged.

**Rationale**

- Matches spec requirements for non-corruption and continuity.
- Maintains pipeline robustness when external services are flaky.

## Resolved Clarifications

- Provider choice: OpenAI via official .NET SDK; Hugging Face via direct REST calls.
- JSON schema difference: OpenAI is SDK; HF uses `inputs`/`parameters` style payload.
- DI requirement: both providers are injectable services.
