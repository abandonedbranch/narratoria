using UnifiedInference.Abstractions;

namespace UnifiedInference.Core;

// Maps GenerationSettings to provider-specific text parameters (placeholder stubs).
public static class SettingsMapperText
{
    public static object ToOpenAiOptions(GenerationSettings settings)
    {
        // TODO: map temperature, top_p, max_tokens, presence/frequency penalties, stop, seed (if supported)
        return new object();
    }

    public static object ToOllamaOptions(GenerationSettings settings)
    {
        // TODO: map temperature, top_p, top_k, num_predict, stop, repeat penalties if available
        return new object();
    }

    public static object ToHuggingFaceOptions(GenerationSettings settings)
    {
        // TODO: map temperature, top_p, top_k, max_new_tokens, stop, seed (if supported)
        return new object();
    }
}
