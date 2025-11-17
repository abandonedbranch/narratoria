using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NarratoriaClient.Services;

public sealed class FakeOpenAiChatService : IOpenAiChatService
{
    private readonly ILogger<FakeOpenAiChatService> _logger;
    private readonly ILogBuffer _logBuffer;

    public FakeOpenAiChatService(ILogger<FakeOpenAiChatService> logger, ILogBuffer logBuffer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logBuffer = logBuffer ?? throw new ArgumentNullException(nameof(logBuffer));
    }

    public async IAsyncEnumerable<ChatCompletionUpdate> StreamChatCompletionAsync(
        ChatCompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var response = BuildResponse(request);
        if (string.IsNullOrEmpty(response))
        {
            yield break;
        }

        _logBuffer.Log(nameof(FakeOpenAiChatService), LogLevel.Information, "Fake response generated.", new Dictionary<string, object?>
        {
            ["model"] = request.Model,
            ["length"] = response.Length
        });

        var tokens = response.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var first = true;
        foreach (var token in tokens)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();

            var prefix = first ? string.Empty : " ";
            first = false;
            yield return new ChatCompletionUpdate(0, "assistant", prefix + token, null, false);
        }

        yield return new ChatCompletionUpdate(0, null, null, "stop", true);
    }

    private static string BuildResponse(ChatCompletionRequest request)
    {
        try
        {
            var messages = request.Messages ?? Array.Empty<ChatPromptMessage>();
            var lastUser = messages.LastOrDefault(m => string.Equals(m.Role, "user", StringComparison.OrdinalIgnoreCase));
            if (lastUser is null)
            {
                return "[FAKE NARRATOR] Awaiting your first move.";
            }

            var prompt = lastUser.Content ?? string.Empty;
            var builder = new StringBuilder();
            builder.Append("[FAKE NARRATOR] You said: \"");
            builder.Append(prompt);
            builder.Append("\". Let us continue the tale.");
            return builder.ToString();
        }
        catch (Exception ex)
        {
            // Defensive: never let the fake service disrupt testing.
            return $"[FAKE NARRATOR] Unable to craft a response ({ex.Message}).";
        }
    }
}
