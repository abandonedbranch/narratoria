using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Narration;
using Narratoria.Narration.Attachments;
using Narratoria.Storage;
using Narratoria.Storage.IndexedDb;

namespace Narratoria.Tests;

[TestClass]
public class ProgramStartupSmokeTests
{
    private sealed class InMemoryIndexedDbStorage : IIndexedDbStorageService, IIndexedDbStorageWithQuota
    {
        private readonly ConcurrentDictionary<string, byte[]> _store = new(StringComparer.Ordinal);

        public ValueTask<StorageResult<Unit>> DeleteAsync(IndexedDbDeleteRequest request, CancellationToken cancellationToken)
        {
            _store.TryRemove(request.Key, out _);
            return ValueTask.FromResult(StorageResult<Unit>.Success(Unit.Value));
        }

        public ValueTask<StorageResult<T?>> GetAsync<T>(IndexedDbGetRequest<T> request, CancellationToken cancellationToken)
        {
            if (_store.TryGetValue(request.Key, out var payload))
            {
                var value = request.Serializer.DeserializeAsync(new IndexedDbSerializedValue(payload, payload.Length), cancellationToken).GetAwaiter().GetResult();
                return ValueTask.FromResult(StorageResult<T?>.Success(value));
            }

            return ValueTask.FromResult(StorageResult<T?>.Success(default));
        }

        public ValueTask<StorageResult<IReadOnlyList<IndexedDbRecord<T>>>> ListAsync<T>(IndexedDbListRequest<T> request, CancellationToken cancellationToken)
        {
            var results = new List<IndexedDbRecord<T>>();
            foreach (var kvp in _store)
            {
                var value = request.Serializer.DeserializeAsync(new IndexedDbSerializedValue(kvp.Value, kvp.Value.Length), cancellationToken).GetAwaiter().GetResult();
                results.Add(new IndexedDbRecord<T>(kvp.Key, value));
            }
            return ValueTask.FromResult(StorageResult<IReadOnlyList<IndexedDbRecord<T>>>.Success(results));
        }

        public ValueTask<StorageResult<Unit>> PutAsync<T>(IndexedDbPutRequest<T> request, CancellationToken cancellationToken)
        {
            var serialized = request.Serializer.SerializeAsync(request.Value, cancellationToken).GetAwaiter().GetResult();
            _store[request.Key] = serialized.Payload ?? Array.Empty<byte>();
            return ValueTask.FromResult(StorageResult<Unit>.Success(Unit.Value));
        }

        public ValueTask<StorageResult<Unit>> PutIfCanAccommodateAsync<T>(IndexedDbPutRequest<T> request, CancellationToken cancellationToken)
        {
            return PutAsync(request, cancellationToken);
        }

        public ValueTask<StorageResult<Unit>> PutSerializedAsync(IndexedDbPutSerializedRequest request, CancellationToken cancellationToken)
        {
            _store[request.Key] = request.Payload ?? Array.Empty<byte>();
            return ValueTask.FromResult(StorageResult<Unit>.Success(Unit.Value));
        }
    }

    private sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseDefaultServiceProvider((_, options) =>
            {
                options.ValidateScopes = false;
                options.ValidateOnBuild = false;
            });

            builder.ConfigureAppConfiguration(cfg =>
            {
                var settings = new Dictionary<string, string?>
                {
                    ["OpenAi:ApiKey"] = "test-key",
                    ["OpenAi:Model"] = "test-model",
                    ["OpenAi:Endpoint"] = "https://api.openai.com/v1",
                    ["ProviderDispatch:Timeout"] = "00:00:10",
                    ["ProviderDispatch:SystemModel"] = "test-system-model",
                    ["SystemPromptProfile:ProfileId"] = "test-profile",
                    ["SystemPromptProfile:PromptText"] = "system prompt",
                    ["SystemPromptProfile:Version"] = "v1"
                };
                cfg.AddInMemoryCollection(settings!);
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IIndexedDbStorageService>();
                services.RemoveAll<IIndexedDbStorageWithQuota>();
                services.RemoveAll<IIndexedDbStorageMetrics>();
                services.AddSingleton<IIndexedDbStorageMetrics, NoOpIndexedDbMetrics>();
                services.AddSingleton<IIndexedDbStorageService, InMemoryIndexedDbStorage>();
                services.AddSingleton<IIndexedDbStorageWithQuota>(sp => (InMemoryIndexedDbStorage)sp.GetRequiredService<IIndexedDbStorageService>());
            });
        }
    }

    private sealed class NoOpIndexedDbMetrics : IIndexedDbStorageMetrics
    {
        public void RecordBytesRead(long bytes) { }
        public void RecordBytesWritten(long bytes) { }
        public void RecordLatency(string operation, string store, TimeSpan duration) { }
        public void RecordResult(string operation, string store, string status, string errorClass) { }
    }

    [TestMethod]
    public async Task Program_Startup_SmokeTest_ServiceGraphBuilds()
    {
        await using var factory = new TestWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Resolve a few critical services to ensure DI graph is intact
        _ = services.GetRequiredService<NarrationPipelineService>();
        _ = services.GetRequiredService<INarrationPipelineFactory>();
        _ = services.GetRequiredService<INarrationSessionStore>();
        _ = services.GetRequiredService<IAttachmentIngestionService>();

        // Issue a trivial request to verify app startup pipeline
        var client = factory.CreateClient();
        var response = await client.GetAsync("/not-found");
        response.EnsureSuccessStatusCode();
    }
}