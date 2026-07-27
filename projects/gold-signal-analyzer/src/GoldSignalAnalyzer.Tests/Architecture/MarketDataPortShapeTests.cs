using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GoldSignalAnalyzer.Application.Ports;
using GoldSignalAnalyzer.Domain.Entities;
using GoldSignalAnalyzer.Domain.Enums;
using GoldSignalAnalyzer.Domain.ValueObjects;
using Xunit;

namespace GoldSignalAnalyzer.Tests.Architecture;

/// <summary>
/// FR-36 §9 gate, CORRECTED to the user-provided literal spec §9 (see CYCLE2-SLICE1-CORRECTION.md).
/// Proves <see cref="IMarketDataProvider"/> is (1) structurally read-only — no order/trade/position/
/// login-shaped member (AC-36.1, the no-order invariant made structural, mirroring the Python RM2
/// gate); (2) byte-exact to the 9 literal §9 members including the two streaming members that the
/// earlier reconstruction wrongly excluded (AC-36.2, ADR-7 corrected). Streaming is read-only:
/// <c>IAsyncEnumerable&lt;T&gt;</c> is a PULL stream, not a manufactured push.
/// </summary>
public sealed class MarketDataPortShapeTests
{
    // Live-execution / login shapes that must NEVER appear on a read-only port. "stream" and
    // "connect"/"disconnect" are read-only and deliberately NOT in this set.
    private static readonly Regex ForbiddenShaped =
        new("order|trade|position|login|buy|sell|modify|deal|withdraw|send",
            RegexOptions.IgnoreCase);

    private static IEnumerable<MemberInfo> AllMembers() =>
        typeof(IMarketDataProvider).GetMembers(BindingFlags.Public | BindingFlags.Instance);

    [Fact]
    public void AC36_1_no_member_name_is_order_trade_or_login_shaped()
    {
        foreach (var member in AllMembers())
        {
            Assert.False(ForbiddenShaped.IsMatch(member.Name),
                $"IMarketDataProvider member '{member.Name}' is order/trade/login-shaped — the port must be read-only.");
        }
    }

    [Fact]
    public void AC36_2_port_exposes_the_two_read_only_streaming_members()
    {
        var streamTicks = typeof(IMarketDataProvider).GetMethod("StreamTicksAsync");
        Assert.NotNull(streamTicks);
        Assert.Equal(typeof(IAsyncEnumerable<MarketTick>), streamTicks!.ReturnType);

        var streamCandles = typeof(IMarketDataProvider).GetMethod("StreamCandlesAsync");
        Assert.NotNull(streamCandles);
        Assert.Equal(typeof(IAsyncEnumerable<Candle>), streamCandles!.ReturnType);
    }

    [Fact]
    public void AC36_2_no_push_based_IObservable_member_remains()
    {
        // Streaming is PULL (IAsyncEnumerable). A push primitive (IObservable) is still excluded.
        foreach (var type in MemberValueTypes())
            foreach (var t in UnwrapTypes(type))
                if (t.IsGenericType)
                    Assert.False(t.GetGenericTypeDefinition() == typeof(IObservable<>),
                        "IMarketDataProvider must expose no IObservable<> member (streaming is pull-based IAsyncEnumerable).");
    }

    /// <summary>
    /// Byte-exact lock on literal §9: the exact member set (1 property + 8 methods), each method's
    /// return type, and each method's parameter (type, name) sequence. A drift from the literal
    /// spec fails here, not silently.
    /// </summary>
    [Fact]
    public void byte_exact_to_literal_spec_section_9()
    {
        var propertyNames = typeof(IMarketDataProvider)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name).OrderBy(n => n).ToArray();
        Assert.Equal(new[] { "ProviderName" }, propertyNames);

        var providerName = typeof(IMarketDataProvider).GetProperty("ProviderName");
        Assert.NotNull(providerName);
        Assert.Equal(typeof(string), providerName!.PropertyType);
        Assert.False(providerName.CanWrite); // get-only

        var methodNames = typeof(IMarketDataProvider)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => !m.IsSpecialName) // exclude property accessors
            .Select(m => m.Name).OrderBy(n => n).ToArray();
        Assert.Equal(
            new[]
            {
                "ConnectAsync", "DisconnectAsync", "GetAccountSnapshotAsync",
                "GetAvailableSymbolsAsync", "GetHistoricalCandlesAsync",
                "GetSymbolSpecificationAsync", "StreamCandlesAsync", "StreamTicksAsync",
            },
            methodNames);

        AssertSignature("ConnectAsync", typeof(Task<ProviderConnectionResult>),
            (typeof(CancellationToken), "cancellationToken"));
        AssertSignature("DisconnectAsync", typeof(Task),
            (typeof(CancellationToken), "cancellationToken"));
        // AccountSnapshot? is a compile-time nullable-reference annotation; at runtime the return
        // type is Task<AccountSnapshot>.
        AssertSignature("GetAccountSnapshotAsync", typeof(Task<AccountSnapshot>),
            (typeof(CancellationToken), "cancellationToken"));
        AssertSignature("GetAvailableSymbolsAsync", typeof(Task<IReadOnlyList<MarketSymbol>>),
            (typeof(CancellationToken), "cancellationToken"));
        AssertSignature("GetSymbolSpecificationAsync", typeof(Task<SymbolSpecification>),
            (typeof(string), "symbol"), (typeof(CancellationToken), "cancellationToken"));
        AssertSignature("GetHistoricalCandlesAsync", typeof(Task<IReadOnlyList<Candle>>),
            (typeof(string), "symbol"), (typeof(Timeframe), "timeframe"),
            (typeof(DateTimeOffset), "from"), (typeof(DateTimeOffset), "to"),
            (typeof(CancellationToken), "cancellationToken"));
        AssertSignature("StreamTicksAsync", typeof(IAsyncEnumerable<MarketTick>),
            (typeof(string), "symbol"), (typeof(CancellationToken), "cancellationToken"));
        AssertSignature("StreamCandlesAsync", typeof(IAsyncEnumerable<Candle>),
            (typeof(string), "symbol"), (typeof(Timeframe), "timeframe"),
            (typeof(CancellationToken), "cancellationToken"));
    }

    private static void AssertSignature(
        string method, Type expectedReturn, params (Type Type, string Name)[] expectedParams)
    {
        var mi = typeof(IMarketDataProvider).GetMethod(method);
        Assert.NotNull(mi);
        Assert.Equal(expectedReturn, mi!.ReturnType);

        var actual = mi.GetParameters();
        Assert.Equal(expectedParams.Length, actual.Length);
        for (var i = 0; i < expectedParams.Length; i++)
        {
            Assert.Equal(expectedParams[i].Type, actual[i].ParameterType);
            Assert.Equal(expectedParams[i].Name, actual[i].Name);
        }
    }

    private static IEnumerable<Type> MemberValueTypes()
    {
        foreach (var m in typeof(IMarketDataProvider).GetMethods(BindingFlags.Public | BindingFlags.Instance))
            yield return m.ReturnType;
        foreach (var p in typeof(IMarketDataProvider).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            yield return p.PropertyType;
    }

    /// <summary>Yield a type plus its generic args (unwrapping Task&lt;T&gt;, IReadOnlyList&lt;T&gt;, …).</summary>
    private static IEnumerable<Type> UnwrapTypes(Type type)
    {
        yield return type;
        if (type.IsGenericType)
            foreach (var arg in type.GetGenericArguments())
                foreach (var inner in UnwrapTypes(arg))
                    yield return inner;
    }
}
