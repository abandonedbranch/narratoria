using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UnifiedInference.Abstractions;
using UnifiedInference.Core;

namespace UnifiedInference.Providers.HuggingFace;

public sealed class HuggingFaceCapabilities
{
    private readonly HttpClient _httpClient;
    private readonly string _metadataBaseUrl;
    private readonly string? _apiToken;
    private readonly ConcurrentDictionary<string, ModelCapabilities> _cache = new(StringComparer.OrdinalIgnoreCase);

    public HuggingFaceCapabilities(HttpClient httpClient, string? apiToken, string? metadataBaseUrl = null)
    {
        _httpClient = httpClient;
        _apiToken = apiToken;
        _metadataBaseUrl = string.IsNullOrWhiteSpace(metadataBaseUrl) ? "https://huggingface.co/api" : metadataBaseUrl.TrimEnd('/');
    }

    public Task<ModelCapabilities> GetCachedOrFetchAsync(string modelId, CancellationToken cancellationToken)
    {
        return _cache.TryGetValue(modelId, out var cached)
            ? Task.FromResult(cached)
            : FetchAsync(modelId, cancellationToken);
    }

    private async Task<ModelCapabilities> FetchAsync(string modelId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_metadataBaseUrl}/models/{modelId}");
        if (!string.IsNullOrWhiteSpace(_apiToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiToken);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var fallback = ModelCapabilitiesDefaults.Unknown(modelId);
            _cache[modelId] = fallback;
            return fallback;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        var root = document.RootElement;
        var pipelineTag = root.TryGetProperty("pipeline_tag", out var pipelineElement) ? pipelineElement.GetString() : null;
        var gated = root.TryGetProperty("gated", out var gatedElement) && gatedElement.GetBoolean();
        string? inferenceStatus = null;
        if (root.TryGetProperty("inference", out var inferenceElement) && inferenceElement.TryGetProperty("status", out var statusElement))
        {
            inferenceStatus = statusElement.GetString();
        }

        var capabilities = ModelCapabilitiesDefaults.ForPipelineTag(modelId, pipelineTag, gated, inferenceStatus);
        _cache[modelId] = capabilities;
        return capabilities;
    }
}
