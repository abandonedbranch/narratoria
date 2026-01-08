namespace Narratoria.Pipeline.Transforms;

public sealed class PrefixTextTransform(string prefix) : IPipelineTransform
{
    private readonly string _prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));

    public PipelineChunkType InputType => PipelineChunkType.Text;

    public PipelineChunkType OutputType => PipelineChunkType.Text;

    public async IAsyncEnumerable<PipelineChunk> TransformAsync(
        IAsyncEnumerable<PipelineChunk> input,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var chunk in input.WithCancellation(cancellationToken))
        {
            if (chunk is not TextChunk textChunk)
            {
                throw new PipelineStageException(PipelineFailureKind.TransformFailed, $"Expected text chunk, got '{chunk.Type}'");
            }

            yield return new TextChunk(_prefix + textChunk.Text, textChunk.Metadata);
        }
    }
}
