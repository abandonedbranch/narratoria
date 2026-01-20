using UnifiedInference.Abstractions;
using UnifiedInference.Core;

namespace UnifiedInference.Providers.HuggingFace;

public sealed class HuggingFaceInferenceClient
{
    private readonly HttpClient _http;

    public HuggingFaceInferenceClient(HttpClient http)
    {
        _http = http;
    }

    // Placeholder for text generation over HF inference APIs.
    public Task<TextResponse> GenerateTextAsync(TextRequest request, CancellationToken cancellationToken)
    {
        _ = request;
        _ = cancellationToken;
        throw Errors.ModalityNotSupported("text", InferenceProvider.HuggingFace, request.ModelId);
    }
}
