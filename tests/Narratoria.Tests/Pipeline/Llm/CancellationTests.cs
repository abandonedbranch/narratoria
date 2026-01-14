using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Pipeline;
using Narratoria.Pipeline.Transforms.Llm;
using Narratoria.Pipeline.Transforms.Llm.Providers;

namespace Narratoria.Tests.Pipeline.Llm;

[TestClass]
public sealed class CancellationTests
{
    [TestMethod]
    public async Task RewriteTransform_WhenCanceled_PropagatesCancellationAsCanceledOutcome()
    {
        var service = new CancelAwareService(delay: Timeout.InfiniteTimeSpan);
        var transform = new RewriteNarrationTransform(service, NullLogger<RewriteNarrationTransform>.Instance);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(25));

        var (outcome, _, _) = await LlmPipelineHarness.RunAsync(
            [new TextChunk("Input.", PipelineChunkMetadata.Empty)],
            [transform],
            cts.Token);

        Assert.AreEqual(PipelineOutcomeStatus.Canceled, outcome.Status);
    }

    [TestMethod]
    public async Task SummaryTransform_WhenCanceled_PropagatesCancellationAsCanceledOutcome()
    {
        var service = new CancelAwareService(delay: Timeout.InfiniteTimeSpan);
        var transform = new StorySummaryTransform(service, NullLogger<StorySummaryTransform>.Instance);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(25));

        var (outcome, _, _) = await LlmPipelineHarness.RunAsync(
            [new TextChunk("Input.", PipelineChunkMetadata.Empty)],
            [transform],
            cts.Token);

        Assert.AreEqual(PipelineOutcomeStatus.Canceled, outcome.Status);
    }

    private sealed class CancelAwareService(TimeSpan delay) : ITextGenerationService
    {
        public async Task<TextGenerationResponse> GenerateAsync(TextGenerationRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken);
            return new TextGenerationResponse { GeneratedText = "unused" };
        }
    }
}
