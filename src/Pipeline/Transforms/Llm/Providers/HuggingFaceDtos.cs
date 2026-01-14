using System.Text.Json;
using System.Text.Json.Serialization;

namespace Narratoria.Pipeline.Transforms.Llm.Providers;

public sealed record HuggingFaceInferenceRequest
{
    [JsonPropertyName("inputs")]
    public required string Inputs { get; init; }

    [JsonPropertyName("parameters")]
    public Dictionary<string, JsonElement>? Parameters { get; init; }

    [JsonPropertyName("options")]
    public Dictionary<string, JsonElement>? Options { get; init; }
}

public sealed record HuggingFaceGeneratedTextResponse
{
    [JsonPropertyName("generated_text")]
    public required string GeneratedText { get; init; }
}
