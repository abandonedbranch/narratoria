using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NarratoriaClient.Services;

namespace NarratoriaClient.Services.Pipeline;

public sealed class LegacyNarrationPipelineStage : INarrationPipelineStage
{
    private static readonly Regex CommandPattern = new(@"@(?<command>[A-Za-z][\w-]*)", RegexOptions.Compiled);

    private readonly IAppDataService _appData;
    private readonly IOpenAiChatService _chatService;
    private readonly ILogger<LegacyNarrationPipelineStage> _logger;
    private readonly ILogBuffer _logBuffer;

    public LegacyNarrationPipelineStage(
        IAppDataService appData,
        IOpenAiChatService chatService,
        ILogger<LegacyNarrationPipelineStage> logger,
        ILogBuffer logBuffer)
    {
        _appData = appData ?? throw new ArgumentNullException(nameof(appData));
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logBuffer = logBuffer ?? throw new ArgumentNullException(nameof(logBuffer));
    }

    public string StageName => "legacy-narration";

    public int Order => 0;

    public async Task ExecuteAsync(NarrationPipelineContext context, CancellationToken cancellationToken)
    {
        var trimmed = context.PlayerInput;

        Log(LogLevel.Information, "Player submitted a message.", new Dictionary<string, object?>
        {
            ["length"] = trimmed.Length,
            ["preview"] = BuildPreview(trimmed)
        });

        await _appData.AppendPlayerMessageAsync(trimmed, cancellationToken).ConfigureAwait(false);
        context.ReportStatus(NarrationStatus.Connecting, "Connecting to the narrator…");

        try
        {
            var narratorResponse = await GenerateNarrationAsync(context, cancellationToken).ConfigureAwait(false);
            await _appData.AppendNarratorMessageAsync(narratorResponse, cancellationToken).ConfigureAwait(false);
            context.ReportStatus(NarrationStatus.Idle, "Narrator is ready.");
            Log(LogLevel.Information, "Narrator response stored.", new Dictionary<string, object?>
            {
                ["length"] = narratorResponse.Length,
                ["preview"] = BuildPreview(narratorResponse)
            });
        }
        catch (OperationCanceledException)
        {
            context.ReportStatus(NarrationStatus.Idle, "Narrator is ready.");
            Log(LogLevel.Warning, "Narration request cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to obtain narration from the configured API.");
            var diagnostic = $"Narrator error: {ex.Message}";
            await _appData.AppendNarratorMessageAsync(diagnostic, cancellationToken).ConfigureAwait(false);
            context.ReportStatus(NarrationStatus.Disconnected, "Narrator left the table. Retry to reconnect.");
            Log(LogLevel.Error, "Narration request failed.", new Dictionary<string, object?>
            {
                ["exception"] = ex.Message
            });
        }
    }

    private async Task<string> GenerateNarrationAsync(NarrationPipelineContext context, CancellationToken cancellationToken)
    {
        var promptSettings = await _appData.GetPromptSettingsAsync(cancellationToken).ConfigureAwait(false);
        var history = await _appData.GetChatHistoryAsync(cancellationToken).ConfigureAwait(false);
        var filteredHistory = FilterSystemCommands(history);
        var contextSummary = await BuildContextSummaryAsync(filteredHistory, cancellationToken).ConfigureAwait(false);

        Log(LogLevel.Debug, "Preparing narration request.", new Dictionary<string, object?>
        {
            ["historyCount"] = history.Count,
            ["includedCount"] = filteredHistory.Count
        });

        var messages = new List<ChatPromptMessage>();
        var narratorPrompt = promptSettings.Narrator;
        if (!string.IsNullOrWhiteSpace(narratorPrompt.Content))
        {
            messages.Add(new ChatPromptMessage
            {
                Role = "system",
                Content = narratorPrompt.Content,
                Name = BuildMessageName(narratorPrompt.Title)
            });
        }

        if (!string.IsNullOrWhiteSpace(contextSummary))
        {
            messages.Add(new ChatPromptMessage
            {
                Role = "system",
                Content = contextSummary,
                Name = "context"
            });
        }

        foreach (var entry in filteredHistory)
        {
            var role = entry.Role == ChatMessageRole.Player ? "user" : "assistant";
            messages.Add(new ChatPromptMessage
            {
                Role = role,
                Content = entry.Content,
                Name = BuildMessageName(entry.Author)
            });
        }

        var apiSettings = await _appData.GetApiSettingsAsync(cancellationToken).ConfigureAwait(false);
        var narratorModel = apiSettings.Narrator.Model;
        if (string.IsNullOrWhiteSpace(narratorModel))
        {
            throw new InvalidOperationException("A model must be configured before requesting narration.");
        }

        context.ReportStatus(NarrationStatus.Connecting, $"Connecting to {narratorModel}…");

        var request = new ChatCompletionRequest
        {
            Model = narratorModel,
            Messages = messages
        };

        Log(LogLevel.Information, "Dispatching narration request.", new Dictionary<string, object?>
        {
            ["model"] = request.Model,
            ["messageCount"] = request.Messages.Count
        });

        var builder = new StringBuilder();
        var streamStarted = false;
        await foreach (var update in _chatService.StreamChatCompletionAsync(request, cancellationToken).ConfigureAwait(false))
        {
            if (!streamStarted)
            {
                streamStarted = true;
                context.ReportStatus(NarrationStatus.Writing, "Narrator is writing…");
            }

            if (!string.IsNullOrEmpty(update.ContentDelta))
            {
                builder.Append(update.ContentDelta);
            }
        }

        var narration = builder.ToString().Trim();
        if (string.IsNullOrWhiteSpace(narration))
        {
            narration = "The narrator had nothing to add.";
        }

        return narration;
    }

