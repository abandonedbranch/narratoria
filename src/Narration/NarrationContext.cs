using System.Collections.Immutable;
using Narratoria.OpenAi;

namespace Narratoria.Narration;

public sealed record NarrationContext
{
    public required Guid SessionId { get; init; }
    public required string PlayerPrompt { get; init; }
    public ImmutableArray<string> PriorNarration { get; init; } = [];
    public ImmutableArray<string> WorkingNarration { get; init; } = [];
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = ImmutableDictionary<string, string>.Empty;
    public required TraceMetadata Trace { get; init; }
}

public sealed record NarrationRequest(Guid SessionId, string PlayerPrompt, TraceMetadata Trace, IReadOnlyDictionary<string, string>? Metadata = null);
