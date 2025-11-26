using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace NarratoriaClient.Services.Pipeline;

public sealed class InputPreprocessorStage : INarrationPipelineStage
{
    private static readonly Regex CommandPattern = new(@"@(?<command>[A-Za-z][\w-]*)", RegexOptions.Compiled);
    private static readonly Regex LeadingCommandPattern = new(@"^@(?<command>[A-Za-z][\w-]*)(\s+)?", RegexOptions.Compiled);
    private static readonly Regex LeadingSlashCommandPattern = new(@"^/(?<command>[^\s]+)", RegexOptions.Compiled);
    private static readonly HashSet<string> RoutingCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "narrator",
        "system",
        "sketch"
    };

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

        context.TargetWorkflow = "narrator";
        context.IsCommand = false;
        context.CommandName = null;
        context.CommandArgs = null;

        // Slash command detection
        var leadingSlash = LeadingSlashCommandPattern.Match(normalized);
        if (leadingSlash.Success && leadingSlash.Length > 1 && !(normalized.Length > 1 && normalized[1] == ' '))
        {
            context.IsCommand = true;
            context.CommandName = leadingSlash.Groups["command"].Value.Trim();
            var remainder = normalized[leadingSlash.Length..].TrimStart();
            context.CommandArgs = string.IsNullOrWhiteSpace(remainder) ? null : remainder;
            var commandText = context.CommandArgs is null
                ? $"/{context.CommandName}"
                : $"/{context.CommandName} {context.CommandArgs}";

            // Store as @command so existing chat rendering surfaces command components.
            var normalizedForHistory = context.CommandArgs is null
                ? $"@{context.CommandName}"
                : $"@{context.CommandName} {context.CommandArgs}";

            context.NormalizedInput = normalizedForHistory;
            _logger.LogDebug("Slash command detected. Command={Command}, Args={Args}", context.CommandName, context.CommandArgs);
            return Task.CompletedTask;
        }

        var leading = LeadingCommandPattern.Match(normalized);
        if (leading.Success && normalized.Length > 1 && normalized[1] != ' ')
        {
            var token = leading.Groups["command"].Value;
            var trimmedToken = token.Trim();

            if (RoutingCommands.Contains(trimmedToken))
            {
                context.TargetWorkflow = trimmedToken.ToLowerInvariant();
                normalized = normalized[(leading.Length)..].TrimStart();
            }
        }

        context.NormalizedInput = normalized;

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Input normalized. Length={Length}. CommandDetected={Command}. TargetWorkflow={Workflow}", normalized.Length, context.IsCommand, context.TargetWorkflow);
        }

        return Task.CompletedTask;
    }
}
