using System;

namespace Narratoria.Narration;

public sealed record NarrationOpenAiOptions
{
    public string Model { get; init; } = "gpt-4o-mini";

    public string ApiKey { get; init; } = string.Empty;

    public string Endpoint { get; init; } = "https://api.openai.com/v1";

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    public bool Idempotent { get; init; } = true;

    public string? OrganizationId { get; init; }

    public string? ProjectId { get; init; }
}
