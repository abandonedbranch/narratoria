using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using Narratoria.OpenAi;
using Narratoria.Storage;
using Narratoria.Storage.IndexedDb;

namespace Narratoria.Narration;

public sealed class NarrationContextSerializer : IIndexedDbValueSerializer<NarrationContext>
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly string[] EphemeralMetadataPrefixes =
    [
        "system_prompt_",
        "content_guardian_"
    ];

    public ValueTask<IndexedDbSerializedValue> SerializeAsync(NarrationContext value, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var persisted = new PersistedNarrationContext(
            value.SessionId,
            value.PlayerPrompt,
            value.PriorNarration,
            StripEphemeralMetadata(value.Metadata),
            value.Trace);

        var payload = JsonSerializer.SerializeToUtf8Bytes(persisted, Options);
        return ValueTask.FromResult(new IndexedDbSerializedValue(payload, payload.LongLength));
    }

    public ValueTask<NarrationContext> DeserializeAsync(IndexedDbSerializedValue payload, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var persisted = JsonSerializer.Deserialize<PersistedNarrationContext>(payload.Payload, Options)
                ?? throw new InvalidOperationException("Unable to deserialize narration context.");

            return ValueTask.FromResult(new NarrationContext
            {
                SessionId = persisted.SessionId,
                PlayerPrompt = persisted.PlayerPrompt,
                PriorNarration = persisted.PriorNarration,
                WorkingNarration = ImmutableArray<string>.Empty,
                Metadata = StripEphemeralMetadata(persisted.Metadata),
                WorkingContextSegments = ImmutableArray<ContextSegment>.Empty,
                Trace = persisted.Trace
            });
        }
        catch (JsonException)
        {
            var legacy = JsonSerializer.Deserialize<NarrationContext>(payload.Payload, Options)
                ?? throw new InvalidOperationException("Unable to deserialize narration context.");

            return ValueTask.FromResult(legacy with
            {
                WorkingNarration = ImmutableArray<string>.Empty,
                WorkingContextSegments = ImmutableArray<ContextSegment>.Empty,
                Metadata = StripEphemeralMetadata(legacy.Metadata)
            });
        }
    }

    private static ImmutableDictionary<string, string> StripEphemeralMetadata(IReadOnlyDictionary<string, string> metadata)
    {
        if (metadata.Count == 0)
        {
            return ImmutableDictionary<string, string>.Empty;
        }

        var immutable = metadata as ImmutableDictionary<string, string>
            ?? metadata.ToImmutableDictionary(StringComparer.Ordinal);

        foreach (var prefix in EphemeralMetadataPrefixes)
        {
            var keysToRemove = immutable.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
            foreach (var key in keysToRemove)
            {
                immutable = immutable.Remove(key);
            }
        }

        return immutable;
    }

    private sealed record PersistedNarrationContext(
        Guid SessionId,
        string PlayerPrompt,
        ImmutableArray<string> PriorNarration,
        ImmutableDictionary<string, string> Metadata,
        TraceMetadata Trace);
}

public sealed class IndexedDbNarrationSessionStore : INarrationSessionStore
{
    private readonly IIndexedDbStorageService _storage;
    private readonly IIndexedDbStorageWithQuota _quotaStorage;
    private readonly IIndexedDbValueSerializer<NarrationContext> _serializer;
    private readonly IIndexedDbValueSerializer<SessionRecord> _sessionSerializer;
    private readonly IIndexedDbValueSerializer<NarrationTurnRecord> _turnSerializer;
    private readonly IndexedDbStoreDefinition _contextStore;
    private readonly IndexedDbStoreDefinition _sessionsStore;
    private readonly IndexedDbStoreDefinition _turnsStore;
    private readonly StorageScope _scope;
    private readonly ILogger<IndexedDbNarrationSessionStore> _logger;

