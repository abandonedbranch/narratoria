using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Components;
using Narratoria.Narration;
using System.Collections.Immutable;

namespace Narratoria.Tests;

[TestClass]
public class PipelineObserverViewAdapterTests
{
    private static readonly ImmutableArray<NarrationStageKind> Order = ImmutableArray.Create(
        NarrationStageKind.Sanitize,
        NarrationStageKind.Context,
        NarrationStageKind.Lore,
        NarrationStageKind.Llm);

    [TestMethod]
    public void OnStageCompleted_SetsStatusFromTelemetry()
    {
        var turn = new NarrationPipelineTurnView
        {
            TurnId = Guid.NewGuid(),
            UserPrompt = "Test",
            Stages = Order.Select(k => new NarrationStageView { Kind = k, Status = NarrationStageStatus.Running }).ToImmutableArray(),
            Output = new NarrationOutputView { IsStreaming = true }
        };

        var current = turn;
        var adapter = new PipelineObserverViewAdapter(Order, _ => current, t => current = t);

        var telemetry = new NarrationStageTelemetry("Llm", "success", "none", Guid.NewGuid(), new Narratoria.OpenAi.TraceMetadata("t","r"), TimeSpan.FromMilliseconds(10));
        adapter.OnStageCompleted(telemetry);

        var llm = current.Stages.First(s => s.Kind == NarrationStageKind.Llm);
        Assert.AreEqual(NarrationStageStatus.Completed, llm.Status);
    }

    [TestMethod]
    public void OnError_MarksStageFailed()
    {
        var turn = new NarrationPipelineTurnView
        {
            TurnId = Guid.NewGuid(),
            UserPrompt = "Test",
            Stages = Order.Select(k => new NarrationStageView { Kind = k, Status = NarrationStageStatus.Running }).ToImmutableArray(),
            Output = new NarrationOutputView { IsStreaming = true }
        };

        var current = turn;
        var adapter = new PipelineObserverViewAdapter(Order, _ => current, t => current = t);

        var error = new NarrationPipelineError(NarrationPipelineErrorClass.ProviderError, "boom", Guid.NewGuid(), new Narratoria.OpenAi.TraceMetadata("t","r"), "Llm");
        adapter.OnError(error);

        var llm = current.Stages.First(s => s.Kind == NarrationStageKind.Llm);
        Assert.AreEqual(NarrationStageStatus.Failed, llm.Status);
        Assert.AreEqual(NarrationPipelineErrorClass.ProviderError.ToString(), llm.ErrorClass);
    }

    [TestMethod]
    public void OnTokensStreamed_SetsOutputStreaming()
    {
        var turn = new NarrationPipelineTurnView
        {
            TurnId = Guid.NewGuid(),
            UserPrompt = "Test",
            Stages = Order.Select(k => new NarrationStageView { Kind = k, Status = NarrationStageStatus.Pending }).ToImmutableArray(),
            Output = new NarrationOutputView { IsStreaming = false }
        };

        var current = turn;
        var adapter = new PipelineObserverViewAdapter(Order, _ => current, t => current = t);

        adapter.OnTokensStreamed(Guid.NewGuid(), 1);
        Assert.IsTrue(current.Output.IsStreaming);
    }
}
