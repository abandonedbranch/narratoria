# Phase 0 Research: UnifiedInference HF-Only

## Decisions

- Scope: Restrict to Hugging Face only; remove OpenAI and Ollama paths from spec and capability rules.
- Client Library: Adopt tryAGI/HuggingFace client (NuGet ID/version to be pinned; assumed to wrap HF Inference API). Use `HttpClient` fallback only if a needed modality API is missing.
- Endpoint Usage: Use the universal POST `https://api-inference.huggingface.co/models/{modelId}` with bearer token; allow base URL override for private endpoints.
- Capability Discovery: Pull from `https://huggingface.co/api/models` (pipeline_tag, gated, inference status) and cache; treat gated/unloaded models as unsupported unless allow-listed.
- Settings Mapping: Map `GenerationSettings` to HF parameters (`temperature`, `top_p`, `top_k`, `max_new_tokens`, `do_sample`, `repetition_penalty`, `return_full_text`, `stop`, `seed` when supported). For diffusion, map `guidance_scale`, `num_inference_steps`, `height/width`, `scheduler`, and optional `negative_prompt`.
- Resilience: Add retry/backoff on 503 with `wait_for_model=true` and honor `Retry-After`; allow `use_cache=false` override to force fresh generations.
- Modalities: Support text and image now; audio/video remain best-effort if tryAGI/HuggingFace exposes them, otherwise mark unsupported and throw `NotSupportedException` per constitution.
- Performance Goal: Align with HF warm-path expectations (text p50 < 2s, image p50 < 15s) and avoid unnecessary retries; treat rate-limit errors as caller-visible.

## Rationale

- HF-only simplifies surface area and matches team direction while leveraging the broad HF model catalog and billing.
- tryAGI/HuggingFace provides typed helpers over the Inference API, reducing manual payload shaping; retaining `HttpClient` fallback avoids lock-in if gaps arise.
- Using `/api/models` metadata enables safer capability gating (pipeline tags, gating, status) than name heuristics.
- Expanded settings mapping brings parity with HF pipelines and avoids underpowered generations versus HF UI defaults.
- Explicit resilience for cold starts and cache control matches HF guidance and prevents opaque failures.

## Alternatives Considered

- Keep OpenAI/Ollama for “just in case”: rejected to reduce surface and maintenance; can be reintroduced via new spec if needed.
- Only raw `HttpClient` with custom JSON: rejected; tryAGI/HuggingFace likely covers retries/serialization; fallback retained for gaps.
- Full multimodal (video/audio) commitment: rejected until tryAGI/HuggingFace confirms stable endpoints; mark unsupported to stay deterministic.

## Best Practices

- DI-Friendly: accept preconfigured `HttpClient`/tryAGI client; avoid static singletons.
- Cancellation: honor `CancellationToken` across all calls and retries.
- Immutability: prefer records/init-only for settings and responses; `ProviderOverrides` remains opaque.
- Mapping: document ignored/unsupported settings; for unsupported modalities throw `NotSupportedException` early using capability checks.
- Streaming: if tryAGI/HuggingFace exposes streaming, surface optional streaming APIs; otherwise return buffered responses.
