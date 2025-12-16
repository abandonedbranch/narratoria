namespace Narratoria.Storage.IndexedDb;

public sealed record IndexedDbSchema
{
    public required string DatabaseName { get; init; }

    public required int Version { get; init; }

    public required IReadOnlyList<IndexedDbStoreDefinition> Stores { get; init; }
}

public sealed record IndexedDbStoreDefinition
{
    public required string Name { get; init; }

    public required string KeyPath { get; init; }

    public bool AutoIncrement { get; init; }

    public IReadOnlyList<IndexedDbIndexDefinition> Indexes { get; init; } = Array.Empty<IndexedDbIndexDefinition>();
}

public sealed record IndexedDbIndexDefinition
{
    public required string Name { get; init; }

    public required string KeyPath { get; init; }

    public bool Unique { get; init; }

    public bool MultiEntry { get; init; }
}
