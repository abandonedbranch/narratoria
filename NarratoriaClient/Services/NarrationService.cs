using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NarratoriaClient.Services.Pipeline;

namespace NarratoriaClient.Services;

public interface INarrationService
{
    event EventHandler<NarrationStatusChangedEventArgs>? StatusChanged;
    Task ProcessPlayerMessageAsync(string content, CancellationToken cancellationToken = default);
}

public sealed class NarrationService : INarrationService
{
    private readonly INarrationPipeline _pipeline;
    private readonly ILogBuffer _logBuffer;
    private readonly ILogger<NarrationService> _logger;

    public event EventHandler<NarrationStatusChangedEventArgs>? StatusChanged;

    public NarrationService(INarrationPipeline pipeline, ILogger<NarrationService> logger, ILogBuffer logBuffer)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logBuffer = logBuffer ?? throw new ArgumentNullException(nameof(logBuffer));
    }

    public async Task ProcessPlayerMessageAsync(string content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        var context = new NarrationPipelineContext(content, NotifyStatus);
        var execution = _pipeline.Execute(context, cancellationToken);

        try
        {
            await foreach (var lifecycleEvent in execution.ReadEventsAsync(cancellationToken).ConfigureAwait(false))
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace("Lifecycle event received: {Stage} {Kind}", lifecycleEvent.Stage, lifecycleEvent.Kind);
                }
            }

            await execution.Completion.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            NotifyStatus(NarrationStatus.Idle, "Narrator is ready.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Narration pipeline failed.");
            NotifyStatus(NarrationStatus.Disconnected, "Narration pipeline failed.");
            throw;
        }
    }

    private void NotifyStatus(NarrationStatus status, string message)
    {
        StatusChanged?.Invoke(this, new NarrationStatusChangedEventArgs(status, message));
        _logBuffer.Log(nameof(NarrationService), LogLevel.Debug, "Narration status changed.", new Dictionary<string, object?>
        {
            ["status"] = status.ToString(),
            ["message"] = message
        });
    }
}

public enum NarrationStatus
{
    Idle,
    Connecting,
    Writing,
    Disconnected
}

public sealed record NarrationStatusChangedEventArgs(NarrationStatus Status, string Message);
