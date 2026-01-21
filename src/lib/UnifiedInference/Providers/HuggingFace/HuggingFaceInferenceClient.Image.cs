using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using UnifiedInference.Abstractions;
using UnifiedInference.Core;

namespace UnifiedInference.Providers.HuggingFace;

public sealed partial class HuggingFaceInferenceClient
{
    public async Task<ImageResponse> GenerateImageAsync(ImageRequest request, CancellationToken cancellationToken)
    {
        var baseUrl = GetBaseUrl(request.Settings);
        var url = $"{baseUrl.TrimEnd('/')}/{Uri.EscapeDataString(request.ModelId)}";

        // Prefer JSON payload for text-to-image models
        var payload = new JsonObject
        {
            ["inputs"] = request.Prompt,
            ["parameters"] = JsonSerializer.SerializeToNode(SettingsMapperMedia.ToImageOptions(request.Settings))
        };

        using var httpReq = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };
        var token = GetAuthToken(request.Settings);
        if (!string.IsNullOrWhiteSpace(token))
        {
            httpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var resp = await _http.SendAsync(httpReq, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        var contentType = resp.Content.Headers.ContentType?.MediaType ?? string.Empty;
        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = await resp.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            return new ImageResponse(bytes, null, new { url });
        }

        // Fallback: parse JSON for base64 image
        var text = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var bytesFromJson = TryParseImageBytes(text);
        return new ImageResponse(bytesFromJson, null, new { url });
    }

    private static string? GetAuthToken(GenerationSettings s)
    {
        if (s.ProviderOverrides is JsonObject map && map.TryGetPropertyValue("hf_token", out var node))
        {
            return node?.ToString();
        }
        return null;
    }

    private static byte[]? TryParseImageBytes(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            // Common patterns: { "image": "base64..." } or [ { "bytes": "base64" } ]
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("image", out var img) && img.ValueKind == JsonValueKind.String)
                {
                    return DecodeBase64(img.GetString());
                }
                if (root.TryGetProperty("bytes", out var b) && b.ValueKind == JsonValueKind.String)
                {
                    return DecodeBase64(b.GetString());
                }
            }
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in root.EnumerateArray())
                {
                    if (el.ValueKind == JsonValueKind.Object)
                    {
                        if (el.TryGetProperty("image", out var img) && img.ValueKind == JsonValueKind.String)
                            return DecodeBase64(img.GetString());
                        if (el.TryGetProperty("bytes", out var b) && b.ValueKind == JsonValueKind.String)
                            return DecodeBase64(b.GetString());
                    }
                }
            }
        }
        catch
        {
            // ignore parse errors
        }
        return null;
    }

    private static byte[]? DecodeBase64(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        try { return Convert.FromBase64String(s); } catch { return null; }
    }
}