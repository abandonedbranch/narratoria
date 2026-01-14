namespace Narratoria.Pipeline.Transforms.Llm.Providers;

public interface ITextGenerationService
{
    Task<TextGenerationResponse> GenerateAsync(TextGenerationRequest request, CancellationToken cancellationToken);
}
