namespace UnifiedInference.Abstractions;

public sealed record ModelCapabilities(
    bool SupportsText,
    bool SupportsImage,
    bool SupportsAudioTts,
    bool SupportsAudioStt,
    bool SupportsVideo,
    bool SupportsMusic,
    CapabilitySettings Support
)
{
    public static ModelCapabilities Disabled() => new(
        SupportsText: false,
        SupportsImage: false,
        SupportsAudioTts: false,
        SupportsAudioStt: false,
        SupportsVideo: false,
        SupportsMusic: false,
        Support: CapabilitySettings.All(false)
    );
}

public sealed record CapabilitySettings(
    bool Temperature,
    bool TopP,
    bool TopK,
    bool MaxTokens,
    bool PresencePenalty,
    bool FrequencyPenalty,
    bool StopSequences,
    bool Seed
)
{
    public static CapabilitySettings All(bool value) => new(
        Temperature: value,
        TopP: value,
        TopK: value,
        MaxTokens: value,
        PresencePenalty: value,
        FrequencyPenalty: value,
        StopSequences: value,
        Seed: value
    );
}
