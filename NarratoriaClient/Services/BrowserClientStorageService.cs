using Microsoft.JSInterop;
using System.Threading;

namespace NarratoriaClient.Services;

public sealed class BrowserClientStorageService : IClientStorageService
{
    private readonly IJSRuntime _js;
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    public BrowserClientStorageService(IJSRuntime js)
    {
        _js = js;
        _moduleTask = new(() => _js.InvokeAsync<IJSObjectReference>("import", "./js/storage.js").AsTask());
    }

    public async Task<string?> GetItemAsync(StorageArea area, string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var module = await _moduleTask.Value;
            return await module.InvokeAsync<string?>("getItem", cancellationToken, GetAreaName(area), key);
        }
        catch (InvalidOperationException ex)
        {
            throw new ClientStorageUnavailableException(ex);
        }
        catch (JSException ex)
        {
            throw new ClientStorageUnavailableException(ex);
        }
    }

    public async Task SetItemAsync(StorageArea area, string key, string? value, CancellationToken cancellationToken = default)
    {
        try
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("setItem", cancellationToken, GetAreaName(area), key, value);
        }
        catch (InvalidOperationException ex)
        {
            throw new ClientStorageUnavailableException(ex);
        }
        catch (JSException ex)
        {
            throw new ClientStorageUnavailableException(ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
        {
            try
            {
                var module = await _moduleTask.Value;
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Circuit ended; JS runtime is gone.
            }
        }
    }

    private static string GetAreaName(StorageArea area)
    {
        return area switch
        {
            StorageArea.Local => "local",
            StorageArea.Session => "session",
            _ => "local"
        };
    }
}
