using OpenAI;
using OpenAI.Chat;

namespace Narratoria.OpenAi;

public sealed class OpenAiChatStreamingProvider : IOpenAiStreamingProvider
{
    private readonly ChatClient _client;

    public OpenAiChatStreamingProvider(
        string model,
        OpenAiProviderCredentials credentials,
        Uri endpoint,
        IReadOnlyDictionary<string, string>? additionalHeaders = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(credentials);

        var options = new OpenAIClientOptions();
        if (endpoint is not null)
        {
            options.Endpoint = endpoint;
        }

        if (additionalHeaders is not null)
        {
            if (additionalHeaders.TryGetValue("OpenAI-Organization", out var organizationId) && !string.IsNullOrEmpty(organizationId))
            {
                options.OrganizationId = organizationId;
            }

            if (additionalHeaders.TryGetValue("OpenAI-Project", out var projectId) && !string.IsNullOrEmpty(projectId))
            {
                options.ProjectId = projectId;
            }
        }

        var apiKeyCredential = new System.ClientModel.ApiKeyCredential(credentials.ApiKey);
        _client = new ChatClient(model, apiKeyCredential, options);
    }

    public IAsyncEnumerable<StreamingChatCompletionUpdate> StreamAsync(
        SerializedPrompt prompt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        var userMessage = ChatMessage.CreateUserMessage(prompt.Payload);
        var options = new ChatCompletionOptions();

        if (prompt.Metadata is not null)
        {
            foreach (var (key, value) in prompt.Metadata)
            {
                options.Metadata[key] = value;
            }
        }

        return _client.CompleteChatStreamingAsync(new[] { userMessage }, options, cancellationToken);
    }
}
