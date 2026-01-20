using UnifiedInference.Abstractions;

namespace UnifiedInference.Core;

public static class ModelCapabilitiesDefaults
{
    public static ModelCapabilities Disabled() => ModelCapabilities.Disabled();

    public static ModelCapabilities TextOnly(CapabilitySettings settings) => new(
        SupportsText: true,
        SupportsImage: false,
        SupportsAudioTts: false,
        SupportsAudioStt: false,
        SupportsVideo: false,
        SupportsMusic: false,
        Support: settings
    );
}
