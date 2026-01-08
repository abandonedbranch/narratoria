namespace Narratoria.Pipeline;

public interface IPipelineTransform
{
    PipelineChunkType InputType { get; }

    PipelineChunkType OutputType { get; }

    IAsyncEnumerable<PipelineChunk> TransformAsync(
        IAsyncEnumerable<PipelineChunk> input,
        CancellationToken cancellationToken);
}
