using GoldSignalAnalyzer.Application.Ports;
using GoldSignalAnalyzer.MT5Bridge.Process;
using GoldSignalAnalyzer.MT5Bridge.Transport;
using Microsoft.Extensions.DependencyInjection;

namespace GoldSignalAnalyzer.MT5Bridge;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the MT5 bridge (arch §4, ADR-12).
    ///
    /// DEFAULT (opt-in OFF) — the Production composition root (GsaHost) calls this with no argument,
    /// so it composes NOTHING live-capable: neither the <see cref="PythonBridgeProcess"/> spawner nor
    /// the keyed <c>IMarketDataProvider("mt5-bridge")</c> provider are registered (C9). The keyless
    /// default <see cref="IMarketDataProvider"/> is untouched — it stays <c>NullMarketDataProvider</c>
    /// in every environment, including Production (AC-49.1). Going live is never an ambient DI swap.
    ///
    /// OPT-IN (<paramref name="enableLiveBridge"/> = true) — an EXPLICIT, deliberate resolution by a
    /// future connectivity feature. Registers the transport/process seams and
    /// <see cref="Mt5BridgeClient"/> as a single concrete singleton, and exposes it via the keyed
    /// registration <c>IMarketDataProvider("mt5-bridge")</c> that FORWARDS to that SAME singleton
    /// (C12 — one client, one child process). It still does NOT bind the keyless default.
    /// </summary>
    public static IServiceCollection AddGsaMt5Bridge(this IServiceCollection services, bool enableLiveBridge = false)
    {
        if (!enableLiveBridge)
            return services; // inert: Production-safe by construction (C9)

        // Default launch/runtime options — replaceable by the caller before this call if they wish.
        services.AddSingleton(new BridgeLaunchOptions());
        services.AddSingleton(new Mt5BridgeClientOptions());

        services.AddSingleton<IBridgeProcess, PythonBridgeProcess>();
        services.AddSingleton<IBridgeTransport, LoopbackHttpBridgeTransport>();

        // One concrete singleton owns the child-process lifetime.
        services.AddSingleton<Mt5BridgeClient>();

        // Keyed opt-in provider FORWARDS to the same concrete singleton (C12) — NOT a second instance.
        services.AddKeyedSingleton<IMarketDataProvider>(
            "mt5-bridge",
            (sp, _) => sp.GetRequiredService<Mt5BridgeClient>());

        return services;
    }
}
