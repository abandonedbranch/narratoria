namespace Narratoria.Pipeline;

public abstract record PipelineChunk(PipelineChunkType Type, PipelineChunkMetadata Metadata)
{
    public PipelineChunkMetadata Metadata { get; init; } = Metadata ?? PipelineChunkMetadata.Empty;
}

public sealed record BytesChunk(ReadOnlyMemory<byte> Bytes, PipelineChunkMetadata Metadata)
    : PipelineChunk(PipelineChunkType.Bytes, Metadata);

public sealed record TextChunk(string Text, PipelineChunkMetadata Metadata)
    : PipelineChunk(PipelineChunkType.Text, Metadata);
