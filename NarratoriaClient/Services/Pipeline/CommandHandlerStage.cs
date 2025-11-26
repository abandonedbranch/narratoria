using Microsoft.Extensions.Logging;

namespace NarratoriaClient.Services.Pipeline;

public sealed class CommandHandlerStage : INarrationPipelineStage
{
    private readonly ILogBuffer _logBuffer;

    public CommandHandlerStage(ILogBuffer logBuffer)
    {
        _logBuffer = logBuffer ?? throw new ArgumentNullException(nameof(logBuffer));
    }

    public string StageName => "command-handler";

    public int Order => 2;

    public Task ExecuteAsync(NarrationPipelineContext context, CancellationToken cancellationToken)
    {
        if (context.IsSystemCommand)
        {
            context.ShouldContinue = false;
            _logBuffer.Log(StageName, LogLevel.Information, "System command detected; pipeline short-circuited.", new Dictionary<string, object?>
            {
                ["input"] = context.NormalizedInput ?? context.PlayerInput
            });
        }

        return Task.CompletedTask;
    }
}
