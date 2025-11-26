using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace NarratoriaClient.Services.Pipeline;

public sealed class InputPreprocessorStage : INarrationPipelineStage
{
    private static readonly Regex CommandPattern = new(@"@(?<command>[A-Za-z][\w-]*)", RegexOptions.Compiled);

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
        context.IsSystemCommand = ContainsSystemCommand(normalized);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Input normalized. Length={Length}. CommandDetected={Command}", normalized.Length, context.IsSystemCommand);
        }

        return Task.CompletedTask;
    }

    private static bool ContainsSystemCommand(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        var text = content;
        var cursor = 0;
        while (cursor < text.Length)
        {
            var match = CommandPattern.Match(text, cursor);
            if (!match.Success)
            {
                break;
            }

            var matchIndex = match.Index;
            var isEscaped = matchIndex > 0 && text[matchIndex - 1] == '\\';
            cursor = matchIndex + match.Length;

            if (isEscaped)
            {
                continue;
            }

            return true;
        }

        return false;
    }
}
