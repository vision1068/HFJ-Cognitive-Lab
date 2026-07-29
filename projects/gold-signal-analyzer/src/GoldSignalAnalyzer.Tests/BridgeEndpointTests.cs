using GoldSignalAnalyzer.Application.Bridge;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

// FR-8 / NFR-2: bridge endpoint is loopback-only by construction.
public class BridgeEndpointTests
{
    [Theory]
    [InlineData("http://127.0.0.1:9001/")]
    [InlineData("http://localhost:9001/")]
    [InlineData("http://[::1]:9001/")]
    public void Accepts_loopback_hosts(string uri)
    {
        var ep = BridgeEndpoint.Parse(uri);
        Assert.True(BridgeEndpoint.IsLoopback(ep.BaseUri));
    }

    [Theory]
    [InlineData("http://10.0.0.5:9001/")]
    [InlineData("http://192.168.1.20:9001/")]
    [InlineData("http://example.com/")]
    [InlineData("http://0.0.0.0:9001/")]
    public void Rejects_non_loopback_hosts(string uri)
        => Assert.Throws<ArgumentException>(() => BridgeEndpoint.Parse(uri));

    [Fact]
    public void Loopback_factory_builds_127_0_0_1()
    {
        var ep = BridgeEndpoint.Loopback(9001);
        Assert.Equal("127.0.0.1", ep.BaseUri.Host);
    }
}
