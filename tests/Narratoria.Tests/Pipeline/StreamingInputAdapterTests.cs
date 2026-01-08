using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Pipeline;
using Narratoria.Pipeline.Text;
using Narratoria.Pipeline.Transforms;

namespace Narratoria.Tests.Pipeline;

[TestClass]
public sealed class StreamingInputAdapterTests
{
    [TestMethod]
    public async Task BytesToText_DecodesOnlyWhenEncodingContractIsDeclared()
    {
        var config = new TextSourceConfig
        {
            ByteStream = TextInputAdapters.FromBytes(Encoding.UTF8.GetBytes("Hi")),
            ByteStreamEncodingName = "utf-8",
        };

        var source = new TextPromptSource(config);
        var transforms = new IPipelineTransform[] { new DecodeBytesToTextTransform() };
        var sink = new TextCollectingSink();

        var definition = new PipelineDefinition<string>(source, transforms, sink);
        var runner = new PipelineRunner();

        var result = await runner.RunAsync(definition, CancellationToken.None);

        Assert.AreEqual(PipelineOutcomeStatus.Completed, result.Outcome.Status);
        Assert.AreEqual("Hi", result.SinkResult);
    }

    [TestMethod]
    public async Task BytesToText_WhenMissingEncodingContract_FailsWithDecodeFailure()
    {
        var source = new InlineBytesSource();
        var transforms = new IPipelineTransform[] { new DecodeBytesToTextTransform() };
        var sink = new TextCollectingSink();

        var definition = new PipelineDefinition<string>(source, transforms, sink);
        var runner = new PipelineRunner();

        var result = await runner.RunAsync(definition, CancellationToken.None);

        Assert.AreEqual(PipelineOutcomeStatus.Failed, result.Outcome.Status);
        Assert.AreEqual(PipelineFailureKind.DecodeFailure, result.Outcome.FailureKind);
    }

    private sealed class InlineBytesSource : IPipelineSource
    {
        public PipelineChunkType OutputType => PipelineChunkType.Bytes;

        public async IAsyncEnumerable<PipelineChunk> StreamAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return new BytesChunk(Encoding.UTF8.GetBytes("Hi"), PipelineChunkMetadata.Empty);
        }
    }
}
