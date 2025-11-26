using Microsoft.Extensions.Logging;

namespace NarratoriaClient.Services.Pipeline;

public sealed class ModelRouterStage : INarrationPipelineStage
{
    private readonly IAppDataService _appData;
    private readonly ILogBuffer _logBuffer;

    public ModelRouterStage(IAppDataService appData, ILogBuffer logBuffer)
    {
        _appData = appData ?? throw new ArgumentNullException(nameof(appData));
        _logBuffer = logBuffer ?? throw new ArgumentNullException(nameof(logBuffer));
    }

    public string StageName => "model-router";

    public int Order => 3;

    public async Task ExecuteAsync(NarrationPipelineContext context, CancellationToken cancellationToken)
    {
        var apiSettings = await _appData.GetApiSettingsAsync(cancellationToken).ConfigureAwait(false);
        var narratorModel = apiSettings.Narrator.Model;
        if (string.IsNullOrWhiteSpace(narratorModel))
        {
            throw new InvalidOperationException("A model must be configured before requesting narration.");
        }

        context.SelectedModel = narratorModel;
        context.ReportStatus(NarrationStatus.Connecting, $"Connecting to {narratorModel}…");

        _logBuffer.Log(StageName, LogLevel.Information, "Model selected.", new Dictionary<string, object?>
        {
            ["model"] = narratorModel
        });
    }
}
