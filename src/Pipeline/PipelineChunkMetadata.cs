namespace Narratoria.Pipeline;

public sealed record PipelineChunkMetadata(
    string? TextEncodingName = null,
    IReadOnlyDictionary<string, string>? Annotations = null)
{
    public static PipelineChunkMetadata Empty { get; } = new();

    public PipelineChunkMetadata WithAnnotation(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Annotation key cannot be empty.", nameof(key));
        }

        var merged = Annotations is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(Annotations, StringComparer.Ordinal);

        merged[key] = value;

        return this with { Annotations = merged };
    }

    public static PipelineChunkMetadata Merge(params PipelineChunkMetadata[] metadatas)
    {
        if (metadatas is null)
        {
            throw new ArgumentNullException(nameof(metadatas));
        }

        string? textEncodingName = null;
        Dictionary<string, string>? annotations = null;

        foreach (var metadata in metadatas)
        {
            if (metadata is null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(metadata.TextEncodingName))
            {
                textEncodingName = metadata.TextEncodingName;
            }

            if (metadata.Annotations is not null)
            {
                annotations ??= new(StringComparer.Ordinal);
                foreach (var kvp in metadata.Annotations)
                {
                    annotations[kvp.Key] = kvp.Value;
                }
            }
        }

        return new PipelineChunkMetadata(textEncodingName, annotations);
    }
}
