using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using UnifiedInference.Abstractions;
using UnifiedInference.Core;

namespace UnifiedInference.Providers.Ollama;

public sealed class OllamaInferenceClient(HttpClient http, string baseUrl)
{
    private readonly HttpClient _http = http;
    private readonly Uri _baseUri = new Uri(baseUrl, UriKind.Absolute);

    // POST {base}/api/generate { model, prompt, stream=false, options:{...} }
    public async Task<TextResponse> GenerateTextAsync(TextRequest request, CancellationToken cancellationToken)
    {
        var url = new Uri(_baseUri, "/api/generate");
        var payload = new JsonObject
        {
            ["model"] = request.ModelId,
            ["prompt"] = request.Prompt,
            ["stream"] = false,
            ["options"] = JsonSerializer.SerializeToNode(SettingsMapperText.ToOllamaOptions(request.Settings))
        };

        using var httpReq = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };

        using var resp = await _http.SendAsync(httpReq, cancellationToken).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        var content = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        var text = ParseResponse(content);
        return new TextResponse(text ?? string.Empty, null, new { url = url.ToString() });
    }

    private static string? ParseResponse(string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("response", out var r) && r.ValueKind == JsonValueKind.String)
                return r.GetString();
        }
        catch
        {
            // ignore
        }
        return content;
    }
}
