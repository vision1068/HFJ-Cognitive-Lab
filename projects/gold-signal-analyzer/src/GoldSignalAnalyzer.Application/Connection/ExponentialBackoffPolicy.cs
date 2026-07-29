namespace GoldSignalAnalyzer.Application.Connection;

public sealed record BackoffOptions(
    TimeSpan Initial,
    TimeSpan Max,
    double Multiplier = 2.0,
    double JitterFraction = 0.20)
{
    public static BackoffOptions Default =>
        new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30));
}

/// <summary>
/// NFR-4: exponential backoff with jitter for auto-reconnect. Base delay is
/// deterministic (Initial * Multiplier^(attempt-1), capped at Max); jitter is
/// applied through an injectable RNG so tests are deterministic. Pure math,
/// no timers, no sleeping — the caller owns the actual delay.
/// </summary>
public sealed class ExponentialBackoffPolicy
{
    private readonly BackoffOptions _o;

    public ExponentialBackoffPolicy(BackoffOptions? options = null)
    {
        _o = options ?? BackoffOptions.Default;
        if (_o.Initial <= TimeSpan.Zero) throw new ArgumentException("Initial must be positive.");
        if (_o.Max < _o.Initial) throw new ArgumentException("Max must be >= Initial.");
        if (_o.Multiplier < 1.0) throw new ArgumentException("Multiplier must be >= 1.");
        if (_o.JitterFraction is < 0 or > 1) throw new ArgumentException("JitterFraction must be 0..1.");
    }

    /// <summary>Deterministic base delay for a 1-based attempt number.</summary>
    public TimeSpan BaseDelayForAttempt(int attempt)
    {
        if (attempt < 1) throw new ArgumentOutOfRangeException(nameof(attempt), "Attempt is 1-based.");
        var factor = Math.Pow(_o.Multiplier, attempt - 1);
        var ms = _o.Initial.TotalMilliseconds * factor;
        var cappedMs = Math.Min(ms, _o.Max.TotalMilliseconds);
        return TimeSpan.FromMilliseconds(cappedMs);
    }

    /// <summary>
    /// Base delay plus symmetric jitter of +/- JitterFraction, clamped to
    /// [0, Max]. <paramref name="rng"/> must return a value in [0,1);
    /// defaults to <see cref="Random.Shared"/>.
    /// </summary>
    public TimeSpan NextDelay(int attempt, Func<double>? rng = null)
    {
        var baseDelay = BaseDelayForAttempt(attempt);
        rng ??= Random.Shared.NextDouble;
        var r = rng();
        if (r is < 0 or >= 1) throw new ArgumentOutOfRangeException(nameof(rng), "RNG must yield [0,1).");
        // map [0,1) -> [-1, 1)
        var signed = (r * 2.0) - 1.0;
        var jittered = baseDelay.TotalMilliseconds * (1.0 + _o.JitterFraction * signed);
        var clampedMs = Math.Clamp(jittered, 0.0, _o.Max.TotalMilliseconds);
        return TimeSpan.FromMilliseconds(clampedMs);
    }
}
