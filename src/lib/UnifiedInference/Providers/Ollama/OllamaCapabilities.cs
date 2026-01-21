using UnifiedInference.Abstractions;
using UnifiedInference.Core;

namespace UnifiedInference.Providers.Ollama;

public sealed class OllamaCapabilities
{
    public Task<ModelCapabilities> GetAsync(string modelId, CancellationToken cancellationToken)
    {
        _ = modelId;
        _ = cancellationToken;

        var settings = new CapabilitySettings(
            Temperature: true,
            TopP: true,
            TopK: true,
            MaxTokens: true,
            PresencePenalty: false,
            FrequencyPenalty: false,
            StopSequences: true,
            Seed: false
        );

        return Task.FromResult(ModelCapabilitiesDefaults.TextOnly(settings));
    }
}
