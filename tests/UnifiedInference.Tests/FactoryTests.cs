using System.Net.Http;
using UnifiedInference.Factory;
using Xunit;

namespace UnifiedInference.Tests;

public class FactoryTests
{
    [Fact]
    public void Factory_returns_client_with_shared_httpclient()
    {
        var http = new HttpClient();
        var client = InferenceClientFactory.Create("token", http);

        Assert.Same(http, client.HttpClient);
    }
}
