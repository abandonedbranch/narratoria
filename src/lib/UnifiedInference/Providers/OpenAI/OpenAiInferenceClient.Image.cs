using System.Reflection;
using OpenAI.Images;
using UnifiedInference.Abstractions;

namespace UnifiedInference.Providers.OpenAI;

public sealed partial class OpenAiInferenceClient
{
    public async Task<ImageResponse> GenerateImageAsync(ImageRequest request, CancellationToken cancellationToken)
    {
        var imageClient = _client.GetImageClient(request.ModelId);

        var options = CreateImageOptions(request);

        var response = await imageClient.GenerateImageAsync(request.Prompt, options, cancellationToken).ConfigureAwait(false);
        var generated = response.Value;

        byte[]? bytes = null;
        try
        {
            var bin = generated.ImageBytes;
            var toArray = bin?.GetType().GetMethod("ToArray", BindingFlags.Public | BindingFlags.Instance);
            bytes = (byte[]?)toArray?.Invoke(bin, null);
        }
        catch { }

        Uri? uri = null;
        try { uri = generated.ImageUri; } catch { }

        return new ImageResponse(bytes, uri, null);
    }

    private static ImageGenerationOptions CreateImageOptions(ImageRequest request)
    {
        return new ImageGenerationOptions();
    }
}
