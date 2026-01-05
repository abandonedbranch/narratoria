namespace Narratoria.Pipeline;

public interface IPipelineSink<TSinkResult>
{
    PipelineChunkType InputType { get; }

    ValueTask<TSinkResult> ConsumeAsync(IAsyncEnumerable<PipelineChunk> input, CancellationToken cancellationToken);
}
