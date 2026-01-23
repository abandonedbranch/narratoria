using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TryAGI.HuggingFace;
using UnifiedInference.Abstractions;
using UnifiedInference.Core;
using UnifiedInference.Providers.HuggingFace;

namespace UnifiedInference;

public sealed class UnifiedInferenceClient : IUnifiedInferenceClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _metadataBaseUrl;
    private readonly string? _apiToken;
    private readonly HuggingFaceCapabilities _capabilities;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    public HuggingFaceClient? TryAgiClient { get; }
    public HttpClient HttpClient => _httpClient;

    public UnifiedInferenceClient(
        HttpClient httpClient,
        string? apiToken = null,
        string? baseUrl = null,
        string? metadataBaseUrl = null,
        HuggingFaceClient? tryAgiClient = null,
        Func<TimeSpan, CancellationToken, Task>? delayStrategy = null)
    {
        _httpClient = httpClient;
        _apiToken = apiToken;
        _baseUrl = string.IsNullOrWhiteSpace(baseUrl) ? "https://api-inference.huggingface.co/models" : baseUrl.TrimEnd('/');
        _metadataBaseUrl = string.IsNullOrWhiteSpace(metadataBaseUrl) ? "https://huggingface.co/api" : metadataBaseUrl.TrimEnd('/');
        _capabilities = new HuggingFaceCapabilities(httpClient, apiToken, _metadataBaseUrl);
        TryAgiClient = tryAgiClient;
        _delay = delayStrategy ?? Task.Delay;
    }

    public Task<ModelCapabilities> GetCapabilitiesAsync(string modelId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new ArgumentException("modelId is required", nameof(modelId));
        }

        return _capabilities.GetCachedOrFetchAsync(modelId, cancellationToken);
    }

    public async Task<TextResponse> GenerateTextAsync(TextRequest request, CancellationToken cancellationToken)
    {
        var caps = await GetCapabilitiesAsync(request.ModelId, cancellationToken).ConfigureAwait(false);
        EnforceModality(caps.SupportsText, "text", request.ModelId, caps);

        var parameters = SettingsMapperText.ToTextParameters(request.Settings, caps);
        MergeOverrides(parameters, request.Settings.ProviderOverrides);
        var options = SettingsMapperText.ToOptions(request.Settings);
        ApplyOptionOverrides(options, request.Settings.ProviderOverrides);
        if (!options.ContainsKey("wait_for_model"))
        {
            options["wait_for_model"] = request.Settings.WaitForModel ?? true;
        }

        var payload = new Dictionary<string, object>
        {
            ["inputs"] = request.Prompt,
            ["parameters"] = parameters,
            ["options"] = options
        };

        var response = await SendJsonWithRetryAsync(request.ModelId, payload, request.Settings, cancellationToken).ConfigureAwait(false);
        return await ParseTextResponseAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ImageResponse> GenerateImageAsync(ImageRequest request, CancellationToken cancellationToken)
    {
        var caps = await GetCapabilitiesAsync(request.ModelId, cancellationToken).ConfigureAwait(false);
        EnforceModality(caps.SupportsImage, "image", request.ModelId, caps);

        var parameters = SettingsMapperMedia.ToImageParameters(request, caps);
        MergeOverrides(parameters, request.Settings.ProviderOverrides);
        var options = SettingsMapperMedia.ToOptions(request.Settings);
        ApplyOptionOverrides(options, request.Settings.ProviderOverrides);
        if (!options.ContainsKey("wait_for_model"))
        {
            options["wait_for_model"] = request.Settings.WaitForModel ?? true;
        }

        var payload = new Dictionary<string, object>
        {
            ["inputs"] = request.Prompt,
            ["parameters"] = parameters,
            ["options"] = options
        };

        var response = await SendJsonWithRetryAsync(request.ModelId, payload, request.Settings, cancellationToken).ConfigureAwait(false);
        return await ParseImageResponseAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public Task<AudioResponse> GenerateAudioAsync(AudioRequest request, CancellationToken cancellationToken)
    {
        return Task.FromException<AudioResponse>(Errors.UnsupportedModality("audio", request.ModelId, "Audio not enabled for HF in this build"));
    }

    public Task<VideoResponse> GenerateVideoAsync(VideoRequest request, CancellationToken cancellationToken)
    {
        return Task.FromException<VideoResponse>(Errors.UnsupportedModality("video", request.ModelId, "Video hooks are disabled"));
    }

    public Task<MusicResponse> GenerateMusicAsync(MusicRequest request, CancellationToken cancellationToken)
    {
        return Task.FromException<MusicResponse>(Errors.UnsupportedModality("music", request.ModelId, "Music hooks are disabled"));
    }

    private static void EnforceModality(bool supported, string modality, string modelId, ModelCapabilities caps)
    {
        if (!supported)
        {
            var reason = caps.Gated ? "model is gated" : caps.InferenceStatus is { Length: > 0 } && caps.InferenceStatus != "loaded" ? $"status={caps.InferenceStatus}" : caps.PipelineTag ?? "unknown pipeline";
            throw Errors.UnsupportedModality(modality, modelId, reason);
        }
    }

    private async Task<HttpResponseMessage> SendJsonWithRetryAsync(string modelId, IDictionary<string, object> payload, GenerationSettings settings, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
        var attempts = 0;
        while (true)
        {
            using var request = BuildRequest(modelId, json, settings);
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.ServiceUnavailable)
            {
                return response;
            }

            attempts++;
            if (attempts >= 3)
            {
                return response;
            }

            var delay = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(2);
            await _delay(delay, cancellationToken).ConfigureAwait(false);
        }
    }

    private HttpRequestMessage BuildRequest(string modelId, string json, GenerationSettings settings)
    {
        var url = $"{_baseUrl}/{modelId}";
        var message = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var token = settings.ProviderOverrides != null && settings.ProviderOverrides.TryGetValue("hf_token", out var overrideToken) && overrideToken is string tokenString
            ? tokenString
            : _apiToken;
        if (!string.IsNullOrWhiteSpace(token))
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (settings.ProviderOverrides != null)
        {
            foreach (var header in settings.ProviderOverrides)
            {
                if (header.Key.StartsWith("header:", StringComparison.OrdinalIgnoreCase) && header.Value is string headerValue)
                {
                    var headerName = header.Key.Substring("header:".Length);
                    message.Headers.TryAddWithoutValidation(headerName, headerValue);
                }
            }
        }

        return message;
    }

    private static void MergeOverrides(IDictionary<string, object> parameters, IDictionary<string, object>? overrides)
    {
        if (overrides is null)
        {
            return;
        }

        foreach (var kvp in overrides)
        {
            if (parameters.ContainsKey(kvp.Key))
            {
                continue;
            }

            parameters[kvp.Key] = kvp.Value;
        }
    }

    private static void ApplyOptionOverrides(IDictionary<string, object> options, IDictionary<string, object>? overrides)
    {
        if (overrides is null)
        {
            return;
        }

        foreach (var kvp in overrides)
        {
            if ((kvp.Key.Equals("use_cache", StringComparison.OrdinalIgnoreCase) || kvp.Key.Equals("wait_for_model", StringComparison.OrdinalIgnoreCase)) && kvp.Value is bool flag)
            {
                options[kvp.Key] = flag;
            }
        }
    }

    private static async Task<TextResponse> ParseTextResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw Errors.HttpFailure(response.StatusCode, content);
        }

        static object RootToAnonymous(JsonElement root)
        {
            return JsonSerializer.Deserialize<object>(root.GetRawText()) ?? new { };
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            string? text = null;
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                var first = root[0];
                if (first.TryGetProperty("generated_text", out var generated))
                {
                    text = generated.GetString();
                }
                else if (first.TryGetProperty("text", out var textProp))
                {
                    text = textProp.GetString();
                }
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("generated_text", out var generated))
                {
                    text = generated.GetString();
                }
                else if (root.TryGetProperty("text", out var textProp))
                {
                    text = textProp.GetString();
                }
            }

            text ??= content;
            return new TextResponse
            {
                Text = text,
                ProviderMetadata = new Dictionary<string, object>
                {
                    ["raw"] = RootToAnonymous(document.RootElement)
                }
            };
        }
        catch (JsonException)
        {
            return new TextResponse
            {
                Text = content,
                ProviderMetadata = new Dictionary<string, object>()
            };
        }
    }

    private static async Task<ImageResponse> ParseImageResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var contentType = response.Content.Headers.ContentType?.MediaType;
        if (response.IsSuccessStatusCode && contentType is not null && contentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            return new ImageResponse { Bytes = bytes, ProviderMetadata = new Dictionary<string, object> { ["content_type"] = contentType } };
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw Errors.HttpFailure(response.StatusCode, content);
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("bytes_base64", out var base64))
                {
                    var bytes = Convert.FromBase64String(base64.GetString() ?? string.Empty);
                    return new ImageResponse { Bytes = bytes, ProviderMetadata = new Dictionary<string, object>() };
                }

                if (root.TryGetProperty("uri", out var uriProp))
                {
                    return new ImageResponse { Uri = uriProp.GetString(), ProviderMetadata = new Dictionary<string, object>() };
                }
            }

            return new ImageResponse { ProviderMetadata = new Dictionary<string, object> { ["raw"] = content } };
        }
        catch (JsonException)
        {
            return new ImageResponse { ProviderMetadata = new Dictionary<string, object> { ["raw"] = content } };
        }
    }
}
