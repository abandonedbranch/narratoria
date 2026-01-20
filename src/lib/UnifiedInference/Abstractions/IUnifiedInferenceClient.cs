namespace UnifiedInference.Abstractions;

public interface IUnifiedInferenceClient
{
    Task<TextResponse> GenerateTextAsync(TextRequest request, CancellationToken cancellationToken = default);
    Task<ImageResponse> GenerateImageAsync(ImageRequest request, CancellationToken cancellationToken = default);
    Task<AudioResponse> GenerateAudioTtsAsync(AudioRequest request, CancellationToken cancellationToken = default);
    Task<AudioResponse> GenerateAudioSttAsync(AudioRequest request, CancellationToken cancellationToken = default);
    Task<VideoResponse> GenerateVideoAsync(VideoRequest request, CancellationToken cancellationToken = default);
    Task<MusicResponse> GenerateMusicAsync(MusicRequest request, CancellationToken cancellationToken = default);
    Task<ModelCapabilities> GetCapabilitiesAsync(InferenceProvider provider, string modelId, CancellationToken cancellationToken = default);
}
