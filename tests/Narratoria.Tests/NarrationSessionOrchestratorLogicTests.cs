using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Components;
using System.Collections.Immutable;

namespace Narratoria.Tests;

[TestClass]
public class NarrationSessionOrchestratorLogicTests
{
    private static readonly ImmutableArray<NarrationStageKind> DefaultOrder = ImmutableArray.Create(
        NarrationStageKind.Sanitize,
        NarrationStageKind.Context,
        NarrationStageKind.Lore,
        NarrationStageKind.Llm);

    [TestMethod]
    public void CreateNewTurn_SetsRunningForLlm()
    {
        var turn = NarrationSessionOrchestratorLogic.CreateNewTurn("Hello", DefaultOrder);
        Assert.AreEqual(true, turn.Output.IsStreaming);
        var llm = turn.Stages.First(s => s.Kind == NarrationStageKind.Llm);
        Assert.AreEqual(NarrationStageStatus.Running, llm.Status);
    }

    [TestMethod]
    public void ApplyStreamSegment_AppendsText()
    {
        var turn = NarrationSessionOrchestratorLogic.CreateNewTurn("Hi", DefaultOrder);
        turn = NarrationSessionOrchestratorLogic.ApplyStreamSegment(turn, "A");
        turn = NarrationSessionOrchestratorLogic.ApplyStreamSegment(turn, "B");
        Assert.AreEqual(2, turn.Output.StreamedSegments.Length);
        Assert.AreEqual("A", turn.Output.StreamedSegments[0]);
        Assert.AreEqual("B", turn.Output.StreamedSegments[1]);
    }

    [TestMethod]
    public void FinalizeTurn_CompletesStagesAndSetsFinalText()
    {
        var turn = NarrationSessionOrchestratorLogic.CreateNewTurn("Test", DefaultOrder);
        var finalized = NarrationSessionOrchestratorLogic.FinalizeTurn(turn, new[] { "X", "Y" }, DefaultOrder);
        Assert.AreEqual(false, finalized.Output.IsStreaming);
        Assert.AreEqual("XY", finalized.Output.FinalText);
        Assert.IsTrue(finalized.Stages.All(s => s.Status == NarrationStageStatus.Completed));
    }
}
