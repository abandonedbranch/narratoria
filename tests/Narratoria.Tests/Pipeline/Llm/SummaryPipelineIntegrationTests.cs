using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Pipeline;
using Narratoria.Pipeline.Transforms.Llm;
using Narratoria.Pipeline.Transforms.Llm.Providers;
using Narratoria.Pipeline.Transforms.Llm.StoryState;

namespace Narratoria.Tests.Pipeline.Llm;

[TestClass]
public sealed class SummaryPipelineIntegrationTests
{
    [TestMethod]
    public async Task Pipeline_SummaryTransform_UpdatesAcrossMultipleChunks()
    {
        var counter = 0;
        var service = new FakeTextGenerationService(_ =>
        {
            counter++;
            return new TextGenerationResponse { GeneratedText = $"Summary v{counter}" };
        });

        var transform = new StorySummaryTransform(service, NullLogger<StorySummaryTransform>.Instance);

        var initial = StoryState.Empty("s1");
        var initialMetadata = StoryStateAnnotations.Write(PipelineChunkMetadata.Empty, initial);

        var chunks = new[]
        {
            new TextChunk("Chunk1", initialMetadata),
            new TextChunk("Chunk2", initialMetadata),
        };

        var (outcome, _, annotations) = await LlmPipelineHarness.RunAsync(chunks, [transform]);

        Assert.AreEqual(PipelineOutcomeStatus.Completed, outcome.Status);

        var parsed = StoryStateJson.TryDeserialize(annotations[StoryStateAnnotations.StoryStateJsonKey]);
        Assert.IsNotNull(parsed);
        Assert.AreEqual("Summary v2", parsed.Summary);
        Assert.AreEqual(2, parsed.Version);
    }
}
