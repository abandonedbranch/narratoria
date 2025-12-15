using System.Collections.Immutable;

namespace Narratoria.Components;

public enum NarrationStageStatus
{
    Pending,
    Running,
    Completed,
    Skipped,
    Failed
}

public readonly record struct NarrationStageKind(string Name)
{
    public static NarrationStageKind Sanitize { get; } = new("Sanitize");
    public static NarrationStageKind Context { get; } = new("Context");
    public static NarrationStageKind Lore { get; } = new("Lore");
    public static NarrationStageKind Llm { get; } = new("Llm");
    public static NarrationStageKind Image { get; } = new("Image");

    public static NarrationStageKind Custom(string name) => new(name);

    public override string ToString() => Name;
}

public sealed record NarrationStageView
{
    public required NarrationStageKind Kind { get; init; }
    public required NarrationStageStatus Status { get; init; }
    public TimeSpan? Duration { get; init; }
    public int? PromptTokens { get; init; }
    public int? CompletionTokens { get; init; }
    public string? Model { get; init; }
    public string? ErrorClass { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed record NarrationOutputView
{
    public bool IsStreaming { get; init; }
    public string? FinalText { get; init; }
    public ImmutableArray<string> StreamedSegments { get; init; } = ImmutableArray<string>.Empty;
}

public sealed record NarrationPipelineTurnView
{
    public required Guid TurnId { get; init; }
    public required string UserPrompt { get; init; }
    public DateTimeOffset? PromptAt { get; init; }
    public required ImmutableArray<NarrationStageView> Stages { get; init; }
    public required NarrationOutputView Output { get; init; }
}

public sealed record NarrationStreamState
{
    public required Guid TurnId { get; init; }
    public required NarrationStageKind Stage { get; init; }
    public required ImmutableArray<string> Segments { get; init; } = ImmutableArray<string>.Empty;
}

public sealed record NarrationStageHover
{
    public required Guid TurnId { get; init; }
    public required NarrationStageKind Stage { get; init; }
    public TimeSpan? Duration { get; init; }
    public int? PromptTokens { get; init; }
    public int? CompletionTokens { get; init; }
    public string? Model { get; init; }
}
