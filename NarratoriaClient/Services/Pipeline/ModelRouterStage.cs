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

    public int Order => 4;

    public async Task ExecuteAsync(NarrationPipelineContext context, CancellationToken cancellationToken)
    {
        var apiSettings = await _appData.GetApiSettingsAsync(cancellationToken).ConfigureAwait(false);
        string? model = null;
        string workflow = context.TargetWorkflow;

        if (workflow.Equals("system", StringComparison.OrdinalIgnoreCase))
        {
            model = apiSettings.System.Model;
        }
        else if (workflow.Equals("sketch", StringComparison.OrdinalIgnoreCase))
        {
            context.ShouldContinue = false;
            _logBuffer.Log(StageName, LogLevel.Information, "Sketch workflow requested but not implemented; skipping narrator request.", new Dictionary<string, object?>
            {
                ["workflow"] = workflow
            });
            return;
        }
        else
        {
            model = apiSettings.Narrator.Model;
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new InvalidOperationException("A model must be configured before requesting narration.");
        }

        context.SelectedModel = model;
        context.ReportStatus(NarrationStatus.Connecting, $"Connecting to {model}…");

        _logBuffer.Log(StageName, LogLevel.Information, "Model selected.", new Dictionary<string, object?>
        {
            ["model"] = model,
            ["workflow"] = workflow
        });
    }
}
