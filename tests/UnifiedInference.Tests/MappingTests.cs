using UnifiedInference.Core;
using UnifiedInference.Abstractions;

public class MappingTests
{
    [Fact]
    public void OpenAI_Text_Mapping()
    {
        var s = new GenerationSettings(Temperature: 0.5, TopP: 0.9, MaxTokens: 128, PresencePenalty: 0.1, FrequencyPenalty: 0.2, StopSequences: new[] { "END" });
        var d = SettingsMapperText.ToOpenAiOptions(s);
        Assert.Equal(0.5, d["temperature"]);
        Assert.Equal(0.9, d["top_p"]);
        Assert.Equal(128, d["max_tokens"]);
        Assert.Equal(0.1, d["presence_penalty"]);
        Assert.Equal(0.2, d["frequency_penalty"]);
        Assert.Contains("END", (System.Collections.Generic.IEnumerable<string>)d["stop"]);
        Assert.False(d.ContainsKey("top_k"));
    }

    [Fact]
    public void Ollama_Text_Mapping()
    {
        var s = new GenerationSettings(Temperature: 0.7, TopP: 0.8, TopK: 50, MaxTokens: 64, StopSequences: new[] { "\n" });
        var d = SettingsMapperText.ToOllamaOptions(s);
        Assert.Equal(0.7, d["temperature"]);
        Assert.Equal(0.8, d["top_p"]);
        Assert.Equal(50, d["top_k"]);
        Assert.Equal(64, d["num_predict"]);
        Assert.Contains("\n", (System.Collections.Generic.IEnumerable<string>)d["stop"]);
    }

    [Fact]
    public void HuggingFace_Text_Mapping()
    {
        var s = new GenerationSettings(Temperature: 0.3, TopP: 0.95, TopK: 20, MaxTokens: 200, StopSequences: new[] { "STOP" });
        var d = SettingsMapperText.ToHuggingFaceOptions(s);
        Assert.Equal(0.3, d["temperature"]);
        Assert.Equal(0.95, d["top_p"]);
        Assert.Equal(20, d["top_k"]);
        Assert.Equal(200, d["max_new_tokens"]);
        Assert.Contains("STOP", (System.Collections.Generic.IEnumerable<string>)d["stop"]);
    }
}
