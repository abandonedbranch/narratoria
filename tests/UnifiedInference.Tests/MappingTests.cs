using System.Collections.Generic;
using UnifiedInference.Abstractions;
using UnifiedInference.Core;
using Xunit;

namespace UnifiedInference.Tests;

public class MappingTests
{
    [Fact]
    public void TextMapping_includes_expected_parameters()
    {
        var settings = new GenerationSettings
        {
            Temperature = 0.7f,
            TopP = 0.9f,
            TopK = 50,
            MaxNewTokens = 128,
            DoSample = true,
            RepetitionPenalty = 1.1f,
            ReturnFullText = false,
            StopSequences = new List<string> { "STOP", "\n" },
            Seed = 42
        };

        var parameters = SettingsMapperText.ToTextParameters(settings, null);

        Assert.Equal(0.7f, parameters["temperature"]);
        Assert.Equal(0.9f, parameters["top_p"]);
        Assert.Equal(50, parameters["top_k"]);
        Assert.Equal(128, parameters["max_new_tokens"]);
        Assert.Equal(true, parameters["do_sample"]);
        Assert.Equal(1.1f, parameters["repetition_penalty"]);
        Assert.Equal(false, parameters["return_full_text"]);
        Assert.Equal(new List<string> { "STOP", "\n" }, parameters["stop"]);
        Assert.Equal(42, parameters["seed"]);
    }

    [Fact]
    public void ImageMapping_includes_diffusion_options()
    {
        var request = new ImageRequest
        {
            ModelId = "stabilityai/stable-diffusion-2",
            Prompt = "a fox",
            NegativePrompt = "blurry",
            Height = 768,
            Width = 512,
            Settings = new GenerationSettings
            {
                GuidanceScale = 7.5f,
                NumInferenceSteps = 30,
                Scheduler = "dpmsolver++",
                Seed = 1234
            }
        };

        var parameters = SettingsMapperMedia.ToImageParameters(request, null);

        Assert.Equal(7.5f, parameters["guidance_scale"]);
        Assert.Equal(30, parameters["num_inference_steps"]);
        Assert.Equal(768, parameters["height"]);
        Assert.Equal(512, parameters["width"]);
        Assert.Equal("dpmsolver++", parameters["scheduler"]);
        Assert.Equal("blurry", parameters["negative_prompt"]);
        Assert.Equal(1234, parameters["seed"]);
    }
}
