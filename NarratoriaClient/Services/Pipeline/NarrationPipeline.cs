using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace NarratoriaClient.Services.Pipeline;

public sealed class NarrationPipeline : INarrationPipeline
{
    private readonly IReadOnlyList<INarrationPipelineStage> _stages;
    private readonly ILogger<NarrationPipeline> _logger;

    public NarrationPipeline(IEnumerable<INarrationPipelineStage> stages, ILogger<NarrationPipeline> logger)
    {
        _stages = stages
            .OrderBy(stage => stage.Order)
            .ToList()
            .AsReadOnly();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public NarrationPipelineExecution Execute(NarrationPipelineContext context, CancellationToken cancellationToken = default)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var channel = Channel.CreateUnbounded<NarrationLifecycleEvent>(new UnboundedChannelOptions
        {
            SingleWriter = true,
            SingleReader = false
        });

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var completion = Task.Run(async () =>
        {
            try
            {
                var shortCircuited = false;
                foreach (var stage in _stages)
                {
                    linkedCts.Token.ThrowIfCancellationRequested();
                    await PublishEventAsync(channel.Writer, context.CorrelationId, stage.StageName, NarrationLifecycleEventKind.StageStarting, null, null, linkedCts.Token);

                    try
                    {
                        await stage.ExecuteAsync(context, linkedCts.Token).ConfigureAwait(false);
                        await PublishEventAsync(channel.Writer, context.CorrelationId, stage.StageName, NarrationLifecycleEventKind.StageCompleted, null, null, linkedCts.Token);
                    }
                    catch (OperationCanceledException ex) when (linkedCts.IsCancellationRequested)
                    {
                        await PublishEventAsync(channel.Writer, context.CorrelationId, stage.StageName, NarrationLifecycleEventKind.StageFailed, null, ex, CancellationToken.None);
                        throw;
                    }
                    catch (Exception ex)
                    {
                        await PublishEventAsync(channel.Writer, context.CorrelationId, stage.StageName, NarrationLifecycleEventKind.StageFailed, null, ex, CancellationToken.None);
                        throw;
                    }

                    if (!context.ShouldContinue)
                    {
                        shortCircuited = true;
                        break;
                    }
                }

                var metadata = shortCircuited
                    ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["reason"] = "short-circuited" }
                    : null;

                await PublishEventAsync(channel.Writer, context.CorrelationId, "pipeline", NarrationLifecycleEventKind.PipelineCompleted, metadata, null, CancellationToken.None);
            }
            catch (OperationCanceledException ex)
            {
                await PublishEventAsync(channel.Writer, context.CorrelationId, "pipeline", NarrationLifecycleEventKind.PipelineFailed, null, ex, CancellationToken.None);
                throw;
            }
            catch (Exception ex)
            {
                await PublishEventAsync(channel.Writer, context.CorrelationId, "pipeline", NarrationLifecycleEventKind.PipelineFailed, null, ex, CancellationToken.None);
                throw;
            }
            finally
            {
                channel.Writer.TryComplete();
                linkedCts.Dispose();
            }
        }, CancellationToken.None);

        return new NarrationPipelineExecution(channel.Reader, completion);
    }

    private Task PublishEventAsync(
        ChannelWriter<NarrationLifecycleEvent> writer,
        Guid correlationId,
        string stage,
        NarrationLifecycleEventKind kind,
        IReadOnlyDictionary<string, object?>? metadata,
        Exception? exception,
        CancellationToken cancellationToken)
    {
        var lifecycleEvent = new NarrationLifecycleEvent(
            correlationId,
            stage,
            kind,
            DateTimeOffset.UtcNow,
            metadata,
            exception);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Pipeline event {Stage} {Kind}", stage, kind);
        }

        return writer.WriteAsync(lifecycleEvent, cancellationToken).AsTask();
    }
}
