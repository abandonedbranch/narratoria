using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Pipeline;
using Narratoria.Pipeline.Transforms;

namespace Narratoria.Tests.Pipeline;

[TestClass]
public sealed class TransformCompatibilityTests
{
    [TestMethod]
    public async Task Runner_AppliesTransformsInDeterministicOrder()
    {
        var source = new InlineTextSource("X");
        var transforms = new IPipelineTransform[]
        {
            new PrefixTextTransform("A"),
            new PrefixTextTransform("B"),
        };
        var sink = new CollectingTextSink();

        var definition = new PipelineDefinition<string>(source, transforms, sink);
        var runner = new PipelineRunner();

        var result = await runner.RunAsync(definition, CancellationToken.None);

        Assert.AreEqual(PipelineOutcomeStatus.Completed, result.Outcome.Status);
        Assert.AreEqual("BAX", result.SinkResult);
    }

    [TestMethod]
    public async Task AnnotateTransform_AddsAnnotationVisibleDownstream()
    {
        var source = new InlineTextSource("X");
        var transforms = new IPipelineTransform[]
        {
            new AnnotateTransform("source", "memory"),
        };
        var sink = new MetadataCapturingSink();

        var definition = new PipelineDefinition<IReadOnlyDictionary<string, string>>(source, transforms, sink);
        var runner = new PipelineRunner();

        var result = await runner.RunAsync(definition, CancellationToken.None);

        Assert.AreEqual(PipelineOutcomeStatus.Completed, result.Outcome.Status);
        Assert.IsNotNull(result.SinkResult);
        Assert.AreEqual("memory", result.SinkResult["source"]);
    }

    [TestMethod]
    public async Task TextAccumulator_FlushesByChunkCountAndAtEndOfStream()
    {
        var source = new InlineTextSource("a", "b", "c");
        var transforms = new IPipelineTransform[]
        {
            new TextAccumulatorTransform(maxChunks: 2),
        };
        var sink = new CapturingChunksSink();

        var definition = new PipelineDefinition<IReadOnlyList<string>>(source, transforms, sink);
        var runner = new PipelineRunner();

        var result = await runner.RunAsync(definition, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "ab", "c" }, result.SinkResult.ToArray());
    }

    [TestMethod]
    public async Task TextAccumulator_FlushesByUnicodeScalarValueCharacterCount()
    {
        // "💩" is one Unicode scalar value (code point), but two UTF-16 code units.
        var source = new InlineTextSource("💩", "a");
        var transforms = new IPipelineTransform[]
        {
            new TextAccumulatorTransform(maxCharacters: 2),
        };
        var sink = new CapturingChunksSink();

        var definition = new PipelineDefinition<IReadOnlyList<string>>(source, transforms, sink);
        var runner = new PipelineRunner();

        var result = await runner.RunAsync(definition, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "💩a" }, result.SinkResult.ToArray());
    }

    [TestMethod]
    public async Task Runner_WhenTypesIncompatible_FailsFastWithTypeMismatch()
    {
        var source = new InlineTextSource("X");
        var sink = new BytesSink();

        var definition = new PipelineDefinition<int>(source, Array.Empty<IPipelineTransform>(), sink);
        var runner = new PipelineRunner();

        var result = await runner.RunAsync(definition, CancellationToken.None);

        Assert.AreEqual(PipelineOutcomeStatus.Failed, result.Outcome.Status);
        Assert.AreEqual(PipelineFailureKind.TypeMismatch, result.Outcome.FailureKind);
    }

    private sealed class InlineTextSource(params string[] chunks) : IPipelineSource
    {
        public PipelineChunkType OutputType => PipelineChunkType.Text;

        public async IAsyncEnumerable<PipelineChunk> StreamAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var chunk in chunks)
            {
                yield return new TextChunk(chunk, PipelineChunkMetadata.Empty);
            }
        }
    }

    private sealed class CollectingTextSink : IPipelineSink<string>
    {
        public PipelineChunkType InputType => PipelineChunkType.Text;

        public async ValueTask<string> ConsumeAsync(IAsyncEnumerable<PipelineChunk> input, CancellationToken cancellationToken)
        {
            var builder = new System.Text.StringBuilder();
            await foreach (var chunk in input.WithCancellation(cancellationToken))
            {
                builder.Append(((TextChunk)chunk).Text);
            }
            return builder.ToString();
        }
    }

    private sealed class CapturingChunksSink : IPipelineSink<IReadOnlyList<string>>
    {
        private readonly List<string> _chunks = [];

        public PipelineChunkType InputType => PipelineChunkType.Text;

        public async ValueTask<IReadOnlyList<string>> ConsumeAsync(IAsyncEnumerable<PipelineChunk> input, CancellationToken cancellationToken)
        {
            await foreach (var chunk in input.WithCancellation(cancellationToken))
            {
                _chunks.Add(((TextChunk)chunk).Text);
            }

            return _chunks;
        }
    }

    private sealed class MetadataCapturingSink : IPipelineSink<IReadOnlyDictionary<string, string>>
    {
        public PipelineChunkType InputType => PipelineChunkType.Text;

        public async ValueTask<IReadOnlyDictionary<string, string>> ConsumeAsync(
            IAsyncEnumerable<PipelineChunk> input,
            CancellationToken cancellationToken)
        {
            PipelineChunkMetadata? last = null;

            await foreach (var chunk in input.WithCancellation(cancellationToken))
            {
                last = chunk.Metadata;
            }

            return last?.Annotations ?? new Dictionary<string, string>();
        }
    }

    private sealed class BytesSink : IPipelineSink<int>
    {
        public PipelineChunkType InputType => PipelineChunkType.Bytes;

        public ValueTask<int> ConsumeAsync(IAsyncEnumerable<PipelineChunk> input, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Should not run due to incompatibility");
    }
}
