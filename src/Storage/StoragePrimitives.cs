namespace Narratoria.Storage;

public enum StorageErrorClass
{
    None = 0,
    NotSupported,
    TransactionFailure,
    SerializationError,
    MigrationFailure,
    QuotaDenied,
    QuotaUnavailable
}

public readonly record struct StorageError(StorageErrorClass ErrorClass, string Message, string? Details = null)
{
    public static StorageError NotSupported(string message, string? details = null) => new(StorageErrorClass.NotSupported, message, details);

    public static StorageError Transaction(string message, string? details = null) => new(StorageErrorClass.TransactionFailure, message, details);

    public static StorageError Serialization(string message, string? details = null) => new(StorageErrorClass.SerializationError, message, details);

    public static StorageError Migration(string message, string? details = null) => new(StorageErrorClass.MigrationFailure, message, details);

    public static StorageError Quota(string message, string? details = null) => new(StorageErrorClass.QuotaDenied, message, details);

    public static StorageError QuotaUnavailable(string message, string? details = null) => new(StorageErrorClass.QuotaUnavailable, message, details);
}

public readonly record struct StorageResult<T>(bool Ok, T? Value, StorageError? Error)
{
    public static StorageResult<T> Success(T value) => new(true, value, null);

    public static StorageResult<T> Failure(StorageError error) => new(false, default, error);
}

public readonly record struct StorageScope(string Database, string Store);

public readonly record struct QuotaReport(long UsageBytes, long QuotaBytes, long AvailableBytes, bool? CanAccommodate, string Source, string ProviderId);

public interface IStorageQuotaAwareness
{
    ValueTask<StorageResult<QuotaReport>> CheckAsync(StorageScope scope, long? requestedBytes, CancellationToken cancellationToken);
}

public readonly record struct Unit
{
    public static readonly Unit Value = new();
}
