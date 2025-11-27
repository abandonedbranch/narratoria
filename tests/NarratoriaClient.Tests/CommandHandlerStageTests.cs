using Microsoft.Extensions.Logging.Abstractions;
using NarratoriaClient.Components;
using NarratoriaClient.Services;
using NarratoriaClient.Services.Pipeline;
using Microsoft.Extensions.Logging;

namespace NarratoriaClient.Tests;

public class CommandHandlerStageTests
{
    [Fact]
    public async Task ThrowsOnUnknownCommand()
    {
        var stage = new CommandHandlerStage(new TestLogBuffer(), new TransientCommandLog(), new CommandEventBus());
        var context = new NarrationPipelineContext("/doesnotexist");
        context.IsCommand = true;
        context.CommandName = "doesnotexist";
        context.CommandArgs = null;

        await Assert.ThrowsAsync<InvalidOperationException>(() => stage.ExecuteAsync(context, CancellationToken.None));
    }

    [Fact]
    public async Task ShortCircuitsOnKnownCommand()
    {
        var stage = new CommandHandlerStage(new TestLogBuffer(), new TransientCommandLog(), new CommandEventBus());
        var context = new NarrationPipelineContext("/help");
        context.IsCommand = true;
        context.CommandName = "help";

        await stage.ExecuteAsync(context, CancellationToken.None);

        Assert.False(context.ShouldContinue);
    }

    [Fact]
    public async Task PublishesCommandEvent()
    {
        var bus = new CommandEventBus();
        CommandEvent? received = null;
        bus.CommandReceived += (_, e) => received = e;

        var stage = new CommandHandlerStage(new TestLogBuffer(), new TransientCommandLog(), bus);
        var context = new NarrationPipelineContext("/help");
        context.IsCommand = true;
        context.CommandName = "help";
        context.CommandArgs = "abc";
        context.ActiveSessionId = "session-1";

        await stage.ExecuteAsync(context, CancellationToken.None);

        Assert.NotNull(received);
        Assert.Equal("help", received!.Token);
        Assert.Equal("abc", received.Args);
        Assert.Equal("session-1", received.SessionId);
    }
}

internal sealed class TestLogBuffer : ILogBuffer
{
    public event EventHandler? EntriesChanged;
    public IReadOnlyList<LogEntry> GetEntries() => Array.Empty<LogEntry>();
    public void Log(string category, LogLevel level, string message, IReadOnlyDictionary<string, object?>? metadata = null) { }
}
