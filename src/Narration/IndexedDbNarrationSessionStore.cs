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
            var keysToRemove = immutable.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal));
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
    private readonly IndexedDbStoreDefinition _store;
    private readonly StorageScope _scope;
    private readonly ILogger<IndexedDbNarrationSessionStore> _logger;

    public IndexedDbNarrationSessionStore(
        IIndexedDbStorageService storage,
        IIndexedDbStorageWithQuota quotaStorage,
        IndexedDbStoreDefinition store,
        StorageScope scope,
        ILogger<IndexedDbNarrationSessionStore> logger,
        IIndexedDbValueSerializer<NarrationContext>? serializer = null)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _quotaStorage = quotaStorage ?? throw new ArgumentNullException(nameof(quotaStorage));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _scope = scope;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializer = serializer ?? new NarrationContextSerializer();
    }

    public async ValueTask<NarrationContext?> LoadAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var query = new IndexedDbQueryOptions("session_id", sessionId.ToString(), 1);
        var request = new IndexedDbListRequest<NarrationContext>
        {
            Store = _store,
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
            Store = _store,
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

    public static IndexedDbStoreDefinition CreateStoreDefinition(string name = "narration_sessions")
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
}
