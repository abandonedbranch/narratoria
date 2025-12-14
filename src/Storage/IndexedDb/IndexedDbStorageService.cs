using System.Diagnostics;
using Microsoft.JSInterop;

namespace Narratoria.Storage.IndexedDb;

public sealed class IndexedDbStorageService : IIndexedDbStorageService, IAsyncDisposable
{
    private static readonly IReadOnlyDictionary<string, object?> EmptyIndexValues = new Dictionary<string, object?>(StringComparer.Ordinal);

    private readonly IJSRuntime _jsRuntime;
    private readonly IndexedDbSchema _schema;
    private readonly IIndexedDbStorageMetrics _metrics;
    private readonly ILogger<IndexedDbStorageService> _logger;
    private readonly SemaphoreSlim _moduleLock = new(1, 1);

    private IJSObjectReference? _module;
    private bool _disposed;

    public IndexedDbStorageService(IJSRuntime jsRuntime, IndexedDbSchema schema, IIndexedDbStorageMetrics metrics, ILogger<IndexedDbStorageService> logger)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<StorageResult<Unit>> PutAsync<T>(IndexedDbPutRequest<T> request, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Serializer);
        cancellationToken.ThrowIfCancellationRequested();

        var traceId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        IndexedDbSerializedValue serialized;

