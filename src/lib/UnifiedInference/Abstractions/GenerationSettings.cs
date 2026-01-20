using System.Text.Json.Nodes;

namespace UnifiedInference.Abstractions;

public sealed record GenerationSettings(
    double? Temperature = null,
    double? TopP = null,
    int? TopK = null,
    int? MaxTokens = null,
    double? PresencePenalty = null,
    double? FrequencyPenalty = null,
    IReadOnlyList<string>? StopSequences = null,
    int? Seed = null,
    JsonObject? ProviderOverrides = null
);
