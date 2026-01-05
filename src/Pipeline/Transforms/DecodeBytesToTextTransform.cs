using System.Text;
using Narratoria.Pipeline;

namespace Narratoria.Pipeline.Transforms;

public sealed class DecodeBytesToTextTransform : IPipelineTransform
{
    public PipelineChunkType InputType => PipelineChunkType.Bytes;

    public PipelineChunkType OutputType => PipelineChunkType.Text;

    public async IAsyncEnumerable<PipelineChunk> TransformAsync(
        IAsyncEnumerable<PipelineChunk> input,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var chunk in input.WithCancellation(cancellationToken))
        {
            if (chunk is not BytesChunk bytesChunk)
            {
                throw new PipelineStageException(PipelineFailureKind.TransformFailed, $"Expected bytes chunk, got '{chunk.Type}'");
            }

            var encodingName = bytesChunk.Metadata.TextEncodingName;
            if (string.IsNullOrWhiteSpace(encodingName))
            {
                throw new PipelineDecodeException("Bytes chunk missing declared text encoding");
            }

            Encoding encoding;
            try
            {
                encoding = Encoding.GetEncoding(encodingName);
            }
            catch (Exception ex)
            {
                throw new PipelineDecodeException($"Unsupported encoding '{encodingName}'", ex);
            }

            string text;
            try
            {
                text = encoding.GetString(bytesChunk.Bytes.Span);
            }
            catch (Exception ex)
            {
                throw new PipelineDecodeException("Failed to decode bytes", ex);
            }

            yield return new TextChunk(text, bytesChunk.Metadata);
        }
    }
}
