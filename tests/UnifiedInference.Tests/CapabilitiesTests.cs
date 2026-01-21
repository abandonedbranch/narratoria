using RichardSzalay.MockHttp;
using OpenAI;
using UnifiedInference.Abstractions;
using UnifiedInference.Core;
using UnifiedInference.Providers.HuggingFace;
using UnifiedInference.Providers.Ollama;
using UnifiedInference.Providers.OpenAI;

namespace UnifiedInference.Tests;

public class CapabilitiesTests
{
    [Fact]
    public async Task DefaultCapabilities_Enable_OpenAi_Text_And_Image()
    {
        var caps = new DefaultCapabilitiesProvider();
        var result = await caps.GetAsync(InferenceProvider.OpenAI, "gpt-4o-mini", CancellationToken.None);

        Assert.True(result.SupportsText);
        Assert.True(result.SupportsImage);
        Assert.True(result.Support.Temperature);
        Assert.False(result.Support.TopK);
    }

    [Fact]
    public async Task DefaultCapabilities_Enable_HuggingFace_Text_Only()
    {
        var caps = new DefaultCapabilitiesProvider();
        var result = await caps.GetAsync(InferenceProvider.HuggingFace, "any/text-model", CancellationToken.None);

        Assert.True(result.SupportsText);
        Assert.False(result.SupportsImage);
    }

    [Fact]
    public async Task UnifiedClient_Throws_When_Text_Unsupported()
    {
        var stubCaps = new StubCapabilities(ModelCapabilities.Disabled());
        var client = new UnifiedInferenceClient(
            new OpenAiInferenceClient(new OpenAIClient("dummy")),
            new OllamaInferenceClient(new HttpClient(new MockHttpMessageHandler()), "http://localhost:11434"),
            new HuggingFaceInferenceClient(new HttpClient(new MockHttpMessageHandler())),
            capabilities: stubCaps);

        var req = new TextRequest(InferenceProvider.OpenAI, "gpt-4o", "hi", new GenerationSettings());
        await Assert.ThrowsAsync<NotSupportedException>(() => client.GenerateTextAsync(req));
    }

    private sealed class StubCapabilities : ICapabilitiesProvider
    {
        private readonly ModelCapabilities _result;
        public StubCapabilities(ModelCapabilities result)
        {
            _result = result;
        }

        public Task<ModelCapabilities> GetAsync(InferenceProvider provider, string modelId, CancellationToken cancellationToken)
        {
            _ = provider;
            _ = modelId;
            _ = cancellationToken;
            return Task.FromResult(_result);
        }
    }
}
