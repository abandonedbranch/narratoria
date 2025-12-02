namespace Narratoria.Storage.IndexedDb;

public readonly record struct IndexedDbSerializedValue(byte[] Payload, long? DeclaredSizeBytes)
{
    public long SizeBytes => DeclaredSizeBytes ?? Payload.LongLength;
}

public interface IIndexedDbValueSerializer<T>
{
    ValueTask<IndexedDbSerializedValue> SerializeAsync(T value, CancellationToken cancellationToken);

    ValueTask<T> DeserializeAsync(IndexedDbSerializedValue payload, CancellationToken cancellationToken);
}
