namespace Narratoria.OpenAi;

public sealed record OpenAiProviderCredentials(string ApiKey);

public sealed record OpenAiRequestPolicy(TimeSpan Timeout, bool Idempotent)
{
    public static readonly OpenAiRequestPolicy Default = new(TimeSpan.FromSeconds(30), true);
}

public sealed record OpenAiRequestContext(
    HttpClient Client,
    Uri Endpoint,
    OpenAiProviderCredentials Credentials,
    OpenAiRequestPolicy Policy,
    ILogger Logger,
    IOpenAiApiServiceMetrics Metrics,
    TraceMetadata Trace,
    IReadOnlyDictionary<string, string>? AdditionalHeaders = null);
