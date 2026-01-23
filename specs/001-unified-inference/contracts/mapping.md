# Mapping Rules: UnifiedInference (HF-Only)

Mapping aligns `GenerationSettings` with Hugging Face Inference parameters. Unsupported settings are dropped unless explicitly allowed by model capabilities.

## Text Mapping (HF)
- `temperature` → `temperature`
- `top_p` → `top_p`
- `top_k` → `top_k`
- `max_new_tokens` → `max_new_tokens`
- `do_sample` → `do_sample`
- `repetition_penalty` → `repetition_penalty`
- `return_full_text` → `return_full_text`
- `stop_sequences` → `stop`
- `seed` → `seed` (only when pipeline supports it)

## Image Mapping (HF Diffusion)
- `guidance_scale` → `guidance_scale`
- `num_inference_steps` → `num_inference_steps`
- `height`/`width` → `height`/`width`
- `scheduler` → `scheduler`
- `negative_prompt` (request-level or setting) → `negative_prompt`
- `seed` → `seed` when supported

## Options
- `use_cache` and `wait_for_model` flow into `options`. `wait_for_model` defaults to `true` on retries for 503/cold paths.
- `ProviderOverrides` merges opaque keys into `parameters` unless a mapped key already exists. Special keys:
	- `hf_token`: overrides Authorization bearer token
	- `header:<Name>`: adds custom HTTP header

See implementation in [SettingsMapper.Text.cs](../../src/lib/UnifiedInference/Core/SettingsMapper.Text.cs) and [SettingsMapper.Media.cs](../../src/lib/UnifiedInference/Core/SettingsMapper.Media.cs).
