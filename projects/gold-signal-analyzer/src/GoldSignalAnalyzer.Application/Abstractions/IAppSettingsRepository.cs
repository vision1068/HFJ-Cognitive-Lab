namespace GoldSignalAnalyzer.Application.Abstractions;

/// <summary>
/// Repository over the non-secret key/value AppSettings table. Implementations MUST reject
/// writes whose key matches the secret denylist (gate B3).
/// </summary>
public interface IAppSettingsRepository
{
    Task SetAsync(string key, string value, CancellationToken ct = default);
    Task<string?> GetAsync(string key, CancellationToken ct = default);
}
