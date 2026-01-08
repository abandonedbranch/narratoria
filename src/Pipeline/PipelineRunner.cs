namespace Narratoria.Pipeline;

public sealed class PipelineRunner
{
    public async Task<PipelineRunResult<TSinkResult>> RunAsync<TSinkResult>(
        PipelineDefinition<TSinkResult> definition,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var compatibilityFailure = ValidateCompatibility(definition);
        if (compatibilityFailure is not null)
        {
            return new PipelineRunResult<TSinkResult>(
                PipelineOutcome.Failed(PipelineFailureKind.TypeMismatch, compatibilityFailure),
                default!);
        }

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            var stream = definition.Source.StreamAsync(linkedCts.Token);

            foreach (var transform in definition.Transforms)
            {
                stream = transform.TransformAsync(stream, linkedCts.Token);
            }

            var sinkResult = await definition.Sink.ConsumeAsync(stream, linkedCts.Token).ConfigureAwait(false);

            // If the sink completed early (did not fully enumerate), cancel upstream work.
            linkedCts.Cancel();

            if (cancellationToken.IsCancellationRequested)
            {
                return new PipelineRunResult<TSinkResult>(PipelineOutcome.Canceled(), sinkResult);
            }

            return new PipelineRunResult<TSinkResult>(PipelineOutcome.Completed(), sinkResult);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new PipelineRunResult<TSinkResult>(PipelineOutcome.Canceled(), default!);
        }
        catch (PipelineDecodeException ex)
        {
            return new PipelineRunResult<TSinkResult>(
                PipelineOutcome.Failed(PipelineFailureKind.DecodeFailure, ex.SafeMessage),
                default!);
        }
        catch (PipelineBlockedException ex)
        {
            return new PipelineRunResult<TSinkResult>(PipelineOutcome.Blocked(ex.SafeMessage), default!);
        }
        catch (PipelineStageException ex)
        {
            return new PipelineRunResult<TSinkResult>(
                PipelineOutcome.Failed(ex.FailureKind, ex.SafeMessage),
                default!);
        }
        catch (Exception ex)
        {
            var message = $"Pipeline failed with unexpected error of type '{ex.GetType().Name}'.";
            return new PipelineRunResult<TSinkResult>(
                PipelineOutcome.Failed(PipelineFailureKind.Unknown, message),
                default!);
        }
    }

    private static string? ValidateCompatibility<TSinkResult>(PipelineDefinition<TSinkResult> definition)
    {
        var currentType = definition.Source.OutputType;

        foreach (var transform in definition.Transforms)
        {
            if (transform.InputType != currentType)
            {
                return $"Transform input type mismatch. Expected '{currentType}', got '{transform.InputType}'.";
            }

            currentType = transform.OutputType;
        }

        if (definition.Sink.InputType != currentType)
        {
            return $"Sink input type mismatch. Expected '{currentType}', got '{definition.Sink.InputType}'.";
        }

        return null;
    }
}

public sealed class PipelineBlockedException(string safeMessage, Exception? innerException = null)
    : Exception(safeMessage, innerException)
{
    public string SafeMessage { get; } = safeMessage;
}

public sealed class PipelineDecodeException(string safeMessage, Exception? innerException = null)
    : Exception(safeMessage, innerException)
{
    public string SafeMessage { get; } = safeMessage;
}

public sealed class PipelineStageException(PipelineFailureKind failureKind, string safeMessage, Exception? innerException = null)
    : Exception(safeMessage, innerException)
{
    public PipelineFailureKind FailureKind { get; } = failureKind;
    public string SafeMessage { get; } = safeMessage;
}
