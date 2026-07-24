using GoldSignalAnalyzer.Infrastructure.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// GATE B2: log an event carrying denylisted properties through the REAL enricher + a capturing
/// sink; assert masked values appear and raw values do not. Also asserts a nested/destructured
/// secret is masked (recursion, Aud-F4) while a non-secret sibling (ServerName) is preserved
/// (no over-masking, QA-F12).
/// </summary>
public sealed class MaskingEnricherTests
{
    private sealed class CapturingSink : ILogEventSink
    {
        public readonly List<LogEvent> Events = new();
        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }

    [Fact]
    public void B2_enricher_masks_denylisted_properties_including_nested_and_preserves_non_secrets()
    {
        var sink = new CapturingSink();
        using (var logger = new LoggerConfiguration()
                   .Enrich.With(new SensitiveDataMaskingEnricher())
                   .WriteTo.Sink(sink)
                   .CreateLogger())
        {
            logger.Information(
                "auth {password} {AccountNumber} {ServerName} {@Config}",
                "hunter2",
                "123456789",
                "Exness-Server-Demo",
                new { Token = "nested-secret-abc", ServerName = "Exness-Server-Demo" });
        }

        var evt = Assert.Single(sink.Events);
        var rendered = evt.RenderMessage();

        // Top-level secrets masked; raw values absent.
        Assert.Equal(SensitiveDataMaskingEnricher.Mask, Scalar(evt, "password"));
        Assert.Equal(SensitiveDataMaskingEnricher.Mask, Scalar(evt, "AccountNumber"));
        Assert.DoesNotContain("hunter2", rendered);
        Assert.DoesNotContain("123456789", rendered);

        // Non-secret operational value preserved (no over-masking).
        Assert.Equal("Exness-Server-Demo", Scalar(evt, "ServerName"));
        Assert.Contains("Exness-Server-Demo", rendered);

        // Nested/destructured secret masked (recursion); nested non-secret preserved.
        var structure = Assert.IsType<StructureValue>(evt.Properties["Config"]);
        var tokenProp = structure.Properties.Single(p => p.Name == "Token");
        Assert.Equal(SensitiveDataMaskingEnricher.Mask, ((ScalarValue)tokenProp.Value).Value);
        var nestedServer = structure.Properties.Single(p => p.Name == "ServerName");
        Assert.Equal("Exness-Server-Demo", ((ScalarValue)nestedServer.Value).Value);
        Assert.DoesNotContain("nested-secret-abc", rendered);
    }

    private static object? Scalar(LogEvent evt, string name)
        => ((ScalarValue)evt.Properties[name]).Value;
}
