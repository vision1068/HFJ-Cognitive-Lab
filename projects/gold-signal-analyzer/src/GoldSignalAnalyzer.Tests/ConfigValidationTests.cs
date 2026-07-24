using GoldSignalAnalyzer.Application.Configuration;
using GoldSignalAnalyzer.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// GATE QA-F11 / NFR-9: invalid config must fail loud. Forcing IOptions&lt;AppConfiguration&gt;.Value
/// triggers the IValidateOptions validator (the same one ValidateOnStart runs at host start).
/// </summary>
public sealed class ConfigValidationTests
{
    [Fact]
    public void QAF11_invalid_config_throws_on_options_resolution()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppConfiguration:Scoring:BuyThreshold"] = "999" // out of 0..100 range
            })
            .Build();

        var services = new ServiceCollection();
        services.AddGsaConfiguration(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => _ = provider.GetRequiredService<IOptions<AppConfiguration>>().Value);
    }

    [Fact]
    public void QAF11_valid_default_config_resolves()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddGsaConfiguration(configuration);
        using var provider = services.BuildServiceProvider();

        var value = provider.GetRequiredService<IOptions<AppConfiguration>>().Value;
        Assert.NotNull(value);
        Assert.InRange(value.Scoring.BuyThreshold, 0, 100);
    }
}
