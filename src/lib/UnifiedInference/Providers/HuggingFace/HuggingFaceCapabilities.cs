using UnifiedInference.Abstractions;
using UnifiedInference.Core;

namespace UnifiedInference.Providers.HuggingFace;

public sealed class HuggingFaceCapabilities
{
    public Task<ModelCapabilities> GetAsync(string modelId, CancellationToken cancellationToken)
    {
        _ = modelId;
        _ = cancellationToken;

        // Conservative default: assume text generation supported; other modalities disabled unless explicitly modeled.
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
