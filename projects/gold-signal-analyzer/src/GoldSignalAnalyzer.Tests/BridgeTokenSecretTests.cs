using GoldSignalAnalyzer.Application.Security;
using GoldSignalAnalyzer.Infrastructure.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// FR-37 / Phase 2.5 cond D1. The bridge token is a secret: <c>bridgetoken</c> is on the denylist
/// (exact, case-insensitive) and the masking enricher masks a <c>BridgeToken</c> log property so
/// the token never appears in logs, even partially.
/// </summary>
public sealed class BridgeTokenSecretTests
{
    private sealed class CapturingSink : ILogEventSink
    {
        public readonly List<LogEvent> Events = new();
        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }

    [Fact]
    public void D1_bridgetoken_is_on_the_secret_denylist()
    {
        Assert.True(SecretDenylist.IsSecret("bridgetoken"));
        Assert.True(SecretDenylist.IsSecret("BridgeToken"));   // case-insensitive
        Assert.True(SecretDenylist.IsSecret("BRIDGETOKEN"));
    }

    [Fact]
    public void D1_enricher_masks_the_bridge_token_property()
    {
        var sink = new CapturingSink();
        using (var logger = new LoggerConfiguration()
                   .Enrich.With(new SensitiveDataMaskingEnricher())
                   .WriteTo.Sink(sink)
                   .CreateLogger())
        {
            logger.Information("bridge started {BridgeToken}", "raw-bridge-token-0xDEADBEEF");
        }

        var evt = Assert.Single(sink.Events);
        Assert.Equal(SensitiveDataMaskingEnricher.Mask, ((ScalarValue)evt.Properties["BridgeToken"]).Value);
        Assert.DoesNotContain("raw-bridge-token-0xDEADBEEF", evt.RenderMessage());
    }
}
