using NarratoriaClient.Services;

namespace NarratoriaClient.Tests;

public class AttachmentServiceTests
{
    [Fact]
    public async Task RejectsNonTextContent()
    {
        var service = new AttachmentService();
        using var stream = new MemoryStream(new byte[] { 0x01, 0x02 });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddAttachmentAsync("session", "bin.dat", "application/octet-stream", stream));
    }

    [Fact]
    public async Task RejectsOversizedContent()
    {
        var service = new AttachmentService();
        var big = new byte[6 * 1024 * 1024];
        using var stream = new MemoryStream(big);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddAttachmentAsync("session", "big.txt", "text/plain", stream));
    }

    [Fact]
    public async Task AddsAndListsAttachments()
    {
        var service = new AttachmentService();
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
