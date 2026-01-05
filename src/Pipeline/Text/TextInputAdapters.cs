namespace Narratoria.Pipeline.Text;

public static class TextInputAdapters
{
    public static async IAsyncEnumerable<string> FromString(string value)
    {
        yield return value;
        await Task.CompletedTask;
    }

    public static async IAsyncEnumerable<ReadOnlyMemory<byte>> FromBytes(params byte[][] chunks)
    {
        foreach (var chunk in chunks)
        {
            yield return chunk;
            await Task.CompletedTask;
        }
    }
}
