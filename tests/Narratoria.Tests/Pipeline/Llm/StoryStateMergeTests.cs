using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Pipeline.Transforms.Llm.StoryState;

namespace Narratoria.Tests.Pipeline.Llm;

[TestClass]
public sealed class StoryStateMergeTests
{
    [TestMethod]
    public void ApplyUpdate_IncrementsVersion_AndIsNonDestructive()
    {
        var existing = new StoryState
        {
            SessionId = "s1",
            Version = 0,
            Characters =
            [
                new CharacterRecord
                {
                    Id = "c1",
                    DisplayName = "Ada",
                    Traits = new Dictionary<string, string> { ["role"] = "wizard" },
                    Provenance = new TransformProvenance { TransformName = "seed", Confidence = 0.9 },
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
                        Provenance = new TransformProvenance { TransformName = "seed", Confidence = 0.9 },
                    },
                ],
                Provenance = new TransformProvenance { TransformName = "seed", Confidence = 1.0 },
            },
        };

        var lowConfidenceUpdate = new StoryStateUpdate
        {
            CharactersToUpsert =
            [
                new CharacterRecord
                {
                    Id = "c1",
                    DisplayName = "Ada Lovelace",
                    Traits = new Dictionary<string, string> { ["role"] = "thief", ["hair"] = "black" },
                    Provenance = new TransformProvenance { TransformName = "llm", Confidence = 0.2 },
                },
            ],
        };

        var merged = StoryStateMerge.ApplyUpdate(existing, lowConfidenceUpdate, "llm", DateTimeOffset.Parse("2026-01-01T00:00:00Z"));

        Assert.AreEqual(1, merged.Version);
        Assert.AreEqual("Ada", merged.Characters[0].DisplayName, "Low-confidence update should not overwrite existing displayName.");
        Assert.AreEqual("wizard", merged.Characters[0].Traits!["role"], "Low-confidence update should not overwrite existing traits.");
        Assert.AreEqual("black", merged.Characters[0].Traits!["hair"], "Low-confidence update should still merge in new non-conflicting traits.");

        var removeInventoryUpdate = new StoryStateUpdate
        {
            InventoryUpdates =
            [
                new InventoryItemUpdate
                {
                    Operation = InventoryItemOperation.Remove,
                    Item = new InventoryItem
                    {
                        Id = "i1",
                        DisplayName = "Lantern",
                        Provenance = new TransformProvenance { TransformName = "llm", Confidence = 0.9 },
                    },
                },
            ],
        };

        var merged2 = StoryStateMerge.ApplyUpdate(merged, removeInventoryUpdate, "llm", DateTimeOffset.Parse("2026-01-01T00:00:01Z"));
        Assert.AreEqual(2, merged2.Version);
        Assert.AreEqual(0, merged2.Inventory.Items.Count);
    }
}
