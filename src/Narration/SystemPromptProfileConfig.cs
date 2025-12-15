namespace Narratoria.Narration;

public sealed record SystemPromptProfileConfig
{
    public required string ProfileId { get; init; }
    public required string PromptText { get; init; }
    public string[] Instructions { get; init; } = Array.Empty<string>();
    public required string Version { get; init; }
}
