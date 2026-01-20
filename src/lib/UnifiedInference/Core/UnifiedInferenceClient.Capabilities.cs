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
    public Task<ModelCapabilities> GetAsync(InferenceProvider provider, string modelId, CancellationToken cancellationToken)
    {
        _ = provider;
        _ = modelId;
        _ = cancellationToken;
        return Task.FromResult(ModelCapabilitiesDefaults.Disabled());
    }
}
