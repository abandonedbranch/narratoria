# Security & Resilience Guidance

## HTTP Clients
- Inject and reuse `HttpClient` instances; do not construct per request.
- Set reasonable timeouts (e.g., 30–60s) at the client level externally; the library honors `CancellationToken`.
- Configure retries/backoff using a handler (e.g., Polly) in the calling app; the library does not implement retries to avoid hidden behavior.

## Authentication
- Hugging Face: provide an access token via `GenerationSettings.ProviderOverrides["hf_token"]`; the client will set `Authorization: Bearer ...` on requests.
- OpenAI: provide the API key when constructing `OpenAIClient`.
- Ollama: typically local; secure remote deployments via network controls and any applicable auth.

## Data Handling
- For images, we stream responses with `ResponseHeadersRead` to reduce buffering; callers should write to disk or process streams promptly for large payloads.
- Be mindful of memory usage when materializing large media; consider streaming APIs in provider-specific clients when available.

## Headers & Content Types
- The library sets JSON bodies; When using Hugging Face endpoints, binary image responses are supported.
- Callers may add custom headers at the provided `HttpClient` level for advanced scenarios.

## Secrets
- Do not hardcode secrets; use environment variables or secure secret stores.
- Avoid logging tokens or payload content.
