using System.Text;

namespace Narratoria.Pipeline.Transforms;

public sealed class TextAccumulatorTransform : IPipelineTransform
{
    private readonly int? _maxUtf8Bytes;
    private readonly int? _maxCharacters;
    private readonly int? _maxChunks;

    public TextAccumulatorTransform(
        int? maxUtf8Bytes = null,
        int? maxCharacters = null,
        int? maxChunks = null)
    {
        if (maxUtf8Bytes is null && maxCharacters is null && maxChunks is null)
        {
            throw new ArgumentException("At least one threshold must be provided.");
        }

        _maxUtf8Bytes = ValidateThreshold(maxUtf8Bytes, nameof(maxUtf8Bytes));
        _maxCharacters = ValidateThreshold(maxCharacters, nameof(maxCharacters));
        _maxChunks = ValidateThreshold(maxChunks, nameof(maxChunks));
    }

    public PipelineChunkType InputType => PipelineChunkType.Text;

    public PipelineChunkType OutputType => PipelineChunkType.Text;

    public async IAsyncEnumerable<PipelineChunk> TransformAsync(
        IAsyncEnumerable<PipelineChunk> input,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var bufferedText = new StringBuilder();
        var bufferedChunkCount = 0;
        var bufferedUtf8Bytes = 0;
        var bufferedChars = 0;
        var bufferedMetadata = new List<PipelineChunkMetadata>();

        void ResetBuffer()
        {
            bufferedText.Clear();
            bufferedChunkCount = 0;
            bufferedUtf8Bytes = 0;
            bufferedChars = 0;
            bufferedMetadata.Clear();
        }

        await foreach (var chunk in input.WithCancellation(cancellationToken))
        {
            if (chunk is not TextChunk textChunk)
            {
                throw new PipelineStageException(PipelineFailureKind.TransformFailed, $"Expected text chunk, got '{chunk.Type}'");
            }

            bufferedText.Append(textChunk.Text);
            bufferedChunkCount++;
            bufferedUtf8Bytes += Encoding.UTF8.GetByteCount(textChunk.Text);
            bufferedChars += CountUnicodeScalarValues(textChunk.Text);
            bufferedMetadata.Add(textChunk.Metadata);

            if (ShouldFlush(bufferedUtf8Bytes, bufferedChars, bufferedChunkCount))
            {
                var mergedMetadata = PipelineChunkMetadata.Merge(bufferedMetadata.ToArray());
                var text = bufferedText.ToString();
                ResetBuffer();
                yield return new TextChunk(text, mergedMetadata);
            }
        }

        if (bufferedChunkCount > 0)
        {
            var mergedMetadata = PipelineChunkMetadata.Merge(bufferedMetadata.ToArray());
            var text = bufferedText.ToString();
            ResetBuffer();
            yield return new TextChunk(text, mergedMetadata);
        }
    }

    private bool ShouldFlush(int bufferedUtf8Bytes, int bufferedChars, int bufferedChunkCount)
    {
        var flush = false;

        if (_maxUtf8Bytes is not null)
        {
            flush |= bufferedUtf8Bytes >= _maxUtf8Bytes.Value;
        }

        if (_maxCharacters is not null)
        {
            flush |= bufferedChars >= _maxCharacters.Value;
        }

        if (_maxChunks is not null)
        {
            flush |= bufferedChunkCount >= _maxChunks.Value;
        }

        return flush;
    }

    private static int? ValidateThreshold(int? threshold, string paramName)
    {
        if (threshold is null)
        {
            return null;
        }

        if (threshold <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, threshold, "Threshold must be greater than 0.");
        }

        return threshold.Value;
    }

    private static int CountUnicodeScalarValues(string value)
    {
        var count = 0;
        foreach (var _ in value.EnumerateRunes())
        {
            count++;
        }

        return count;
    }
}
