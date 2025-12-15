using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Narration;

namespace Narratoria.Tests;

[TestClass]
public sealed class ConfigSystemPromptProfileResolverTests
{
    [TestMethod]
    public async Task ReturnsProfileWhenConfigured()
    {
        var cfg = new SystemPromptProfileConfig
        {
            ProfileId = "default",
            PromptText = "system text",
            Instructions = new[] { "ins1", "", "ins2" },
            Version = "v1"
        };
        var sut = new ConfigSystemPromptProfileResolver(Options.Create(cfg), NullLogger<ConfigSystemPromptProfileResolver>.Instance);
        var result = await sut.ResolveAsync(Guid.NewGuid(), CancellationToken.None);
        Assert.IsNotNull(result);
        Assert.AreEqual("default", result!.ProfileId);
        Assert.AreEqual("v1", result.Version);
        CollectionAssert.AreEqual(new[] { "ins1", "ins2" }, result.Instructions.ToArray());
    }

    [TestMethod]
    public async Task ReturnsNullWhenPromptMissing()
    {
        var cfg = new SystemPromptProfileConfig
        {
            ProfileId = "default",
            PromptText = "",
            Instructions = Array.Empty<string>(),
            Version = "v1"
        };
        var sut = new ConfigSystemPromptProfileResolver(Options.Create(cfg), NullLogger<ConfigSystemPromptProfileResolver>.Instance);
        var result = await sut.ResolveAsync(Guid.NewGuid(), CancellationToken.None);
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task ThrowsOnCancellation()
    {
        var cfg = new SystemPromptProfileConfig
        {
            ProfileId = "default",
            PromptText = "system",
            Instructions = Array.Empty<string>(),
            Version = "v1"
        };
        var sut = new ConfigSystemPromptProfileResolver(Options.Create(cfg), NullLogger<ConfigSystemPromptProfileResolver>.Instance);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () => await sut.ResolveAsync(Guid.NewGuid(), cts.Token));
    }
}
