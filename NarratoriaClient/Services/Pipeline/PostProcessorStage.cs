using Microsoft.Extensions.Logging;

namespace NarratoriaClient.Services.Pipeline;

public sealed class PostProcessorStage : INarrationPipelineStage
{
    private readonly ILogBuffer _logBuffer;

    public PostProcessorStage(ILogBuffer logBuffer)
    {
        _logBuffer = logBuffer ?? throw new ArgumentNullException(nameof(logBuffer));
    }

    public string StageName => "post-processor";

    public int Order => 5;

    public Task ExecuteAsync(NarrationPipelineContext context, CancellationToken cancellationToken)
    {
        // Placeholder for future lore/style enforcement. Currently just passes through.
        _logBuffer.Log(StageName, LogLevel.Debug, "Post-processing complete.", null);
        return Task.CompletedTask;
    }
}
