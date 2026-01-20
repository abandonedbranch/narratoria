using UnifiedInference.Abstractions;

namespace UnifiedInference.Core;

public static class Errors
{
    public static NotSupportedException ModalityNotSupported(string modality, InferenceProvider provider, string modelId) =>
        new($"Modality '{modality}' is not supported for provider '{provider}' and model '{modelId}'.");

    public static InvalidOperationException UnsupportedSetting(string setting, InferenceProvider provider, string modelId) =>
        new($"Setting '{setting}' is not supported for provider '{provider}' and model '{modelId}'.");
}
