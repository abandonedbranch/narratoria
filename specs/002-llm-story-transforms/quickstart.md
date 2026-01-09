# Quickstart: LLM Story Transforms

This feature adds LLM-backed `IPipelineTransform`s that operate on `TextChunk` streams.

## What you get

- Rewrite transform: produces narration-ready prose
- Summary transform: maintains rolling recap
- Character tracker: maintains character roster
- Inventory tracker: maintains player inventory

## Expected chaining

Recommended order:

1. Rewrite
2. Summary
3. Character tracker
4. Inventory tracker

Character and inventory trackers should always run after summary so they can use the latest recap.

## Provider setup

You will configure one of:

- OpenAI provider (official OpenAI .NET SDK)
- Hugging Face provider (direct HTTP via `HttpClient`)

Both providers are injectable services behind the same tiny abstraction used by transforms.

## Using transforms in a pipeline

1. Build a `PipelineDefinition<TSinkResult>` with a text source (e.g., `TextPromptSource`).
2. Add the transforms in the order above.
3. Choose a sink (e.g., `TextCollectingSink`).
4. Run the definition with `PipelineRunner.RunAsync` and a `CancellationToken`.

## Deterministic testing

- Unit tests use a fake LLM provider that returns fixed outputs for given inputs.
- No tests should call external networks.

## Where state lives

- Story state (summary/characters/inventory) is carried forward as JSON stored in `PipelineChunkMetadata.Annotations`.
- Original input text is preserved alongside rewritten text for traceability.
