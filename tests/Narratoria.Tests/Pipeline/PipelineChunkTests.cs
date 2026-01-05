using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Pipeline;

namespace Narratoria.Tests.Pipeline;

[TestClass]
public sealed class PipelineChunkTests
{
    [TestMethod]
    public void TextChunk_HasTextType()
    {
        var chunk = new TextChunk("hi", PipelineChunkMetadata.Empty);

        Assert.AreEqual(PipelineChunkType.Text, chunk.Type);
        Assert.AreEqual("hi", chunk.Text);
    }

    [TestMethod]
    public void Metadata_Merge_PrefersLaterValuesAndMergesAnnotations()
    {
        var a = PipelineChunkMetadata.Empty
            .WithAnnotation("k1", "v1")
            .WithAnnotation("k2", "v2");

        var b = new PipelineChunkMetadata(TextEncodingName: "utf-8")
            .WithAnnotation("k2", "v2b")
            .WithAnnotation("k3", "v3");

        var merged = PipelineChunkMetadata.Merge(a, b);

        Assert.AreEqual("utf-8", merged.TextEncodingName);
        Assert.IsNotNull(merged.Annotations);
        Assert.AreEqual("v1", merged.Annotations["k1"]);
        Assert.AreEqual("v2b", merged.Annotations["k2"]);
        Assert.AreEqual("v3", merged.Annotations["k3"]);
    }
}
