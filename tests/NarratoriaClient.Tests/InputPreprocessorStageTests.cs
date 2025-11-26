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
        Assert.True(context.IsSystemCommand);
    }

    [Fact]
    public async Task MarksNonCommandInput()
    {
        var stage = new InputPreprocessorStage(NullLogger<InputPreprocessorStage>.Instance);
        var context = new NarrationPipelineContext("Walk north");

        await stage.ExecuteAsync(context, CancellationToken.None);

        Assert.Equal("Walk north", context.NormalizedInput);
        Assert.False(context.IsSystemCommand);
    }
}
