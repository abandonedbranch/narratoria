using System.Collections.Generic;

namespace UnifiedInference.Abstractions;

public sealed record GenerationSettings
{
    public float? Temperature { get; init; }
    public float? TopP { get; init; }
    public int? TopK { get; init; }
    public int? MaxNewTokens { get; init; }
    public bool? DoSample { get; init; }
    public float? RepetitionPenalty { get; init; }
    public bool? ReturnFullText { get; init; }
    public IReadOnlyList<string>? StopSequences { get; init; }
    public int? Seed { get; init; }

    // Diffusion options
    public float? GuidanceScale { get; init; }
    public int? NumInferenceSteps { get; init; }
    public int? Height { get; init; }
    public int? Width { get; init; }
    public string? Scheduler { get; init; }
    public string? NegativePrompt { get; init; }

    // HF execution options
    public bool? UseCache { get; init; }
    public bool? WaitForModel { get; init; }

    public IDictionary<string, object>? ProviderOverrides { get; init; }
}
