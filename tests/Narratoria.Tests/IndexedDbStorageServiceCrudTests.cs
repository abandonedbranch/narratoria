using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Storage;
using Narratoria.Storage.IndexedDb;

namespace Narratoria.Tests;

[TestClass]
public sealed class IndexedDbStorageServiceCrudTests
{
    [TestMethod]
    public async Task GetAsync_ReturnsNull_WhenRecordIsMissing()
    {
        var module = new FakeJsModule { GetResult = null };
        var jsRuntime = new FakeJsRuntime(module);
        var schema = CreateSchema();
        var metrics = new NullIndexedDbStorageMetrics();

        await using var service = new IndexedDbStorageService(jsRuntime, schema, metrics, NullLogger<IndexedDbStorageService>.Instance);

        var store = schema.Stores[0];
        var result = await service.GetAsync(new IndexedDbGetRequest<string>
        {
            Store = store,
            Key = "k1",
            Serializer = new Utf8StringSerializer(),
            Scope = new StorageScope(schema.DatabaseName, store.Name)
        }, CancellationToken.None);

        Assert.IsTrue(result.Ok);
        Assert.IsNull(result.Value);
        Assert.AreEqual("get", module.LastIdentifier);
    }

    [TestMethod]
    public async Task GetAsync_DeserializesPayload_WhenRecordExists()
    {
        var module = new FakeJsModule
        {
            GetResult = new FakeJsModule.SerializedRecord("k1", Encoding.UTF8.GetBytes("hello"))
        };
        var jsRuntime = new FakeJsRuntime(module);
        var schema = CreateSchema();
        var metrics = new NullIndexedDbStorageMetrics();

        await using var service = new IndexedDbStorageService(jsRuntime, schema, metrics, NullLogger<IndexedDbStorageService>.Instance);

        var store = schema.Stores[0];
        var result = await service.GetAsync(new IndexedDbGetRequest<string>
        {
            Store = store,
            Key = "k1",
            Serializer = new Utf8StringSerializer(),
            Scope = new StorageScope(schema.DatabaseName, store.Name)
        }, CancellationToken.None);

        Assert.IsTrue(result.Ok);
        Assert.AreEqual("hello", result.Value);
        Assert.AreEqual("get", module.LastIdentifier);
    }

    [TestMethod]
    public async Task DeleteAsync_InvokesDel_WithExpectedArgs()
    {
        var module = new FakeJsModule();
        var jsRuntime = new FakeJsRuntime(module);
        var schema = CreateSchema();
        var metrics = new NullIndexedDbStorageMetrics();

        await using var service = new IndexedDbStorageService(jsRuntime, schema, metrics, NullLogger<IndexedDbStorageService>.Instance);

        var store = schema.Stores[0];
        var result = await service.DeleteAsync(new IndexedDbDeleteRequest
        {
            Store = store,
            Key = "k1",
            Scope = new StorageScope(schema.DatabaseName, store.Name)
        }, CancellationToken.None);

        Assert.IsTrue(result.Ok);
        Assert.AreEqual("del", module.LastIdentifier);
        Assert.IsNotNull(module.LastArgs);

        var args = module.LastArgs!;
        var schemaArg = GetPropertyValue<object>(args, "Schema");
        Assert.IsNotNull(schemaArg);
        Assert.AreEqual(schema.DatabaseName, GetPropertyValue<string>(schemaArg!, "DatabaseName"));
        Assert.AreEqual(store.Name, GetPropertyValue<string>(args, "StoreName"));
        Assert.AreEqual("k1", GetPropertyValue<string>(args, "Key"));
    }

    private static IndexedDbSchema CreateSchema()
    {
        return new IndexedDbSchema
        {
            DatabaseName = "test_db",
            Version = 1,
            Stores = new[]
            {
                new IndexedDbStoreDefinition
                {
                    Name = "test_store",
                    KeyPath = "Key",
                    AutoIncrement = false
                }
            }
        };
    }

    private static T? GetPropertyValue<T>(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property is null)
        {
            return default;
        }

        return (T?)property.GetValue(instance);
    }

    private sealed class Utf8StringSerializer : IIndexedDbValueSerializer<string>
    {
        public ValueTask<IndexedDbSerializedValue> SerializeAsync(string value, CancellationToken cancellationToken)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            return new ValueTask<IndexedDbSerializedValue>(new IndexedDbSerializedValue(bytes, bytes.Length));
        }

        public ValueTask<string> DeserializeAsync(IndexedDbSerializedValue payload, CancellationToken cancellationToken)
        {
            return new ValueTask<string>(Encoding.UTF8.GetString(payload.Payload));
        }
    }

    private sealed class NullIndexedDbStorageMetrics : IIndexedDbStorageMetrics
    {
        public void RecordLatency(string operation, string store, TimeSpan duration) { }

        public void RecordResult(string operation, string store, string status, string errorClass) { }

        public void RecordBytesWritten(long bytes) { }

        public void RecordBytesRead(long bytes) { }
    }

    private sealed class FakeJsRuntime : IJSRuntime
    {
        private readonly IJSObjectReference _module;

        public FakeJsRuntime(IJSObjectReference module)
        {
            _module = module;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier == "import")
            {
                return new ValueTask<TValue>((TValue)_module);
            }

            throw new InvalidOperationException($"Unexpected JS runtime call: {identifier}");
        }
    }

    private sealed class FakeJsModule : IJSObjectReference
    {
        public sealed record SerializedRecord(string Key, byte[] Payload);

        public string? LastIdentifier { get; private set; }
        public object? LastArgs { get; private set; }

        public SerializedRecord? GetResult { get; init; }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            LastIdentifier = identifier;
            LastArgs = args is { Length: > 0 } ? args[0] : null;

            if (identifier == "get")
            {
                if (GetResult is null)
                {
                    return new ValueTask<TValue>(default(TValue)!);
                }

                var instance = CreateSerializedRecord(typeof(TValue), GetResult.Key, GetResult.Payload);
                return new ValueTask<TValue>((TValue)instance);
            }

            if (identifier == "del")
            {
                object boxed = true;
                return new ValueTask<TValue>((TValue)boxed);
            }

            throw new InvalidOperationException($"Unexpected module call: {identifier}");
        }

        public ValueTask InvokeVoidAsync(string identifier, object?[]? args)
        {
            return InvokeVoidAsync(identifier, CancellationToken.None, args);
        }

        public ValueTask InvokeVoidAsync(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            LastIdentifier = identifier;
            LastArgs = args is { Length: > 0 } ? args[0] : null;
            return ValueTask.CompletedTask;
        }

        private static object CreateSerializedRecord(Type targetType, string key, byte[] payload)
        {
            // Handle Nullable<T> where T is the private record type used by IndexedDbStorageService.
            var type = Nullable.GetUnderlyingType(targetType) ?? targetType;

            var instance = Activator.CreateInstance(
                type,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object?[] { key, payload },
                culture: null);

            return instance ?? throw new InvalidOperationException($"Unable to create instance of {type.FullName}");
        }
    }
}
