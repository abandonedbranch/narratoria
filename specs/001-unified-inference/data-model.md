# Phase 1: Data Model

## Entities

- GenerationSettings
  - temperature: float [0..2]
  - top_p: float [0..1]
  - top_k: int >= 0
  - max_tokens: int >= 1
  - presence_penalty: float
  - frequency_penalty: float
  - stop_sequences: string[]
  - seed: int? (provider-dependent)
  - ProviderOverrides: Map<string, object>

- InferenceProvider (enum)
  - OpenAI, Ollama, HuggingFace

- ModelCapabilities
  - supportsText: bool
  - supportsImage: bool
  - supportsAudioTts: bool
  - supportsAudioStt: bool
  - supportsVideo: bool (best-effort)
  - supportsMusic: bool (hooks-only; default false)
  - settingsSupport: Map<string, bool> (e.g., temperature, top_p, top_k, max_tokens, penalties, stop, seed)
  - notes: string?

- Requests/Responses
  - TextRequest/TextResponse
    - TextRequest: provider, modelId, messages/prompt, system role?, stream?: bool
    - TextResponse: text, tokensUsed, providerMetadata
  - ImageRequest/ImageResponse
    - ImageRequest: provider, modelId, prompt, size?, count?
    - ImageResponse: bytes or uri, providerMetadata
  - AudioRequest/AudioResponse
    - AudioRequest: provider, modelId, mode (TTS/STT), input (text or audio bytes), voice?, language?
    - AudioResponse: audioBytes or transcriptText, providerMetadata
  - VideoRequest/VideoResponse
    - VideoRequest: provider, modelId, prompt, duration?, quality?
    - VideoResponse: bytes or uri, providerMetadata
  - MusicRequest/MusicResponse (hooks-only)
    - MusicRequest: provider, modelId, prompt/parameters
    - MusicResponse: bytes or uri, providerMetadata

## Relationships

- GenerationSettings are applied to all Requests; per-provider mapping rules determine effective options.
- ModelCapabilities guide whether modality is permitted before making Requests.
- ProviderOverrides allow provider-specific tuning without expanding unified surface.

## Validation Rules

- If capabilities indicate unsupported modality or setting, prevent call or throw `NotSupportedException`.
- Ensure `modelId` is non-empty; provider is specified.
- `max_tokens`, `top_k` non-negative; `temperature/top_p` within bounds.
- Audio STT requires audio input; TTS requires text input.

## State Transitions

- None persisted; request lifecycle: Created → Validated → Executed → Response/Error.
