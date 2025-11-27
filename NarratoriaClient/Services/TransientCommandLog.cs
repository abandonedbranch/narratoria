namespace NarratoriaClient.Services;

public sealed record TransientCommandEntry(
    string Id,
    string SessionId,
    string Author,
    string Token,
    string? Args,
    DateTimeOffset Timestamp,
    bool IsError = false,
    string? Message = null);

public interface ITransientCommandLog
{
    event EventHandler? EntriesChanged;
    IReadOnlyList<TransientCommandEntry> GetEntries(string sessionId);
    void AddEntry(TransientCommandEntry entry);
    void Clear(string sessionId);
}

public sealed class TransientCommandLog : ITransientCommandLog
{
    private readonly object _gate = new();
    private readonly Dictionary<string, List<TransientCommandEntry>> _entries = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler? EntriesChanged;

    public IReadOnlyList<TransientCommandEntry> GetEntries(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Array.Empty<TransientCommandEntry>();
        }

        lock (_gate)
        {
            return _entries.TryGetValue(sessionId, out var list)
                ? list.OrderBy(e => e.Timestamp).ToList()
                : Array.Empty<TransientCommandEntry>();
        }
    }

    public void AddEntry(TransientCommandEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.SessionId))
        {
            return;
        }

        lock (_gate)
        {
            if (!_entries.TryGetValue(entry.SessionId, out var list))
            {
                list = new List<TransientCommandEntry>();
                _entries[entry.SessionId] = list;
            }

            list.Add(entry);
        }

        EntriesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return;
        }

        lock (_gate)
        {
            _entries.Remove(sessionId);
        }

        EntriesChanged?.Invoke(this, EventArgs.Empty);
    }
}
