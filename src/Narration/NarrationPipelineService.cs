using System.Collections.Immutable;

namespace Narratoria.Narration;

public sealed class NarrationPipelineService
{
    private readonly NarrationMiddlewareNext _pipeline;

    public NarrationPipelineService(IEnumerable<NarrationMiddleware>? middleware = null)
    {
        var chain = middleware?.ToImmutableArray() ?? ImmutableArray<NarrationMiddleware>.Empty;
        _pipeline = BuildPipeline(chain);
    }

    public ValueTask<MiddlewareResult> RunAsync(NarrationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var initialResult = MiddlewareResult.FromContext(context);
        return _pipeline(context, initialResult, cancellationToken);
    }

    private static NarrationMiddlewareNext BuildPipeline(ImmutableArray<NarrationMiddleware> middleware)
    {
        NarrationMiddlewareNext next = static (context, result, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(result);
        };

        for (var i = middleware.Length - 1; i >= 0; i--)
        {
            var middlewareStep = middleware[i];
            var current = next;
            next = (context, result, cancellationToken) => middlewareStep(context, result, current, cancellationToken);
        }

        return next;
    }
}
