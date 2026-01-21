using UnifiedInference.Abstractions;

namespace UnifiedInference.Providers.OpenAI;

public sealed class OpenAiCapabilities
{
    public Task<ModelCapabilities> GetAsync(string modelId, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var id = modelId.ToLowerInvariant();

        var supportsImage = id.Contains("gpt-4o") || id.Contains("gpt-4.1") || id.Contains("dall-e");
        var supportsTts = id.Contains("tts") || id.Contains("gpt-4o-audio") || id.Contains("gpt-4o-mini-tts");
        var supportsStt = id.Contains("whisper") || id.Contains("gpt-4o-audio");

        var settings = new CapabilitySettings(
            Temperature: true,
            TopP: true,
            TopK: false,
            MaxTokens: true,
            PresencePenalty: true,
            FrequencyPenalty: true,
            StopSequences: true,
            Seed: false
        );

        return Task.FromResult(new ModelCapabilities(
            SupportsText: true,
            SupportsImage: supportsImage,
            SupportsAudioTts: supportsTts,
            SupportsAudioStt: supportsStt,
            SupportsVideo: false,
            SupportsMusic: false,
            Support: settings
        ));
    }
}
