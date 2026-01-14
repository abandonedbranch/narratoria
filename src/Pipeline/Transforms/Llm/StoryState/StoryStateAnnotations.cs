namespace Narratoria.Pipeline.Transforms.Llm.StoryState;

public static class StoryStateAnnotations
{
    public const string StoryStateJsonKey = "narratoria.story_state_json";

    public const string StoryStateSchemaVersionKey = "narratoria.story_state_schema_version";

    public const string StoryStateSchemaVersionValue = "specs/002-llm-story-transforms/contracts/story-state.schema.json";

    public const string OriginalTextKey = "narratoria.original_text";

    public const string SessionIdKey = "narratoria.session_id";

    public const string TurnIndexKey = "narratoria.turn_index";

    public static StoryState ReadOrCreate(PipelineChunkMetadata metadata, string transformName)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var sessionId = TryGetAnnotation(metadata, SessionIdKey) ?? "default";

        if (TryGetAnnotation(metadata, StoryStateJsonKey) is string json)
        {
            var parsed = StoryStateJson.TryDeserialize(json);
            if (parsed is not null)
            {
                return parsed;
            }
        }

        return StoryState.Empty(sessionId, transformName);
    }

    public static PipelineChunkMetadata Write(PipelineChunkMetadata metadata, StoryState state)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(state);

        var json = StoryStateJson.Serialize(state);

        return metadata
            .WithAnnotation(StoryStateJsonKey, json)
            .WithAnnotation(StoryStateSchemaVersionKey, StoryStateSchemaVersionValue);
    }

    public static string? TryGetAnnotation(PipelineChunkMetadata metadata, string key)
    {
        if (metadata.Annotations is null)
        {
            return null;
        }

        return metadata.Annotations.TryGetValue(key, out var value) ? value : null;
    }

    public static int? TryGetTurnIndex(PipelineChunkMetadata metadata)
    {
        if (TryGetAnnotation(metadata, TurnIndexKey) is not string raw)
        {
            return null;
        }

        return int.TryParse(raw, out var value) && value >= 0 ? value : null;
    }
}
