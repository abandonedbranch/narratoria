using System.Collections.Immutable;

namespace Narratoria.Narration;

public sealed class StaticSystemPromptProfileResolver : ISystemPromptProfileResolver
{
    private readonly SystemPromptProfile _profile;

    public StaticSystemPromptProfileResolver(SystemPromptProfile profile)
    {
        _profile = profile;
    }

    public ValueTask<SystemPromptProfile?> ResolveAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        return new ValueTask<SystemPromptProfile?>(_profile);
    }
}
