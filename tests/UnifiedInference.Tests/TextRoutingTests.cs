using RichardSzalay.MockHttp;
using OpenAI;
using UnifiedInference.Abstractions;
using UnifiedInference.Factory;

namespace UnifiedInference.Tests;

public class TextRoutingTests
{
    [Fact]
    public async Task Routes_To_HuggingFace_Text()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, "https://api-inference.huggingface.co/models/*")
            .Respond("application/json", "[{\"generated_text\":\"Hello HF\"}]");
        var http = new HttpClient(mock);

        var client = InferenceClientFactory.Create(
            openAiClient: new OpenAIClient("dummy"),
            ollamaHttp: new HttpClient(new MockHttpMessageHandler()),
            ollamaBaseUrl: "http://localhost:11434",
            huggingFaceHttp: http
        );

        var req = new TextRequest(InferenceProvider.HuggingFace, "some/model", "Hi", new GenerationSettings());
        var res = await client.GenerateTextAsync(req);
        Assert.Equal("Hello HF", res.Text);
    }

    [Fact]
    public async Task Routes_To_Ollama_Text()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, "http://localhost:11434/api/generate")
            .Respond("application/json", "{\"response\":\"Hello Ollama\"}");
        var http = new HttpClient(mock);

        var client = InferenceClientFactory.Create(
            openAiClient: new OpenAIClient("dummy"),
            ollamaHttp: http,
            ollamaBaseUrl: "http://localhost:11434",
            huggingFaceHttp: new HttpClient(new MockHttpMessageHandler())
        );

        var req = new TextRequest(InferenceProvider.Ollama, "llama3", "Hi", new GenerationSettings(Temperature: 0.7));
        var res = await client.GenerateTextAsync(req);
        Assert.Equal("Hello Ollama", res.Text);
    }
}
