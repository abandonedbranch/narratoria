using System.Text;
using Narratoria.Pipeline;

namespace Narratoria.Pipeline.Text;

public sealed class TextCollectingSink : IPipelineSink<string>
{
    private readonly StringBuilder _builder = new();

    public PipelineChunkType InputType => PipelineChunkType.Text;

    public string CollectedText => _builder.ToString();

    public async ValueTask<string> ConsumeAsync(IAsyncEnumerable<PipelineChunk> input, CancellationToken cancellationToken)
    {
        await foreach (var chunk in input.WithCancellation(cancellationToken))
        {
            if (chunk is not TextChunk textChunk)
            {
                throw new PipelineStageException(PipelineFailureKind.SinkFailed, $"Expected text chunk, got '{chunk.Type}'");
            }

            _builder.Append(textChunk.Text);
        }

        return CollectedText;
    }
}
