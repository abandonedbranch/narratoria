using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace Narratoria.Pipeline.Transforms.Llm.Providers;

public sealed class OpenAiTextGenerationService : ITextGenerationService
{
    private readonly OpenAiProviderOptions _options;
    private readonly ILogger<OpenAiTextGenerationService> _logger;
    private readonly ChatClient _client;

    public OpenAiTextGenerationService(OpenAiProviderOptions options, ILogger<OpenAiTextGenerationService> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _client = new ChatClient(model: _options.Model, apiKey: _options.ApiKey);
    }

    public async Task<TextGenerationResponse> GenerateAsync(TextGenerationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        // The OpenAI .NET SDK wraps the underlying HTTP stack; cancellation is best-effort via overloads.
        ChatCompletion completion = await _client.CompleteChatAsync(
            [new UserChatMessage(request.Prompt)],
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var text = completion.Content.Count > 0 ? completion.Content[0].Text : string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("OpenAI completion returned empty content.");
        }

        return new TextGenerationResponse
        {
            GeneratedText = text,
            Metadata = new TextGenerationMetadata { Model = _options.Model },
        };
    }
}
