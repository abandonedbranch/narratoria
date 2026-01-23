using System.Collections.Generic;
using UnifiedInference.Abstractions;

namespace UnifiedInference.Core;

public static class ModelCapabilitiesDefaults
{
    public static ModelCapabilities Unknown(string modelId)
    {
        return new ModelCapabilities
        {
            SupportsText = false,
            SupportsImage = false,
            SupportsAudioTts = false,
            SupportsAudioStt = false,
            SupportsVideo = false,
            SupportsMusic = false,
            SettingsSupport = new Dictionary<string, bool>(),
            PipelineTag = null,
            Gated = false,
            InferenceStatus = null,
            Notes = $"Capabilities for {modelId} have not been fetched."
        };
    }

    public static ModelCapabilities ForPipelineTag(string modelId, string? pipelineTag, bool gated, string? status)
    {
        var supportsText = pipelineTag is "text-generation" or "text2text-generation" or "conversational";
        var supportsImage = pipelineTag is not null && pipelineTag.Contains("image", System.StringComparison.OrdinalIgnoreCase);
        var supportsAudioTts = pipelineTag is "text-to-speech";
        var supportsAudioStt = pipelineTag is "automatic-speech-recognition";
        var supportsVideo = pipelineTag is not null && pipelineTag.Contains("video", System.StringComparison.OrdinalIgnoreCase);
        var supportsMusic = pipelineTag is not null && pipelineTag.Contains("music", System.StringComparison.OrdinalIgnoreCase);

        return new ModelCapabilities
        {
            SupportsText = supportsText,
            SupportsImage = supportsImage,
            SupportsAudioTts = supportsAudioTts,
            SupportsAudioStt = supportsAudioStt,
            SupportsVideo = supportsVideo,
            SupportsMusic = supportsMusic,
            SettingsSupport = BuildDefaultSettingsSupport(supportsText, supportsImage),
            PipelineTag = pipelineTag,
            Gated = gated,
            InferenceStatus = status,
            Notes = status is not null && status != "loaded" ? $"Model is {status}." : null
        };
    }

    private static IDictionary<string, bool> BuildDefaultSettingsSupport(bool supportsText, bool supportsImage)
    {
        var map = new Dictionary<string, bool>(System.StringComparer.OrdinalIgnoreCase);
        if (supportsText)
        {
            map["temperature"] = true;
            map["top_p"] = true;
            map["top_k"] = true;
            map["max_new_tokens"] = true;
            map["do_sample"] = true;
            map["repetition_penalty"] = true;
            map["return_full_text"] = true;
            map["stop"] = true;
            map["seed"] = true;
        }

        if (supportsImage)
        {
            map["guidance_scale"] = true;
            map["num_inference_steps"] = true;
            map["height"] = true;
            map["width"] = true;
            map["scheduler"] = true;
            map["negative_prompt"] = true;
        }

        return map;
    }
}
