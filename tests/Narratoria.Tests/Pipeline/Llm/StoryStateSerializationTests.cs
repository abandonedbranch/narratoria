using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Pipeline.Transforms.Llm.StoryState;

namespace Narratoria.Tests.Pipeline.Llm;

[TestClass]
public sealed class StoryStateSerializationTests
{
    [TestMethod]
    public void StoryStateJson_RoundTrips()
    {
        var state = new StoryState
        {
            SessionId = "s1",
            Version = 3,
            LastUpdated = "2026-01-01T00:00:00Z",
            Summary = "A short recap",
            Characters =
            [
                new CharacterRecord
                {
                    Id = "c1",
                    DisplayName = "Ada",
                    LastSeen = "In the library",
                    Provenance = new TransformProvenance { TransformName = "test", Confidence = 0.9, SourceSnippet = "Ada waved.", ChunkIndex = 2 },
                },
            ],
            Inventory = new InventoryState
            {
                Items =
                [
                    new InventoryItem
                    {
                        Id = "i1",
                        DisplayName = "Lantern",
                        Quantity = 1,
                        Notes = "Brass",
                        Provenance = new TransformProvenance { TransformName = "test", Confidence = 0.8, SourceSnippet = "You pick up a lantern.", ChunkIndex = 2 },
                    },
                ],
                Provenance = new TransformProvenance { TransformName = "test", Confidence = 1.0 },
            },
        };

        var json = StoryStateJson.Serialize(state);
        var parsed = StoryStateJson.TryDeserialize(json);

        Assert.IsNotNull(parsed);
        Assert.AreEqual("s1", parsed.SessionId);
        Assert.AreEqual(3, parsed.Version);
        Assert.AreEqual("A short recap", parsed.Summary);
        Assert.AreEqual(1, parsed.Characters.Count);
        Assert.AreEqual("Ada", parsed.Characters[0].DisplayName);
        Assert.AreEqual(1, parsed.Inventory.Items.Count);
        Assert.AreEqual("Lantern", parsed.Inventory.Items[0].DisplayName);
    }
}
