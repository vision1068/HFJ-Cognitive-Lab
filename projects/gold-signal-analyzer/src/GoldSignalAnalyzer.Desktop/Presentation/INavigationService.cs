namespace GoldSignalAnalyzer.Desktop.Presentation;

/// <summary>
/// Presentation abstraction (arch §7). View-models depend on this, not on WPF navigation, so they
/// stay testable. <c>NavigateTo</c> resolves the target VM from the DI container and swaps
/// <see cref="CurrentViewModel"/>; the shell binds a ContentControl to it and a VM→View DataTemplate
/// map renders the right view.
/// </summary>
public interface INavigationService
{
    /// <summary>The view-model currently hosted by the shell's content region.</summary>
    object? CurrentViewModel { get; }

    /// <summary>Raised after <see cref="CurrentViewModel"/> changes.</summary>
    event EventHandler? CurrentViewModelChanged;

    /// <summary>Resolve <typeparamref name="TViewModel"/> from DI and make it the active screen.</summary>
    void NavigateTo<TViewModel>() where TViewModel : class;

    /// <summary>Resolve <paramref name="viewModelType"/> from DI and make it the active screen.</summary>
    void NavigateTo(Type viewModelType);
}
