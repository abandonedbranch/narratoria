using RichardSzalay.MockHttp;
using OpenAI;
using UnifiedInference.Abstractions;
using UnifiedInference.Core;
using UnifiedInference.Providers.HuggingFace;
using UnifiedInference.Providers.Ollama;
using UnifiedInference.Providers.OpenAI;

public class VideoMusicGatingTests
{
    private static UnifiedInferenceClient MakeClient(ICapabilitiesProvider? caps = null)
    {
        var openAi = new OpenAiInferenceClient(new OpenAIClient("dummy"));
        var ollama = new OllamaInferenceClient(new HttpClient(new MockHttpMessageHandler()), "http://localhost:11434");
        var hf = new HuggingFaceInferenceClient(new HttpClient(new MockHttpMessageHandler()));
        return new UnifiedInferenceClient(openAi, ollama, hf, capabilities: caps ?? new DefaultCapabilitiesProvider());
    }

    [Theory]
    [InlineData(InferenceProvider.OpenAI, "gpt-4o")]
    [InlineData(InferenceProvider.Ollama, "llama3")]
    [InlineData(InferenceProvider.HuggingFace, "stabilityai/stable-diffusion")]
    public async Task GenerateVideo_Is_NotSupported(InferenceProvider provider, string modelId)
    {
        var client = MakeClient();
        var req = new VideoRequest(provider, modelId, "make a short clip", TimeSpan.FromSeconds(5), new GenerationSettings());
        await Assert.ThrowsAsync<NotSupportedException>(() => client.GenerateVideoAsync(req));
    }

    [Theory]
    [InlineData(InferenceProvider.OpenAI, "gpt-4o")]
    [InlineData(InferenceProvider.Ollama, "llama3")]
    [InlineData(InferenceProvider.HuggingFace, "stabilityai/stable-diffusion")]
    public async Task GenerateMusic_Is_NotSupported(InferenceProvider provider, string modelId)
    {
        var client = MakeClient();
        var req = new MusicRequest(provider, modelId, "compose a short tune", new GenerationSettings());
        await Assert.ThrowsAsync<NotSupportedException>(() => client.GenerateMusicAsync(req));
    }
}
