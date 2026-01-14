using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Pipeline.Transforms.Llm.StoryState;

namespace Narratoria.Tests.Pipeline.Llm;

[TestClass]
public sealed class TrackerOutputParsingTests
{
    [TestMethod]
    public void StoryStateUpdateJson_ParsesInventoryOperationEnums()
    {
        var json = """
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
                  "confidence": 1.0
                }
              }
            }
          ]
        }
        """;

        var ok = StoryStateUpdateJson.TryDeserialize(json, out var update);

        Assert.IsTrue(ok);
        Assert.IsNotNull(update);
        Assert.IsNotNull(update.InventoryUpdates);
        Assert.AreEqual(1, update.InventoryUpdates.Count);
        Assert.AreEqual(InventoryItemOperation.Remove, update.InventoryUpdates[0].Operation);
        Assert.AreEqual("i1", update.InventoryUpdates[0].Item.Id);
    }

    [TestMethod]
    public void StoryStateUpdateJson_WhenNotJson_ReturnsFalse()
    {
        var ok = StoryStateUpdateJson.TryDeserialize("nope", out var update);
        Assert.IsFalse(ok);
        Assert.IsNull(update);
    }
}
