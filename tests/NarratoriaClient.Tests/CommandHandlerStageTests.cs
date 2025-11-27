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
}

internal sealed class TestLogBuffer : ILogBuffer
{
    public event EventHandler? EntriesChanged;
    public IReadOnlyList<LogEntry> GetEntries() => Array.Empty<LogEntry>();
    public void Log(string category, LogLevel level, string message, IReadOnlyDictionary<string, object?>? metadata = null) { }
}