    public IndexedDbNarrationSessionStore(
        IIndexedDbStorageService storage,
        IIndexedDbStorageWithQuota quotaStorage,
        IndexedDbStoreDefinition contextStore,
        IndexedDbStoreDefinition sessionsStore,
        IndexedDbStoreDefinition turnsStore,
        StorageScope scope,
        ILogger<IndexedDbNarrationSessionStore> logger,
        IIndexedDbValueSerializer<NarrationContext>? serializer = null,
        IIndexedDbValueSerializer<SessionRecord>? sessionSerializer = null,
        IIndexedDbValueSerializer<NarrationTurnRecord>? turnSerializer = null)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _quotaStorage = quotaStorage ?? throw new ArgumentNullException(nameof(quotaStorage));
        _contextStore = contextStore ?? throw new ArgumentNullException(nameof(contextStore));
        _sessionsStore = sessionsStore ?? throw new ArgumentNullException(nameof(sessionsStore));
        _turnsStore = turnsStore ?? throw new ArgumentNullException(nameof(turnsStore));
        _scope = scope;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializer = serializer ?? new NarrationContextSerializer();
        _sessionSerializer = sessionSerializer ?? new SessionRecordSerializer();
        _turnSerializer = turnSerializer ?? new NarrationTurnRecordSerializer();
    }

    public async ValueTask<NarrationContext?> LoadAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var query = new IndexedDbQueryOptions("session_id", sessionId.ToString(), 1);
        var request = new IndexedDbListRequest<NarrationContext>
        {
            Store = _contextStore,
            Serializer = _serializer,
            Query = query,
            Scope = _scope
        };

        var result = await _storage.ListAsync(request, cancellationToken).ConfigureAwait(false);
        if (!result.Ok)
        {
            _logger.LogWarning(
                "Narration session load failed session={SessionId} errorClass={ErrorClass} message={Message}",
                sessionId,
                result.Error?.ErrorClass,
                result.Error?.Message);
            return null;
        }

        var records = result.Value ?? Array.Empty<IndexedDbRecord<NarrationContext>>();
        var record = records.FirstOrDefault();
        return record.Equals(default) ? null : record.Value;
    }

    public async ValueTask SaveAsync(NarrationContext context, CancellationToken cancellationToken)
    {
        var request = new IndexedDbPutRequest<NarrationContext>
        {
            Store = _contextStore,
            Key = context.SessionId.ToString(),
            Value = context,
            Serializer = _serializer,
            Scope = _scope,
            IndexValues = new Dictionary<string, object?>
            {
                ["session_id"] = context.SessionId.ToString()
            }
        };

        var result = await _quotaStorage.PutIfCanAccommodateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!result.Ok)
        {
            var errorClass = result.Error?.ErrorClass.ToString() ?? "Unknown";
            _logger.LogWarning(
                "Narration session save failed session={SessionId} errorClass={ErrorClass} message={Message}",
                context.SessionId,
                errorClass,
                result.Error?.Message);
            throw new InvalidOperationException($"Failed to persist narration context: {errorClass}");
        }
    }

    public static IndexedDbStoreDefinition CreateContextStoreDefinition(string name = "narration_sessions")
    {
        return new IndexedDbStoreDefinition
        {
            Name = name,
            KeyPath = "SessionId",
            AutoIncrement = false,
            Indexes = new[]
            {
                new IndexedDbIndexDefinition { Name = "session_id", KeyPath = "SessionId", Unique = true, MultiEntry = false }
            }
        };
    }

    public static IndexedDbStoreDefinition CreateSessionsStoreDefinition(string name = "sessions")
    {
        return new IndexedDbStoreDefinition
        {
            Name = name,
            KeyPath = "SessionId",
            AutoIncrement = false,
            Indexes = new[]
            {
                new IndexedDbIndexDefinition { Name = "session_id", KeyPath = "SessionId", Unique = true, MultiEntry = false }
            }
        };
    }

    public static IndexedDbStoreDefinition CreateTurnsStoreDefinition(string name = "turns")
    {
        return new IndexedDbStoreDefinition
        {
            Name = name,
            KeyPath = "TurnKey",
            AutoIncrement = false,
            Indexes = new[]
            {
                new IndexedDbIndexDefinition { Name = "session_id", KeyPath = "SessionId", Unique = false, MultiEntry = false }
            }
        };
    }

    private static string MakeTurnKey(Guid sessionId, Guid turnId) => $"{sessionId:D}:{turnId:D}";

    public async ValueTask<SessionRecord> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var title = string.IsNullOrWhiteSpace(request.InitialTitle) ? "Untitled Session" : request.InitialTitle!.Trim();
        var session = new SessionRecord
        {
            SessionId = request.SessionId,
            Title = title,
            IsTitleUserSet = !string.IsNullOrWhiteSpace(request.InitialTitle),
            CreatedAt = now,
            UpdatedAt = now
        };

        var putSession = new IndexedDbPutRequest<SessionRecord>
        {
            Store = _sessionsStore,
            Key = session.SessionId.ToString(),
            Value = session,
            Serializer = _sessionSerializer,
            Scope = _scope,
            IndexValues = new Dictionary<string, object?>
            {
                ["session_id"] = session.SessionId.ToString()
            }
        };
        var sessionResult = await _quotaStorage.PutIfCanAccommodateAsync(putSession, cancellationToken).ConfigureAwait(false);
        if (!sessionResult.Ok)
        {
            var errorClass = sessionResult.Error?.ErrorClass.ToString() ?? "Unknown";
            _logger.LogWarning("Session create failed session={SessionId} errorClass={ErrorClass} message={Message}", request.SessionId, errorClass, sessionResult.Error?.Message);
            throw new InvalidOperationException($"Failed to create session: {errorClass}");
        }

        var emptyContext = new NarrationContext
        {
            SessionId = request.SessionId,
            PlayerPrompt = string.Empty,
            PriorNarration = ImmutableArray<string>.Empty,
            WorkingNarration = ImmutableArray<string>.Empty,
            WorkingContextSegments = ImmutableArray<ContextSegment>.Empty,
            Metadata = ImmutableDictionary<string, string>.Empty,
            Trace = request.Trace
        };

        await SaveAsync(emptyContext, cancellationToken).ConfigureAwait(false);
        return session;
    }

    public async ValueTask<IReadOnlyList<SessionRecord>> ListSessionsAsync(CancellationToken cancellationToken)
    {
        var listRequest = new IndexedDbListRequest<SessionRecord>
        {
            Store = _sessionsStore,
            Serializer = _sessionSerializer,
            Scope = _scope,
            Query = IndexedDbQueryOptions.All
        };
        var result = await _storage.ListAsync(listRequest, cancellationToken).ConfigureAwait(false);
        if (!result.Ok)
        {
            _logger.LogWarning("List sessions failed errorClass={ErrorClass} message={Message}", result.Error?.ErrorClass, result.Error?.Message);
            return Array.Empty<SessionRecord>();
        }

        var values = result.Value?.Select(r => r.Value).ToList() ?? new List<SessionRecord>();
        return values.OrderByDescending(s => s.UpdatedAt).ToList();
    }

    public async ValueTask DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        // Delete session record
        var delSession = new IndexedDbDeleteRequest { Store = _sessionsStore, Key = sessionId.ToString(), Scope = _scope };
        _ = await _storage.DeleteAsync(delSession, cancellationToken).ConfigureAwait(false);

        // Delete context
        var delContext = new IndexedDbDeleteRequest { Store = _contextStore, Key = sessionId.ToString(), Scope = _scope };
        _ = await _storage.DeleteAsync(delContext, cancellationToken).ConfigureAwait(false);

        // Delete turns by session index
        var listTurns = new IndexedDbListRequest<NarrationTurnRecord>
        {
            Store = _turnsStore,
            Serializer = _turnSerializer,
            Scope = _scope,
            Query = new IndexedDbQueryOptions("session_id", sessionId.ToString(), null)
        };
        var turns = await _storage.ListAsync(listTurns, cancellationToken).ConfigureAwait(false);
        if (turns.Ok && turns.Value is not null)
        {
            foreach (var rec in turns.Value)
            {
                var del = new IndexedDbDeleteRequest { Store = _turnsStore, Key = rec.Key, Scope = _scope };
                _ = await _storage.DeleteAsync(del, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async ValueTask<IReadOnlyList<NarrationTurnRecord>> ListTurnsAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var listRequest = new IndexedDbListRequest<NarrationTurnRecord>
        {
            Store = _turnsStore,
            Serializer = _turnSerializer,
            Scope = _scope,
            Query = new IndexedDbQueryOptions("session_id", sessionId.ToString(), null)
        };
        var result = await _storage.ListAsync(listRequest, cancellationToken).ConfigureAwait(false);
        if (!result.Ok)
        {
            _logger.LogWarning("List turns failed session={SessionId} errorClass={ErrorClass} message={Message}", sessionId, result.Error?.ErrorClass, result.Error?.Message);
            return Array.Empty<NarrationTurnRecord>();
        }

        var values = result.Value?.Select(r => r.Value).ToList() ?? new List<NarrationTurnRecord>();
        return values.OrderBy(t => t.CreatedAt).ToList();
    }

    public async ValueTask UpsertTurnAsync(NarrationTurnRecord record, CancellationToken cancellationToken)
    {
        var key = MakeTurnKey(record.SessionId, record.TurnId);

        var getRequest = new IndexedDbGetRequest<NarrationTurnRecord>
        {
            Store = _turnsStore,
            Key = key,
            Serializer = _turnSerializer,
            Scope = _scope
        };
        var existing = await _storage.GetAsync(getRequest, cancellationToken).ConfigureAwait(false);
        if (!existing.Ok)
        {
            var errorClass = existing.Error?.ErrorClass.ToString() ?? "Unknown";
            throw new InvalidOperationException($"Failed to load existing turn: {errorClass}");
        }

        if (existing.Value is { IsFinal: true } existingFinal)
        {
            // If final and different, reject
            if (!Equals(existingFinal, record))
            {
                throw new InvalidOperationException("Cannot modify final turn record");
            }
            return;
        }

        var toSave = record with { UpdatedAt = DateTimeOffset.UtcNow };

        var put = new IndexedDbPutRequest<NarrationTurnRecord>
        {
            Store = _turnsStore,
            Key = key,
            Value = toSave,
            Serializer = _turnSerializer,
            Scope = _scope,
            IndexValues = new Dictionary<string, object?>
            {
                ["session_id"] = record.SessionId.ToString()
            }
        };
        var result = await _quotaStorage.PutIfCanAccommodateAsync(put, cancellationToken).ConfigureAwait(false);
        if (!result.Ok)
        {
            var errorClass = result.Error?.ErrorClass.ToString() ?? "Unknown";
            throw new InvalidOperationException($"Failed to upsert turn: {errorClass}");
        }
    }

    public async ValueTask DeleteTurnAsync(Guid sessionId, Guid turnId, CancellationToken cancellationToken)
    {
        var key = MakeTurnKey(sessionId, turnId);
        var del = new IndexedDbDeleteRequest { Store = _turnsStore, Key = key, Scope = _scope };
        _ = await _storage.DeleteAsync(del, cancellationToken).ConfigureAwait(false);
    }
}
