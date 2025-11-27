using System.Collections.Concurrent;
using System.Text.Json;

namespace NarratoriaClient.Services;

public interface IAttachmentService
{
    Task<IReadOnlyList<AttachmentRecord>> GetAttachmentsAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<AttachmentRecord> AddAttachmentAsync(string sessionId, string fileName, string contentType, Stream content, CancellationToken cancellationToken = default);
    Task RemoveAttachmentAsync(string sessionId, string attachmentId, CancellationToken cancellationToken = default);
    Task RemoveAllAsync(string sessionId, CancellationToken cancellationToken = default);
    Task ReplaceAttachmentsAsync(string sessionId, IEnumerable<AttachmentRecord> attachments, CancellationToken cancellationToken = default);
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
    private const string StoragePrefix = "narratoria/v1/attachments/";

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/plain",
        "text/markdown",
        "text/x-markdown"
    };

    private const long MaxSizeBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IClientStorageService _storage;

    public AttachmentService(IClientStorageService storage)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }

    public Task<IReadOnlyList<AttachmentRecord>> GetAttachmentsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return LoadAsync(sessionId, cancellationToken);
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

        var list = (await LoadAsync(sessionId, cancellationToken).ConfigureAwait(false)).ToList();
        list.Add(record);
        await SaveAsync(sessionId, list, cancellationToken).ConfigureAwait(false);

        return record;
    }

    public Task RemoveAttachmentAsync(string sessionId, string attachmentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(attachmentId))
        {
            return Task.CompletedTask;
        }

        return RemoveInternalAsync(sessionId, id => string.Equals(id, attachmentId, StringComparison.OrdinalIgnoreCase), cancellationToken);
    }

    public Task RemoveAllAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Task.CompletedTask;
        }

        return RemoveInternalAsync(sessionId, _ => true, cancellationToken);
    }

    public async Task ReplaceAttachmentsAsync(string sessionId, IEnumerable<AttachmentRecord> attachments, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session id must be provided.", nameof(sessionId));
        }

        var list = attachments?.Select(a => Clone(a)).ToList() ?? new List<AttachmentRecord>();
        await SaveAsync(sessionId, list, cancellationToken).ConfigureAwait(false);
    }

    private async Task RemoveInternalAsync(string sessionId, Func<string, bool> predicate, CancellationToken cancellationToken)
    {
        var list = (await LoadAsync(sessionId, cancellationToken).ConfigureAwait(false)).ToList();
        var filtered = list.Where(a => !predicate(a.Id)).ToList();
        await SaveAsync(sessionId, filtered, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<AttachmentRecord>> LoadAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session id must be provided.", nameof(sessionId));
        }

        var key = BuildKey(sessionId);
        var stored = await _storage.GetItemAsync(StorageArea.Local, key, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(stored))
        {
            return Array.Empty<AttachmentRecord>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<AttachmentRecord>>(stored, SerializerOptions) ?? new List<AttachmentRecord>();
        }
        catch (JsonException)
        {
            return Array.Empty<AttachmentRecord>();
        }
    }

    private async Task SaveAsync(string sessionId, IReadOnlyList<AttachmentRecord> attachments, CancellationToken cancellationToken)
    {
        var key = BuildKey(sessionId);
        var payload = JsonSerializer.Serialize(attachments, SerializerOptions);
        await _storage.SetItemAsync(StorageArea.Local, key, payload, cancellationToken).ConfigureAwait(false);
    }

    private static string BuildKey(string sessionId) => $"{StoragePrefix}{sessionId}";

    private static AttachmentRecord Clone(AttachmentRecord record)
    {
        return record with { TextContent = record.TextContent };
    }
}
