using GoldSignalAnalyzer.Domain;
using GoldSignalAnalyzer.Presentation.Mvvm;

namespace GoldSignalAnalyzer.Presentation;

/// <summary>One selectable timeframe option for the dashboard selector (value + display label).</summary>
public sealed record TimeFrameChoice(TimeFrame Value, string Label);

/// <summary>
/// Cycle 8 (FR-39): the runtime timeframe selector's view-model. It exposes the curated
/// standard set (M1/M5/M15/M30/H1/H4/D1, default H1) and the current <see cref="Selected"/>
/// timeframe, and raises <see cref="Changed"/> when the user picks a different one so the
/// composition root can recalculate the signal + chart at the new timeframe.
///
/// It is a PASSIVE control surface: it only chooses a candle bucket size. It has NO
/// command and NO order/execution affordance (INV-1) — a reflection guard test enforces
/// this. Fully unit-testable (no WPF).
/// </summary>
public sealed class TimeFrameSelectionViewModel : ViewModelBase
{
    /// <summary>The curated standard timeframe set, in ascending order. Every one is
    /// supported by the domain enum AND the Python bridge mapping (D8-1).</summary>
    public static readonly IReadOnlyList<TimeFrame> Curated = new[]
    {
        TimeFrame.M1, TimeFrame.M5, TimeFrame.M15, TimeFrame.M30,
        TimeFrame.H1, TimeFrame.H4, TimeFrame.D1,
    };

    private TimeFrame _selected;

    public TimeFrameSelectionViewModel(TimeFrame initial = TimeFrame.H1)
    {
        _selected = initial;
    }

    /// <summary>The choices bound to the ComboBox (value + label), in curated order.</summary>
    public IReadOnlyList<TimeFrameChoice> Choices { get; } =
        Curated.Select(tf => new TimeFrameChoice(tf, tf.ToString())).ToList();

    /// <summary>
    /// The currently-selected timeframe. Setting it to a DIFFERENT value raises
    /// <see cref="Changed"/> exactly once; setting it to the same value is a no-op.
    /// Genuinely settable, so a TwoWay ComboBox.SelectedValue binding is correct here
    /// (unlike the read-only-property/default-TwoWay trap from the 2026-07-31 lesson).
    /// </summary>
    public TimeFrame Selected
    {
        get => _selected;
        set
        {
            if (_selected == value) return;
            _selected = value;
            OnPropertyChanged();
            Changed?.Invoke(this, value);
        }
    }

    /// <summary>Raised when the user selects a different timeframe.</summary>
    public event EventHandler<TimeFrame>? Changed;

    /// <summary>Static label for the control (never a probability/percentage — INV-5).</summary>
    public string Label => "Analysis timeframe:";
}
