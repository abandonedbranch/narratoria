using System.Collections.Concurrent;

namespace NarratoriaClient.Services;

public interface IAttachmentService
{
    Task<IReadOnlyList<AttachmentRecord>> GetAttachmentsAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<AttachmentRecord> AddAttachmentAsync(string sessionId, string fileName, string contentType, Stream content, CancellationToken cancellationToken = default);
    Task RemoveAttachmentAsync(string sessionId, string attachmentId, CancellationToken cancellationToken = default);
}

public sealed record AttachmentRecord(
    string Id,
    string SessionId,
    string FileName,
    string ContentType,
    long Size,
    string TextContent,
    DateTimeOffset UploadedAt);

public sealed class AttachmentService : IAttachmentService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/plain",
        "text/markdown",
        "text/x-markdown"
    };

    private const long MaxSizeBytes = 5 * 1024 * 1024; // 5 MB

    private readonly ConcurrentDictionary<string, List<AttachmentRecord>> _store = new(StringComparer.OrdinalIgnoreCase);

    public Task<IReadOnlyList<AttachmentRecord>> GetAttachmentsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session id must be provided.", nameof(sessionId));
        }

        _store.TryGetValue(sessionId, out var list);
        return Task.FromResult<IReadOnlyList<AttachmentRecord>>(list?.ToList() ?? new List<AttachmentRecord>());
    }

    public async Task<AttachmentRecord> AddAttachmentAsync(string sessionId, string fileName, string contentType, Stream content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session id must be provided.", nameof(sessionId));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Filename must be provided.", nameof(fileName));
        }

        if (string.IsNullOrWhiteSpace(contentType) || !AllowedContentTypes.Contains(contentType))
        {
            throw new InvalidOperationException("Only plain-text attachments are supported.");
        }

        using var memory = new MemoryStream();
        await content.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);

        if (memory.Length > MaxSizeBytes)
        {
            throw new InvalidOperationException("Attachment exceeds the 5 MB size limit.");
        }

        var textContent = System.Text.Encoding.UTF8.GetString(memory.ToArray());

        var record = new AttachmentRecord(
            Guid.NewGuid().ToString("N"),
            sessionId,
            fileName,
            contentType,
            memory.Length,
            textContent,
            DateTimeOffset.UtcNow);

        var list = _store.GetOrAdd(sessionId, _ => new List<AttachmentRecord>());
        lock (list)
        {
            list.Add(record);
        }

        return record;
    }

    public Task RemoveAttachmentAsync(string sessionId, string attachmentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(attachmentId))
        {
            return Task.CompletedTask;
        }

        if (_store.TryGetValue(sessionId, out var list))
        {
            lock (list)
            {
                list.RemoveAll(a => string.Equals(a.Id, attachmentId, StringComparison.OrdinalIgnoreCase));
            }
        }

        return Task.CompletedTask;
    }
}
