using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Narration;
using Narratoria.OpenAi;

namespace Narratoria.Tests;

[TestClass]
public sealed class NarrationContentGuardianMiddlewareTests
{
    [TestMethod]
    public async Task InjectsSystemPromptAndMetadata()
    {
        var observer = new RecordingObserver();
        var middleware = new NarrationContentGuardianMiddleware(observer);
        var context = new NarrationContext
        {
            SessionId = Guid.NewGuid(),
            PlayerPrompt = "Continue the story.",
            PriorNarration = ImmutableArray<string>.Empty,
            WorkingNarration = ImmutableArray<string>.Empty,
            Metadata = ImmutableDictionary<string, string>.Empty,
            WorkingContextSegments = ImmutableArray<ContextSegment>.Empty,
            Trace = new TraceMetadata("trace", "request")
        };

        NarrationContext? received = null;
        var result = MiddlewareResult.FromContext(context);
        await middleware.InvokeAsync(context, result, CaptureContext, CancellationToken.None);

        Assert.IsNotNull(received);
        Assert.IsTrue(received!.WorkingContextSegments.Length >= 2);
        StringAssert.StartsWith(received.WorkingContextSegments[0].Content, "You are the Narratoria Content Guardian");
        Assert.AreEqual("content_guardian_middleware", received.WorkingContextSegments[0].Source);
        Assert.AreEqual("system", received.WorkingContextSegments[0].Role);
        Assert.AreEqual("player_prompt", received.WorkingContextSegments[1].Source);
        Assert.AreEqual("true", received.Metadata["content_guardian_applied"]);
        Assert.AreEqual("success", observer.StageTelemetries.Single().Status);
        Assert.AreEqual("content_guardian_injection", observer.StageTelemetries.Single().Stage);

        ValueTask<MiddlewareResult> CaptureContext(NarrationContext updated, MiddlewareResult downstream, CancellationToken ct)
        {
            received = updated;
            return ValueTask.FromResult(downstream);
        }
    }

    [TestMethod]
    public async Task SkipsWhenAlreadyApplied()
    {
        var observer = new RecordingObserver();
        var middleware = new NarrationContentGuardianMiddleware(observer);
        var metadata = ImmutableDictionary<string, string>.Empty.Add("content_guardian_applied", "true");
        var segments = ImmutableArray.Create(new ContextSegment("system", "existing", "existing"));

        var context = new NarrationContext
        {
            SessionId = Guid.NewGuid(),
            PlayerPrompt = "prompt",
            PriorNarration = [],
            WorkingNarration = [],
            Metadata = metadata,
            WorkingContextSegments = segments,
            Trace = new TraceMetadata("trace", "request")
        };

        var result = MiddlewareResult.FromContext(context);
        var downstream = await middleware.InvokeAsync(context, result, (ctx, res, ct) => ValueTask.FromResult(res), CancellationToken.None);

        Assert.AreSame(result.StreamedNarration, downstream.StreamedNarration);
        Assert.AreSame(context, await downstream.UpdatedContext);
        Assert.AreEqual("skipped", observer.StageTelemetries.Single().Status);
        Assert.AreEqual("content_guardian_injection", observer.StageTelemetries.Single().Stage);
    }

    private sealed class RecordingObserver : INarrationPipelineObserver
    {
        private readonly List<NarrationStageTelemetry> _telemetries = new();

        public IReadOnlyList<NarrationStageTelemetry> StageTelemetries => _telemetries;

        public void OnError(NarrationPipelineError error)
        {
        }

        public void OnStageCompleted(NarrationStageTelemetry telemetry) => _telemetries.Add(telemetry);

        public void OnTokensStreamed(Guid sessionId, int tokenCount)
        {
        }
    }
}
