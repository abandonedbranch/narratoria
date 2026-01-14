using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Pipeline;
using Narratoria.Pipeline.Transforms.Llm;
using Narratoria.Pipeline.Transforms.Llm.Providers;

namespace Narratoria.Tests.Pipeline.Llm;

[TestClass]
public sealed class StreamingAndMetadataTests
{
    [TestMethod]
    public async Task RewriteTransform_PreservesPassThroughAnnotations()
    {
        var service = new FakeTextGenerationService(_ => new TextGenerationResponse { GeneratedText = "Rewritten." });
        var transform = new RewriteNarrationTransform(service, NullLogger<RewriteNarrationTransform>.Instance);

        var metadata = PipelineChunkMetadata.Empty
            .WithAnnotation("narratoria.run_id", "r1")
            .WithAnnotation("custom.key", "custom.value");

        var (_, _, annotations) = await LlmPipelineHarness.RunAsync(
            [new TextChunk("Input.", metadata)],
            [transform]);

        Assert.AreEqual("r1", annotations["narratoria.run_id"]);
        Assert.AreEqual("custom.value", annotations["custom.key"]);
    }

    [TestMethod]
    public async Task SummaryTransform_DoesNotBufferPastFirstChunk_WhenSinkStopsEarly()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var disposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var service = new FakeTextGenerationService(_ => new TextGenerationResponse { GeneratedText = "Summary." });
        var transform = new StorySummaryTransform(service, NullLogger<StorySummaryTransform>.Instance);

        var source = new GatedSource(gate, disposed);
        var sink = new StopAfterFirstChunkSink();
        var runner = new PipelineRunner();

        var result = await runner.RunAsync(new PipelineDefinition<int>(source, [transform], sink), CancellationToken.None);

        Assert.AreEqual(PipelineOutcomeStatus.Completed, result.Outcome.Status);
        Assert.AreEqual(1, result.SinkResult);

        // If the transform (or runner) buffered aggressively, we'd deadlock on gate.
        // Disposal indicates upstream cancellation flowed and the enumerator exited.
        await WaitOrFail(disposed.Task, TimeSpan.FromSeconds(2));
    }

    private static async Task WaitOrFail(Task task, TimeSpan timeout)
    {
        var finished = await Task.WhenAny(task, Task.Delay(timeout));
        if (finished != task)
        {
            Assert.Fail("Timed out waiting for expected completion.");
        }

        await task;
    }

    private sealed class GatedSource(TaskCompletionSource gate, TaskCompletionSource disposed) : IPipelineSource
    {
        public PipelineChunkType OutputType => PipelineChunkType.Text;

        public async IAsyncEnumerable<PipelineChunk> StreamAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            try
            {
                yield return new TextChunk("Chunk1", PipelineChunkMetadata.Empty);
                await gate.Task.WaitAsync(cancellationToken);
                yield return new TextChunk("Chunk2", PipelineChunkMetadata.Empty);
            }
            finally
            {
                disposed.TrySetResult();
            }
        }
    }

    private sealed class StopAfterFirstChunkSink : IPipelineSink<int>
    {
        public PipelineChunkType InputType => PipelineChunkType.Text;

        public async ValueTask<int> ConsumeAsync(IAsyncEnumerable<PipelineChunk> input, CancellationToken cancellationToken)
        {
            var count = 0;
            await foreach (var chunk in input.WithCancellation(cancellationToken))
            {
                _ = (TextChunk)chunk;
                count++;
                break;
            }

            return count;
        }
    }
}
