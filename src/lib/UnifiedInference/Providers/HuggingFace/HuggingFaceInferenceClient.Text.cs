using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using UnifiedInference.Abstractions;
using UnifiedInference.Core;

namespace UnifiedInference.Providers.HuggingFace;

public sealed partial class HuggingFaceInferenceClient(HttpClient http)
{
    private readonly HttpClient _http = http;

    // Generic model endpoint: POST https://api-inference.huggingface.co/models/{modelId}
    public async Task<TextResponse> GenerateTextAsync(TextRequest request, CancellationToken cancellationToken)
    {
        var baseUrl = GetBaseUrl(request.Settings);
        var url = $"{baseUrl.TrimEnd('/')}/{Uri.EscapeDataString(request.ModelId)}";

        var payload = new JsonObject
        {
            ["inputs"] = request.Prompt,
            ["parameters"] = JsonSerializer.SerializeToNode(SettingsMapperText.ToHuggingFaceOptions(request.Settings))
        };

        using var httpReq = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };
        var token = GetAuthToken(request.Settings);
        if (!string.IsNullOrWhiteSpace(token))
        {
            httpReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        using var resp = await _http.SendAsync(httpReq, cancellationToken).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        var content = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        var text = ParseGeneratedText(content);
        return new TextResponse(text ?? string.Empty, null, new { url });
    }

    private static string GetBaseUrl(GenerationSettings s)
    {
        // Allow override via ProviderOverrides["hf_base_url"]; default to generic endpoint
        var defaultUrl = "https://api-inference.huggingface.co/models";
        if (s.ProviderOverrides is JsonObject map && map.TryGetPropertyValue("hf_base_url", out var node))
        {
            return node?.ToString() ?? defaultUrl;
        }
        return defaultUrl;
    }

    private static string? ParseGeneratedText(string content)
    {
        // Try robust parsing across potential shapes
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.String)
            {
                return root.GetString();
            }
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in root.EnumerateArray())
                {
                    if (el.ValueKind == JsonValueKind.Object)
                    {
                        if (el.TryGetProperty("generated_text", out var gt) && gt.ValueKind == JsonValueKind.String)
                            return gt.GetString();
                        if (el.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String)
                            return t.GetString();
                    }
                    else if (el.ValueKind == JsonValueKind.String)
                    {
                        return el.GetString();
                    }
                }
            }
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("generated_text", out var gt) && gt.ValueKind == JsonValueKind.String)
                    return gt.GetString();
                if (root.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String)
                    return t.GetString();
            }
        }
        catch
        {
            // Ignore parse errors; fall back to raw string
        }
        return content;
    }
}
