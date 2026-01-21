using OpenAI;
using OpenAI.Chat;
using UnifiedInference.Abstractions;

namespace UnifiedInference.Providers.OpenAI;

public sealed partial class OpenAiInferenceClient
{
    private readonly OpenAIClient _client;

    public OpenAiInferenceClient(OpenAIClient client)
    {
        _client = client;
    }

    public async Task<TextResponse> GenerateTextAsync(TextRequest request, CancellationToken cancellationToken)
    {
        var chatClient = _client.GetChatClient(request.ModelId);
        var options = new ChatCompletionOptions
        {
            Temperature = (float?)request.Settings.Temperature,
            TopP = (float?)request.Settings.TopP,
            PresencePenalty = (float?)request.Settings.PresencePenalty,
            FrequencyPenalty = (float?)request.Settings.FrequencyPenalty,
            MaxTokens = request.Settings.MaxTokens,
        };
        if (request.Settings.StopSequences is not null)
        {
            foreach (var stop in request.Settings.StopSequences)
            {
                options.StopSequences.Add(stop);
            }
        }

        var chatMessages = new List<ChatMessage> { new UserChatMessage(request.Prompt) };
        var result = await chatClient.CompleteChatAsync(chatMessages, options, cancellationToken).ConfigureAwait(false);
        var completion = result.Value;
        var text = completion?.Content?.FirstOrDefault()?.Text ?? string.Empty;
        var id = completion?.Id;
        return new TextResponse(text, null, new { id });
    }

    // Expose native client for advanced callers.
    public OpenAIClient NativeClient => _client;
}
