using UnifiedInference.Abstractions;
using UnifiedInference.Core;
using UnifiedInference.Providers.HuggingFace;
using UnifiedInference.Providers.Ollama;
using UnifiedInference.Providers.OpenAI;

namespace UnifiedInference.Core;

public sealed partial class UnifiedInferenceClient : IUnifiedInferenceClient
{
    private readonly OpenAiInferenceClient _openAi;
    private readonly OllamaInferenceClient _ollama;
    private readonly HuggingFaceInferenceClient _huggingFace;
    private readonly ICapabilitiesProvider _caps;

    public UnifiedInferenceClient(
        OpenAiInferenceClient openAi,
        OllamaInferenceClient ollama,
        HuggingFaceInferenceClient huggingFace,
        ICapabilitiesProvider? capabilities = null)
    {
        _openAi = openAi;
        _ollama = ollama;
        _huggingFace = huggingFace;
        _caps = capabilities ?? new DefaultCapabilitiesProvider();
    }

    public async Task<TextResponse> GenerateTextAsync(TextRequest request, CancellationToken cancellationToken = default)
    {
        var caps = await _caps.GetAsync(request.Provider, request.ModelId, cancellationToken).ConfigureAwait(false);
        if (!caps.SupportsText)
        {
            throw Errors.ModalityNotSupported("text", request.Provider, request.ModelId);
        }

        return request.Provider switch
        {
            InferenceProvider.OpenAI => await _openAi.GenerateTextAsync(request, cancellationToken).ConfigureAwait(false),
            InferenceProvider.Ollama => await _ollama.GenerateTextAsync(request, cancellationToken).ConfigureAwait(false),
            InferenceProvider.HuggingFace => await _huggingFace.GenerateTextAsync(request, cancellationToken).ConfigureAwait(false),
            _ => throw Errors.ModalityNotSupported("text", request.Provider, request.ModelId)
        };
    }

    // Other modalities to be implemented in separate partials/files.
    public async Task<ImageResponse> GenerateImageAsync(ImageRequest request, CancellationToken cancellationToken = default)
    {
        var caps = await _caps.GetAsync(request.Provider, request.ModelId, cancellationToken).ConfigureAwait(false);
        if (!caps.SupportsImage)
        {
            throw Errors.ModalityNotSupported("image", request.Provider, request.ModelId);
        }

        return request.Provider switch
        {
            InferenceProvider.OpenAI => await _openAi.GenerateImageAsync(request, cancellationToken).ConfigureAwait(false),
            InferenceProvider.HuggingFace => await _huggingFace.GenerateImageAsync(request, cancellationToken).ConfigureAwait(false),
            _ => throw Errors.ModalityNotSupported("image", request.Provider, request.ModelId)
        };
    }

    public async Task<AudioResponse> GenerateAudioTtsAsync(AudioRequest request, CancellationToken cancellationToken = default)
    {
        var caps = await _caps.GetAsync(request.Provider, request.ModelId, cancellationToken).ConfigureAwait(false);
        if (!caps.SupportsAudioTts)
        {
            throw Errors.ModalityNotSupported("audio-tts", request.Provider, request.ModelId);
        }

        return request.Provider switch
        {
            InferenceProvider.OpenAI => await _openAi.GenerateAudioTtsAsync(request, cancellationToken).ConfigureAwait(false),
            _ => throw Errors.ModalityNotSupported("audio-tts", request.Provider, request.ModelId)
        };
    }

    public async Task<AudioResponse> GenerateAudioSttAsync(AudioRequest request, CancellationToken cancellationToken = default)
    {
        var caps = await _caps.GetAsync(request.Provider, request.ModelId, cancellationToken).ConfigureAwait(false);
        if (!caps.SupportsAudioStt)
        {
            throw Errors.ModalityNotSupported("audio-stt", request.Provider, request.ModelId);
        }

        return request.Provider switch
        {
            InferenceProvider.OpenAI => await _openAi.GenerateAudioSttAsync(request, cancellationToken).ConfigureAwait(false),
            _ => throw Errors.ModalityNotSupported("audio-stt", request.Provider, request.ModelId)
        };
    }

    public async Task<VideoResponse> GenerateVideoAsync(VideoRequest request, CancellationToken cancellationToken = default)
    {
        var caps = await _caps.GetAsync(request.Provider, request.ModelId, cancellationToken).ConfigureAwait(false);
        if (!caps.SupportsVideo)
        {
            throw Errors.ModalityNotSupported("video", request.Provider, request.ModelId);
        }

        // No providers currently implement video; route would go here when supported.
        throw Errors.ModalityNotSupported("video", request.Provider, request.ModelId);
    }

    public async Task<MusicResponse> GenerateMusicAsync(MusicRequest request, CancellationToken cancellationToken = default)
    {
        var caps = await _caps.GetAsync(request.Provider, request.ModelId, cancellationToken).ConfigureAwait(false);
        if (!caps.SupportsMusic)
        {
            throw Errors.ModalityNotSupported("music", request.Provider, request.ModelId);
        }

        // Music is hooks-only per spec; no provider implementations.
        throw Errors.ModalityNotSupported("music", request.Provider, request.ModelId);
    }

    public Task<ModelCapabilities> GetCapabilitiesAsync(InferenceProvider provider, string modelId, CancellationToken cancellationToken = default)
        => _caps.GetAsync(provider, modelId, cancellationToken);
}
