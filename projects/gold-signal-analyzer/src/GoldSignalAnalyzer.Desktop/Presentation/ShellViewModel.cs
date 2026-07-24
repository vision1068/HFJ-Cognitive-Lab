using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GoldSignalAnalyzer.Desktop.Presentation;

/// <summary>
/// The shell view-model (arch §7). Holds the active <see cref="CurrentViewModel"/> (proxied from the
/// navigation service), the <see cref="NavItems"/> rail across the 17 §30 screens, and the
/// <see cref="NavigateCommand"/>. Registered via <c>AddGsaPresentation()</c>.
/// </summary>
public partial class ShellViewModel : ObservableObject
{
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private string _title = "Gold Signal Analyzer - Foundation (Cycle 1)";

    [ObservableProperty]
    private string _statusBanner = "Not connected. Live MT5 connectivity is Cycle 2+ (roadmap).";

    public ShellViewModel(INavigationService navigation)
    {
        _navigation = navigation;
        _navigation.CurrentViewModelChanged += OnCurrentViewModelChanged;

        // Nav rail: pair each §30 title (authoritative order) with its screen VM type (same order).
        var items = new List<NavItem>(Section30Screens.Titles.Count);
        for (var i = 0; i < ScreenCatalog.ViewModelTypes.Count; i++)
            items.Add(new NavItem(Section30Screens.Titles[i], ScreenCatalog.ViewModelTypes[i]));
        NavItems = items;

        // Land on the first screen so the shell is never a blank ContentControl.
        if (NavItems.Count > 0)
            _navigation.NavigateTo(NavItems[0].ViewModelType);
    }

    /// <summary>The 17-screen navigation rail (arch §7).</summary>
    public IReadOnlyList<NavItem> NavItems { get; }

    /// <summary>The active screen VM, rendered by the shell ContentControl via the VM→View map.</summary>
    public object? CurrentViewModel => _navigation.CurrentViewModel;

    [RelayCommand]
    private void Navigate(NavItem? item)
    {
        if (item is not null)
            _navigation.NavigateTo(item.ViewModelType);
    }

    private void OnCurrentViewModelChanged(object? sender, EventArgs e) =>
        OnPropertyChanged(nameof(CurrentViewModel));
}
