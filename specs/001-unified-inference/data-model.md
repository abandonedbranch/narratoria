# Phase 1: Data Model (HF-Only)

## Entities

- GenerationSettings
  - temperature: float [0..2]
  - top_p: float [0..1]
  - top_k: int >= 0
  - max_new_tokens: int >= 1
  - do_sample: bool?
  - repetition_penalty: float?
  - return_full_text: bool?
  - stop_sequences: string[]
  - seed: int? (when HF pipeline supports)
  - diffusion options: guidance_scale: float?, num_inference_steps: int?, height/width: int?, scheduler: string?, negative_prompt: string?
  - cache/warmth: use_cache?: bool, wait_for_model?: bool
  - ProviderOverrides: Map<string, object> for passthrough HF parameters

- InferenceProvider (enum)
  - HuggingFace

- ModelCapabilities
  - supportsText: bool
  - supportsImage: bool
  - supportsAudioTts: bool (best-effort; default false)
  - supportsAudioStt: bool (best-effort; default false)
  - supportsVideo: bool (best-effort; default false)
  - supportsMusic: bool (hooks-only; default false)
  - settingsSupport: Map<string, bool> (temperature, top_p, top_k, max_new_tokens, do_sample, repetition_penalty, stop, seed, diffusion params)
  - pipeline_tag: string? (e.g., text-generation, image-to-image)
  - gated: bool
  - inference_status: string? (loaded/loading/scale-to-zero/failed)
  - notes: string?

- Requests/Responses
  - TextRequest/TextResponse
    - TextRequest: modelId, prompt/messages, stream?: bool, settings
    - TextResponse: text, tokensUsed?, providerMetadata (raw HF response)
  - ImageRequest/ImageResponse
    - ImageRequest: modelId, prompt, negativePrompt?, height?, width?, settings
    - ImageResponse: bytes or uri, providerMetadata
  - AudioRequest/AudioResponse (best-effort)
    - AudioRequest: modelId, mode (TTS/STT), textInput or audio bytes, language?, voice?, settings
    - AudioResponse: audioBytes or transcriptText, providerMetadata
  - VideoRequest/VideoResponse (best-effort hooks)
    - VideoRequest: modelId, prompt, duration?, quality?, settings
    - VideoResponse: bytes or uri, providerMetadata
  - MusicRequest/MusicResponse (hooks-only, default unsupported)

## Relationships

- GenerationSettings apply to all requests; HF mapping logic selects applicable parameters per pipeline_tag.
- ModelCapabilities are fetched per modelId from HF metadata and cached; generation must check capabilities before execution.
- ProviderOverrides allow explicit HF parameters (e.g., `use_cache`, custom scheduler) without expanding the unified surface.

## Validation Rules

- Block or throw `NotSupportedException` when capabilities mark a modality or setting unsupported/gated.
- `modelId` must be non-empty; pipeline_tag must match requested modality when known.
- Numeric bounds: `max_new_tokens` >= 1; `top_k` >= 0; `temperature` in [0,2]; `top_p` in (0,1]; `guidance_scale` >= 0; `num_inference_steps` >= 1 when provided.
- Audio STT requires audio input; TTS requires text input; video/music default to unsupported unless capability allows.

## State Transitions

- None persisted; request lifecycle: Created → Validated → Executed → Response/Error.
