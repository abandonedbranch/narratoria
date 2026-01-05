using Narratoria.Pipeline;

namespace Narratoria.Pipeline.Transforms;

public sealed class AnnotateTransform(string key, string value) : IPipelineTransform
{
    private readonly string _key = key ?? throw new ArgumentNullException(nameof(key));
    private readonly string _value = value ?? throw new ArgumentNullException(nameof(value));

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

            yield return textChunk with { Metadata = textChunk.Metadata.WithAnnotation(_key, _value) };
        }
    }
}
