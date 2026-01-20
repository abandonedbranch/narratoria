using UnifiedInference.Abstractions;

namespace UnifiedInference.Core;

// Maps GenerationSettings to provider-specific media parameters (placeholder stubs).
public static class SettingsMapperMedia
{
    public static object ToImageOptions(GenerationSettings settings)
    {
        // TODO: map size and common sampling params where supported
        return new object();
    }

    public static object ToAudioOptions(GenerationSettings settings)
    {
        // TODO: map audio-related params (if any) per provider
        return new object();
    }

    public static object ToVideoOptions(GenerationSettings settings)
    {
        // TODO: map video-related params (if any) per provider
        return new object();
    }
}
