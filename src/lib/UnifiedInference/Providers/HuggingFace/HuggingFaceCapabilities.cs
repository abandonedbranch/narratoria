using UnifiedInference.Abstractions;
using UnifiedInference.Core;

namespace UnifiedInference.Providers.HuggingFace;

public sealed class HuggingFaceCapabilities
{
    public Task<ModelCapabilities> GetAsync(string modelId, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var id = modelId.ToLowerInvariant();

        var supportsImage = id.Contains("stable-diffusion") || id.Contains("sdxl") || id.Contains("diffusion");

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

        var caps = new ModelCapabilities(
            SupportsText: true,
            SupportsImage: supportsImage,
            SupportsAudioTts: false,
            SupportsAudioStt: false,
            SupportsVideo: false,
            SupportsMusic: false,
            Support: settings
        );
        return Task.FromResult(caps);
    }
}
