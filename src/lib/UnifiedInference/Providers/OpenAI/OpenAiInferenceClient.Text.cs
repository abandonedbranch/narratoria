using OpenAI;
using UnifiedInference.Abstractions;
using UnifiedInference.Core;

namespace UnifiedInference.Providers.OpenAI;

public sealed class OpenAiInferenceClient
{
    private readonly OpenAIClient _client;

    public OpenAiInferenceClient(OpenAIClient client)
    {
        _client = client;
    }

    // Placeholder for text generation; will call official SDK once wired.
    public Task<TextResponse> GenerateTextAsync(TextRequest request, CancellationToken cancellationToken)
    {
        _ = request;
        _ = cancellationToken;
        throw Errors.ModalityNotSupported("text", InferenceProvider.OpenAI, request.ModelId);
    }

    // Expose native client for advanced callers.
    public OpenAIClient NativeClient => _client;
}
