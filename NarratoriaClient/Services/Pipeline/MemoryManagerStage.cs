using Microsoft.Extensions.Logging;

namespace NarratoriaClient.Services.Pipeline;

public sealed class MemoryManagerStage : INarrationPipelineStage
{
    private readonly IAppDataService _appData;
    private readonly ILogBuffer _logBuffer;

    public MemoryManagerStage(IAppDataService appData, ILogBuffer logBuffer)
    {
        _appData = appData ?? throw new ArgumentNullException(nameof(appData));
        _logBuffer = logBuffer ?? throw new ArgumentNullException(nameof(logBuffer));
    }

    public string StageName => "memory-manager";

    public int Order => 6;

    public async Task ExecuteAsync(NarrationPipelineContext context, CancellationToken cancellationToken)
    {
        if (context.IsSystemCommand)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(context.GeneratedNarration))
        {
            throw new InvalidOperationException("No narrator text available to persist.");
        }

        await _appData.AppendNarratorMessageAsync(context.GeneratedNarration, cancellationToken).ConfigureAwait(false);
        context.ReportStatus(NarrationStatus.Idle, "Narrator is ready.");

        _logBuffer.Log(StageName, LogLevel.Information, "Narrator message persisted.", new Dictionary<string, object?>
        {
            ["length"] = context.GeneratedNarration.Length
        });
    }
}
