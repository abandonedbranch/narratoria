namespace Narratoria.Pipeline.Transforms.Llm.Providers;

public sealed record GenerationSettings
{
    public double? Temperature { get; init; }

    public int? MaxOutputTokens { get; init; }
}

public sealed record TextGenerationRequest
{
    public required string Prompt { get; init; }

    public GenerationSettings Settings { get; init; } = new();
}

public sealed record TextGenerationMetadata
{
    public string? Model { get; init; }

    public int? InputTokens { get; init; }

    public int? OutputTokens { get; init; }
}

public sealed record TextGenerationResponse
{
    public required string GeneratedText { get; init; }

    public TextGenerationMetadata Metadata { get; init; } = new();
}
