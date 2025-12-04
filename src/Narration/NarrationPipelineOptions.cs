namespace Narratoria.Narration;

public sealed record NarrationPipelineOptions
{
    public TimeSpan ProviderTimeout { get; init; } = TimeSpan.FromSeconds(30);
}