        try
        {
            serialized = await request.Serializer.SerializeAsync(request.Value, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var error = StorageError.Serialization("Failed to serialize payload", ex.Message);
            _metrics.RecordResult("put", request.Store.Name, "failure", error.ErrorClass.ToString());
            _logger.LogError(ex, "IndexedDB put serialization failure trace={TraceId} request={RequestId} store={Store} key={Key}", traceId, requestId, request.Store.Name, request.Key);
            return StorageResult<Unit>.Failure(error);
        }

        if (serialized.Payload is null)
        {
            var error = StorageError.Serialization("Serializer returned null payload");
            _metrics.RecordResult("put", request.Store.Name, "failure", error.ErrorClass.ToString());
            _logger.LogError("IndexedDB put serialization returned null payload trace={TraceId} request={RequestId} store={Store} key={Key}", traceId, requestId, request.Store.Name, request.Key);
            return StorageResult<Unit>.Failure(error);
        }

        var putRequest = new IndexedDbPutSerializedRequest
        {
            Store = request.Store,
            Key = request.Key,
            Payload = serialized.Payload,
            SizeBytes = serialized.SizeBytes,
            IndexValues = request.IndexValues,
            Scope = request.Scope
        };

        return await PutSerializedAsync(putRequest, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<StorageResult<Unit>> PutSerializedAsync(IndexedDbPutSerializedRequest request, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var traceId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        if (request.Payload is null)
        {
            var error = StorageError.Serialization("Payload is required");
            _metrics.RecordResult("put", request.Store.Name, "failure", error.ErrorClass.ToString());
            _logger.LogError("IndexedDB put payload missing trace={TraceId} request={RequestId} store={Store} key={Key}", traceId, requestId, request.Store.Name, request.Key);
            return StorageResult<Unit>.Failure(error);
        }

        var module = await EnsureModuleAsync(cancellationToken).ConfigureAwait(false);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var args = new PutArgs(_schema, request.Store.Name, request.Store.KeyPath, request.Store.AutoIncrement, request.Key, request.Payload, request.IndexValues ?? EmptyIndexValues);
            await module.InvokeVoidAsync("put", cancellationToken, args).ConfigureAwait(false);
            stopwatch.Stop();

            _metrics.RecordLatency("put", request.Store.Name, stopwatch.Elapsed);
            _metrics.RecordResult("put", request.Store.Name, "success", StorageErrorClass.None.ToString());
            _metrics.RecordBytesWritten(request.SizeBytes > 0 ? request.SizeBytes : request.Payload.LongLength);
            _logger.LogInformation("IndexedDB put success trace={TraceId} request={RequestId} store={Store} key={Key} elapsedMs={ElapsedMs}", traceId, requestId, request.Store.Name, request.Key, stopwatch.Elapsed.TotalMilliseconds);
            return StorageResult<Unit>.Success(Unit.Value);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _metrics.RecordResult("put", request.Store.Name, "failure", StorageErrorClass.TransactionFailure.ToString());
            _logger.LogWarning("IndexedDB put canceled trace={TraceId} request={RequestId} store={Store} key={Key}", traceId, requestId, request.Store.Name, request.Key);
            throw;
        }
        catch (JSException ex)
        {
            stopwatch.Stop();
            var error = MapJsError(ex);
            _metrics.RecordResult("put", request.Store.Name, "failure", error.ErrorClass.ToString());
            _logger.LogError(ex, "IndexedDB put failure trace={TraceId} request={RequestId} store={Store} key={Key} errorClass={ErrorClass}", traceId, requestId, request.Store.Name, request.Key, error.ErrorClass);
            return StorageResult<Unit>.Failure(error);
        }
    }

    public async ValueTask<StorageResult<IReadOnlyList<IndexedDbRecord<T>>>> ListAsync<T>(IndexedDbListRequest<T> request, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Serializer);
        cancellationToken.ThrowIfCancellationRequested();

        var traceId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var module = await EnsureModuleAsync(cancellationToken).ConfigureAwait(false);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var args = new ListArgs(_schema, request.Store.Name, request.Store.KeyPath, request.Query);
            var rawRecords = await module.InvokeAsync<SerializedRecord[]>("list", cancellationToken, args).ConfigureAwait(false) ?? Array.Empty<SerializedRecord>();

            var results = new List<IndexedDbRecord<T>>(rawRecords.Length);
            foreach (var record in rawRecords)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var serialized = new IndexedDbSerializedValue(record.Payload ?? Array.Empty<byte>(), record.Payload?.Length);
                    var value = await request.Serializer.DeserializeAsync(serialized, cancellationToken).ConfigureAwait(false);
                    results.Add(new IndexedDbRecord<T>(record.Key, value));
                    _metrics.RecordBytesRead(serialized.Payload.Length);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    var error = StorageError.Serialization("Failed to deserialize payload", ex.Message);
                    _metrics.RecordResult("list", request.Store.Name, "failure", error.ErrorClass.ToString());
                    _logger.LogError(ex, "IndexedDB list deserialize failure trace={TraceId} request={RequestId} store={Store}", traceId, requestId, request.Store.Name);
                    return StorageResult<IReadOnlyList<IndexedDbRecord<T>>>.Failure(error);
                }
            }

            stopwatch.Stop();
            _metrics.RecordLatency("list", request.Store.Name, stopwatch.Elapsed);
            _metrics.RecordResult("list", request.Store.Name, "success", StorageErrorClass.None.ToString());
            _logger.LogInformation("IndexedDB list success trace={TraceId} request={RequestId} store={Store} count={Count} elapsedMs={ElapsedMs}", traceId, requestId, request.Store.Name, results.Count, stopwatch.Elapsed.TotalMilliseconds);
            return StorageResult<IReadOnlyList<IndexedDbRecord<T>>>.Success(results);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _metrics.RecordResult("list", request.Store.Name, "failure", StorageErrorClass.TransactionFailure.ToString());
            _logger.LogWarning("IndexedDB list canceled trace={TraceId} request={RequestId} store={Store}", traceId, requestId, request.Store.Name);
            throw;
        }
        catch (JSException ex)
        {
            stopwatch.Stop();
            var error = MapJsError(ex);
            _metrics.RecordResult("list", request.Store.Name, "failure", error.ErrorClass.ToString());
            _logger.LogError(ex, "IndexedDB list failure trace={TraceId} request={RequestId} store={Store} errorClass={ErrorClass}", traceId, requestId, request.Store.Name, error.ErrorClass);
            return StorageResult<IReadOnlyList<IndexedDbRecord<T>>>.Failure(error);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_module is not null)
        {
            await _module.DisposeAsync().ConfigureAwait(false);
        }

        _moduleLock.Dispose();
        GC.SuppressFinalize(this);
    }

    private async ValueTask<IJSObjectReference> EnsureModuleAsync(CancellationToken cancellationToken)
    {
        if (_module is not null)
        {
            return _module;
        }

        await _moduleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_module is null)
            {
                _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, "./js/indexedDbStorage.js").ConfigureAwait(false);
            }
        }
        finally
        {
            _moduleLock.Release();
        }

        return _module;
    }

    private static StorageError MapJsError(JSException exception)
    {
        var message = exception.Message ?? "IndexedDB failure";
        var details = exception.StackTrace;
        if (message.Contains("NotSupportedError", StringComparison.OrdinalIgnoreCase) || message.Contains("indexedDB is not defined", StringComparison.OrdinalIgnoreCase))
        {
            return StorageError.NotSupported(message, details);
        }

        if (message.Contains("VersionError", StringComparison.OrdinalIgnoreCase) || message.Contains("upgrade", StringComparison.OrdinalIgnoreCase))
        {
            return StorageError.Migration(message, details);
        }

        return StorageError.Transaction(message, details);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(IndexedDbStorageService));
        }
    }

    private sealed record PutArgs(IndexedDbSchema Schema, string StoreName, string KeyPath, bool AutoIncrement, string Key, byte[] Payload, IReadOnlyDictionary<string, object?> IndexValues);

    private sealed record ListArgs(IndexedDbSchema Schema, string StoreName, string KeyPath, IndexedDbQueryOptions Query);

    private sealed record SerializedRecord(string Key, byte[] Payload);
}
