namespace Narratoria.Narration;

public interface INarrationSessionStore
{
    ValueTask<NarrationContext?> LoadAsync(Guid sessionId, CancellationToken cancellationToken);
    ValueTask SaveAsync(NarrationContext context, CancellationToken cancellationToken);
}

public interface INarrationProvider
{
    IAsyncEnumerable<string> StreamNarrationAsync(NarrationContext context, CancellationToken cancellationToken);
}
