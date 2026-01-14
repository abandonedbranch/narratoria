using Microsoft.Extensions.Logging.Abstractions;
using Narratoria.Pipeline;
using Narratoria.Pipeline.Transforms.Llm.Providers;

namespace Narratoria.Tests.Pipeline.Llm;

internal static class LlmPipelineHarness
{
    public static async Task<(PipelineOutcome Outcome, string CollectedText, IReadOnlyDictionary<string, string> LastAnnotations)> RunAsync(
        IEnumerable<TextChunk> chunks,
        IReadOnlyList<IPipelineTransform> transforms,
        CancellationToken cancellationToken = default)
    {
        var source = new InlineTextChunkSource(chunks);
        var sink = new CollectingTextAndMetadataSink();
        var runner = new PipelineRunner();

        var result = await runner.RunAsync(new PipelineDefinition<(string, IReadOnlyDictionary<string, string>)>(source, transforms, sink), cancellationToken);

        var (text, annotations) = result.SinkResult;
        return (result.Outcome, text, annotations);
    }

    public static (ITextGenerationService Service, NullLoggerFactory LoggerFactory) CreateFake(Func<TextGenerationRequest, TextGenerationResponse> handler)
    {
        var loggerFactory = NullLoggerFactory.Instance;
        return (new FakeTextGenerationService(handler), loggerFactory);
    }

    private sealed class InlineTextChunkSource(IEnumerable<TextChunk> chunks) : IPipelineSource
    {
        private readonly IReadOnlyList<TextChunk> _chunks = chunks.ToArray();

        public PipelineChunkType OutputType => PipelineChunkType.Text;

        public async IAsyncEnumerable<PipelineChunk> StreamAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var chunk in _chunks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return chunk;
                await Task.Yield();
            }
        }
    }

    private sealed class CollectingTextAndMetadataSink : IPipelineSink<(string, IReadOnlyDictionary<string, string>)>
    {
        private readonly System.Text.StringBuilder _builder = new();

        public PipelineChunkType InputType => PipelineChunkType.Text;

        public async ValueTask<(string, IReadOnlyDictionary<string, string>)> ConsumeAsync(
            IAsyncEnumerable<PipelineChunk> input,
            CancellationToken cancellationToken)
        {
            PipelineChunkMetadata? last = null;

            _builder.Clear();

            await foreach (var chunk in input.WithCancellation(cancellationToken))
            {
                if (chunk is not TextChunk textChunk)
                {
                    throw new PipelineStageException(PipelineFailureKind.SinkFailed, $"Expected text chunk, got '{chunk.Type}'");
                }

                _builder.Append(textChunk.Text);
                last = textChunk.Metadata;
            }

            return (_builder.ToString(), last?.Annotations ?? new Dictionary<string, string>());
        }
    }
}
