using System;
using System.Threading;

namespace NarratoriaClient.Services;

public enum StorageArea
{
    Local,
    Session
}

public interface IClientStorageService : IAsyncDisposable
{
    Task<string?> GetItemAsync(StorageArea area, string key, CancellationToken cancellationToken = default);
    Task SetItemAsync(StorageArea area, string key, string? value, CancellationToken cancellationToken = default);
}

public sealed class ClientStorageUnavailableException : Exception
{
    public ClientStorageUnavailableException(Exception innerException)
        : base("Client storage is currently unavailable.", innerException)
    {
    }
}
