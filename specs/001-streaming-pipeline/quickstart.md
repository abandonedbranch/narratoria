# Quickstart: Streaming Narration Pipeline

This quickstart describes minimal, non-UI verification scenarios for the streaming narration pipeline API surface.

## Scenario 1: Minimal pipeline (text source → sink)

- Configure a text source with a prompt equivalent to "Hello".
- Execute a pipeline with no transforms and a collecting sink.
- Verify:
  - The sink observes at least one chunk before the run completes.
  - The run completes with a terminal outcome.

## Scenario 2: Transform chain

- Add a prefix transform (e.g., prefix with "[SAFE] ").
- Execute the pipeline.
- Verify:
  - The sink’s collected text includes the prefix.
  - Chunk ordering is preserved.

## Scenario 3: Streaming bytes with explicit decode contract

- Provide prompt input as a byte stream where metadata declares a supported text decode contract.
- Execute the pipeline.
- Verify:
  - The pipeline begins without buffering the entire input.
  - Bytes→text normalization only succeeds when a decode contract is declared.

## Scenario 4: Cancellation

- Start a pipeline run and cancel it mid-stream.
- Verify:
  - No further chunks are delivered after cancellation.
  - The run terminates with a canceled outcome.
