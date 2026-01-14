using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Pipeline;
using Narratoria.Pipeline.Transforms.Llm;
using Narratoria.Pipeline.Transforms.Llm.Providers;
using Narratoria.Pipeline.Transforms.Llm.StoryState;

namespace Narratoria.Tests.Pipeline.Llm;

[TestClass]
public sealed class RewriteTransformTests
{
    [TestMethod]
    public async Task RewriteTransform_RewritesText_AndPreservesOriginalAnnotation()
    {
        var service = new FakeTextGenerationService(request => new TextGenerationResponse { GeneratedText = "Clean prose." });
        var transform = new RewriteNarrationTransform(service, NullLogger<RewriteNarrationTransform>.Instance);

        var input = new[]
        {
            new TextChunk("I has a apple.", PipelineChunkMetadata.Empty),
        };

        var output = await CollectAsync(transform.TransformAsync(AsAsync(input), CancellationToken.None));

        Assert.AreEqual(1, output.Count);
        var chunk = (TextChunk)output[0];
        Assert.AreEqual("Clean prose.", chunk.Text);
        Assert.IsNotNull(chunk.Metadata.Annotations);
        Assert.AreEqual("I has a apple.", chunk.Metadata.Annotations[StoryStateAnnotations.OriginalTextKey]);
    }

    [TestMethod]
    public async Task RewriteTransform_OnProviderFailure_PassesThroughText_AndDoesNotCorruptState()
    {
        var service = new FakeTextGenerationService(_ => throw new InvalidOperationException("boom"));
        var transform = new RewriteNarrationTransform(service, NullLogger<RewriteNarrationTransform>.Instance);

        var metadata = PipelineChunkMetadata.Empty
            .WithAnnotation(StoryStateAnnotations.StoryStateJsonKey, "{\"sessionId\":\"s1\",\"version\":0,\"characters\":[],\"inventory\":{\"items\":[],\"provenance\":{\"transformName\":\"seed\",\"confidence\":1}}}");

        var input = new[]
        {
            new TextChunk("Original.", metadata),
        };

        var output = await CollectAsync(transform.TransformAsync(AsAsync(input), CancellationToken.None));

        var chunk = (TextChunk)output[0];
        Assert.AreEqual("Original.", chunk.Text);
        Assert.IsNotNull(chunk.Metadata.Annotations);
        Assert.AreEqual("Original.", chunk.Metadata.Annotations[StoryStateAnnotations.OriginalTextKey]);
        Assert.AreEqual(metadata.Annotations![StoryStateAnnotations.StoryStateJsonKey], chunk.Metadata.Annotations[StoryStateAnnotations.StoryStateJsonKey]);
    }

    private static async IAsyncEnumerable<PipelineChunk> AsAsync(IEnumerable<PipelineChunk> chunks)
    {
        foreach (var chunk in chunks)
        {
            yield return chunk;
            await Task.Yield();
        }
    }

    private static async Task<List<PipelineChunk>> CollectAsync(IAsyncEnumerable<PipelineChunk> stream)
    {
        var items = new List<PipelineChunk>();
        await foreach (var chunk in stream)
        {
            items.Add(chunk);
        }

        return items;
    }
}
