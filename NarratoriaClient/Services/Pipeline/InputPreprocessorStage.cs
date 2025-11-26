using Microsoft.Extensions.Logging;

namespace NarratoriaClient.Services.Pipeline;

public sealed class InputPreprocessorStage : INarrationPipelineStage
{
    private readonly ILogger<InputPreprocessorStage> _logger;

    public InputPreprocessorStage(ILogger<InputPreprocessorStage> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string StageName => "input-preprocessor";

    public int Order => 0;

    public Task ExecuteAsync(NarrationPipelineContext context, CancellationToken cancellationToken)
    {
        var normalized = context.PlayerInput.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Input must not be empty after normalization.");
        }

        context.NormalizedInput = normalized;

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Input normalized. Length={Length}", normalized.Length);
        }

        return Task.CompletedTask;
    }
}
