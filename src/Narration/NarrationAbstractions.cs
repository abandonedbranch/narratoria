namespace Narratoria.Narration;

public interface INarrationSessionStore
{
    ValueTask<NarrationContext?> LoadAsync(Guid sessionId, CancellationToken cancellationToken);
    ValueTask SaveAsync(NarrationContext context, CancellationToken cancellationToken);

    ValueTask<SessionRecord> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken)
        => ValueTask.FromResult(new SessionRecord
        {
            SessionId = request.SessionId,
            Title = string.IsNullOrWhiteSpace(request.InitialTitle) ? "Untitled Session" : request.InitialTitle!.Trim(),
            IsTitleUserSet = !string.IsNullOrWhiteSpace(request.InitialTitle),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

    ValueTask<IReadOnlyList<SessionRecord>> ListSessionsAsync(CancellationToken cancellationToken)
        => ValueTask.FromResult<IReadOnlyList<SessionRecord>>(Array.Empty<SessionRecord>());

    ValueTask DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    ValueTask<IReadOnlyList<NarrationTurnRecord>> ListTurnsAsync(Guid sessionId, CancellationToken cancellationToken)
        => ValueTask.FromResult<IReadOnlyList<NarrationTurnRecord>>(Array.Empty<NarrationTurnRecord>());

    ValueTask UpsertTurnAsync(NarrationTurnRecord record, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    ValueTask DeleteTurnAsync(Guid sessionId, Guid turnId, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    ValueTask RenameSessionAsync(Guid sessionId, string title, bool isUserSet, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}

public interface INarrationProvider
{
    IAsyncEnumerable<string> StreamNarrationAsync(NarrationContext context, CancellationToken cancellationToken);
}
