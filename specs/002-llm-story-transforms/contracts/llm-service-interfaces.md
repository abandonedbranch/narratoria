# Contracts: LLM Provider Interfaces

**Feature**: [spec.md](../spec.md)

This document defines the externally observable service contracts the LLM transforms rely on.

## Core Abstraction

### `ITextGenerationService`

**Intent**: Given a prompt, return generated text.

**Inputs**

- `prompt` (string)
- `settings` (GenerationSettings)
- `cancellationToken`

**Output**

- `generatedText` (string)
- optional metadata (model id, token counts) when available

**Notes**

- Implementations must be injectable.
- Implementations must honor cancellation.

## Provider Implementations

### OpenAI provider

- Uses the official OpenAI .NET SDK.
- Authenticated with an API key.
- Model is configurable.

### Hugging Face provider

- Uses `HttpClient` to call the Hugging Face Inference REST API.
- Authenticated with HF API token.
- Endpoint is derived from model id.

## Transform contracts

### Rewrite transform

- Input: `TextChunk`
- Output: `TextChunk` (rewritten text)
- Metadata: preserves original input in an annotation

### Summary transform

- Input: `TextChunk`
- Output: `TextChunk` (pass-through text)
- Metadata: updates summary annotation

### Character / Inventory transforms

- Input: `TextChunk`
- Output: `TextChunk` (pass-through text)
- Metadata: updates JSON story-state annotations
