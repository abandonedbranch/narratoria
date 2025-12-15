namespace Narratoria.Narration;

public sealed record ProviderDispatchOptions
{
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
    public string? Model { get; init; }
}
