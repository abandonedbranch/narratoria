using System.Text;
using Microsoft.Extensions.Logging;

namespace NarratoriaClient.Services.Pipeline;

public sealed class LlmClientStage : INarrationPipelineStage
{
    private readonly IOpenAiChatService _chatService;
    private readonly ILogBuffer _logBuffer;
    private readonly ILogger<LlmClientStage> _logger;

    public LlmClientStage(IOpenAiChatService chatService, ILogBuffer logBuffer, ILogger<LlmClientStage> logger)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _logBuffer = logBuffer ?? throw new ArgumentNullException(nameof(logBuffer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string StageName => "llm-client";

    public int Order => 4;

    public async Task ExecuteAsync(NarrationPipelineContext context, CancellationToken cancellationToken)
    {
        if (context.PromptMessages is null)
        {
            throw new InvalidOperationException("Prompt messages must be assembled before invoking the LLM.");
        }

        if (string.IsNullOrWhiteSpace(context.SelectedModel))
        {
            throw new InvalidOperationException("A model must be selected before invoking the LLM.");
        }

        var request = new ChatCompletionRequest
        {
            Model = context.SelectedModel,
            Messages = context.PromptMessages
        };

        _logBuffer.Log(StageName, LogLevel.Information, "Dispatching narration request.", new Dictionary<string, object?>
        {
            ["model"] = request.Model,
            ["messageCount"] = request.Messages.Count
        });

        var builder = new StringBuilder();
        var streamStarted = false;

        try
        {
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
        }
        catch (OperationCanceledException)
        {
            context.ReportStatus(NarrationStatus.Idle, "Narrator is ready.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM request failed.");
            context.ReportStatus(NarrationStatus.Disconnected, "Narrator left the table. Retry to reconnect.");
            throw;
        }

        var narration = builder.ToString().Trim();
        if (string.IsNullOrWhiteSpace(narration))
        {
            narration = "The narrator had nothing to add.";
        }

        context.GeneratedNarration = narration;

        _logBuffer.Log(StageName, LogLevel.Information, "Narrator response received.", new Dictionary<string, object?>
        {
            ["length"] = narration.Length,
            ["preview"] = BuildPreview(narration)
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
