using System.Collections.Immutable;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Components;

namespace Narratoria.Tests;

[TestClass]
public sealed class NarrationPipelineLogLogicTests
{
    [TestMethod]
    public void IsStageOrderValid_RejectsEmptyAndDuplicates()
    {
        Assert.IsFalse(NarrationPipelineLogLogic.IsStageOrderValid(Array.Empty<NarrationStageKind>()));
        var dup = new[] { NarrationStageKind.Sanitize, NarrationStageKind.Sanitize };
        Assert.IsFalse(NarrationPipelineLogLogic.IsStageOrderValid(dup));
        var ok = new[] { NarrationStageKind.Sanitize, NarrationStageKind.Context, NarrationStageKind.Llm };
        Assert.IsTrue(NarrationPipelineLogLogic.IsStageOrderValid(ok));
    }

    [TestMethod]
    public void AreTurnsAligned_EnsuresOneStagePerOrderKind()
    {
        var order = new[] { NarrationStageKind.Sanitize, NarrationStageKind.Context };
        var goodTurn = new NarrationPipelineTurnView
        {
            TurnId = Guid.NewGuid(),
            UserPrompt = "p",
            Stages = ImmutableArray.Create(
                new NarrationStageView { Kind = NarrationStageKind.Sanitize, Status = NarrationStageStatus.Completed },
                new NarrationStageView { Kind = NarrationStageKind.Context, Status = NarrationStageStatus.Pending }
            ),
            Output = new NarrationOutputView()
        };
        Assert.IsTrue(NarrationPipelineLogLogic.AreTurnsAligned(order, new[] { goodTurn }));

        var badTurnMissing = goodTurn with { Stages = ImmutableArray.Create(new NarrationStageView { Kind = NarrationStageKind.Sanitize, Status = NarrationStageStatus.Completed }) };
        Assert.IsFalse(NarrationPipelineLogLogic.AreTurnsAligned(order, new[] { badTurnMissing }));

        var badTurnWrong = goodTurn with { Stages = ImmutableArray.Create(
            new NarrationStageView { Kind = NarrationStageKind.Sanitize, Status = NarrationStageStatus.Completed },
            new NarrationStageView { Kind = NarrationStageKind.Llm, Status = NarrationStageStatus.Completed }
        )};
        Assert.IsFalse(NarrationPipelineLogLogic.AreTurnsAligned(order, new[] { badTurnWrong }));
    }

    [TestMethod]
    public void HasStreamMismatch_DetectsNonLatestStreamingOrRunning()
    {
        var t1 = new NarrationPipelineTurnView
        {
            TurnId = Guid.NewGuid(),
            UserPrompt = "p1",
            Stages = ImmutableArray.Create(new NarrationStageView { Kind = NarrationStageKind.Sanitize, Status = NarrationStageStatus.Running }),
            Output = new NarrationOutputView { IsStreaming = true }
        };
        var t2 = new NarrationPipelineTurnView
        {
            TurnId = Guid.NewGuid(),
            UserPrompt = "p2",
            Stages = ImmutableArray.Create(new NarrationStageView { Kind = NarrationStageKind.Sanitize, Status = NarrationStageStatus.Pending }),
            Output = new NarrationOutputView { IsStreaming = false }
        };
        Assert.IsTrue(NarrationPipelineLogLogic.HasStreamMismatch(new[] { t1, t2 }));
        Assert.IsFalse(NarrationPipelineLogLogic.HasStreamMismatch(new[] { t2 }));
    }

    [TestMethod]
    public void RenderOutput_ConcatsSegmentsAndShowsEllipsisWhenStreaming()
    {
        var o1 = new NarrationOutputView { IsStreaming = false, FinalText = "done" };
        Assert.AreEqual("done", NarrationPipelineLogLogic.RenderOutput(o1));
        var o2 = new NarrationOutputView { IsStreaming = true, StreamedSegments = ImmutableArray.Create("Hello", " world") };
        StringAssert.Contains(NarrationPipelineLogLogic.RenderOutput(o2), "Hello world");
        StringAssert.Contains(NarrationPipelineLogLogic.RenderOutput(o2), "...");
        var o3 = new NarrationOutputView { IsStreaming = true };
        Assert.AreEqual("...", NarrationPipelineLogLogic.RenderOutput(o3));
        var o4 = new NarrationOutputView { IsStreaming = false };
        Assert.AreEqual(string.Empty, NarrationPipelineLogLogic.RenderOutput(o4));
    }
}
