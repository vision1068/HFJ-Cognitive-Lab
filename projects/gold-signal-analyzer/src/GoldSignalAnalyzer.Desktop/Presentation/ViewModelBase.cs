using CommunityToolkit.Mvvm.ComponentModel;

namespace GoldSignalAnalyzer.Desktop.Presentation;

/// <summary>
/// Base for every view-model (arch §7). Inherits <see cref="ObservableObject"/> and adds the
/// shared <c>Title</c>, <c>IsBusy</c> state and an <see cref="OnNavigatedTo"/> hook the
/// navigation service calls after the VM becomes the active screen.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Called by the navigation service once this VM becomes the active screen.</summary>
    public virtual void OnNavigatedTo()
    {
    }
}
