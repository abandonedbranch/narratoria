using Microsoft.Extensions.Logging;
using Narratoria.Pipeline.Transforms.Llm.Providers;

namespace Narratoria.Pipeline.Transforms.Llm;

public static class LlmTransformErrorHandling
{
    public static async Task<string?> TryGenerateTextAsync(
        ITextGenerationService service,
        TextGenerationRequest request,
        ILogger logger,
        string transformName,
        PipelineChunkMetadata metadata,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(metadata);

        try
        {
            var response = await service.GenerateAsync(request, cancellationToken).ConfigureAwait(false);
            return response.GeneratedText;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "{Transform} provider call failed. {Context}", transformName, FormatContext(metadata));
            return null;
        }
    }

    public static string FormatContext(PipelineChunkMetadata metadata)
    {
        var annotations = metadata.Annotations;
        if (annotations is null)
        {
            return "(no annotations)";
        }

        annotations.TryGetValue("narratoria.session_id", out var sessionId);
        annotations.TryGetValue("narratoria.turn_id", out var turnId);
        annotations.TryGetValue("narratoria.turn_index", out var turnIndex);
        annotations.TryGetValue("narratoria.run_id", out var runId);

        return $"session_id={sessionId ?? "?"}, turn_id={turnId ?? "?"}, turn_index={turnIndex ?? "?"}, run_id={runId ?? "?"}";
    }
}
