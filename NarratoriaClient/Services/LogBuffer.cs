using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace NarratoriaClient.Services;

public sealed record LogEntry(
    DateTimeOffset Timestamp,
    string Category,
    LogLevel Level,
    string Message,
    IReadOnlyDictionary<string, object?> Metadata);

public interface ILogBuffer
{
    event EventHandler? EntriesChanged;
    IReadOnlyList<LogEntry> GetEntries();
    void Log(string category, LogLevel level, string message, IReadOnlyDictionary<string, object?>? metadata = null);
}

public sealed class LogBuffer : ILogBuffer
{
    private static readonly IReadOnlyDictionary<string, object?> EmptyMetadata =
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    private readonly object _gate = new();
    private readonly List<LogEntry> _entries = new();

    public event EventHandler? EntriesChanged;

    public IReadOnlyList<LogEntry> GetEntries()
    {
        lock (_gate)
        {
            return _entries.ToList();
        }
    }

    public void Log(string category, LogLevel level, string message, IReadOnlyDictionary<string, object?>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            category = "General";
        }

        var snapshot = metadata is null
            ? EmptyMetadata
            : new Dictionary<string, object?>(metadata, StringComparer.OrdinalIgnoreCase);

        var entry = new LogEntry(DateTimeOffset.UtcNow, category, level, message, snapshot);

        lock (_gate)
        {
            _entries.Add(entry);
        }

        EntriesChanged?.Invoke(this, EventArgs.Empty);
    }
}
