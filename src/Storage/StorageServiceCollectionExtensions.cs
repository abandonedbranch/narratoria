using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Narratoria.Storage.IndexedDb;

namespace Narratoria.Storage;

public static class StorageServiceCollectionExtensions
{
    public static IServiceCollection AddStorageQuotaAwareness(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IStorageQuotaMetrics, LoggingStorageQuotaMetrics>();
        services.AddScoped<IStorageQuotaEstimator, IndexedDbStorageEstimateClient>();
        services.AddScoped<IStorageQuotaProvider, IndexedDbStorageQuotaProvider>();
        services.AddScoped<IStorageQuotaAwareness, StorageQuotaAwarenessService>();

        return services;
    }

    public static IServiceCollection AddIndexedDbStorageService(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IIndexedDbStorageService>(sp =>
        {
            var jsRuntime = sp.GetRequiredService<IJSRuntime>();
            var schema = sp.GetRequiredService<IndexedDbSchema>();
            var metrics = sp.GetRequiredService<IIndexedDbStorageMetrics>();
            var logger = sp.GetRequiredService<ILogger<IndexedDbStorageService>>();
            var quota = sp.GetService<IStorageQuotaAwareness>();
            return new IndexedDbStorageService(jsRuntime, schema, metrics, logger, quota);
        });

        return services;
    }
}
