namespace Narratoria.Pipeline;

public readonly record struct PipelineChunkType
{
    public static PipelineChunkType Bytes { get; } = new("bytes");
    public static PipelineChunkType Text { get; } = new("text");

    public PipelineChunkType(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Chunk type name cannot be empty.", nameof(name));
        }

        Name = name;
    }

    public string Name { get; }

    public override string ToString() => Name;
}
