using NarratoriaClient.Services;

namespace NarratoriaClient.Tests;

public class AttachmentServiceTests
{
    private sealed class InMemoryStorage : IClientStorageService
    {
        private readonly Dictionary<string, string?> _store = new();

        public Task<string?> GetItemAsync(StorageArea area, string key, CancellationToken cancellationToken = default)
        {
            _store.TryGetValue(key, out var value);
            return Task.FromResult<string?>(value);
        }

        public Task SetItemAsync(StorageArea area, string key, string? value, CancellationToken cancellationToken = default)
        {
            if (value is null)
            {
                _store.Remove(key);
            }
            else
            {
                _store[key] = value;
            }

            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact]
    public async Task RejectsNonTextContent()
    {
        var service = new AttachmentService(new InMemoryStorage());
        using var stream = new MemoryStream(new byte[] { 0x01, 0x02 });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddAttachmentAsync("session", "bin.dat", "application/octet-stream", stream));
    }

    [Fact]
    public async Task RejectsOversizedContent()
    {
        var service = new AttachmentService(new InMemoryStorage());
        var big = new byte[6 * 1024 * 1024];
        using var stream = new MemoryStream(big);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddAttachmentAsync("session", "big.txt", "text/plain", stream));
    }

    [Fact]
    public async Task AddsAndListsAttachments()
    {
        var service = new AttachmentService(new InMemoryStorage());
        var data = System.Text.Encoding.UTF8.GetBytes("hello world");
        using var stream = new MemoryStream(data);

        var record = await service.AddAttachmentAsync("session1", "note.txt", "text/plain", stream);
        Assert.Equal("session1", record.SessionId);
        Assert.Equal("note.txt", record.FileName);

        var list = await service.GetAttachmentsAsync("session1");
        Assert.Single(list);
        Assert.Equal(record.Id, list[0].Id);
    }
}
