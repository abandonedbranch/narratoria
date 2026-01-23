using System.Net.Http;
using UnifiedInference;

namespace UnifiedInference.Factory;

public static class InferenceClientFactory
{
    public static UnifiedInferenceClient Create(
        string apiKey,
        HttpClient? httpClient = null,
        string? baseUrl = null,
        string? metadataBaseUrl = null)
    {
        var client = httpClient ?? new HttpClient();
        return new UnifiedInferenceClient(client, apiKey, baseUrl, metadataBaseUrl);
    }
}
