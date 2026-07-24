namespace GoldSignalAnalyzer.Infrastructure.Logging;

/// <summary>
/// Static helper for values that must appear partially in the UI/logs (arch §5). Nothing
/// sensitive is logged in Cycle 1; the helper exists now (FR-3 acceptance) for Cycle-2 use.
/// </summary>
public static class MaskingHelper
{
    public const string Mask = "***MASKED***";

    /// <summary>Renders an account number as last-2 only (e.g. <c>*******47</c>).</summary>
    public static string MaskAccount(string? account)
    {
        if (string.IsNullOrEmpty(account)) return Mask;
        if (account.Length <= 2) return new string('*', account.Length);
        return new string('*', account.Length - 2) + account[^2..];
    }

    /// <summary>Renders a path as its file name only, dropping directory structure.</summary>
    public static string MaskPath(string? path)
    {
        if (string.IsNullOrEmpty(path)) return Mask;
        var name = System.IO.Path.GetFileName(path);
        return string.IsNullOrEmpty(name) ? Mask : ".../" + name;
    }
}
