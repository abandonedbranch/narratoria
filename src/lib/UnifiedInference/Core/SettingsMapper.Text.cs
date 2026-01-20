using UnifiedInference.Abstractions;

namespace UnifiedInference.Core;

// Maps GenerationSettings to provider-specific text parameters
public static class SettingsMapperText
{
    public static Dictionary<string, object> ToOpenAiOptions(GenerationSettings s)
    {
        var opts = new Dictionary<string, object>();
        if (s.Temperature is not null) opts["temperature"] = s.Temperature.Value;
        if (s.TopP is not null) opts["top_p"] = s.TopP.Value;
        if (s.MaxTokens is not null) opts["max_tokens"] = s.MaxTokens.Value;
        if (s.PresencePenalty is not null) opts["presence_penalty"] = s.PresencePenalty.Value;
        if (s.FrequencyPenalty is not null) opts["frequency_penalty"] = s.FrequencyPenalty.Value;
        if (s.StopSequences is not null) opts["stop"] = s.StopSequences;
        // Seed generally unsupported; ignore unless provider exposes it via overrides
        return opts;
    }

    public static Dictionary<string, object> ToOllamaOptions(GenerationSettings s)
    {
        var opts = new Dictionary<string, object>();
        if (s.Temperature is not null) opts["temperature"] = s.Temperature.Value;
        if (s.TopP is not null) opts["top_p"] = s.TopP.Value;
        if (s.TopK is not null) opts["top_k"] = s.TopK.Value;
        if (s.MaxTokens is not null) opts["num_predict"] = s.MaxTokens.Value;
        if (s.StopSequences is not null) opts["stop"] = s.StopSequences;
        return opts;
    }

    public static Dictionary<string, object> ToHuggingFaceOptions(GenerationSettings s)
    {
        var opts = new Dictionary<string, object>();
        if (s.Temperature is not null) opts["temperature"] = s.Temperature.Value;
        if (s.TopP is not null) opts["top_p"] = s.TopP.Value;
        if (s.TopK is not null) opts["top_k"] = s.TopK.Value;
        if (s.MaxTokens is not null) opts["max_new_tokens"] = s.MaxTokens.Value;
        if (s.StopSequences is not null) opts["stop"] = s.StopSequences;
        return opts;
    }
}
