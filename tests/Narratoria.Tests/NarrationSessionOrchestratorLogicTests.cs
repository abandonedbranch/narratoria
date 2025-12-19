using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Components;
using System.Collections.Immutable;

namespace Narratoria.Tests;

[TestClass]
public class NarrationSessionOrchestratorLogicTests
{
    private static readonly ImmutableArray<NarrationStageKind> DefaultOrder = ImmutableArray.Create(
        NarrationStageKind.Custom("session_load"),
        NarrationStageKind.Custom("system_prompt_injection"),
        NarrationStageKind.Custom("content_guardian_injection"),
        NarrationStageKind.Custom("attachment_ingestion"),
        NarrationStageKind.Custom("provider_dispatch"),
        NarrationStageKind.Custom("persist_context"));

    [TestMethod]
    public void CreateNewTurn_SetsFirstStageRunning_AndSkipsAttachmentWhenNone()
    {
        var turn = NarrationSessionOrchestratorLogic.CreateNewTurn("Hello", DefaultOrder, expectedAttachmentCount: 0);
        Assert.AreEqual(true, turn.Output.IsStreaming);
        Assert.AreEqual(DefaultOrder.Length, turn.Stages.Length);

        Assert.AreEqual(NarrationStageStatus.Running, turn.Stages[0].Status);

        for (var i = 1; i < turn.Stages.Length; i++)
        {
            if (turn.Stages[i].Kind.Name == "attachment_ingestion") continue;
            Assert.AreEqual(NarrationStageStatus.Pending, turn.Stages[i].Status);
        }

        var attachment = turn.Stages.First(s => s.Kind.Name == "attachment_ingestion");
        Assert.AreEqual(NarrationStageStatus.Skipped, attachment.Status);
    }

    [TestMethod]
    public void CreateNewTurn_WithExpectedAttachments_DoesNotSkipAttachmentStage()
    {
        var turn = NarrationSessionOrchestratorLogic.CreateNewTurn("Hello", DefaultOrder, expectedAttachmentCount: 2);

        Assert.AreEqual(NarrationStageStatus.Running, turn.Stages[0].Status);
        var attachment = turn.Stages.First(s => s.Kind.Name == "attachment_ingestion");
        Assert.AreEqual(NarrationStageStatus.Pending, attachment.Status);
    }

    [TestMethod]
    public void ApplyStreamSegment_AppendsText()
    {
        var turn = NarrationSessionOrchestratorLogic.CreateNewTurn("Hi", DefaultOrder, expectedAttachmentCount: 0);
        turn = NarrationSessionOrchestratorLogic.ApplyStreamSegment(turn, "A");
        turn = NarrationSessionOrchestratorLogic.ApplyStreamSegment(turn, "B");
        Assert.AreEqual(2, turn.Output.StreamedSegments.Length);
        Assert.AreEqual("A", turn.Output.StreamedSegments[0]);
        Assert.AreEqual("B", turn.Output.StreamedSegments[1]);
    }

    [TestMethod]
    public void FinalizeTurn_SetsFinalText_WithoutOverridingStageStatuses()
    {
        var turn = NarrationSessionOrchestratorLogic.CreateNewTurn("Test", DefaultOrder, expectedAttachmentCount: 0);
        var finalized = NarrationSessionOrchestratorLogic.FinalizeTurn(turn, new[] { "X", "Y" });
        Assert.AreEqual(false, finalized.Output.IsStreaming);
        Assert.AreEqual("XY", finalized.Output.FinalText);
        Assert.AreEqual(turn.Stages, finalized.Stages);
    }
}
