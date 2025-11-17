using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace NarratoriaClient.Services;

public interface IOpenAiChatService
{
    IAsyncEnumerable<ChatCompletionUpdate> StreamChatCompletionAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default);
}

public sealed record ChatPromptMessage
{
    public string Role { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? Name { get; init; }
}

public sealed record ChatCompletionRequest
{
    public required string Model { get; init; }
    public IReadOnlyList<ChatPromptMessage> Messages { get; init; } = [];
    public double? Temperature { get; init; }
    public double? TopP { get; init; }
    public int? MaxTokens { get; init; }
    public double? PresencePenalty { get; init; }
    public double? FrequencyPenalty { get; init; }
    public IReadOnlyList<string>? StopSequences { get; init; }
    public IReadOnlyDictionary<string, object?>? AdditionalProperties { get; init; }
    public IReadOnlyDictionary<string, string>? AdditionalHeaders { get; init; }
}

public sealed record ChatCompletionUpdate(int ChoiceIndex, string? Role, string? ContentDelta, string? FinishReason, bool IsCompleted);

public sealed class OpenAiChatService : IOpenAiChatService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly IAppDataService _appData;
    private readonly ILogger<OpenAiChatService> _logger;
    private readonly ILogBuffer _logBuffer;

    public OpenAiChatService(HttpClient httpClient, IAppDataService appData, ILogger<OpenAiChatService> logger, ILogBuffer logBuffer)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _appData = appData ?? throw new ArgumentNullException(nameof(appData));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logBuffer = logBuffer ?? throw new ArgumentNullException(nameof(logBuffer));
    }

    public async IAsyncEnumerable<ChatCompletionUpdate> StreamChatCompletionAsync(ChatCompletionRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Model))
        {
            throw new ArgumentException("A model must be specified for the chat completion request.", nameof(request));
        }

        var apiSettings = await _appData.GetApiSettingsAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(apiSettings.Endpoint))
        {
            throw new InvalidOperationException("An API endpoint must be configured before invoking the OpenAI client.");
        }

        using var httpRequest = BuildHttpRequest(request, apiSettings);

        Log(LogLevel.Information, "Sending streaming request.", new Dictionary<string, object?>
        {
            ["model"] = request.Model,
            ["endpoint"] = apiSettings.Endpoint,
            ["messageCount"] = request.Messages.Count
        });

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            Log(LogLevel.Error, "Chat completion request failed.", new Dictionary<string, object?>
            {
                ["statusCode"] = (int)response.StatusCode,
                ["reason"] = response.ReasonPhrase ?? string.Empty
            });
            var errorPayload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException($"OpenAI-compatible API responded with status {(int)response.StatusCode} ({response.ReasonPhrase}). Payload: {errorPayload}");
        }

        Log(LogLevel.Debug, "Streaming response opened.", new Dictionary<string, object?>
        {
            ["statusCode"] = (int)response.StatusCode
        });

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(responseStream);

        var dataBuffer = new StringBuilder();

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (line is null)
            {
                break;
            }

            if (line.Length == 0)
            {
                if (dataBuffer.Length > 0)
                {
                    foreach (var update in ParseChunk(dataBuffer.ToString()))
                    {
                        yield return update;
                    }

                    dataBuffer.Clear();
                }

                continue;
            }

            if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var dataPart = line[5..].TrimStart();
            if (string.Equals(dataPart, "[DONE]", StringComparison.OrdinalIgnoreCase))
            {
                yield break;
            }

            if (dataBuffer.Length > 0)
            {
                dataBuffer.Append('\n');
            }

            dataBuffer.Append(dataPart);
        }

        if (dataBuffer.Length > 0)
        {
            foreach (var update in ParseChunk(dataBuffer.ToString()))
            {
                yield return update;
            }
        }

        Log(LogLevel.Information, "Streaming response completed.", new Dictionary<string, object?>
        {
            ["model"] = request.Model
        });
    }

    private HttpRequestMessage BuildHttpRequest(ChatCompletionRequest request, ApiSettings apiSettings)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, apiSettings.Endpoint);
        httpRequest.Headers.Accept.Clear();
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        httpRequest.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
        httpRequest.Headers.AcceptEncoding.Clear();
        httpRequest.Headers.AcceptEncoding.ParseAdd("identity");

        var hasCustomAuthorization = false;
        if (request.AdditionalHeaders is not null)
        {
            foreach (var header in request.AdditionalHeaders)
            {
                if (string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase))
                {
                    hasCustomAuthorization = true;
                }

                httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        if (!hasCustomAuthorization)
        {
            var apiKey = apiSettings.ApiKey ?? string.Empty;
            if (apiSettings.ApiKeyRequired && string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("The configured API requires an access key, but none was provided.");
            }

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }
        }

        var payload = BuildPayload(request);
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload, SerializerOptions), Encoding.UTF8, "application/json");

        return httpRequest;
    }

    private static Dictionary<string, object?> BuildPayload(ChatCompletionRequest request)
    {
        var payload = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["model"] = request.Model,
            ["stream"] = true,
            ["messages"] = BuildMessages(request.Messages)
        };

        if (request.Temperature.HasValue)
        {
            payload["temperature"] = request.Temperature.Value;
        }

        if (request.TopP.HasValue)
        {
            payload["top_p"] = request.TopP.Value;
        }

        if (request.MaxTokens.HasValue)
        {
            payload["max_tokens"] = request.MaxTokens.Value;
        }

        if (request.PresencePenalty.HasValue)
        {
            payload["presence_penalty"] = request.PresencePenalty.Value;
        }

        if (request.FrequencyPenalty.HasValue)
        {
            payload["frequency_penalty"] = request.FrequencyPenalty.Value;
        }

        if (request.StopSequences is { Count: > 0 })
        {
            payload["stop"] = request.StopSequences;
        }

        if (request.AdditionalProperties is not null)
        {
            foreach (var kvp in request.AdditionalProperties)
            {
                payload[kvp.Key] = kvp.Value;
            }
        }

        return payload;
    }

    private static List<Dictionary<string, object?>> BuildMessages(IReadOnlyList<ChatPromptMessage> messages)
    {
        var result = new List<Dictionary<string, object?>>(messages.Count);

        foreach (var message in messages)
        {
            if (string.IsNullOrWhiteSpace(message.Role))
            {
                throw new ArgumentException("Each chat message must include a role.", nameof(messages));
            }

            var entry = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["role"] = message.Role,
                ["content"] = message.Content ?? string.Empty
            };

            if (!string.IsNullOrWhiteSpace(message.Name))
            {
                entry["name"] = message.Name;
            }

            result.Add(entry);
        }

        return result;
    }

    private IEnumerable<ChatCompletionUpdate> ParseChunk(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            yield break;
        }

        List<ChatCompletionUpdate> updates;
        try
        {
            updates = ParseChunkInternal(payload);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse streaming payload from OpenAI-compatible API. Payload: {Payload}", payload);
            yield break;
        }

        foreach (var update in updates)
        {
            yield return update;
        }
    }

    private static List<ChatCompletionUpdate> ParseChunkInternal(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        if (!document.RootElement.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var updates = new List<ChatCompletionUpdate>();
        foreach (var choice in choices.EnumerateArray())
        {
            var index = choice.TryGetProperty("index", out var indexElement) && indexElement.ValueKind == JsonValueKind.Number
                ? indexElement.GetInt32()
                : 0;

            string? contentDelta = null;
            string? role = null;
            string? finishReason = null;

            if (choice.TryGetProperty("delta", out var deltaElement) && deltaElement.ValueKind == JsonValueKind.Object)
            {
                if (deltaElement.TryGetProperty("content", out var contentElement) && contentElement.ValueKind == JsonValueKind.String)
                {
                    contentDelta = contentElement.GetString();
                }

                if (deltaElement.TryGetProperty("role", out var roleElement) && roleElement.ValueKind == JsonValueKind.String)
                {
                    role = roleElement.GetString();
                }
            }

            if (choice.TryGetProperty("finish_reason", out var finishElement) && finishElement.ValueKind == JsonValueKind.String)
            {
                finishReason = finishElement.GetString();
            }

            if (contentDelta is null && role is null && finishReason is null)
            {
                continue;
            }

            updates.Add(new ChatCompletionUpdate(index, role, contentDelta, finishReason, !string.IsNullOrEmpty(finishReason)));
        }

        return updates;
    }

    private void Log(LogLevel level, string message, IReadOnlyDictionary<string, object?>? metadata = null)
    {
        _logBuffer.Log(nameof(OpenAiChatService), level, message, metadata);
    }
}
