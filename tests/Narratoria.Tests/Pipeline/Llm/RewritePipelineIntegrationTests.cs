using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Pipeline;
using Narratoria.Pipeline.Transforms.Llm;
using Narratoria.Pipeline.Transforms.Llm.Providers;
using Narratoria.Pipeline.Transforms.Llm.StoryState;

namespace Narratoria.Tests.Pipeline.Llm;

[TestClass]
public sealed class RewritePipelineIntegrationTests
{
    [TestMethod]
    public async Task Pipeline_RewriteTransform_Rewrites_AndAnnotatesOriginal()
    {
        var service = new FakeTextGenerationService(_ => new TextGenerationResponse { GeneratedText = "Rewritten." });
        var transform = new RewriteNarrationTransform(service, NullLogger<RewriteNarrationTransform>.Instance);

        var (outcome, text, annotations) = await LlmPipelineHarness.RunAsync(
            [new TextChunk("Bad gramar.", PipelineChunkMetadata.Empty)],
            [transform]);

        Assert.AreEqual(PipelineOutcomeStatus.Completed, outcome.Status);
        Assert.AreEqual("Rewritten.", text);
        Assert.IsTrue(annotations.ContainsKey(StoryStateAnnotations.OriginalTextKey));
        Assert.AreEqual("Bad gramar.", annotations[StoryStateAnnotations.OriginalTextKey]);
    }
}
