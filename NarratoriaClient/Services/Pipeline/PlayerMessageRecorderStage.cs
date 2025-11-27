using Microsoft.Extensions.Logging;

namespace NarratoriaClient.Services.Pipeline;

public sealed class PlayerMessageRecorderStage : INarrationPipelineStage
{
    private readonly IAppDataService _appData;
    private readonly ILogBuffer _logBuffer;

    public PlayerMessageRecorderStage(IAppDataService appData, ILogBuffer logBuffer)
    {
        _appData = appData ?? throw new ArgumentNullException(nameof(appData));
        _logBuffer = logBuffer ?? throw new ArgumentNullException(nameof(logBuffer));
    }

    public string StageName => "player-message";

    public int Order => 1;

    public async Task ExecuteAsync(NarrationPipelineContext context, CancellationToken cancellationToken)
    {
        if (context.IsCommand)
        {
            _logBuffer.Log(StageName, LogLevel.Debug, "Command detected; player message not persisted.", new Dictionary<string, object?>
            {
                ["input"] = context.NormalizedInput ?? context.PlayerInput
            });
            return;
        }

        var message = context.NormalizedInput ?? context.PlayerInput;
        await _appData.AppendPlayerMessageAsync(message, cancellationToken).ConfigureAwait(false);

        _logBuffer.Log(StageName, LogLevel.Information, "Player message recorded.", new Dictionary<string, object?>
        {
            ["length"] = message.Length,
            ["preview"] = BuildPreview(message)
        });
    }

    private static string BuildPreview(string content, int maxLength = 120)
    {
        var trimmed = content.Trim();
        if (trimmed.Length <= maxLength)
        {
            return trimmed;
        }

        return trimmed[..maxLength] + "…";
    }
}
