using Narratoria.Pipeline;

namespace Narratoria.Pipeline.Text;

public sealed class TextPromptSource(TextSourceConfig config) : IPipelineSource
{
    private readonly TextSourceConfig _config = config ?? throw new ArgumentNullException(nameof(config));

    public PipelineChunkType OutputType
    {
        get
        {
            if (_config.ByteStream is not null)
            {
                return PipelineChunkType.Bytes;
            }

            return PipelineChunkType.Text;
        }
    }

    public async IAsyncEnumerable<PipelineChunk> StreamAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (_config.ByteStream is not null)
        {
            if (string.IsNullOrWhiteSpace(_config.ByteStreamEncodingName))
            {
                throw new PipelineStageException(PipelineFailureKind.SourceFailed, "Byte stream requires an explicit encoding name");
            }

            var metadata = new PipelineChunkMetadata(TextEncodingName: _config.ByteStreamEncodingName);

            await foreach (var bytes in _config.ByteStream.WithCancellation(cancellationToken))
            {
                yield return new BytesChunk(bytes, metadata);
            }

            yield break;
        }

        var textStream = _config.TextStream;

        if (_config.CompleteText is not null)
        {
            textStream = TextInputAdapters.FromString(_config.CompleteText);
        }

        if (textStream is null)
        {
            throw new PipelineStageException(PipelineFailureKind.SourceFailed, "Text source requires either CompleteText, TextStream, or ByteStream");
        }

        await foreach (var text in textStream.WithCancellation(cancellationToken))
        {
            yield return new TextChunk(text, PipelineChunkMetadata.Empty);
        }
    }
}
