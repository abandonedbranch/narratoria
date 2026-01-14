using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Pipeline;
using Narratoria.Pipeline.Transforms.Llm;
using Narratoria.Pipeline.Transforms.Llm.Providers;
using Narratoria.Pipeline.Transforms.Llm.StoryState;

namespace Narratoria.Tests.Pipeline.Llm;

[TestClass]
public sealed class CharacterTrackerTransformTests
{
    [TestMethod]
    public async Task CharacterTrackerTransform_UpsertsNewCharacter()
    {
        var jsonUpdate = """
        {
          "summary": null,
          "inventoryUpdates": null,
          "charactersToUpsert": [
            {
              "id": "c1",
              "displayName": "Ava",
              "aliases": ["The Seer"],
              "traits": { "role": "mage" },
              "relationships": [],
              "lastSeen": "Introduced in the tavern.",
              "provenance": {
                "transformName": "CharacterTrackerTransform",
                "confidence": 0.9,
                "sourceSnippet": "Ava enters the tavern.",
                "chunkIndex": 1
              }
            }
          ]
        }
        """;

        var service = new FakeTextGenerationService(_ => new TextGenerationResponse { GeneratedText = jsonUpdate });
        var transform = new CharacterTrackerTransform(service, NullLogger<CharacterTrackerTransform>.Instance);

        var initial = StoryState.Empty("s1") with { Summary = "Recap." };
        var metadata = StoryStateAnnotations.Write(PipelineChunkMetadata.Empty, initial);

        var (_, _, annotations) = await LlmPipelineHarness.RunAsync(
            [new TextChunk("Rewritten narration.", metadata)],
            [transform]);

        var parsed = StoryStateJson.TryDeserialize(annotations[StoryStateAnnotations.StoryStateJsonKey]);
        Assert.IsNotNull(parsed);
        Assert.AreEqual(1, parsed.Version);
        Assert.AreEqual("Recap.", parsed.Summary);
        Assert.AreEqual(1, parsed.Characters.Count);
        Assert.AreEqual("c1", parsed.Characters[0].Id);
        Assert.AreEqual("Ava", parsed.Characters[0].DisplayName);
        Assert.AreEqual(0.9, parsed.Characters[0].Provenance.Confidence, 0.0001);
    }

    [TestMethod]
    public async Task CharacterTrackerTransform_OnParseFailure_LeavesStoryStateUnchanged()
    {
        var service = new FakeTextGenerationService(_ => new TextGenerationResponse { GeneratedText = "not json" });
        var transform = new CharacterTrackerTransform(service, NullLogger<CharacterTrackerTransform>.Instance);

        var initial = StoryState.Empty("s1") with { Summary = "Recap." };
        var metadata = StoryStateAnnotations.Write(PipelineChunkMetadata.Empty, initial);

        var (_, _, annotations) = await LlmPipelineHarness.RunAsync(
            [new TextChunk("Text.", metadata)],
            [transform]);

        var parsed = StoryStateJson.TryDeserialize(annotations[StoryStateAnnotations.StoryStateJsonKey]);
        Assert.IsNotNull(parsed);
        Assert.AreEqual("Recap.", parsed.Summary);
        Assert.AreEqual(0, parsed.Version);
        Assert.AreEqual(0, parsed.Characters.Count);
    }
}
