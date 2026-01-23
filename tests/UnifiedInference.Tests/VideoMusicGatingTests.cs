using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnifiedInference;
using UnifiedInference.Abstractions;
using Xunit;

namespace UnifiedInference.Tests;

public class VideoMusicGatingTests
{
    [Fact]
    public async Task Audio_is_blocked_when_not_supported()
    {
        var metadata = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"pipeline_tag\":\"text-generation\"}")
        };

        var handler = new QueuedHandler([metadata]);
        var client = new UnifiedInferenceClient(new HttpClient(handler));

        await Assert.ThrowsAsync<NotSupportedException>(() => client.GenerateAudioAsync(
            new AudioRequest { ModelId = "mistral", Mode = AudioMode.TextToSpeech, TextInput = "hi" },
            CancellationToken.None));
    }

    [Fact]
    public async Task Video_is_blocked_when_not_supported()
    {
        var metadata = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"pipeline_tag\":\"text-generation\"}")
        };

        var handler = new QueuedHandler([metadata]);
        var client = new UnifiedInferenceClient(new HttpClient(handler));

        await Assert.ThrowsAsync<NotSupportedException>(() => client.GenerateVideoAsync(
            new VideoRequest { ModelId = "mistral", Prompt = "hi" },
            CancellationToken.None));
    }

    [Fact]
    public async Task Music_is_blocked_when_not_supported()
    {
        var metadata = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"pipeline_tag\":\"text-generation\"}")
        };

        var handler = new QueuedHandler([metadata]);
        var client = new UnifiedInferenceClient(new HttpClient(handler));

        await Assert.ThrowsAsync<NotSupportedException>(() => client.GenerateMusicAsync(
            new MusicRequest { ModelId = "mistral", Prompt = "hi" },
            CancellationToken.None));
    }
}
