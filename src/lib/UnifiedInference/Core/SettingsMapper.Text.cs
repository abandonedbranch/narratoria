using System.Collections.Generic;
using UnifiedInference.Abstractions;

namespace UnifiedInference.Core;

public static class SettingsMapperText
{
    public static IDictionary<string, object> ToTextParameters(GenerationSettings settings, ModelCapabilities? capabilities)
    {
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

        AddIfSupported("temperature", settings.Temperature);
        AddIfSupported("top_p", settings.TopP);
        AddIfSupported("top_k", settings.TopK);
        AddIfSupported("max_new_tokens", settings.MaxNewTokens);
        AddIfSupported("do_sample", settings.DoSample);
        AddIfSupported("repetition_penalty", settings.RepetitionPenalty);
        AddIfSupported("return_full_text", settings.ReturnFullText);

        if (settings.StopSequences is { Count: > 0 })
        {
            AddIfSupported("stop", settings.StopSequences);
        }

        AddIfSupported("seed", settings.Seed);

        return parameters;
    }

    public static IDictionary<string, object> ToOptions(GenerationSettings settings)
    {
        var options = new Dictionary<string, object>();
        if (settings.UseCache is not null)
        {
            options["use_cache"] = settings.UseCache.Value;
        }

        if (settings.WaitForModel is not null)
        {
            options["wait_for_model"] = settings.WaitForModel.Value;
        }

        return options;
    }
}
