using Microsoft.Extensions.Logging.Abstractions;
using NarratoriaClient.Services;
using NarratoriaClient.Services.Pipeline;

namespace NarratoriaClient.Tests;

public class NarrationPipelineTests
{
    [Fact]
    public async Task RunsStagesInOrderAndEmitsLifecycleEvents()
    {
        var callOrder = new List<string>();
        var stages = new INarrationPipelineStage[]
        {
            new TestStage("first", 0, async (context, token) =>
            {
                callOrder.Add("first");
                await Task.Delay(10, token);
            }),
            new TestStage("second", 1, (context, token) =>
            {
                callOrder.Add("second");
                return Task.CompletedTask;
            })
        };

        var pipeline = new NarrationPipeline(stages, NullLogger<NarrationPipeline>.Instance);
        var execution = pipeline.Execute(new NarrationPipelineContext("Hello narrator"));

        var events = new List<NarrationLifecycleEvent>();
        await foreach (var evt in execution.ReadEventsAsync())
        {
            events.Add(evt);
        }

        await execution.Completion;

        Assert.Equal(new[] { "first", "second" }, callOrder);
        Assert.Equal(5, events.Count); // 2 stages * 2 events + pipeline completion
        Assert.Equal(NarrationLifecycleEventKind.StageStarting, events[0].Kind);
        Assert.Equal("first", events[0].Stage);
        Assert.Equal(NarrationLifecycleEventKind.StageCompleted, events[1].Kind);
        Assert.Equal("first", events[1].Stage);
        Assert.Equal(NarrationLifecycleEventKind.StageStarting, events[2].Kind);
        Assert.Equal("second", events[2].Stage);
        Assert.Equal(NarrationLifecycleEventKind.StageCompleted, events[3].Kind);
        Assert.Equal("second", events[3].Stage);
        Assert.Equal("pipeline", events.Last().Stage);
        Assert.Equal(NarrationLifecycleEventKind.PipelineCompleted, events.Last().Kind);
    }

    [Fact]
    public async Task PropagatesStageFailuresAndEmitsFailedEvents()
    {
        var stages = new INarrationPipelineStage[]
        {
            new TestStage("first", 0, (context, token) => Task.CompletedTask),
            new TestStage("second", 1, (context, token) => throw new InvalidOperationException("boom")),
            new TestStage("third", 2, (context, token) => Task.CompletedTask)
        };

        var pipeline = new NarrationPipeline(stages, NullLogger<NarrationPipeline>.Instance);
        var execution = pipeline.Execute(new NarrationPipelineContext("hi"));

        var events = new List<NarrationLifecycleEvent>();
        await foreach (var evt in execution.ReadEventsAsync())
        {
            events.Add(evt);
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => execution.Completion);

        Assert.Contains(events, e => e.Stage == "second" && e.Kind == NarrationLifecycleEventKind.StageFailed);
        var terminal = events.Last();
        Assert.Equal("pipeline", terminal.Stage);
        Assert.Equal(NarrationLifecycleEventKind.PipelineFailed, terminal.Kind);
    }

    [Fact]
    public async Task UnknownCommandFailsPipeline()
    {
        var stages = new INarrationPipelineStage[]
        {
            new TestStage("slash-command", 0, (context, token) =>
            {
                context.IsCommand = true;
                context.CommandName = "nope";
                return Task.CompletedTask;
            }),
            new CommandHandlerStage(new TestLogBuffer())
        };

        var pipeline = new NarrationPipeline(stages, NullLogger<NarrationPipeline>.Instance);
        var execution = pipeline.Execute(new NarrationPipelineContext("/nope"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => execution.Completion);
    }

    [Fact]
    public async Task CancellationStopsPipeline()
    {
        var stages = new INarrationPipelineStage[]
        {
            new TestStage("slow", 0, async (context, token) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5), token);
            }),
            new TestStage("never", 1, (context, token) => Task.CompletedTask)
        };

        var pipeline = new NarrationPipeline(stages, NullLogger<NarrationPipeline>.Instance);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var execution = pipeline.Execute(new NarrationPipelineContext("cancel me"), cts.Token);

        var events = new List<NarrationLifecycleEvent>();
        await foreach (var evt in execution.ReadEventsAsync())
        {
            events.Add(evt);
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution.Completion);

        Assert.Contains(events, e => e.Stage == "slow" && e.Kind == NarrationLifecycleEventKind.StageFailed);
        Assert.Equal(NarrationLifecycleEventKind.PipelineFailed, events.Last().Kind);
    }

    [Fact]
    public async Task ShortCircuitsWhenContextDisablesContinuation()
    {
        var stages = new INarrationPipelineStage[]
        {
            new TestStage("first", 0, (context, token) =>
            {
                context.ShouldContinue = false;
                return Task.CompletedTask;
            }),
            new TestStage("second", 1, (context, token) =>
            {
                throw new InvalidOperationException("Should not run");
            })
        };

        var pipeline = new NarrationPipeline(stages, NullLogger<NarrationPipeline>.Instance);
        var execution = pipeline.Execute(new NarrationPipelineContext("hello"));

        var events = new List<NarrationLifecycleEvent>();
        await foreach (var evt in execution.ReadEventsAsync())
        {
            events.Add(evt);
        }

        await execution.Completion;

        Assert.Equal(3, events.Count); // stage start/complete + pipeline complete
        Assert.Equal(NarrationLifecycleEventKind.PipelineCompleted, events.Last().Kind);
        Assert.Equal("short-circuited", events.Last().Metadata?["reason"]);
    }

    private sealed class TestStage : INarrationPipelineStage
    {
        private readonly Func<NarrationPipelineContext, CancellationToken, Task> _callback;

        public TestStage(string stageName, int order, Func<NarrationPipelineContext, CancellationToken, Task> callback)
        {
            StageName = stageName;
            Order = order;
            _callback = callback;
        }

        public string StageName { get; }

        public int Order { get; }

        public Task ExecuteAsync(NarrationPipelineContext context, CancellationToken cancellationToken)
        {
            return _callback(context, cancellationToken);
        }
    }
}
