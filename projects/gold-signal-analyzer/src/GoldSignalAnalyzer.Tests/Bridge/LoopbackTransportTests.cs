using System.Net.Http;
using System.Reflection;
using GoldSignalAnalyzer.MT5Bridge.Transport;
using Xunit;

namespace GoldSignalAnalyzer.Tests.Bridge;

/// <summary>
/// Loopback-by-construction (ADR-14) and no-logging-handler (C10.2). Non-loopback is not merely
/// rejected — it is UNREPRESENTABLE: no public API accepts a host/URI, and no logging handler can be
/// injected into the HttpClient.
/// </summary>
public sealed class LoopbackTransportTests
{
    [Fact] // ADR-14
    public void Bind_produces_a_loopback_base_address()
    {
        var transport = new LoopbackHttpBridgeTransport();
        transport.Bind(50123, "token");

        Assert.NotNull(transport.BaseAddress);
        Assert.Equal("127.0.0.1", transport.BaseAddress!.Host);
        Assert.True(transport.BaseAddress.IsLoopback);
        Assert.Equal(50123, transport.BaseAddress.Port);
    }

    [Fact] // ADR-14 — no public API accepts a host / hostname / URI
    public void Transport_exposes_no_host_or_uri_accepting_member()
    {
        var t = typeof(LoopbackHttpBridgeTransport);

        foreach (var ctor in t.GetConstructors())
            foreach (var p in ctor.GetParameters())
            {
                Assert.NotEqual(typeof(Uri), p.ParameterType);
                Assert.DoesNotContain("host", p.Name ?? "", StringComparison.OrdinalIgnoreCase);
            }

        // BaseAddress is a getter-only diagnostic; no SETTABLE Uri property may exist.
        foreach (var prop in t.GetProperties())
            Assert.False(prop.PropertyType == typeof(Uri) && prop.CanWrite,
                $"No settable Uri property allowed; found {prop.Name}.");

        foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            foreach (var p in m.GetParameters())
            {
                Assert.NotEqual(typeof(Uri), p.ParameterType);
                Assert.DoesNotContain("host", p.Name ?? "", StringComparison.OrdinalIgnoreCase);
            }
    }

    [Fact] // C10.2 — no request/header logging handler can observe the Authorization header
    public void HttpClient_has_no_delegating_logging_handler()
    {
        var t = typeof(LoopbackHttpBridgeTransport);

        // (a) By construction: no member accepts an HttpClient/HttpMessageHandler/DelegatingHandler,
        // so a logging handler is UNREPRESENTABLE.
        foreach (var member in t.GetConstructors().Cast<MethodBase>()
                     .Concat(t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)))
            foreach (var p in member.GetParameters())
            {
                Assert.False(typeof(HttpMessageHandler).IsAssignableFrom(p.ParameterType),
                    $"No member may accept an HttpMessageHandler; found on {member.Name}.");
                Assert.NotEqual(typeof(HttpClient), p.ParameterType);
            }

        // (b) Behavioural: the constructed HttpClient's handler pipeline has ZERO DelegatingHandlers.
        var transport = new LoopbackHttpBridgeTransport();
        var clientField = t.GetField("_client", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(clientField);
        var client = (HttpClient)clientField!.GetValue(transport)!;

        var handlerField = typeof(HttpMessageInvoker).GetField("_handler", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(handlerField);
        var handler = handlerField!.GetValue(client) as HttpMessageHandler;

        int delegatingCount = 0;
        while (handler is DelegatingHandler dh)
        {
            delegatingCount++;
            handler = dh.InnerHandler;
        }
        Assert.Equal(0, delegatingCount);
    }
}
