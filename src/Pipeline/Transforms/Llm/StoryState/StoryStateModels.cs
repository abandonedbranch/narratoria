using System.Text.Json;

namespace Narratoria.Pipeline.Transforms.Llm.StoryState;

public sealed record TransformProvenance
{
    public required string TransformName { get; init; }

    public required double Confidence { get; init; }

    public string? SourceSnippet { get; init; }

    public int? ChunkIndex { get; init; }
}

public sealed record Relationship
{
    public required string OtherCharacterId { get; init; }

    public required string Relation { get; init; }

    public required TransformProvenance Provenance { get; init; }
}

public sealed record CharacterRecord
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public IReadOnlyList<string>? Aliases { get; init; }

    public IReadOnlyDictionary<string, string>? Traits { get; init; }

    public IReadOnlyList<Relationship>? Relationships { get; init; }

    public string? LastSeen { get; init; }

    public required TransformProvenance Provenance { get; init; }
}

public sealed record InventoryItem
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public int? Quantity { get; init; }

    public string? Notes { get; init; }

    public required TransformProvenance Provenance { get; init; }
}

public sealed record InventoryState
{
    public IReadOnlyList<InventoryItem> Items { get; init; } = Array.Empty<InventoryItem>();

    public required TransformProvenance Provenance { get; init; }

    public static InventoryState Empty(string transformName = "init") => new()
    {
        Items = Array.Empty<InventoryItem>(),
        Provenance = new TransformProvenance { TransformName = transformName, Confidence = 1.0 },
    };
}

public sealed record StoryState
{
    public required string SessionId { get; init; }

    public int Version { get; init; }

    public string? LastUpdated { get; init; }

    public string? Summary { get; init; }

    public IReadOnlyList<CharacterRecord> Characters { get; init; } = Array.Empty<CharacterRecord>();

    public InventoryState Inventory { get; init; } = InventoryState.Empty();

    public static StoryState Empty(string sessionId, string transformName = "init") => new()
    {
        SessionId = string.IsNullOrWhiteSpace(sessionId) ? "default" : sessionId,
        Version = 0,
        Characters = Array.Empty<CharacterRecord>(),
        Inventory = InventoryState.Empty(transformName),
    };
}

public static class StoryStateJson
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    public static string Serialize(StoryState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return JsonSerializer.Serialize(state, SerializerOptions);
    }

    public static StoryState? TryDeserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<StoryState>(json, SerializerOptions);
        }
        catch
        {
            return null;
        }
    }
}
