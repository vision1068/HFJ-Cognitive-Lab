using System.Collections.ObjectModel;
using System.Globalization;
using GoldSignalAnalyzer.Application.Charting;
using GoldSignalAnalyzer.Presentation.Mvvm;

namespace GoldSignalAnalyzer.Presentation;

/// <summary>Pixel geometry for one candle on the canvas. All values are plain
/// doubles ready for a WPF <c>Canvas</c>; nothing here is a market fact. The derived
/// <see cref="BodyHeight"/>/<see cref="WickHeight"/>/<see cref="WickX"/> exist so the
/// XAML can draw <c>Rectangle</c>s by direct binding — no value converter / logic in
/// the view.</summary>
public sealed record CandleGeometry(
    double X,
    double BodyTop,
    double BodyBottom,
    double WickTop,
    double WickBottom,
    double Width,
    bool IsUp)
{
    /// <summary>At least 1px so a doji (open==close) still draws.</summary>
    public double BodyHeight => Math.Max(1.0, BodyBottom - BodyTop);
    public double WickHeight => Math.Max(0.0, WickBottom - WickTop);
    /// <summary>X of the 1px-wide wick (bar centre).</summary>
    public double WickX => X + Width / 2.0 - 0.5;
}

/// <summary>An overlay reduced to an ordered polyline point list (warmup nulls
/// skipped). <see cref="PointsString"/> is a ready-to-bind WPF Polyline.Points value.</summary>
public sealed record ChartOverlayGeometry(string Name, IReadOnlyList<(double X, double Y)> Points)
{
    public string PointsString =>
        string.Join(" ", Points.Select(p =>
            p.X.ToString("0.##", CultureInfo.InvariantCulture) + "," +
            p.Y.ToString("0.##", CultureInfo.InvariantCulture)));
}

/// <summary>
/// FR-27 (AC-27.2..27.5): maps a pure <see cref="ChartData"/> to viewport geometry so
/// a dumb XAML <c>Canvas</c> can draw it. Price→Y is inverted (higher price = smaller
/// Y) and scaled to the supplied height. It computes NO indicator or market values —
/// only coordinates — and draws nothing for empty data (no fabricated bars, INV-4).
/// The XAML shell holds no logic; every number here is unit-tested headlessly.
/// </summary>
public sealed class ChartViewModel : ViewModelBase
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private ChartData _data = ChartData.Empty;
    private double _width = 900;
    private double _height = 260;

    public ObservableCollection<CandleGeometry> Candles { get; } = new();
    public ObservableCollection<ChartOverlayGeometry> Overlays { get; } = new();

    public double Width
    {
        get => _width;
        set { if (SetField(ref _width, value)) Rebuild(); }
    }

    public double Height
    {
        get => _height;
        set { if (SetField(ref _height, value)) Rebuild(); }
    }

    public bool HasData => !_data.IsEmpty;

    /// <summary>AC-27.5: real min/max/count from the data, never a probability.</summary>
    public string PriceRangeLabel => _data.IsEmpty
        ? "No chart data"
        : $"Price {_data.Min.ToString("0.##", Inv)}–{_data.Max.ToString("0.##", Inv)}  |  {_data.Bars.Count} bars";

    public string OverlayLegend => _data.Overlays.Count == 0
        ? ""
        : "Overlays: " + string.Join(", ", _data.Overlays.Select(o => o.Name));

    public void Load(ChartData? data)
    {
        _data = data ?? ChartData.Empty;
        Rebuild();
    }

    private void Rebuild()
    {
        Candles.Clear();
        Overlays.Clear();

        if (!_data.IsEmpty && _width > 0 && _height > 0)
        {
            int n = _data.Bars.Count;
            decimal range = _data.Max - _data.Min;
            // Flat series (range == 0): everything maps to the vertical middle — no divide-by-zero.
            double YFor(decimal price) => range == 0m
                ? _height / 2.0
                : (double)((_data.Max - price) / range) * _height;

            double slot = _width / n;                 // horizontal space per bar
            double bodyWidth = Math.Max(1.0, slot * 0.6);

            for (int i = 0; i < n; i++)
            {
                var b = _data.Bars[i];
                double cx = slot * (i + 0.5);          // bar centre
                double yOpen = YFor(b.Open);
                double yClose = YFor(b.Close);
                Candles.Add(new CandleGeometry(
                    X: cx - bodyWidth / 2.0,
                    BodyTop: Math.Min(yOpen, yClose),
                    BodyBottom: Math.Max(yOpen, yClose),
                    WickTop: YFor(b.High),
                    WickBottom: YFor(b.Low),
                    Width: bodyWidth,
                    IsUp: b.IsUp));
            }

            foreach (var ov in _data.Overlays)
            {
                var pts = new List<(double X, double Y)>();
                for (int i = 0; i < ov.Values.Count && i < n; i++)
                    if (ov.Values[i] is decimal v)         // skip warmup nulls, keep X alignment
                        pts.Add((slot * (i + 0.5), YFor(v)));
                if (pts.Count > 0)
                    Overlays.Add(new ChartOverlayGeometry(ov.Name, pts));
            }
        }

        OnPropertyChanged(nameof(HasData));
        OnPropertyChanged(nameof(PriceRangeLabel));
        OnPropertyChanged(nameof(OverlayLegend));
    }
}
