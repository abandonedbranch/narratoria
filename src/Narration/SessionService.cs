using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace Narratoria.Narration;

public interface ISessionService
{
    ValueTask<string?> GetTitleAsync(Guid sessionId, CancellationToken cancellationToken);

    ValueTask<bool> RenameTitleAsync(Guid sessionId, string title, bool isUserSet, CancellationToken cancellationToken);
}

public sealed record SessionTitleOptions
{
    public int MaxChars { get; init; } = 64;
}

public sealed class SessionService : ISessionService
{
    private readonly INarrationSessionStore _store;
    private readonly ILogger<SessionService> _logger;
    private readonly SessionTitleOptions _options;

    public SessionService(INarrationSessionStore store, ILogger<SessionService> logger, IOptions<SessionTitleOptions>? options = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new SessionTitleOptions();
    }

    public async ValueTask<string?> GetTitleAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var sessions = await _store.ListSessionsAsync(cancellationToken).ConfigureAwait(false);
        var record = sessions.FirstOrDefault(s => s.SessionId == sessionId);
        return record?.Title;
    }

    public async ValueTask<bool> RenameTitleAsync(Guid sessionId, string title, bool isUserSet, CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var normalized = title?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
            {
                _logger.LogWarning("Session rename rejected request={RequestId} session={SessionId} reason=empty_title", requestId, sessionId);
                return false;
            }

            if (normalized.Length > _options.MaxChars)
            {
                _logger.LogWarning("Session rename rejected request={RequestId} session={SessionId} reason=title_too_long maxChars={MaxChars}", requestId, sessionId, _options.MaxChars);
                return false;
            }

            var sessions = await _store.ListSessionsAsync(cancellationToken).ConfigureAwait(false);
            var record = sessions.FirstOrDefault(s => s.SessionId == sessionId);
            if (record is null)
            {
                _logger.LogWarning("Session rename failed request={RequestId} session={SessionId} errorClass=MissingSession", requestId, sessionId);
                return false;
            }

            if (string.Equals(record.Title, normalized, StringComparison.Ordinal))
            {
                _logger.LogInformation("Session rename skipped request={RequestId} session={SessionId} reason=unchanged_title", requestId, sessionId);
                return false;
            }

            if (record.IsTitleUserSet && !isUserSet)
            {
                _logger.LogInformation("Session rename skipped request={RequestId} session={SessionId} reason=user_title_guard", requestId, sessionId);
                return false;
            }

            await _store.RenameSessionAsync(sessionId, normalized, isUserSet, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();
            _logger.LogInformation("Session rename success request={RequestId} session={SessionId} operation=rename elapsedMs={ElapsedMs}", requestId, sessionId, stopwatch.Elapsed.TotalMilliseconds);
            return true;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("Session rename canceled request={RequestId} session={SessionId} operation=rename elapsedMs={ElapsedMs}", requestId, sessionId, stopwatch.Elapsed.TotalMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Session rename failure request={RequestId} session={SessionId} operation=rename elapsedMs={ElapsedMs}", requestId, sessionId, stopwatch.Elapsed.TotalMilliseconds);
            return false;
        }
    }
}
