using System.Runtime.CompilerServices;
using Narratoria.OpenAi;

namespace Narratoria.Narration;

public sealed class OpenAiNarrationProvider : INarrationProvider
{
    private readonly IOpenAiApiService _openAi;
    private readonly INarrationOpenAiContextFactory _contextFactory;
    private readonly INarrationPromptSerializer _promptSerializer;

    public OpenAiNarrationProvider(
        IOpenAiApiService openAi,
        INarrationOpenAiContextFactory contextFactory,
        INarrationPromptSerializer promptSerializer)
    {
        _openAi = openAi ?? throw new ArgumentNullException(nameof(openAi));
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _promptSerializer = promptSerializer ?? throw new ArgumentNullException(nameof(promptSerializer));
    }

    public async IAsyncEnumerable<string> StreamNarrationAsync(NarrationContext context, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var prompt = _promptSerializer.Serialize(context);
        var requestContext = _contextFactory.Create(context);

        await foreach (var token in _openAi.StreamAsync(prompt, requestContext, cancellationToken).WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrEmpty(token.Content))
            {
                yield return token.Content;
            }
        }
    }
}
