using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnifiedInference;
using UnifiedInference.Abstractions;
using Xunit;

namespace UnifiedInference.Tests;

public class CancellationTests
{
    [Fact]
    public async Task GenerateText_honors_cancellation()
    {
        var handler = new DelegateHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"pipeline_tag\":\"text-generation\"}")
        });

        var client = new UnifiedInferenceClient(new HttpClient(handler));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<TaskCanceledException>(() => client.GenerateTextAsync(new TextRequest
        {
            ModelId = "model",
            Prompt = "hi",
            Settings = new GenerationSettings()
        }, cts.Token));
    }
}
