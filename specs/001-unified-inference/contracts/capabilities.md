# Capability Rules (HF-Only)

## Defaults
- Unknown models: all modalities disabled; all settings unsupported until `/api/models` metadata retrieved.
- Capability discovery is per `modelId`; do not assume HF-wide defaults.
- Unsupported or gated modalities/settings must be blocked or throw `NotSupportedException`.

## Hugging Face Heuristics
- Use `/api/models/{id}` or `/api/models?search=...` to read `pipeline_tag`, `tags`, `gated`, and `inference` status.
- Text: enabled when `pipeline_tag` is `text-generation`, `text2text-generation`, or `conversational`.
- Image: enabled when `pipeline_tag` is diffusion/image (e.g., `text-to-image`, `image-to-image`, `image-to-3d`).
- Audio (best-effort): enabled when `pipeline_tag` is `text-to-speech` or `automatic-speech-recognition`; otherwise disabled.
- Video/Music: disabled by default unless `pipeline_tag` explicitly matches video/music; still best-effort and likely unsupported.
- Gated models: mark as unsupported unless caller supplies allow-listed token in overrides.
- Cold/failed models (`inference.status` != `loaded`): treat as temporarily unsupported unless `wait_for_model=true` is set.
- Supported settings: temperature, top_p, top_k, max_new_tokens, do_sample, repetition_penalty, return_full_text, stop; diffusion: guidance_scale, num_inference_steps, height/width, negative_prompt, scheduler; seed only when pipeline supports.

## Caller Guidance
- Always call `GetCapabilitiesAsync(modelId)` before invoking a modality.
- Treat `gated=true` or `inference_status != loaded` as a hard gate unless overrides explicitly opt in.
- When settings are unsupported, omit them or expect `NotSupportedException`.
- If a model is gated/cold/unsupported, fall back to another model whose capabilities allow the requested modality and settings before retrying.
