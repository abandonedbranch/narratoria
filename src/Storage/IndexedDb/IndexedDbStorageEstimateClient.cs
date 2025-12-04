using Microsoft.JSInterop;
using Narratoria.Storage;

namespace Narratoria.Storage.IndexedDb;

public sealed class IndexedDbStorageEstimateClient : IStorageQuotaEstimator, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly SemaphoreSlim _moduleLock = new(1, 1);
    private IJSObjectReference? _module;
    private bool _disposed;

    public IndexedDbStorageEstimateClient(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    }

    public async ValueTask<StorageResult<StorageEstimateSnapshot>> EstimateAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        var module = await EnsureModuleAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var response = await module.InvokeAsync<StorageEstimateResponse>("estimate", cancellationToken).ConfigureAwait(false);
            var usageBytes = Normalize(response.Usage);
            var quotaBytes = Normalize(response.Quota);
            var source = string.IsNullOrWhiteSpace(response.Source) ? "storage-manager" : response.Source;
            return StorageResult<StorageEstimateSnapshot>.Success(new StorageEstimateSnapshot(usageBytes, quotaBytes, source));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (JSException ex)
        {
            var message = ex.Message ?? string.Empty;
            if (message.Contains("NotSupported", StringComparison.OrdinalIgnoreCase))
            {
                return StorageResult<StorageEstimateSnapshot>.Failure(StorageError.NotSupported("StorageManager not supported", ex.Message));
            }

            return StorageResult<StorageEstimateSnapshot>.Failure(StorageError.ProviderFailure("Storage estimate failed", ex.Message));
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
                _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, "./js/storageQuota.js").ConfigureAwait(false);
            }
        }
        finally
        {
            _moduleLock.Release();
        }

        return _module;
    }

    private static long? Normalize(double? value)
    {
        if (value is null || !double.IsFinite(value.Value) || value.Value < 0)
        {
            return null;
        }

        try
        {
            return Convert.ToInt64(value.Value);
        }
        catch (OverflowException)
        {
            return null;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(IndexedDbStorageEstimateClient));
        }
    }

    private sealed record StorageEstimateResponse(double? Usage, double? Quota, string? Source);
}
