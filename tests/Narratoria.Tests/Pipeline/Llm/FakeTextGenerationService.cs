using Narratoria.Pipeline.Transforms.Llm.Providers;

namespace Narratoria.Tests.Pipeline.Llm;

public sealed class FakeTextGenerationService : ITextGenerationService
{
    private readonly Func<TextGenerationRequest, TextGenerationResponse> _handler;

    public FakeTextGenerationService(Func<TextGenerationRequest, TextGenerationResponse> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public Task<TextGenerationResponse> GenerateAsync(TextGenerationRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_handler(request));
    }

    public static FakeTextGenerationService CreatePassthrough() =>
        new(request => new TextGenerationResponse { GeneratedText = request.Prompt });
}