    private async Task<string> BuildContextSummaryAsync(IReadOnlyList<ChatMessageEntry> history, CancellationToken cancellationToken)
    {
        var session = await _appData.GetSessionStateAsync(cancellationToken).ConfigureAwait(false);
        var personas = await _appData.GetPersonasAsync(cancellationToken).ConfigureAwait(false);

        var builder = new StringBuilder();
        builder.AppendLine("Session context:");
        builder.AppendLine($"- Title: {session.Name}");
        builder.AppendLine($"- Created at (UTC): {session.CreatedAt:u}");
        builder.AppendLine($"- Messages recorded: {session.Messages.Count}");

        if (personas.Count > 0)
        {
            builder.AppendLine("- Personas:");
            foreach (var persona in personas)
            {
                builder.Append("  * ");
                builder.Append(persona.Name);
                if (!string.IsNullOrWhiteSpace(persona.Concept))
                {
                    builder.Append(": ");
                    builder.Append(persona.Concept);
                }

                if (!string.IsNullOrWhiteSpace(persona.Backstory))
                {
                    builder.Append(" Backstory: ");
                    builder.Append(persona.Backstory);
                }

                builder.AppendLine();
            }
        }

        if (history.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Chat scrollback:");
            foreach (var entry in history)
            {
                AppendScrollbackEntry(builder, entry);
            }
        }

        var summary = builder.ToString().Trim();
        Log(LogLevel.Debug, "Context summary built.", new Dictionary<string, object?>
        {
            ["length"] = summary.Length,
            ["historyEntries"] = history.Count
        });

        return summary;
    }

    private static string? BuildMessageName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > 64)
        {
            trimmed = trimmed[..64];
        }

        var builder = new StringBuilder(trimmed.Length);
        foreach (var ch in trimmed)
        {
            builder.Append(char.IsLetterOrDigit(ch) ? ch : '-');
        }

        var normalized = builder.ToString().Trim('-');
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private static IReadOnlyList<ChatMessageEntry> FilterSystemCommands(IEnumerable<ChatMessageEntry> history)
    {
        var filtered = new List<ChatMessageEntry>();
        foreach (var entry in history)
        {
            if (!ContainsSystemCommand(entry.Content))
            {
                filtered.Add(entry);
            }
        }

        return filtered;
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

    private static void AppendScrollbackEntry(StringBuilder builder, ChatMessageEntry entry)
    {
        var content = entry.Content?.Trim() ?? string.Empty;
        builder.Append("- ");
        builder.Append(entry.Author);

        if (string.IsNullOrEmpty(content))
        {
            builder.AppendLine(":");
            builder.AppendLine("  (no content)");
            return;
        }

        var normalized = NormalizeLineEndings(content);
        var lines = normalized.Split('\n');
        builder.AppendLine(":");
        foreach (var line in lines)
        {
            builder.Append("  ");
            builder.AppendLine(line);
        }
    }

    private static string NormalizeLineEndings(string value)
    {
        return value
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
    }

    private static string BuildPreview(string content, int maxLength = 120)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var trimmed = content.Trim();
        if (trimmed.Length <= maxLength)
        {
            return trimmed;
        }

        return trimmed[..maxLength] + "…";
    }

    private void Log(LogLevel level, string message, IReadOnlyDictionary<string, object?>? metadata = null)
    {
        _logBuffer.Log(nameof(NarrationService), level, message, metadata);
    }
}
