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

## Cancellation and “latest-wins”

- Transforms and providers are designed to honor `CancellationToken` so callers can implement a “latest input wins” policy by cancelling any prior in-flight run when new input arrives.
- If a consumer stops reading a stream early, upstream cancellation should be requested so any in-flight provider calls can terminate promptly.

## Where state lives

- Story state (summary/characters/inventory) is carried forward as JSON stored in `PipelineChunkMetadata.Annotations`.
- Original input text is preserved alongside rewritten text for traceability.

## Optional run metadata conventions

For forward compatibility with a future UI/editor spec, callers may attach these optional `PipelineChunkMetadata.Annotations` values:

- `narratoria.run_id` (string): stable identifier for one pipeline run invocation
- `narratoria.run_sequence` (int): monotonic sequence number within the run
- `narratoria.input_snapshot_sha256` (string): lowercase hex SHA-256 of the UTF-8 bytes of the full input text snapshot

Transforms should treat these as pass-through metadata when present.
