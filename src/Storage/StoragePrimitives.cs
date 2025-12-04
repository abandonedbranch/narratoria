namespace Narratoria.Storage;

public enum StorageErrorClass
{
    None = 0,
    NotSupported,
    TransactionFailure,
    SerializationError,
    MigrationFailure,
    QuotaDenied,
    QuotaUnavailable,
    CalculationError,
    EstimateUnavailable,
    ProviderFailure
}

public readonly record struct StorageError(StorageErrorClass ErrorClass, string Message, string? Details = null)
{
    public static StorageError NotSupported(string message, string? details = null) => new(StorageErrorClass.NotSupported, message, details);

    public static StorageError Transaction(string message, string? details = null) => new(StorageErrorClass.TransactionFailure, message, details);

    public static StorageError Serialization(string message, string? details = null) => new(StorageErrorClass.SerializationError, message, details);

    public static StorageError Migration(string message, string? details = null) => new(StorageErrorClass.MigrationFailure, message, details);

    public static StorageError Quota(string message, string? details = null) => new(StorageErrorClass.QuotaDenied, message, details);

    public static StorageError QuotaUnavailable(string message, string? details = null) => new(StorageErrorClass.QuotaUnavailable, message, details);

    public static StorageError Calculation(string message, string? details = null) => new(StorageErrorClass.CalculationError, message, details);

    public static StorageError EstimateUnavailable(string message, string? details = null) => new(StorageErrorClass.EstimateUnavailable, message, details);

    public static StorageError ProviderFailure(string message, string? details = null) => new(StorageErrorClass.ProviderFailure, message, details);
}

public readonly record struct StorageResult<T>(bool Ok, T? Value, StorageError? Error)
{
    public static StorageResult<T> Success(T value) => new(true, value, null);

    public static StorageResult<T> Failure(StorageError error) => new(false, default, error);
}

public readonly record struct StorageScope(string Database, string Store);

public readonly record struct QuotaReport(long UsageBytes, long QuotaBytes, long AvailableBytes, bool? CanAccommodate, string Source, string ProviderId);

public readonly record struct StorageQuotaEstimate(long? UsageBytes, long? QuotaBytes, string Source, string ProviderId);

public readonly record struct StorageQuotaEstimationHints(string? StoreName, string? PayloadDescriptor);

public interface IStorageQuotaProvider
{
    ValueTask<StorageResult<StorageQuotaEstimate>> EstimateAsync(StorageScope scope, StorageQuotaEstimationHints? hints, CancellationToken cancellationToken);
}

public interface IStorageQuotaMetrics
{
    void RecordLookup(TimeSpan duration, string status, StorageErrorClass errorClass, string providerId, string source);

    void RecordUsage(long usedBytes, long availableBytes, string providerId, string source);

    void RecordCanAccommodate(bool canAccommodate, string providerId, string source);
}

public interface IStorageQuotaAwareness
{
    ValueTask<StorageResult<QuotaReport>> CheckAsync(StorageScope scope, long? requestedBytes, StorageQuotaEstimationHints? hints, CancellationToken cancellationToken);
}

public readonly record struct Unit
{
    public static readonly Unit Value = new();
}
