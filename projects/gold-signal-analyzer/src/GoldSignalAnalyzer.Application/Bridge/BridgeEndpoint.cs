using System.Net;

namespace GoldSignalAnalyzer.Application.Bridge;

/// <summary>
/// A validated address for the local MT5 bridge. NFR-2 / FR-8: the bridge is
/// bound to loopback only and must never be reachable off-box, so this value
/// object REFUSES to construct for any non-loopback host. That makes
/// "accidentally point the client at a public host" a construction-time
/// failure, not a runtime security incident.
/// </summary>
public sealed record BridgeEndpoint
{
    public Uri BaseUri { get; }

    private BridgeEndpoint(Uri baseUri) => BaseUri = baseUri;

    public static BridgeEndpoint Loopback(int port, bool useHttps = false)
    {
        if (port is < 1 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be 1..65535.");
        var scheme = useHttps ? "https" : "http";
        return new BridgeEndpoint(new Uri($"{scheme}://127.0.0.1:{port}/"));
    }

    /// <summary>
    /// Parses/validates a caller-supplied base URI, rejecting anything that is
    /// not a loopback host (NFR-2).
    /// </summary>
    public static BridgeEndpoint Parse(string baseUri)
    {
        if (!Uri.TryCreate(baseUri, UriKind.Absolute, out var uri))
            throw new ArgumentException($"'{baseUri}' is not an absolute URI.", nameof(baseUri));
        if (!IsLoopback(uri))
            throw new ArgumentException(
                $"Bridge endpoint must be loopback-only (127.0.0.1/::1/localhost); '{uri.Host}' is not permitted.",
                nameof(baseUri));
        return new BridgeEndpoint(uri);
    }

    /// <summary>True only for a host that resolves to a loopback address.</summary>
    public static bool IsLoopback(Uri uri)
    {
        if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
            return true;
        if (uri.HostNameType is UriHostNameType.IPv4 or UriHostNameType.IPv6
            && IPAddress.TryParse(uri.Host, out var ip))
        {
            return IPAddress.IsLoopback(ip);
        }
        return false;
    }
}
