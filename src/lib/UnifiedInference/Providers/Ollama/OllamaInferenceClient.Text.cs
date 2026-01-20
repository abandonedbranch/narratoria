using UnifiedInference.Abstractions;
using UnifiedInference.Core;

namespace UnifiedInference.Providers.Ollama;

public sealed class OllamaInferenceClient
{
    // Placeholder transport; will be HttpClient or native client.
    public Task<TextResponse> GenerateTextAsync(TextRequest request, CancellationToken cancellationToken)
    {
        _ = request;
        _ = cancellationToken;
        throw Errors.ModalityNotSupported("text", InferenceProvider.Ollama, request.ModelId);
    }
}
