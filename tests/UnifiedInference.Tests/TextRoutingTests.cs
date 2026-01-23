using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using UnifiedInference;
using UnifiedInference.Abstractions;
using Xunit;

namespace UnifiedInference.Tests;

public class TextRoutingTests
{
    [Fact]
    public async Task TextGeneration_retries_on_503_and_respects_wait_for_model()
    {
        var metadata = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"pipeline_tag\":\"text-generation\"}")
        };

        var retry = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        retry.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.Zero);

        var success = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[{\"generated_text\":\"ok\"}]")
        };

        var handler = new QueuedHandler([metadata, retry, success]);
        var httpClient = new HttpClient(handler);
        var client = new UnifiedInferenceClient(httpClient, apiToken: "token", delayStrategy: (_, _) => Task.CompletedTask);

        var response = await client.GenerateTextAsync(new TextRequest
        {
            ModelId = "mistral",
            Prompt = "Hello",
            Settings = new GenerationSettings()
        }, CancellationToken.None);

        Assert.Equal("ok", response.Text);
        Assert.Equal(3, handler.Requests.Count);

        // The second request is the first POST (index 1 after GET).
        var payload = handler.Bodies[1]!;
        Assert.Contains("\"wait_for_model\":true", payload);
    }
}
