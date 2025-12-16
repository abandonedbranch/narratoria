namespace Narratoria.OpenAi;

public interface IOpenAiApiService
{
    IAsyncEnumerable<StreamedToken> StreamAsync(SerializedPrompt prompt, OpenAiRequestContext context, CancellationToken cancellationToken);
}
