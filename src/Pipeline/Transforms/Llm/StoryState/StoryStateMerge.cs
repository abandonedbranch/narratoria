namespace Narratoria.Pipeline.Transforms.Llm.StoryState;

public static class StoryStateMerge
{
    public static StoryState ApplyUpdate(StoryState current, StoryStateUpdate update, string transformName, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(update);

        var nextVersion = current.Version + 1;

        var mergedSummary = update.Summary is null
            ? current.Summary
            : update.Summary;

        var mergedCharacters = MergeCharacters(current.Characters, update.CharactersToUpsert);
        var mergedInventory = MergeInventory(current.Inventory, update.InventoryUpdates, transformName);

        return current with
        {
            Version = nextVersion,
            LastUpdated = now.ToString("O"),
            Summary = mergedSummary,
            Characters = mergedCharacters,
            Inventory = mergedInventory,
        };
    }

    private static IReadOnlyList<CharacterRecord> MergeCharacters(
        IReadOnlyList<CharacterRecord> current,
        IReadOnlyList<CharacterRecord>? updates)
    {
        if (updates is null || updates.Count == 0)
        {
            return current;
        }

        var byId = new Dictionary<string, CharacterRecord>(StringComparer.Ordinal);
        foreach (var existing in current)
        {
            byId[existing.Id] = existing;
        }

        foreach (var incoming in updates)
        {
            if (byId.TryGetValue(incoming.Id, out var existing))
            {
                byId[incoming.Id] = MergeCharacter(existing, incoming);
            }
            else
            {
                byId[incoming.Id] = incoming;
            }
        }

        return byId.Values.OrderBy(c => c.Id, StringComparer.Ordinal).ToArray();
    }

    private static CharacterRecord MergeCharacter(CharacterRecord existing, CharacterRecord incoming)
    {
        // Non-destructive: only overwrite fields when incoming has >= confidence.
        var existingConfidence = existing.Provenance.Confidence;
        var incomingConfidence = incoming.Provenance.Confidence;

        if (incomingConfidence < existingConfidence)
        {
            // Still allow merging in new traits/aliases that don't conflict.
            return existing with
            {
                Aliases = MergeLists(existing.Aliases, incoming.Aliases),
                Traits = MergeMaps(existing.Traits, incoming.Traits, overwrite: false),
                Relationships = MergeLists(existing.Relationships, incoming.Relationships),
            };
        }

        return existing with
        {
            DisplayName = incoming.DisplayName,
            Aliases = MergeLists(existing.Aliases, incoming.Aliases),
            Traits = MergeMaps(existing.Traits, incoming.Traits, overwrite: true),
            Relationships = MergeLists(existing.Relationships, incoming.Relationships),
            LastSeen = incoming.LastSeen ?? existing.LastSeen,
            Provenance = incoming.Provenance,
        };
    }

    private static InventoryState MergeInventory(
        InventoryState current,
        IReadOnlyList<InventoryItemUpdate>? updates,
        string transformName)
    {
        if (updates is null || updates.Count == 0)
        {
            return current;
        }

        var byId = new Dictionary<string, InventoryItem>(StringComparer.Ordinal);
        foreach (var existing in current.Items)
        {
            byId[existing.Id] = existing;
        }

        foreach (var update in updates)
        {
            var incoming = update.Item;

            if (update.Operation == InventoryItemOperation.Remove)
            {
                byId.Remove(incoming.Id);
                continue;
            }

            if (byId.TryGetValue(incoming.Id, out var existing))
            {
                byId[incoming.Id] = MergeInventoryItem(existing, incoming);
            }
            else
            {
                byId[incoming.Id] = incoming;
            }
        }

        return current with
        {
            Items = byId.Values.OrderBy(i => i.Id, StringComparer.Ordinal).ToArray(),
            Provenance = new TransformProvenance { TransformName = transformName, Confidence = 1.0 },
        };
    }

    private static InventoryItem MergeInventoryItem(InventoryItem existing, InventoryItem incoming)
    {
        var existingConfidence = existing.Provenance.Confidence;
        var incomingConfidence = incoming.Provenance.Confidence;

        if (incomingConfidence < existingConfidence)
        {
            return existing;
        }

        return existing with
        {
            DisplayName = incoming.DisplayName,
            Quantity = incoming.Quantity ?? existing.Quantity,
            Notes = incoming.Notes ?? existing.Notes,
            Provenance = incoming.Provenance,
        };
    }

    private static IReadOnlyList<T>? MergeLists<T>(IReadOnlyList<T>? existing, IReadOnlyList<T>? incoming)
    {
        if (existing is null || existing.Count == 0)
        {
            return incoming;
        }

        if (incoming is null || incoming.Count == 0)
        {
            return existing;
        }

        var merged = new List<T>(existing.Count + incoming.Count);
        merged.AddRange(existing);
        merged.AddRange(incoming);
        return merged;
    }

    private static IReadOnlyDictionary<string, string>? MergeMaps(
        IReadOnlyDictionary<string, string>? existing,
        IReadOnlyDictionary<string, string>? incoming,
        bool overwrite)
    {
        if (existing is null || existing.Count == 0)
        {
            return incoming;
        }

        if (incoming is null || incoming.Count == 0)
        {
            return existing;
        }

        var merged = new Dictionary<string, string>(existing, StringComparer.Ordinal);
        foreach (var kvp in incoming)
        {
            if (overwrite || !merged.ContainsKey(kvp.Key))
            {
                merged[kvp.Key] = kvp.Value;
            }
        }

        return merged;
    }
}
