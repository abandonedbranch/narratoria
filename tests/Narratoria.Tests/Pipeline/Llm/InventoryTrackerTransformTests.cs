using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Pipeline;
using Narratoria.Pipeline.Transforms.Llm;
using Narratoria.Pipeline.Transforms.Llm.Providers;
using Narratoria.Pipeline.Transforms.Llm.StoryState;

namespace Narratoria.Tests.Pipeline.Llm;

[TestClass]
public sealed class InventoryTrackerTransformTests
{
    [TestMethod]
    public async Task InventoryTrackerTransform_AddsAndRemovesItem()
    {
        var addUpdate = """
        {
          "summary": null,
          "charactersToUpsert": null,
          "inventoryUpdates": [
            {
              "operation": "upsert",
              "item": {
                "id": "i1",
                "displayName": "Rusty Key",
                "quantity": 1,
                "notes": "Opens the cellar door.",
                "provenance": {
                  "transformName": "InventoryTrackerTransform",
                  "confidence": 0.95,
                  "sourceSnippet": "You pick up a rusty key.",
                  "chunkIndex": 1
                }
              }
            }
          ]
        }
        """;

        var removeUpdate = """
        {
          "summary": null,
          "charactersToUpsert": null,
          "inventoryUpdates": [
            {
              "operation": "remove",
              "item": {
                "id": "i1",
                "displayName": "Rusty Key",
                "quantity": 1,
                "notes": null,
                "provenance": {
                  "transformName": "InventoryTrackerTransform",
                  "confidence": 1.0,
                  "sourceSnippet": "The key breaks.",
                  "chunkIndex": 2
                }
              }
            }
          ]
        }
        """;

        var calls = 0;
        var service = new FakeTextGenerationService(_ =>
        {
            calls++;
            return new TextGenerationResponse { GeneratedText = calls == 1 ? addUpdate : removeUpdate };
        });

        var transform = new InventoryTrackerTransform(service, NullLogger<InventoryTrackerTransform>.Instance);

        var initial = StoryState.Empty("s1");
        var metadata = StoryStateAnnotations.Write(PipelineChunkMetadata.Empty, initial);

        var (_, _, annotationsAfterAdd) = await LlmPipelineHarness.RunAsync(
            [new TextChunk("Text.", metadata)],
            [transform]);

        var parsedAfterAdd = StoryStateJson.TryDeserialize(annotationsAfterAdd[StoryStateAnnotations.StoryStateJsonKey]);
        Assert.IsNotNull(parsedAfterAdd);
        Assert.AreEqual(1, parsedAfterAdd.Version);
        Assert.AreEqual(1, parsedAfterAdd.Inventory.Items.Count);
        Assert.AreEqual("i1", parsedAfterAdd.Inventory.Items[0].Id);

        var metadata2 = StoryStateAnnotations.Write(PipelineChunkMetadata.Empty, parsedAfterAdd);

        var (_, _, annotationsAfterRemove) = await LlmPipelineHarness.RunAsync(
            [new TextChunk("Text.", metadata2)],
            [transform]);

        var parsedAfterRemove = StoryStateJson.TryDeserialize(annotationsAfterRemove[StoryStateAnnotations.StoryStateJsonKey]);
        Assert.IsNotNull(parsedAfterRemove);
        Assert.AreEqual(2, parsedAfterRemove.Version);
        Assert.AreEqual(0, parsedAfterRemove.Inventory.Items.Count);
    }
}
