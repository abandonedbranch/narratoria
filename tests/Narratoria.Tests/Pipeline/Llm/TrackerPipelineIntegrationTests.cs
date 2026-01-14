using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Pipeline;
using Narratoria.Pipeline.Transforms.Llm;
using Narratoria.Pipeline.Transforms.Llm.Prompts;
using Narratoria.Pipeline.Transforms.Llm.Providers;
using Narratoria.Pipeline.Transforms.Llm.StoryState;

namespace Narratoria.Tests.Pipeline.Llm;

[TestClass]
public sealed class TrackerPipelineIntegrationTests
{
    [TestMethod]
    public async Task Trackers_SeeRewrittenTextAndLatestSummary()
    {
        const string rewritten = "REWRITTEN narration.";
        const string updatedSummary = "UPDATED recap.";

        var service = new FakeTextGenerationService(req =>
        {
            if (req.Prompt.Contains(PromptTemplates.RewriteInstructions, StringComparison.Ordinal))
            {
                return new TextGenerationResponse { GeneratedText = rewritten };
            }

            if (req.Prompt.Contains(PromptTemplates.SummaryInstructions, StringComparison.Ordinal))
            {
                return new TextGenerationResponse { GeneratedText = updatedSummary };
            }

            if (req.Prompt.Contains(PromptTemplates.CharacterExtractionInstructions, StringComparison.Ordinal))
            {
                // Assert prompt sees downstream context.
                StringAssert.Contains(req.Prompt, rewritten);
                StringAssert.Contains(req.Prompt, updatedSummary);

                return new TextGenerationResponse
                {
                    GeneratedText = """
                    {
                      "summary": null,
                      "inventoryUpdates": null,
                      "charactersToUpsert": [
                        {
                          "id": "c1",
                          "displayName": "Ava",
                          "aliases": [],
                          "traits": {},
                          "relationships": [],
                          "lastSeen": null,
                          "provenance": { "transformName": "CharacterTrackerTransform", "confidence": 0.8 }
                        }
                      ]
                    }
                    """
                };
            }

            if (req.Prompt.Contains(PromptTemplates.InventoryExtractionInstructions, StringComparison.Ordinal))
            {
                StringAssert.Contains(req.Prompt, rewritten);
                StringAssert.Contains(req.Prompt, updatedSummary);

                return new TextGenerationResponse
                {
                    GeneratedText = """
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
                            "notes": null,
                            "provenance": { "transformName": "InventoryTrackerTransform", "confidence": 0.8 }
                          }
                        }
                      ]
                    }
                    """
                };
            }

            Assert.Fail("Unexpected prompt type.");
            return new TextGenerationResponse { GeneratedText = string.Empty };
        });

        var transforms = new IPipelineTransform[]
        {
            new RewriteNarrationTransform(service, NullLogger<RewriteNarrationTransform>.Instance),
            new StorySummaryTransform(service, NullLogger<StorySummaryTransform>.Instance),
            new CharacterTrackerTransform(service, NullLogger<CharacterTrackerTransform>.Instance),
            new InventoryTrackerTransform(service, NullLogger<InventoryTrackerTransform>.Instance),
        };

        var statefulMetadata = PipelineChunkMetadata.Empty
            .WithAnnotation("narratoria.session_id", "s1")
            .WithAnnotation("narratoria.turn_index", "1");

        var (outcome, collectedText, annotations) = await LlmPipelineHarness.RunAsync(
            [new TextChunk("Original narration.", statefulMetadata)],
            transforms);

        Assert.AreEqual(PipelineOutcomeStatus.Completed, outcome.Status);
        Assert.AreEqual(rewritten, collectedText);

        var parsed = StoryStateJson.TryDeserialize(annotations[StoryStateAnnotations.StoryStateJsonKey]);
        Assert.IsNotNull(parsed);
        Assert.AreEqual(updatedSummary, parsed.Summary);
        Assert.AreEqual(1, parsed.Characters.Count);
        Assert.AreEqual(1, parsed.Inventory.Items.Count);
    }
}
