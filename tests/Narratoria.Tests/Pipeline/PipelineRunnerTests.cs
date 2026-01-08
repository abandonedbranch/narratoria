using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Pipeline;

namespace Narratoria.Tests.Pipeline;

[TestClass]
public sealed class PipelineRunnerTests
{
    [TestMethod]
    public async Task Runner_StreamsIncrementally_SinkObservesFirstChunkBeforeCompletion()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sawFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var source = new GatedTextSource(gate);
        var sink = new SignalingTextSink(sawFirst);

        var definition = new PipelineDefinition<string>(source, Array.Empty<IPipelineTransform>(), sink);
        var runner = new PipelineRunner();

        var runTask = runner.RunAsync(definition, CancellationToken.None);

        await sawFirst.Task;

        Assert.AreEqual("A", sink.CollectedText);

        gate.SetResult();

        var result = await runTask;

        Assert.AreEqual(PipelineOutcomeStatus.Completed, result.Outcome.Status);
        Assert.AreEqual("AB", result.SinkResult);
    }

    [TestMethod]
    public async Task Runner_Cancels_RunTerminatesAndStopsFurtherDelivery()
    {
        using var cts = new CancellationTokenSource();
        var sawFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var source = new InfiniteTextSource();
        var sink = new CancelAfterFirstChunkSink(cts, sawFirst);

        var definition = new PipelineDefinition<int>(source, Array.Empty<IPipelineTransform>(), sink);
        var runner = new PipelineRunner();

        var result = await runner.RunAsync(definition, cts.Token);

        Assert.AreEqual(PipelineOutcomeStatus.Canceled, result.Outcome.Status);
        Assert.AreEqual(1, result.SinkResult);
    }

    [TestMethod]
    public async Task Runner_WhenSinkStopsEarly_SourceEnumeratorIsDisposed()
    {
        var disposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var source = new DisposingTextSource(disposed);
        var sink = new StopAfterFirstChunkSink();

        var definition = new PipelineDefinition<int>(source, Array.Empty<IPipelineTransform>(), sink);
        var runner = new PipelineRunner();

        var result = await runner.RunAsync(definition, CancellationToken.None);
        Assert.AreEqual(PipelineOutcomeStatus.Completed, result.Outcome.Status);
        Assert.AreEqual(1, result.SinkResult);

        await disposed.Task;
    }

    [TestMethod]
    public async Task Runner_WhenBlockedExceptionThrown_ReturnsBlockedOutcome()
    {
        var source = new GatedTextSource(new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        var transform = new ThrowBlockedTransform();
        var sink = new SignalingTextSink(new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));

        var definition = new PipelineDefinition<string>(source, new[] { (IPipelineTransform)transform }, sink);
        var runner = new PipelineRunner();

        var result = await runner.RunAsync(definition, CancellationToken.None);

        Assert.AreEqual(PipelineOutcomeStatus.Blocked, result.Outcome.Status);
        Assert.AreEqual("Blocked", result.Outcome.SafeMessage);
    }

    [TestMethod]
    public async Task Runner_WhenUnexpectedExceptionThrown_ReturnsUnknownFailureWithTypeInMessage()
    {
        var source = new GatedTextSource(new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        var sink = new ThrowingSink();

        var definition = new PipelineDefinition<string>(source, Array.Empty<IPipelineTransform>(), sink);
        var runner = new PipelineRunner();

        var result = await runner.RunAsync(definition, CancellationToken.None);

        Assert.AreEqual(PipelineOutcomeStatus.Failed, result.Outcome.Status);
        Assert.AreEqual(PipelineFailureKind.Unknown, result.Outcome.FailureKind);
        Assert.IsNotNull(result.Outcome.SafeMessage);
        StringAssert.Contains(result.Outcome.SafeMessage, nameof(InvalidOperationException));
    }

    private sealed class GatedTextSource(TaskCompletionSource gate) : IPipelineSource
    {
        public PipelineChunkType OutputType => PipelineChunkType.Text;

        public async IAsyncEnumerable<PipelineChunk> StreamAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return new TextChunk("A", PipelineChunkMetadata.Empty);
            await gate.Task;
            yield return new TextChunk("B", PipelineChunkMetadata.Empty);
        }
    }

    private sealed class SignalingTextSink(TaskCompletionSource sawFirst) : IPipelineSink<string>
    {
        private readonly List<string> _chunks = [];

        public PipelineChunkType InputType => PipelineChunkType.Text;

        public string CollectedText => string.Concat(_chunks);

        public async ValueTask<string> ConsumeAsync(IAsyncEnumerable<PipelineChunk> input, CancellationToken cancellationToken)
        {
            await foreach (var chunk in input.WithCancellation(cancellationToken))
            {
                var text = ((TextChunk)chunk).Text;
                _chunks.Add(text);

                if (_chunks.Count == 1)
                {
                    sawFirst.SetResult();
                }
            }

            return CollectedText;
        }
    }

    private sealed class InfiniteTextSource : IPipelineSource
    {
        public PipelineChunkType OutputType => PipelineChunkType.Text;

        public async IAsyncEnumerable<PipelineChunk> StreamAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var i = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new TextChunk(i.ToString(), PipelineChunkMetadata.Empty);
                i++;
                await Task.Yield();
            }
        }
    }

    private sealed class CancelAfterFirstChunkSink(CancellationTokenSource cts, TaskCompletionSource sawFirst) : IPipelineSink<int>
    {
        public PipelineChunkType InputType => PipelineChunkType.Text;

        public async ValueTask<int> ConsumeAsync(IAsyncEnumerable<PipelineChunk> input, CancellationToken cancellationToken)
        {
            var count = 0;
            await foreach (var chunk in input.WithCancellation(cancellationToken))
            {
                _ = (TextChunk)chunk;
                count++;
                sawFirst.TrySetResult();
                cts.Cancel();
                break;
            }

            return count;
        }
    }

    private sealed class DisposingTextSource(TaskCompletionSource disposed) : IPipelineSource
    {
        public PipelineChunkType OutputType => PipelineChunkType.Text;

        public async IAsyncEnumerable<PipelineChunk> StreamAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            try
            {
                yield return new TextChunk("A", PipelineChunkMetadata.Empty);
                await Task.Delay(Timeout.Infinite, cancellationToken);
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

    private sealed class ThrowBlockedTransform : IPipelineTransform
    {
        public PipelineChunkType InputType => PipelineChunkType.Text;
        public PipelineChunkType OutputType => PipelineChunkType.Text;

        public async IAsyncEnumerable<PipelineChunk> TransformAsync(
            IAsyncEnumerable<PipelineChunk> input,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var _ in input.WithCancellation(cancellationToken))
            {
                throw new PipelineBlockedException("Blocked");
            }

            yield break;
        }
    }

    private sealed class ThrowingSink : IPipelineSink<string>
    {
        public PipelineChunkType InputType => PipelineChunkType.Text;

        public ValueTask<string> ConsumeAsync(IAsyncEnumerable<PipelineChunk> input, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Boom");
    }
}
