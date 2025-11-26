using Microsoft.Extensions.Logging;
using NarratoriaClient.Components;

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
        if (context.IsCommand)
        {
            var commandToken = ChatCommandRegistry.NormalizeToken(context.CommandName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(commandToken))
            {
                context.ShouldContinue = false;
                return Task.CompletedTask;
            }

            if (!ChatCommandRegistry.TryGetComponentType(commandToken, out _))
            {
                throw new InvalidOperationException($"Unknown command '/{commandToken}'.");
            }

            context.ShouldContinue = false;
            _logBuffer.Log(StageName, LogLevel.Information, "Command detected; pipeline short-circuited.", new Dictionary<string, object?>
            {
                ["command"] = commandToken,
                ["args"] = context.CommandArgs ?? string.Empty
            });
        }

        return Task.CompletedTask;
    }
}
