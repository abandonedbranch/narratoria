using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnifiedInference;
using Xunit;

namespace UnifiedInference.Tests;

public class CapabilitiesTests
{
    [Fact]
    public async Task Capability_fetch_maps_pipeline_tag_and_status()
    {
        var metadata = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"pipeline_tag\":\"text-to-image\",\"gated\":true,\"inference\":{\"status\":\"loading\"}}")
        };

        var handler = new QueuedHandler([metadata]);
        var client = new UnifiedInferenceClient(new HttpClient(handler));

        var caps = await client.GetCapabilitiesAsync("stabilityai/stable-diffusion", CancellationToken.None);

        Assert.True(caps.SupportsImage);
        Assert.False(caps.SupportsText);
        Assert.True(caps.Gated);
        Assert.Equal("loading", caps.InferenceStatus);
        Assert.Equal("text-to-image", caps.PipelineTag);
    }
}
