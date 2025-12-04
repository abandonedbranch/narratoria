namespace Narratoria.Narration;

public delegate ValueTask<MiddlewareResult> NarrationMiddleware(NarrationContext context, MiddlewareResult result, NarrationMiddlewareNext next, CancellationToken cancellationToken);

public delegate ValueTask<MiddlewareResult> NarrationMiddlewareNext(NarrationContext context, MiddlewareResult result, CancellationToken cancellationToken);

public sealed record MiddlewareResult(IAsyncEnumerable<string> StreamedNarration, ValueTask<NarrationContext> UpdatedContext)
{
    public static MiddlewareResult FromContext(NarrationContext context) => new(AsyncEnumerable.Empty<string>(), ValueTask.FromResult(context));
}
