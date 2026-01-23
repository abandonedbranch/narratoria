using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnifiedInference;
using UnifiedInference.Abstractions;
using Xunit;

namespace UnifiedInference.Tests;

public class ErrorHandlingTests
{
    [Fact]
    public async Task Text_errors_surface_payload_message()
    {
        var metadata = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"pipeline_tag\":\"text-generation\"}")
        };

        var failure = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"error\":\"bad request\"}")
        };

        var handler = new QueuedHandler([metadata, failure]);
        var client = new UnifiedInferenceClient(new HttpClient(handler));

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.GenerateTextAsync(new TextRequest
        {
            ModelId = "model",
            Prompt = "hello",
            Settings = new GenerationSettings()
        }, CancellationToken.None));

        Assert.Contains("bad request", ex.Message);
    }

    [Fact]
    public async Task Image_errors_surface_payload_message()
    {
        var metadata = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"pipeline_tag\":\"text-to-image\"}")
        };

        var failure = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"error\":\"invalid image\"}")
        };

        var handler = new QueuedHandler([metadata, failure]);
        var client = new UnifiedInferenceClient(new HttpClient(handler));

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.GenerateImageAsync(new ImageRequest
        {
            ModelId = "model",
            Prompt = "hello",
            Settings = new GenerationSettings()
        }, CancellationToken.None));

        Assert.Contains("invalid image", ex.Message);
    }

    [Fact]
    public async Task ProviderOverrides_are_injected_into_parameters()
    {
        var metadata = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"pipeline_tag\":\"text-generation\"}")
        };

        var success = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[{\"generated_text\":\"ok\"}]")
        };

        var handler = new QueuedHandler([metadata, success]);
        var client = new UnifiedInferenceClient(new HttpClient(handler));

        var settings = new GenerationSettings
        {
            ProviderOverrides = new Dictionary<string, object> { { "custom", "value" } }
        };

        await client.GenerateTextAsync(new TextRequest
        {
            ModelId = "model",
            Prompt = "hi",
            Settings = settings
        }, CancellationToken.None);

        var payload = handler.Bodies[1]!;
        Assert.Contains("\"custom\":\"value\"", payload);
    }
}
