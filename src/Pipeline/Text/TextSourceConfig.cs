namespace Narratoria.Pipeline.Text;

public sealed record TextSourceConfig
{
    public string? CompleteText { get; init; }

    public IAsyncEnumerable<string>? TextStream { get; init; }

    public IAsyncEnumerable<ReadOnlyMemory<byte>>? ByteStream { get; init; }

    public string? ByteStreamEncodingName { get; init; }
}
