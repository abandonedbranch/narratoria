using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Narratoria.Pipeline.Transforms.Llm.Providers;

public sealed class HuggingFaceTextGenerationService : ITextGenerationService
{
    private static readonly Uri DefaultBaseUri = new("https://api-inference.huggingface.co/models/", UriKind.Absolute);

    private readonly HttpClient _httpClient;
    private readonly HuggingFaceProviderOptions _options;
    private readonly ILogger<HuggingFaceTextGenerationService> _logger;

    public HuggingFaceTextGenerationService(
        HttpClient httpClient,
        HuggingFaceProviderOptions options,
        ILogger<HuggingFaceTextGenerationService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TextGenerationResponse> GenerateAsync(TextGenerationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var endpoint = _options.BaseUri ?? new Uri(DefaultBaseUri, _options.Model);

        var hfRequest = new HuggingFaceInferenceRequest
        {
            Inputs = request.Prompt,
            Parameters = BuildParameters(request.Settings),
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(hfRequest, options: SerializerOptions.Shared),
        };

        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiToken);

        using var response = await _httpClient.SendAsync(
            message,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var parsed = await response.Content.ReadFromJsonAsync<HuggingFaceGeneratedTextResponse[]>(
            SerializerOptions.Shared,
            cancellationToken).ConfigureAwait(false);

        if (parsed is null || parsed.Length == 0)
        {
            _logger.LogWarning("HuggingFace response was empty.");
            return new TextGenerationResponse { GeneratedText = string.Empty, Metadata = new TextGenerationMetadata { Model = _options.Model } };
        }

        return new TextGenerationResponse
        {
            GeneratedText = parsed[0].GeneratedText,
            Metadata = new TextGenerationMetadata { Model = _options.Model },
        };
    }

    private static Dictionary<string, JsonElement>? BuildParameters(GenerationSettings settings)
    {
        Dictionary<string, JsonElement>? parameters = null;

        if (settings.MaxOutputTokens is int maxTokens)
        {
            parameters ??= new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            parameters["max_new_tokens"] = JsonSerializer.SerializeToElement(maxTokens, SerializerOptions.Shared);
        }

        if (settings.Temperature is double temperature)
        {
            parameters ??= new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            parameters["temperature"] = JsonSerializer.SerializeToElement(temperature, SerializerOptions.Shared);
        }

        return parameters;
    }

    private static class SerializerOptions
    {
        public static readonly JsonSerializerOptions Shared = new(JsonSerializerDefaults.Web);
    }
}
