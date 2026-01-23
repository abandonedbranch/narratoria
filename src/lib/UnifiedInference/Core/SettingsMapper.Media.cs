using System.Collections.Generic;
using UnifiedInference.Abstractions;

namespace UnifiedInference.Core;

public static class SettingsMapperMedia
{
    public static IDictionary<string, object> ToImageParameters(ImageRequest request, ModelCapabilities? capabilities)
    {
        var settings = request.Settings;
        var parameters = new Dictionary<string, object>();

        void AddIfSupported(string key, object? value)
        {
            if (value is null)
            {
                return;
            }

            if (capabilities == null || !capabilities.SettingsSupport.TryGetValue(key, out var supported) || supported)
            {
                parameters[key] = value;
            }
        }

        AddIfSupported("guidance_scale", settings.GuidanceScale);
        AddIfSupported("num_inference_steps", settings.NumInferenceSteps);
        AddIfSupported("height", request.Height ?? settings.Height);
        AddIfSupported("width", request.Width ?? settings.Width);
        AddIfSupported("scheduler", settings.Scheduler);
        AddIfSupported("negative_prompt", request.NegativePrompt ?? settings.NegativePrompt);
        AddIfSupported("seed", settings.Seed);

        return parameters;
    }

    public static IDictionary<string, object> ToOptions(GenerationSettings settings)
    {
        return SettingsMapperText.ToOptions(settings);
    }
}
