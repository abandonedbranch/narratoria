using System.Text.Json;
using System.Text.Json.Serialization;

namespace Narratoria.Pipeline.Transforms.Llm.StoryState;

public enum InventoryItemOperation
{
    Upsert = 0,
    Remove = 1,
}

public sealed record InventoryItemUpdate
{
    public required InventoryItemOperation Operation { get; init; }

    public required InventoryItem Item { get; init; }
}

public sealed record StoryStateUpdate
{
    public string? Summary { get; init; }

    public IReadOnlyList<CharacterRecord>? CharactersToUpsert { get; init; }

    public IReadOnlyList<InventoryItemUpdate>? InventoryUpdates { get; init; }
}

public static class StoryStateUpdateJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static bool TryDeserialize(string json, out StoryStateUpdate? update)
    {
        update = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            update = JsonSerializer.Deserialize<StoryStateUpdate>(json, Options);
            return update is not null;
        }
        catch
        {
            return false;
        }
    }
}
