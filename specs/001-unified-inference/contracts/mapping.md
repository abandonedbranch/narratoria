# Mapping Rules: UnifiedInference

This document summarizes how unified `GenerationSettings` map to provider-specific options.

## Text Mapping
- OpenAI: `temperature`, `top_p`, `max_tokens`, `presence_penalty`, `frequency_penalty`, `stop` → mapped directly. `top_k`/`seed` ignored.
- Ollama: `temperature`, `top_p`, `top_k`, `num_predict` (from `max_tokens`), `stop` → mapped. Penalties ignored.
- Hugging Face: `temperature`, `top_p`, `top_k`, `max_new_tokens` (from `max_tokens`), `stop` → mapped. Penalties ignored.

See implementation in [SettingsMapper.Text.cs](../../src/lib/UnifiedInference/Core/SettingsMapper.Text.cs).

## Media Mapping
Image/Audio/Video mapping currently minimal and conservative; providers vary widely.

- Image (OpenAI): `ImageGenerationOptions` supports `Size` (enum), response formats. We avoid strict enums and default to provider defaults for stability.
- Image (Hugging Face): `parameters` passed as generic JSON via `SettingsMapperMedia.ToImageOptions()` (currently placeholder). Extend as models require.
- Audio TTS (OpenAI): `SpeechGenerationOptions` and `GeneratedSpeechVoice` used; select a voice by name where available.
- Audio STT (OpenAI): `AudioTranscriptionOptions.Language` optionally set via initializer to honor init-only.
- Video: Best-effort/optional; no mapping implemented.

See stubs in [SettingsMapper.Media.cs](../../src/lib/UnifiedInference/Core/SettingsMapper.Media.cs) for extension points.

## Overrides
Per-provider advanced options can be passed via `GenerationSettings.ProviderOverrides` (opaque JSON object). Document specific keys as needed (e.g., `hf_base_url`).
