using GoldSignalAnalyzer.Application.Charting;
using GoldSignalAnalyzer.Presentation;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// Cycle 4 (FR-27, AC-27.2/27.3/27.4/27.5) — the pure geometry view-model. Price→pixel
/// mapping is asserted against hand-computed coordinates; empty data draws nothing.
/// </summary>
public class ChartViewModelTests
{
    // Three bars, price extent fixed to [100,200] so Y math is hand-derivable.
    //   Height 200, range 100  ⇒  YFor(price) = (200 - price) / 100 * 200
    //     price 200 → 0 (top) ; price 100 → 200 (bottom) ; price 150 → 100 (mid)
    //   Width 300, 3 bars      ⇒  slot 100, centres 50/150/250, bodyWidth 60
    private static ChartData ThreeBar()
    {
        var t = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var bars = new List<ChartBar>
        {
            new(t,               Open: 100m, High: 200m, Low: 100m, Close: 200m), // up, spans full range
            new(t.AddHours(1),   Open: 150m, High: 150m, Low: 150m, Close: 100m), // down
            new(t.AddHours(2),   Open: 150m, High: 150m, Low: 150m, Close: 150m), // flat at mid
        };
        var overlays = new List<ChartOverlaySeries>
        {
            new("EMA", new decimal?[] { null, 150m, 200m }), // index 0 is warmup ⇒ skipped
        };
        return new ChartData(bars, overlays, Min: 100m, Max: 200m);
    }

    private static ChartViewModel Loaded()
    {
        var vm = new ChartViewModel { Width = 300, Height = 200 };
        vm.Load(ThreeBar());
        return vm;
    }

    [Fact] // AC-27.2: candle body/wick map to hand-computed pixel coordinates
    public void Geometry_maps_price_to_inverted_scaled_pixels()
    {
        var g = Loaded().Candles;
        Assert.Equal(3, g.Count);

        // bar 0: open 100 → y200, close 200 → y0  ⇒ body spans [0,200]
        Assert.Equal(0.0, g[0].BodyTop, 3);
        Assert.Equal(200.0, g[0].BodyBottom, 3);
        Assert.True(g[0].IsUp);
        Assert.Equal(20.0, g[0].X, 3);        // centre 50 − bodyWidth/2 (30)
        Assert.Equal(49.5, g[0].WickX, 3);    // centre 50 − 0.5

        // bar 1: close 100 (down) → body bottom at y200; wick top/bottom at y100 (price 150)
        Assert.False(g[1].IsUp);
        Assert.Equal(200.0, g[1].BodyBottom, 3);
    }

    [Fact] // AC-27.2: overlay skips warmup null, aligns X to bar centres, correct Y
    public void Overlay_polyline_skips_warmup_and_aligns_points()
    {
        var ov = Loaded().Overlays;
        Assert.Single(ov);
        var pts = ov[0].Points;
        Assert.Equal(2, pts.Count);                 // index 0 (null) skipped
        Assert.Equal(150.0, pts[0].X, 3);           // bar 1 centre
        Assert.Equal(100.0, pts[0].Y, 3);           // price 150 → mid
        Assert.Equal(250.0, pts[1].X, 3);           // bar 2 centre
        Assert.Equal(0.0, pts[1].Y, 3);             // price 200 → top
        Assert.Contains("150,100", ov[0].PointsString);
    }

    [Fact] // flat series (min==max) must not divide by zero — everything at mid-height
    public void Flat_series_maps_to_vertical_middle_without_dividing_by_zero()
    {
        var t = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var flat = new ChartData(
            new List<ChartBar> { new(t, 100m, 100m, 100m, 100m) },
            Array.Empty<ChartOverlaySeries>(), Min: 100m, Max: 100m);
        var vm = new ChartViewModel { Width = 300, Height = 200 };
        vm.Load(flat);
        Assert.Equal(100.0, vm.Candles[0].WickTop, 3);   // height/2
        Assert.Equal(100.0, vm.Candles[0].WickBottom, 3);
    }

    [Fact] // AC-27.4: empty data draws nothing — no fabricated geometry
    public void Empty_data_produces_no_geometry()
    {
        var vm = new ChartViewModel();
        vm.Load(ChartData.Empty);
        Assert.False(vm.HasData);
        Assert.Empty(vm.Candles);
        Assert.Empty(vm.Overlays);
        Assert.Equal("No chart data", vm.PriceRangeLabel);
    }

    [Fact] // AC-27.5: axis label carries real min/max/count and never a probability
    public void Price_range_label_is_real_and_not_a_probability()
    {
        var vm = Loaded();
        Assert.Contains("100", vm.PriceRangeLabel);
        Assert.Contains("200", vm.PriceRangeLabel);
        Assert.Contains("3 bars", vm.PriceRangeLabel);
        Assert.DoesNotContain("%", vm.PriceRangeLabel);
        Assert.DoesNotContain("probability", vm.PriceRangeLabel, StringComparison.OrdinalIgnoreCase);
    }

    [Fact] // resizing the viewport rescales geometry (Width/Height are live inputs)
    public void Resizing_rebuilds_geometry()
    {
        var vm = Loaded();
        double before = vm.Candles[0].BodyBottom;   // 200 at Height 200
        vm.Height = 400;
        Assert.Equal(before * 2.0, vm.Candles[0].BodyBottom, 3);
    }
}
