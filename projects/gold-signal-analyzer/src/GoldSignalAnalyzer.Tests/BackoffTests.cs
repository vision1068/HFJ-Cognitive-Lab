using GoldSignalAnalyzer.Application.Connection;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

// NFR-4: exponential backoff with jitter, capped.
public class BackoffTests
{
    [Fact]
    public void Base_delay_grows_exponentially_then_caps()
    {
        var policy = new ExponentialBackoffPolicy(
            new BackoffOptions(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), Multiplier: 2.0));

        Assert.Equal(TimeSpan.FromSeconds(1), policy.BaseDelayForAttempt(1));
        Assert.Equal(TimeSpan.FromSeconds(2), policy.BaseDelayForAttempt(2));
        Assert.Equal(TimeSpan.FromSeconds(4), policy.BaseDelayForAttempt(3));
        Assert.Equal(TimeSpan.FromSeconds(8), policy.BaseDelayForAttempt(4));
        Assert.Equal(TimeSpan.FromSeconds(16), policy.BaseDelayForAttempt(5));
        Assert.Equal(TimeSpan.FromSeconds(30), policy.BaseDelayForAttempt(6));  // capped
        Assert.Equal(TimeSpan.FromSeconds(30), policy.BaseDelayForAttempt(20)); // stays capped
    }

    [Fact]
    public void Jitter_stays_within_configured_band()
    {
        var policy = new ExponentialBackoffPolicy(
            new BackoffOptions(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60), Multiplier: 2.0, JitterFraction: 0.2));

        // rng=0 -> lower bound (-20%), rng->1 -> upper bound (+20%)
        var low = policy.NextDelay(1, () => 0.0);
        var high = policy.NextDelay(1, () => 0.999999);

        Assert.Equal(8.0, low.TotalSeconds, 3);
        Assert.True(high.TotalSeconds > 11.9 && high.TotalSeconds <= 12.0, $"high was {high.TotalSeconds}");
    }

    [Fact]
    public void Jitter_never_exceeds_max_cap()
    {
        var policy = new ExponentialBackoffPolicy(
            new BackoffOptions(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30), JitterFraction: 0.5));
        var d = policy.NextDelay(1, () => 0.999999);
        Assert.True(d <= TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Rejects_invalid_attempt_number()
    {
        var policy = new ExponentialBackoffPolicy();
        Assert.Throws<ArgumentOutOfRangeException>(() => policy.BaseDelayForAttempt(0));
    }
}
