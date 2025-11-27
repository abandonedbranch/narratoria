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
    private const string GlobalKey = "_global";

    public event EventHandler? EntriesChanged;

    public IReadOnlyList<TransientCommandEntry> GetEntries(string sessionId)
    {
        lock (_gate)
        {
            var key = string.IsNullOrWhiteSpace(sessionId) ? GlobalKey : sessionId;
            return _entries.TryGetValue(key, out var list)
                ? list.OrderBy(e => e.Timestamp).ToList()
                : Array.Empty<TransientCommandEntry>();
        }
    }

    public void AddEntry(TransientCommandEntry entry)
    {
        lock (_gate)
        {
            var key = string.IsNullOrWhiteSpace(entry.SessionId) ? GlobalKey : entry.SessionId;

            if (!_entries.TryGetValue(key, out var list))
            {
                list = new List<TransientCommandEntry>();
                _entries[key] = list;
            }

            list.Add(entry);
        }

        EntriesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear(string sessionId)
    {
        lock (_gate)
        {
            var key = string.IsNullOrWhiteSpace(sessionId) ? GlobalKey : sessionId;
            _entries.Remove(key);
        }

        EntriesChanged?.Invoke(this, EventArgs.Empty);
    }
}
