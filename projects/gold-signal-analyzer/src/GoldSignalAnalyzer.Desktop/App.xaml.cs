using System.Windows;
using System.Windows.Markup;
using GoldSignalAnalyzer.Desktop.Presentation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GoldSignalAnalyzer.Desktop;

/// <summary>
/// WPF composition root (arch §4). Builds the generic host from the shared <see cref="GsaHost"/>
/// manifest (also used by tests), starts it (migrates the DB), merges the VM→View DataTemplate map,
/// then resolves and shows the shell.
/// </summary>
public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        MergeScreenTemplates();

        _host = GsaHost.CreateHostBuilder().Build();
        await _host.StartAsync();

        var shell = new ShellWindow
        {
            DataContext = _host.Services.GetRequiredService<ShellViewModel>()
        };
        shell.Show();
    }

    /// <summary>
    /// Loads the loose <c>Views/ScreenTemplates.xaml</c> resource at runtime (arch §7, gate R3) and
    /// merges it into the application resources, so the shell ContentControl renders the right view
    /// for the active screen VM. Loaded via <see cref="XamlReader"/> because same-assembly types
    /// cannot be resolved by markup-compiled XAML in this toolchain.
    /// </summary>
    private void MergeScreenTemplates()
    {
        var uri = new Uri("pack://application:,,,/Views/ScreenTemplates.xaml", UriKind.Absolute);
        using var stream = GetResourceStream(uri).Stream;
        var templates = (ResourceDictionary)XamlReader.Load(stream);
        Resources.MergedDictionaries.Add(templates);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}
