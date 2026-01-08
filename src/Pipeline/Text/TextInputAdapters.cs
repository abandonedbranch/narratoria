namespace Narratoria.Pipeline.Text;

public static class TextInputAdapters
{
    public static IAsyncEnumerable<string> FromString(string value) => new SingleAsyncEnumerable<string>(value);

    public static IAsyncEnumerable<ReadOnlyMemory<byte>> FromBytes(params byte[][] chunks)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        return new EnumerableAsyncEnumerable<ReadOnlyMemory<byte>>(chunks.Select(static c => (ReadOnlyMemory<byte>)c));
    }

    private sealed class SingleAsyncEnumerable<T>(T item) : IAsyncEnumerable<T>
    {
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new Enumerator(item, cancellationToken);

        private sealed class Enumerator(T item, CancellationToken cancellationToken) : IAsyncEnumerator<T>
        {
            private bool _moved;

            public T Current => item;

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;

            public ValueTask<bool> MoveNextAsync()
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_moved)
                {
                    return new ValueTask<bool>(false);
                }

                _moved = true;
                return new ValueTask<bool>(true);
            }
        }
    }

    private sealed class EnumerableAsyncEnumerable<T>(IEnumerable<T> items) : IAsyncEnumerable<T>
    {
        private readonly IEnumerable<T> _items = items ?? throw new ArgumentNullException(nameof(items));

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new Enumerator(_items.GetEnumerator(), cancellationToken);

        private sealed class Enumerator(IEnumerator<T> enumerator, CancellationToken cancellationToken) : IAsyncEnumerator<T>
        {
            private readonly IEnumerator<T> _enumerator = enumerator;
            private readonly CancellationToken _cancellationToken = cancellationToken;

            public T Current => _enumerator.Current;

            public ValueTask DisposeAsync()
            {
                _enumerator.Dispose();
                return ValueTask.CompletedTask;
            }

            public ValueTask<bool> MoveNextAsync()
            {
                _cancellationToken.ThrowIfCancellationRequested();
                return new ValueTask<bool>(_enumerator.MoveNext());
            }
        }
    }
}
