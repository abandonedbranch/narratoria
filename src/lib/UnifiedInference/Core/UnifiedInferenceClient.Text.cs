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

    public Task<TextResponse> GenerateTextAsync(TextRequest request, CancellationToken cancellationToken = default) =>
        request.Provider switch
        {
            InferenceProvider.OpenAI => _openAi.GenerateTextAsync(request, cancellationToken),
            InferenceProvider.Ollama => _ollama.GenerateTextAsync(request, cancellationToken),
            InferenceProvider.HuggingFace => _huggingFace.GenerateTextAsync(request, cancellationToken),
            _ => throw Errors.ModalityNotSupported("text", request.Provider, request.ModelId)
        };

    // Other modalities to be implemented in separate partials/files.
    public Task<ImageResponse> GenerateImageAsync(ImageRequest request, CancellationToken cancellationToken = default) =>
        throw Errors.ModalityNotSupported("image", request.Provider, request.ModelId);

    public Task<AudioResponse> GenerateAudioTtsAsync(AudioRequest request, CancellationToken cancellationToken = default) =>
        throw Errors.ModalityNotSupported("audio-tts", request.Provider, request.ModelId);

    public Task<AudioResponse> GenerateAudioSttAsync(AudioRequest request, CancellationToken cancellationToken = default) =>
        throw Errors.ModalityNotSupported("audio-stt", request.Provider, request.ModelId);

    public Task<VideoResponse> GenerateVideoAsync(VideoRequest request, CancellationToken cancellationToken = default) =>
        throw Errors.ModalityNotSupported("video", request.Provider, request.ModelId);

    public Task<MusicResponse> GenerateMusicAsync(MusicRequest request, CancellationToken cancellationToken = default) =>
        throw Errors.ModalityNotSupported("music", request.Provider, request.ModelId);

    public Task<ModelCapabilities> GetCapabilitiesAsync(InferenceProvider provider, string modelId, CancellationToken cancellationToken = default)
        => _caps.GetAsync(provider, modelId, cancellationToken);
}
