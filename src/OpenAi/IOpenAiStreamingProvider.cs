using OpenAI.Chat;

namespace Narratoria.OpenAi;

public interface IOpenAiStreamingProvider
{
    IAsyncEnumerable<StreamingChatCompletionUpdate> StreamAsync(SerializedPrompt prompt, CancellationToken cancellationToken);
}
