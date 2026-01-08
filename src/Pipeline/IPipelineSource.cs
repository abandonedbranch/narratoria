namespace Narratoria.Pipeline;

public interface IPipelineSource
{
    PipelineChunkType OutputType { get; }

    IAsyncEnumerable<PipelineChunk> StreamAsync(CancellationToken cancellationToken);
}
