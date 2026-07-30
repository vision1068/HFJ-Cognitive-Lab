using System.Windows.Input;
using GoldSignalAnalyzer.Presentation.Mvvm;

namespace GoldSignalAnalyzer.Presentation;

/// <summary>
/// FR-35: the first-run disclaimer acknowledgement gate. Starts un-acknowledged and
/// only flips <see cref="HasAcknowledged"/> once <see cref="AcknowledgeCommand"/> runs,
/// persisting through the injected <see cref="IAcknowledgementStore"/> so a subsequent
/// launch does not re-prompt. There is deliberately no way to bypass the gate other
/// than acknowledging.
/// </summary>
public sealed class DisclaimerViewModel : ViewModelBase
{
    private readonly IAcknowledgementStore _store;
    private readonly RelayCommand _acknowledge;

    public DisclaimerViewModel(IAcknowledgementStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _acknowledge = new RelayCommand(Acknowledge, () => !HasAcknowledged);
    }

    public string Headline => DisclaimerText.Headline;
    public string FullText => DisclaimerText.Full;

    /// <summary>True once acknowledged (this session or a prior one).</summary>
    public bool HasAcknowledged => _store.HasAcknowledged;

    /// <summary>True when the first-run gate must be shown before the dashboard.</summary>
    public bool MustPrompt => !_store.HasAcknowledged;

    public ICommand AcknowledgeCommand => _acknowledge;

    /// <summary>Raised when the user acknowledges, so the shell can close the gate.</summary>
    public event EventHandler? Acknowledged;

    private void Acknowledge()
    {
        if (_store.HasAcknowledged) return;
        _store.Acknowledge();
        OnPropertyChanged(nameof(HasAcknowledged));
        OnPropertyChanged(nameof(MustPrompt));
        _acknowledge.RaiseCanExecuteChanged();
        Acknowledged?.Invoke(this, EventArgs.Empty);
    }
}
