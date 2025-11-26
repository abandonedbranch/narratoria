using Microsoft.Extensions.Logging.Abstractions;
using NarratoriaClient.Services.Pipeline;

namespace NarratoriaClient.Tests;

public class InputPreprocessorStageTests
{
    [Fact]
    public async Task SetsNormalizedInputAndDetectsCommands()
    {
        var stage = new InputPreprocessorStage(NullLogger<InputPreprocessorStage>.Instance);
        var context = new NarrationPipelineContext("  @help please  ");

        await stage.ExecuteAsync(context, CancellationToken.None);

        Assert.Equal("@help please", context.NormalizedInput);
        Assert.False(context.IsCommand);
    }

    [Fact]
    public async Task MarksNonCommandInput()
    {
        var stage = new InputPreprocessorStage(NullLogger<InputPreprocessorStage>.Instance);
        var context = new NarrationPipelineContext("Walk north");

        await stage.ExecuteAsync(context, CancellationToken.None);

        Assert.Equal("Walk north", context.NormalizedInput);
        Assert.False(context.IsCommand);
    }

    [Fact]
    public async Task RoutesToSystemWorkflow()
    {
        var stage = new InputPreprocessorStage(NullLogger<InputPreprocessorStage>.Instance);
        var context = new NarrationPipelineContext("@system Maintain order");

        await stage.ExecuteAsync(context, CancellationToken.None);

        Assert.Equal("system", context.TargetWorkflow);
        Assert.Equal("Maintain order", context.NormalizedInput);
        Assert.False(context.IsCommand);
    }

    [Fact]
    public async Task RoutesToNarratorByDefaultAndAllowsNarratorToken()
    {
        var stage = new InputPreprocessorStage(NullLogger<InputPreprocessorStage>.Instance);

        var withToken = new NarrationPipelineContext("@narrator Tell me a tale");
        await stage.ExecuteAsync(withToken, CancellationToken.None);
        Assert.Equal("narrator", withToken.TargetWorkflow);
        Assert.Equal("Tell me a tale", withToken.NormalizedInput);

        var withoutToken = new NarrationPipelineContext("Tell me a tale");
        await stage.ExecuteAsync(withoutToken, CancellationToken.None);
        Assert.Equal("narrator", withoutToken.TargetWorkflow);
    }

    [Fact]
    public async Task DetectsSlashCommandWithArgs()
    {
        var stage = new InputPreprocessorStage(NullLogger<InputPreprocessorStage>.Instance);
        var context = new NarrationPipelineContext("/help summon torch");

        await stage.ExecuteAsync(context, CancellationToken.None);

        Assert.True(context.IsCommand);
        Assert.Equal("help", context.CommandName);
        Assert.Equal("summon torch", context.CommandArgs);
    }

    [Fact]
    public async Task SlashFollowedBySpaceIsLiteral()
    {
        var stage = new InputPreprocessorStage(NullLogger<InputPreprocessorStage>.Instance);
        var context = new NarrationPipelineContext("/ help me");

        await stage.ExecuteAsync(context, CancellationToken.None);

        Assert.False(context.IsCommand);
        Assert.Equal("/ help me", context.NormalizedInput);
    }
}
