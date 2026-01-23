using System.Collections.Generic;

namespace UnifiedInference.Abstractions;

public sealed record ModelCapabilities
{
    public bool SupportsText { get; init; }
    public bool SupportsImage { get; init; }
    public bool SupportsAudioTts { get; init; }
    public bool SupportsAudioStt { get; init; }
    public bool SupportsVideo { get; init; }
    public bool SupportsMusic { get; init; }
    public IDictionary<string, bool> SettingsSupport { get; init; } = new Dictionary<string, bool>();
    public string? PipelineTag { get; init; }
    public bool Gated { get; init; }
    public string? InferenceStatus { get; init; }
    public string? Notes { get; init; }
}
