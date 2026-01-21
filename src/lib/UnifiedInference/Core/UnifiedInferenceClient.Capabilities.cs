using UnifiedInference.Abstractions;
using UnifiedInference.Core;
using UnifiedInference.Providers.HuggingFace;
using UnifiedInference.Providers.Ollama;
using UnifiedInference.Providers.OpenAI;

namespace UnifiedInference.Core;

public interface ICapabilitiesProvider
{
    Task<ModelCapabilities> GetAsync(InferenceProvider provider, string modelId, CancellationToken cancellationToken);
}

public sealed class DefaultCapabilitiesProvider : ICapabilitiesProvider
{
    private readonly OpenAiCapabilities _openAi;
    private readonly OllamaCapabilities _ollama;
    private readonly HuggingFaceCapabilities _huggingFace;

    public DefaultCapabilitiesProvider(
        OpenAiCapabilities? openAi = null,
        OllamaCapabilities? ollama = null,
        HuggingFaceCapabilities? huggingFace = null)
    {
        _openAi = openAi ?? new OpenAiCapabilities();
        _ollama = ollama ?? new OllamaCapabilities();
        _huggingFace = huggingFace ?? new HuggingFaceCapabilities();
    }

    public Task<ModelCapabilities> GetAsync(InferenceProvider provider, string modelId, CancellationToken cancellationToken) =>
        provider switch
        {
            InferenceProvider.OpenAI => _openAi.GetAsync(modelId, cancellationToken),
            InferenceProvider.Ollama => _ollama.GetAsync(modelId, cancellationToken),
            InferenceProvider.HuggingFace => _huggingFace.GetAsync(modelId, cancellationToken),
            _ => Task.FromResult(ModelCapabilitiesDefaults.Disabled())
        };
}
