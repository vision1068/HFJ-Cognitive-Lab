using Microsoft.Extensions.DependencyInjection;

namespace GoldSignalAnalyzer.Desktop.Presentation;

/// <summary>
/// Default <see cref="INavigationService"/> (arch §7). Resolves the requested VM from the same DI
/// container the app composes (a missing registration therefore fails loud at navigation time),
/// swaps <see cref="CurrentViewModel"/>, invokes the VM's <see cref="ViewModelBase.OnNavigatedTo"/>
/// hook and raises <see cref="CurrentViewModelChanged"/>.
/// </summary>
public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _services;

    public NavigationService(IServiceProvider services) => _services = services;

    public object? CurrentViewModel { get; private set; }

    public event EventHandler? CurrentViewModelChanged;

    public void NavigateTo<TViewModel>() where TViewModel : class => NavigateTo(typeof(TViewModel));

    public void NavigateTo(Type viewModelType)
    {
        ArgumentNullException.ThrowIfNull(viewModelType);

        var viewModel = _services.GetRequiredService(viewModelType);
        CurrentViewModel = viewModel;
        (viewModel as ViewModelBase)?.OnNavigatedTo();
        CurrentViewModelChanged?.Invoke(this, EventArgs.Empty);
    }
}
