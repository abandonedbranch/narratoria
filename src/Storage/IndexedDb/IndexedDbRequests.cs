namespace Narratoria.Storage.IndexedDb;

public readonly record struct IndexedDbQueryOptions(string? IndexName, object? MatchValue, int? Limit)
{
    public static readonly IndexedDbQueryOptions All = new(null, null, null);

    public bool HasFilter => IndexName is not null || MatchValue is not null;
}

public sealed record IndexedDbPutRequest<T>
{
    public required IndexedDbStoreDefinition Store { get; init; }

    public required string Key { get; init; }

    public required T Value { get; init; }

    public required IIndexedDbValueSerializer<T> Serializer { get; init; }

    public IReadOnlyDictionary<string, object?>? IndexValues { get; init; }

    public required StorageScope Scope { get; init; }
}

public sealed record IndexedDbListRequest<T>
{
    public required IndexedDbStoreDefinition Store { get; init; }

    public required IIndexedDbValueSerializer<T> Serializer { get; init; }

    public IndexedDbQueryOptions Query { get; init; } = IndexedDbQueryOptions.All;

    public required StorageScope Scope { get; init; }
}

public readonly record struct IndexedDbRecord<T>(string Key, T Value);
