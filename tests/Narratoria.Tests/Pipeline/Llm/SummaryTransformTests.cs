using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Pipeline;
using Narratoria.Pipeline.Transforms.Llm;
using Narratoria.Pipeline.Transforms.Llm.Providers;
using Narratoria.Pipeline.Transforms.Llm.StoryState;

namespace Narratoria.Tests.Pipeline.Llm;

[TestClass]
public sealed class SummaryTransformTests
{
    [TestMethod]
    public async Task SummaryTransform_UpdatesStoryStateSummary()
    {
        var service = new FakeTextGenerationService(_ => new TextGenerationResponse { GeneratedText = "Updated recap." });
        var transform = new StorySummaryTransform(service, NullLogger<StorySummaryTransform>.Instance);

        var initial = StoryState.Empty("s1") with { Summary = "Old" };
        var metadata = StoryStateAnnotations.Write(PipelineChunkMetadata.Empty, initial);

        var (outcome, text, annotations) = await LlmPipelineHarness.RunAsync(
            [new TextChunk("Rewritten narration.", metadata)],
            [transform]);

        Assert.AreEqual(PipelineOutcomeStatus.Completed, outcome.Status);
        Assert.AreEqual("Rewritten narration.", text);
        Assert.IsTrue(annotations.ContainsKey(StoryStateAnnotations.StoryStateJsonKey));

        var parsed = StoryStateJson.TryDeserialize(annotations[StoryStateAnnotations.StoryStateJsonKey]);
        Assert.IsNotNull(parsed);
        Assert.AreEqual("Updated recap.", parsed.Summary);
        Assert.AreEqual(1, parsed.Version);
    }

    [TestMethod]
    public async Task SummaryTransform_OnProviderFailure_LeavesStoryStateUnchanged()
    {
        var service = new FakeTextGenerationService(_ => throw new InvalidOperationException("boom"));
        var transform = new StorySummaryTransform(service, NullLogger<StorySummaryTransform>.Instance);

        var initial = StoryState.Empty("s1") with { Summary = "Old" };
        var metadata = StoryStateAnnotations.Write(PipelineChunkMetadata.Empty, initial);

        var (_, _, annotations) = await LlmPipelineHarness.RunAsync(
            [new TextChunk("Text.", metadata)],
            [transform]);

        var parsed = StoryStateJson.TryDeserialize(annotations[StoryStateAnnotations.StoryStateJsonKey]);
        Assert.IsNotNull(parsed);
        Assert.AreEqual("Old", parsed.Summary);
        Assert.AreEqual(0, parsed.Version);
    }
}
