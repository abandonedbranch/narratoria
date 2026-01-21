# Capability Rules (UnifiedInference)

## Defaults
- Unknown providers/models: all modalities disabled; all settings unsupported.
- Capability discovery is per provider **and** model. Do not assume a provider-wide capability.
- Unsupported modalities/settings must be gated client-side; calls should throw `NotSupportedException`.

## Provider Heuristics

### OpenAI
- Text: enabled for all models.
- Image: enabled for `gpt-4o*`, `gpt-4.1*`, `dall-e*` models.
- Audio TTS: enabled for models containing `tts`, `gpt-4o-audio`, or `gpt-4o-mini-tts`.
- Audio STT: enabled for `whisper*` and `gpt-4o-audio`.
- Video/Music: disabled by default (best-effort hooks only).
- Supported settings: temperature, top_p, max_tokens, presence_penalty, frequency_penalty, stop. top_k/seed unsupported.

### Ollama
- Text: enabled by default.
- Image/Audio/Video/Music: disabled by default.
- Supported settings: temperature, top_p, top_k, max_tokens, stop. Penalties/seed unsupported.

### Hugging Face
- Text: enabled by default for generic inference endpoints.
- Image/Audio/Video/Music: disabled unless explicitly modeled (future extension).
- Supported settings: temperature, top_p, top_k, max_tokens, stop. Penalties/seed unsupported by default.

## Caller Guidance
- Always call `GetCapabilitiesAsync(provider, modelId)` before invoking a modality.
- Treat any capability flag `false` as authoritative; do not force unsupported calls.
- When settings are unsupported, either omit them or expect `NotSupportedException`.
