using System;
using System.Net;
using System.Text.Json;

namespace UnifiedInference.Core;

public static class Errors
{
    public static NotSupportedException UnsupportedModality(string modality, string modelId, string? reason = null)
    {
        var detail = string.IsNullOrWhiteSpace(reason) ? string.Empty : $" ({reason})";
        return new NotSupportedException($"{modality} not supported for model '{modelId}'{detail}.");
    }

    public static InvalidOperationException CapabilityMissing(string modelId)
    {
        return new InvalidOperationException($"Capabilities for model '{modelId}' are unavailable.");
    }

    public static HttpRequestException HttpFailure(HttpStatusCode statusCode, string content)
    {
        var message = content;
        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("error", out var errorProp))
            {
                message = errorProp.GetString() ?? content;
            }
        }
        catch
        {
            // ignore parse failures; fall back to raw content
        }

        return new HttpRequestException($"HF request failed with {(int)statusCode} {statusCode}: {message}", null, statusCode);
    }
}
