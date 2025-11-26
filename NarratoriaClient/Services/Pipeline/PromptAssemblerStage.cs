using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace NarratoriaClient.Services.Pipeline;

public sealed class PromptAssemblerStage : INarrationPipelineStage
{
    private static readonly Regex CommandPattern = new(@"@(?<command>[A-Za-z][\w-]*)", RegexOptions.Compiled);

    private readonly IAppDataService _appData;
    private readonly ILogBuffer _logBuffer;

    public PromptAssemblerStage(IAppDataService appData, ILogBuffer logBuffer)
    {
        _appData = appData ?? throw new ArgumentNullException(nameof(appData));
        _logBuffer = logBuffer ?? throw new ArgumentNullException(nameof(logBuffer));
    }

    public string StageName => "prompt-assembler";

    public int Order => 2;

    public async Task ExecuteAsync(NarrationPipelineContext context, CancellationToken cancellationToken)
    {
        var promptSettings = await _appData.GetPromptSettingsAsync(cancellationToken).ConfigureAwait(false);
        var history = await _appData.GetChatHistoryAsync(cancellationToken).ConfigureAwait(false);
        var filteredHistory = FilterSystemCommands(history);
        var contextSummary = await BuildContextSummaryAsync(filteredHistory, cancellationToken).ConfigureAwait(false);

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

        context.PromptMessages = messages;

        _logBuffer.Log(StageName, LogLevel.Debug, "Prompt assembled.", new Dictionary<string, object?>
        {
            ["messageCount"] = messages.Count
        });
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

        return builder.ToString().Trim();
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

    private static string BuildMessageName(string? value)
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
}
