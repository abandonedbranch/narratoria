# Security & Resilience Guidance (HF-Only)

## HTTP Clients
- Inject and reuse a single `HttpClient`; avoid per-request instantiation.
- Set timeouts at the client level (e.g., 30–60s) and flow `CancellationToken` through calls.
- The library retries 503 responses with `Retry-After` honoring; additional retry policies (Polly) can be added by callers if desired.

## Authentication
- Provide the HF token via constructor or `GenerationSettings.ProviderOverrides["hf_token"]`; requests use `Authorization: Bearer ...`.
- Do not log tokens; prefer environment variables or secret stores.

## Data Handling
- Image responses may be binary; callers should stream to disk or process without large in-memory buffering when possible.
- Capability lookups use the HF `/api/models/{id}` endpoint; respect HF rate limits and cache where possible.

## Headers & Content Types
- Payloads are JSON with `inputs`, `parameters`, and `options` fields; image responses can be binary or JSON wrappers.
- Custom headers can be injected via `ProviderOverrides` keys prefixed with `header:`.

## Resilience
- 503/cold starts: `wait_for_model` is set when retrying and `Retry-After` is honored to avoid hammering unloaded models.
- Unsupported modalities/settings result in `NotSupportedException` before issuing network calls when capabilities disallow.
