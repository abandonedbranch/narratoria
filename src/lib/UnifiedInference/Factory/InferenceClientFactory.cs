using OpenAI;
using UnifiedInference.Abstractions;
using UnifiedInference.Core;
using UnifiedInference.Providers.HuggingFace;
using UnifiedInference.Providers.Ollama;
using UnifiedInference.Providers.OpenAI;

namespace UnifiedInference.Factory;

public static class InferenceClientFactory
{
    public static UnifiedInferenceClient Create(
        OpenAIClient openAiClient,
        HttpClient ollamaHttp,
        string ollamaBaseUrl,
        HttpClient huggingFaceHttp)
    {
        var openai = new OpenAiInferenceClient(openAiClient);
        var ollama = new OllamaInferenceClient(ollamaHttp, ollamaBaseUrl);
        var hf = new HuggingFaceInferenceClient(huggingFaceHttp);
        return new UnifiedInferenceClient(openai, ollama, hf, capabilities: new DefaultCapabilitiesProvider());
    }
}
