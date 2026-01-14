namespace Narratoria.Pipeline.Transforms.Llm.Providers;

public sealed record OpenAiProviderOptions
{
    public required string ApiKey { get; init; }

    public required string Model { get; init; }

    public Uri? BaseUri { get; init; }
}

public sealed record HuggingFaceProviderOptions
{
    public required string ApiToken { get; init; }

    public required string Model { get; init; }

    public Uri? BaseUri { get; init; }
}
