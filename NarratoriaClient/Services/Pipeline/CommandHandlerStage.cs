using Microsoft.Extensions.Logging;
using NarratoriaClient.Components;

namespace NarratoriaClient.Services.Pipeline;

public sealed class CommandHandlerStage : INarrationPipelineStage
{
    private readonly ILogBuffer _logBuffer;
    private readonly ITransientCommandLog _commandLog;
    private readonly ICommandEventBus _commandEvents;

    public CommandHandlerStage(ILogBuffer logBuffer, ITransientCommandLog commandLog, ICommandEventBus commandEvents)
    {
        _logBuffer = logBuffer ?? throw new ArgumentNullException(nameof(logBuffer));
        _commandLog = commandLog ?? throw new ArgumentNullException(nameof(commandLog));
        _commandEvents = commandEvents ?? throw new ArgumentNullException(nameof(commandEvents));
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
                LogCommand(context, commandToken, isError: true, message: $"Unknown command '/{commandToken}'.");
                throw new InvalidOperationException($"Unknown command '/{commandToken}'.");
            }

            context.ShouldContinue = false;
            _logBuffer.Log(StageName, LogLevel.Information, "Command detected; pipeline short-circuited.", new Dictionary<string, object?>
            {
                ["command"] = commandToken,
                ["args"] = context.CommandArgs ?? string.Empty
            });

            LogCommand(context, commandToken, isError: false, message: context.CommandArgs);
            PublishEvent(context, commandToken, isError: false, message: context.CommandArgs);
        }

        return Task.CompletedTask;
    }

    private void LogCommand(NarrationPipelineContext context, string token, bool isError, string? message)
    {
        var sessionId = context.ActiveSessionId ?? string.Empty;
        _commandLog.AddEntry(new TransientCommandEntry(
            Guid.NewGuid().ToString("N"),
            sessionId,
            "Player",
            token,
            context.CommandArgs,
            DateTimeOffset.UtcNow,
            isError,
            message));
    }

    private void PublishEvent(NarrationPipelineContext context, string token, bool isError, string? message)
    {
        _commandEvents.Publish(new CommandEvent(
            token,
            context.CommandArgs,
            "Player",
            DateTimeOffset.UtcNow,
            isError,
            message,
            context.ActiveSessionId));
    }
}
