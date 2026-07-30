using System.Reflection;
using GoldSignalAnalyzer.Application.Abstractions;
using GoldSignalAnalyzer.Application.Bridge;
using GoldSignalAnalyzer.Domain;
using GoldSignalAnalyzer.Infrastructure.Providers;
using GoldSignalAnalyzer.Presentation.Setup;
using GoldSignalAnalyzer.Testing;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

// FR-10 / NFR-10: the test provider must be gated out of production builds.
// NFR-1: no credential/secret field may exist on connection config.
public class ProductionGatingTests
{
    private static readonly Assembly InfrastructureAssembly = typeof(MetaTrader5MarketDataProvider).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(IMarketDataProvider).Assembly;

    [Fact] // TestMarketDataProvider lives in its own assembly, NOT the production ones.
    public void Test_provider_is_not_in_production_assemblies()
    {
        Assert.DoesNotContain(InfrastructureAssembly.GetTypes(), t => t.Name == "TestMarketDataProvider");
        Assert.DoesNotContain(ApplicationAssembly.GetTypes(), t => t.Name == "TestMarketDataProvider");

        // It exists only in the dedicated Testing assembly.
        Assert.NotEqual(InfrastructureAssembly, typeof(TestMarketDataProvider).Assembly);
        Assert.Equal("GoldSignalAnalyzer.Testing", typeof(TestMarketDataProvider).Assembly.GetName().Name);
    }

    [Fact] // The production Infrastructure assembly does not reference the Testing assembly.
    public void Infrastructure_does_not_reference_testing_assembly()
    {
        var referenced = InfrastructureAssembly.GetReferencedAssemblies().Select(a => a.Name);
        Assert.DoesNotContain("GoldSignalAnalyzer.Testing", referenced);
    }

    [Theory] // FR-11 marker: only the MT5 live provider is IsLive.
    [InlineData(typeof(MetaTrader5MarketDataProvider), true)]
    [InlineData(typeof(CsvHistoricalMarketDataProvider), false)]
    public void Only_the_live_provider_is_flagged_live(Type providerType, bool expectedLive)
    {
        var isLiveProp = providerType.GetProperty(nameof(IMarketDataProvider.IsLive))!;
        // Instantiate cheaply via the parameterless-ish path is not possible; assert via a constructed instance where feasible.
        IMarketDataProvider instance = providerType == typeof(MetaTrader5MarketDataProvider)
            ? new MetaTrader5MarketDataProvider(new Support.FakeBridgeClient())
            : CsvHistoricalMarketDataProvider.ParseCsv(NormalizedSymbol.Gold, TimeFrame.H1,
                new[] { "time,open,high,low,close,volume", "2026-07-30T09:00:00Z,2400,2410,2395,2405,100" });
        Assert.Equal(expectedLive, (bool)isLiveProp.GetValue(instance)!);
    }

    // NFR-1 / NFR-SETUP-1: every persisted CONFIG record must carry NO secret/credential
    // field. The same reflection guard now covers the Cycle-5 SetupProfile (extended with
    // "otp"), so "no secret member" can never regress on either type.
    [Theory]
    [InlineData(typeof(Mt5ConnectionOptions))]
    [InlineData(typeof(SetupProfile))]
    public void Config_records_have_no_secret_field(Type configType)
    {
        string[] forbidden = { "password", "passwd", "pwd", "secret", "investor", "pin", "token", "apikey", "otp" };
        var members = configType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name.ToLowerInvariant());
        foreach (var m in members)
            Assert.DoesNotContain(forbidden, bad => m.Contains(bad));
    }
}
