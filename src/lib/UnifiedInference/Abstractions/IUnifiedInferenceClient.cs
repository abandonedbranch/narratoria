using System.Threading;
using System.Threading.Tasks;

namespace UnifiedInference.Abstractions;

/// <summary>
/// Unified interface over Hugging Face inference for supported modalities.
/// Methods honor <see cref="CancellationToken"/> and surface provider error payloads when available.
/// </summary>
public interface IUnifiedInferenceClient
{
    /// <summary>Fetch model capabilities (pipeline_tag, gating, modality support).</summary>
    Task<ModelCapabilities> GetCapabilitiesAsync(string modelId, CancellationToken cancellationToken);

    /// <summary>Generate text; throws <see cref="NotSupportedException"/> when capabilities disallow the modality.</summary>
    Task<TextResponse> GenerateTextAsync(TextRequest request, CancellationToken cancellationToken);

    /// <summary>Generate an image via HF diffusion pipelines.</summary>
    Task<ImageResponse> GenerateImageAsync(ImageRequest request, CancellationToken cancellationToken);

    /// <summary>Audio best-effort hook (defaults to NotSupported).</summary>
    Task<AudioResponse> GenerateAudioAsync(AudioRequest request, CancellationToken cancellationToken);

    /// <summary>Video best-effort hook (defaults to NotSupported).</summary>
    Task<VideoResponse> GenerateVideoAsync(VideoRequest request, CancellationToken cancellationToken);

    /// <summary>Music best-effort hook (defaults to NotSupported).</summary>
    Task<MusicResponse> GenerateMusicAsync(MusicRequest request, CancellationToken cancellationToken);
}
